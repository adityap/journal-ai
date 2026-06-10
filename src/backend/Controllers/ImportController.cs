using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/v1/imports")]
[Authorize]
public class ImportController : ControllerBase
{
    /// <summary>
    /// Upper bound on entries accepted in a single import, to bound work and memory
    /// for an untrusted payload. Larger archives should be split or handled by a
    /// future async export_jobs-style pipeline.
    /// </summary>
    private const int MaxEntriesPerImport = 5000;

    private readonly AppDbContext _context;
    private readonly EntryService _entryService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(AppDbContext context, EntryService entryService, ILogger<ImportController> logger)
    {
        _context = context;
        _entryService = entryService;
        _logger = logger;
    }

    /// <summary>
    /// Import entries into the caller's own journal.
    /// POST /api/v1/imports
    ///
    /// Accepts the JSON export shape (an object with an <c>entries</c> array), making
    /// export → import a round-trip. The import is deliberately defensive:
    /// <list type="bullet">
    /// <item>Ownership is always the calling user; any id/userId in the file is ignored.</item>
    /// <item>A foreign or unknown <c>categoryId</c> is dropped rather than attached.</item>
    /// <item>Unknown/missing confidentiality falls back to the safer "private".</item>
    /// <item>Duplicates (same title+body+createdAt for this user) are skipped, so
    /// re-importing the same file is idempotent.</item>
    /// </list>
    /// </summary>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ImportEntries([FromBody] ImportRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userGuid);
        if (user == null)
            return Unauthorized();

        if (request?.Entries == null || request.Entries.Count == 0)
            return BadRequest(new { errors = new[] { "No entries to import." } });

        if (request.Entries.Count > MaxEntriesPerImport)
            return BadRequest(new { errors = new[] { $"Too many entries; limit is {MaxEntriesPerImport} per import." } });

        // The caller's own category ids — anything else in the file is dropped so an
        // import can't attach entries to another user's (or a nonexistent) category.
        var ownCategoryIds = await _context.Categories
            .Where(c => c.UserId == userGuid)
            .Select(c => c.Id)
            .ToListAsync();
        var ownCategorySet = ownCategoryIds.ToHashSet();

        // Existing content hashes for this user, so re-importing the same export is a no-op.
        var existingHashes = await _context.Entries
            .Where(e => e.UserId == userGuid && e.OriginalHash != null)
            .Select(e => e.OriginalHash!)
            .ToListAsync();
        var seenHashes = existingHashes.ToHashSet();

        var result = new ImportResultDto();
        var toAdd = new List<Entry>();
        var index = 0;

        foreach (var item in request.Entries)
        {
            index++;

            var title = string.IsNullOrWhiteSpace(item.Title) ? null : item.Title.Trim();
            var body = string.IsNullOrWhiteSpace(item.BodyText) ? null : item.BodyText;

            if (title == null && body == null)
            {
                result.Failed++;
                result.Errors.Add($"Entry {index}: must have a title or body.");
                continue;
            }

            if (title is { Length: > 255 })
            {
                result.Failed++;
                result.Errors.Add($"Entry {index}: title exceeds 255 characters.");
                continue;
            }

            if (item.SentimentScore is < -1 or > 1)
            {
                result.Failed++;
                result.Errors.Add($"Entry {index}: sentiment score must be between -1 and 1.");
                continue;
            }

            // Unknown/missing confidentiality defaults to the more protective "private".
            var confidentiality = item.Confidentiality?.Trim().ToLowerInvariant() switch
            {
                "public" => "public",
                _ => "private"
            };

            var createdAt = item.CreatedAt?.ToUniversalTime() ?? DateTime.UtcNow;
            var hash = ComputeHash(title, body, createdAt);

            // Skip duplicates within the DB and within this same batch.
            if (!seenHashes.Add(hash))
            {
                result.SkippedDuplicates++;
                continue;
            }

            var entry = new Entry
            {
                Id = Guid.NewGuid(),
                UserId = userGuid,
                Title = title,
                BodyText = body,
                Type = "text",
                Confidentiality = confidentiality,
                ConfidentialityMethod = confidentiality == "private" ? "account_password" : "none",
                CategoryId = item.CategoryId is { } cid && ownCategorySet.Contains(cid) ? cid : null,
                Tags = item.Tags,
                SentimentScore = item.SentimentScore,
                SentimentLabel = item.SentimentLabel,
                SentimentModel = item.SentimentScore.HasValue ? "imported" : null,
                CreatedAt = createdAt,
                Source = "import",
                OriginalHash = hash,
                Immutable = false
            };
            entry.ReadOnlyAfter = _entryService.CalculateReadOnlyAfter(user.Timezone, createdAt);
            entry.Immutable = DateTime.UtcNow > entry.ReadOnlyAfter;

            toAdd.Add(entry);
        }

        if (toAdd.Count > 0)
        {
            _context.Entries.AddRange(toAdd);
            _context.AuditLogs.AddRange(toAdd.Select(e => new AuditLog
            {
                EntryId = e.Id,
                UserId = userGuid,
                Action = "import",
                ActorIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            }));
            await _context.SaveChangesAsync();
        }

        result.Imported = toAdd.Count;
        _logger.LogInformation(
            "Import by user {UserId}: {Imported} imported, {Skipped} duplicate(s) skipped, {Failed} failed",
            userGuid, result.Imported, result.SkippedDuplicates, result.Failed);

        return Ok(result);
    }

    /// <summary>
    /// Stable content fingerprint (SHA-256 hex, 64 chars) used for idempotent imports.
    /// Derived from title, body, and creation instant so the same logical entry hashes
    /// identically across exports.
    /// </summary>
    private static string ComputeHash(string? title, string? body, DateTime createdAtUtc)
    {
        var material = $"{title}\n{body}\n{createdAtUtc:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
