using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/v1/entries")]
[Authorize]
public class EntriesController : ControllerBase
{
    private const string UnlockTokenHeader = "X-Unlock-Token";

    private readonly AppDbContext _context;
    private readonly EntryService _entryService;
    private readonly TfIdfService _tfIdf;
    private readonly UnlockService _unlock;
    private readonly ILogger<EntriesController> _logger;

    public EntriesController(AppDbContext context, EntryService entryService, TfIdfService tfIdf, UnlockService unlock, ILogger<EntriesController> logger)
    {
        _context = context;
        _entryService = entryService;
        _tfIdf = tfIdf;
        _unlock = unlock;
        _logger = logger;
    }

    /// <summary>
    /// Create a new entry
    /// POST /api/v1/entries
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateEntry([FromBody] CreateEntryDto createDto)
    {
        // Validate DTO
        var validationErrors = _entryService.ValidateCreateEntry(createDto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        // Get current user from JWT - use the mapped claim type
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userGuid);
        if (user == null)
            return Unauthorized();

        // Create entry
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = createDto.Title,
            BodyText = createDto.BodyText,
            Type = createDto.Type ?? "text",
            Confidentiality = createDto.Confidentiality,
            CategoryId = createDto.CategoryId,
            Tags = createDto.Tags,
            SentimentScore = createDto.SentimentScore,
            SentimentLabel = createDto.SentimentLabel,
            SentimentModel = createDto.SentimentModel,
            Metadata = createDto.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(createDto.Metadata) : null,
            CreatedAt = createDto.CreatedAt ?? DateTime.UtcNow,
            Source = "ui",
            Immutable = false
        };

        // Calculate read_only_after based on user's timezone
        entry.ReadOnlyAfter = _entryService.CalculateReadOnlyAfter(user.Timezone, entry.CreatedAt);

        _context.Entries.Add(entry);
        await _context.SaveChangesAsync();

        // Log audit
        _context.AuditLogs.Add(new AuditLog
        {
            EntryId = entry.Id,
            UserId = user.Id,
            Action = "create",
            ActorIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // The author just wrote this entry, so return it in full (never locked to its creator).
        var responseDto = MapToDto(entry, unlocked: true);
        _logger.LogInformation("Entry created: {EntryId} by user {UserId}", entry.Id, user.Id);

        return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, responseDto);
    }

