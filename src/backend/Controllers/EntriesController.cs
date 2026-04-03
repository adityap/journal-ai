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
    private readonly AppDbContext _context;
    private readonly EntryService _entryService;
    private readonly ILogger<EntriesController> _logger;

    public EntriesController(AppDbContext context, EntryService entryService, ILogger<EntriesController> logger)
    {
        _context = context;
        _entryService = entryService;
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
        // Log entire incoming DTO for debugging
        _logger.LogInformation("CreateEntry - Full DTO: Title={Title}, BodyText={BodyText}, Confidentiality={Conf}, HasSentimentScore={HasScore}", 
            createDto.Title?.Substring(0, Math.Min(30, createDto.Title?.Length ?? 0)) ?? "null", 
            createDto.BodyText?.Substring(0, Math.Min(30, createDto.BodyText?.Length ?? 0)) ?? "null",
            createDto.Confidentiality,
            createDto.SentimentScore.HasValue);
        
        // Log incoming sentiment data
        _logger.LogInformation("CreateEntry - Received sentiment data: Score={SentimentScore}, Label={SentimentLabel}, Model={SentimentModel}",
            createDto.SentimentScore, createDto.SentimentLabel, createDto.SentimentModel);

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

        // Log entry data before save
        _logger.LogInformation("CreateEntry - Entry before save: Score={SentimentScore}, Label={SentimentLabel}", 
            entry.SentimentScore, entry.SentimentLabel);

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

        var responseDto = MapToDto(entry);
        _logger.LogInformation("CreateEntry - Response DTO: Score={SentimentScore}, Label={SentimentLabel}", 
            responseDto.SentimentScore, responseDto.SentimentLabel);
        
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
        [FromQuery] int perPage = 20)
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

        var totalCount = await query.CountAsync();
        var entries = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        var items = entries.Select(MapToDto).ToList();
        
        // Log sentiment data for debugging
        foreach (var item in items)
        {
            _logger.LogInformation("ListEntries - Entry {EntryId}: Title={Title}, Score={SentimentScore}, Label={SentimentLabel}",
                item.Id, item.Title ?? "[No title]", item.SentimentScore, item.SentimentLabel ?? "[No label]");
        }

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

        return Ok(MapToDto(entry));
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

        return Ok(MapToDto(entry));
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

    private static EntryResponseDto MapToDto(Entry entry)
    {
        return new EntryResponseDto
        {
            Id = entry.Id,
            Title = entry.Title,
            BodyText = entry.BodyText,
            Type = entry.Type,
            Confidentiality = entry.Confidentiality,
            CategoryId = entry.CategoryId,
            Tags = entry.Tags,
            SentimentScore = entry.SentimentScore,
            SentimentLabel = entry.SentimentLabel,
            CreatedAt = entry.CreatedAt,
            ReadOnlyAfter = entry.ReadOnlyAfter,
            Immutable = entry.Immutable
        };
    }
}
