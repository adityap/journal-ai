using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JournalAI.Backend.Data;
using JournalAI.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/v1/exports")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExportController> _logger;

    public ExportController(AppDbContext context, ILogger<ExportController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Export the current user's entries as a downloadable file.
    /// GET /api/v1/exports?format=json|markdown
    ///
    /// Synchronous, text-based export covering all of the caller's own entries
    /// (regardless of confidentiality — it is their own data). ZIP-with-media and
    /// background/large exports remain a planned enhancement on the export_jobs table.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ExportEntries([FromQuery] string format = "json")
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var normalized = (format ?? "json").Trim().ToLowerInvariant();
        if (normalized is not ("json" or "markdown" or "md"))
            return BadRequest(new { errors = new[] { "Unsupported format. Use 'json' or 'markdown'." } });

        var entries = await _context.Entries
            .Where(e => e.UserId == userGuid)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        if (normalized == "json")
        {
            var payload = new
            {
                exportedAt = DateTime.UtcNow,
                count = entries.Count,
                entries = entries.Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.BodyText,
                    e.Confidentiality,
                    e.CategoryId,
                    e.Tags,
                    e.SentimentScore,
                    e.SentimentLabel,
                    e.CreatedAt
                })
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Export (json) of {Count} entries by user {UserId}", entries.Count, userGuid);
            return File(Encoding.UTF8.GetBytes(json), "application/json", $"journal-export-{stamp}.json");
        }

        // markdown / md
        var markdown = BuildMarkdown(entries);
        _logger.LogInformation("Export (markdown) of {Count} entries by user {UserId}", entries.Count, userGuid);
        return File(Encoding.UTF8.GetBytes(markdown), "text/markdown", $"journal-export-{stamp}.md");
    }

    private static string BuildMarkdown(List<Entry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Journal Export");
        sb.AppendLine();
        sb.AppendLine($"_Exported {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC · {entries.Count} " +
                      $"{(entries.Count == 1 ? "entry" : "entries")}_");
        sb.AppendLine();

        foreach (var e in entries)
        {
            sb.AppendLine($"## {(string.IsNullOrWhiteSpace(e.Title) ? "(untitled)" : e.Title)}");

            var meta = new List<string> { e.CreatedAt.ToString("yyyy-MM-dd HH:mm") + " UTC", e.Confidentiality };
            if (!string.IsNullOrWhiteSpace(e.SentimentLabel))
                meta.Add(e.SentimentLabel!);
            sb.AppendLine($"_{string.Join(" · ", meta)}_");
            sb.AppendLine();

            if (e.Tags is { Length: > 0 })
            {
                sb.AppendLine(string.Join(" ", e.Tags.Select(t => $"`#{t}`")));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(e.BodyText))
            {
                sb.AppendLine(e.BodyText);
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