    /// <summary>
    /// List entries with pagination and filtering
    /// GET /api/v1/entries?page=1&per_page=20&start=...&end=...&categoryId=...&tag=...
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ListEntries(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        [FromQuery] string? q = null)
    {
        // Use the mapped claim type (ClaimTypes.NameIdentifier) instead of "sub"
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var query = _context.Entries
            .Where(e => e.UserId == userGuid)
            .AsQueryable();

        // Filter by date range
        if (start.HasValue)
            query = query.Where(e => e.CreatedAt >= start.Value);
        if (end.HasValue)
            query = query.Where(e => e.CreatedAt <= end.Value);

        // Filter by category
        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        // Filter by tag (simple string match)
        if (!string.IsNullOrEmpty(tag))
            query = query.Where(e => e.Tags != null && e.Tags.Contains(tag));

        // Text search over title and body (case-insensitive substring match).
        // ToLower() translates on both SQLite and PostgreSQL for provider-agnostic
        // case-insensitivity.
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(e =>
                (e.Title != null && e.Title.ToLower().Contains(term)) ||
                (e.BodyText != null && e.BodyText.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var entries = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        // Resolve the session unlock state once; private entries stay redacted unless unlocked.
        var unlocked = await _unlock.IsUnlockedAsync(userGuid, Request.Headers[UnlockTokenHeader].FirstOrDefault());
        var items = entries.Select(e => MapToDto(e, unlocked)).ToList();

        var result = new PagedResult<EntryResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PerPage = perPage,
            TotalPages = (totalCount + perPage - 1) / perPage
        };

        return Ok(result);
    }

    /// <summary>
    /// Build a TF-IDF similarity graph of the user's entries (for the relationship
    /// mind-map). Nodes are entries; edges connect entries whose title+body text
    /// are similar above the given cosine-similarity threshold.
    /// GET /api/v1/entries/graph?threshold=0.1
    /// </summary>
    [HttpGet("graph")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSimilarityGraph([FromQuery] double threshold = 0.1)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var clamped = Math.Clamp(threshold, 0.0, 1.0);

        var entries = await _context.Entries
            .Where(e => e.UserId == userGuid)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var graph = _tfIdf.BuildSimilarityGraph(entries, clamped);

        // Don't leak private entry labels through the graph: redact node titles/sentiment
        // for locked private entries (edges and dates are kept so the structure still shows).
        var unlocked = await _unlock.IsUnlockedAsync(userGuid, Request.Headers[UnlockTokenHeader].FirstOrDefault());
        if (!unlocked)
        {
            var lockedIds = entries
                .Where(e => string.Equals(e.Confidentiality, "private", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Id)
                .ToHashSet();
            foreach (var node in graph.Nodes.Where(n => lockedIds.Contains(n.Id)))
            {
                node.Title = null;
                node.SentimentLabel = null;
                node.SentimentScore = null;
            }
        }

        return Ok(graph);
    }

    /// <summary>
    /// Get a single entry by ID
    /// GET /api/v1/entries/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEntry(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var entry = await _context.Entries.FindAsync(id);
        if (entry == null)
            return NotFound();

        if (entry.UserId != userGuid)
            return Forbid();

        var unlocked = await _unlock.IsUnlockedAsync(userGuid, Request.Headers[UnlockTokenHeader].FirstOrDefault());
        return Ok(MapToDto(entry, unlocked));
    }

    /// <summary>
    /// Update an entry
    /// PATCH /api/v1/entries/{id}
    /// Only editable before read_only_after boundary, returns 403 if immutable
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateEntry(Guid id, [FromBody] UpdateEntryDto updateDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var entry = await _context.Entries.FindAsync(id);
        if (entry == null)
            return NotFound();

        if (entry.UserId != userGuid)
            return Forbid();

        // Check immutability
        if (!_entryService.IsEntryEditable(entry))
        {
            _logger.LogWarning("Attempt to edit immutable entry {EntryId} by user {UserId}", entry.Id, userGuid);
            return Forbid();
        }

        // Update fields
        if (!string.IsNullOrEmpty(updateDto.Title))
            entry.Title = updateDto.Title;
        if (!string.IsNullOrEmpty(updateDto.BodyText))
            entry.BodyText = updateDto.BodyText;
        if (updateDto.CategoryId.HasValue)
            entry.CategoryId = updateDto.CategoryId.Value;
        if (updateDto.Tags != null)
            entry.Tags = updateDto.Tags;

        entry.UpdatedAt = DateTime.UtcNow;

        _context.Entries.Update(entry);
        await _context.SaveChangesAsync();

        // Log audit
        _context.AuditLogs.Add(new AuditLog
        {
            EntryId = entry.Id,
            UserId = userGuid,
            Action = "update",
            ActorIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Entry updated: {EntryId} by user {UserId}", entry.Id, userGuid);

        // The caller just edited this entry, so return the updated content unredacted.
        return Ok(MapToDto(entry, unlocked: true));
    }

    /// <summary>
    /// Delete an entry
    /// DELETE /api/v1/entries/{id}
    /// Only deletable before read_only_after boundary, returns 403 if immutable
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var entry = await _context.Entries.FindAsync(id);
        if (entry == null)
            return NotFound();

        if (entry.UserId != userGuid)
            return Forbid();

        // Check immutability
        if (!_entryService.IsEntryEditable(entry))
        {
            _logger.LogWarning("Attempt to delete immutable entry {EntryId} by user {UserId}", entry.Id, userGuid);
            return Forbid();
        }

        // Delete media files (enqueue background job in future sprint)
        var mediaFiles = await _context.Media.Where(m => m.EntryId == id).ToListAsync();
        // TODO: Enqueue media deletion job

        // Delete entry
        _context.Entries.Remove(entry);
        await _context.SaveChangesAsync();

        // Log audit
        _context.AuditLogs.Add(new AuditLog
        {
            EntryId = entry.Id,
            UserId = userGuid,
            Action = "delete",
            ActorIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Entry deleted: {EntryId} by user {UserId}", entry.Id, userGuid);

        return NoContent();
    }

    /// <summary>
    /// Map an entry to its response DTO. A private entry that the caller has not unlocked
    /// is returned <see cref="EntryResponseDto.Locked"/> with its content fields (title,
    /// body, tags, sentiment) redacted to null — only non-sensitive metadata (date,
    /// category, confidentiality) is exposed so the timeline can show a locked placeholder.
    /// </summary>
    private static EntryResponseDto MapToDto(Entry entry, bool unlocked)
    {
        var locked = string.Equals(entry.Confidentiality, "private", StringComparison.OrdinalIgnoreCase) && !unlocked;
        return new EntryResponseDto
        {
            Id = entry.Id,
            Title = locked ? null : entry.Title,
            BodyText = locked ? null : entry.BodyText,
            Type = entry.Type,
            Confidentiality = entry.Confidentiality,
            CategoryId = entry.CategoryId,
            Tags = locked ? null : entry.Tags,
            SentimentScore = locked ? null : entry.SentimentScore,
            SentimentLabel = locked ? null : entry.SentimentLabel,
            CreatedAt = entry.CreatedAt,
            ReadOnlyAfter = entry.ReadOnlyAfter,
            Immutable = entry.Immutable,
            Locked = locked
        };
    }
}
