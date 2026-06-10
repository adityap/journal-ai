using System.Text;
using System.Text.Json;
using JournalAI.Backend.Models;

namespace JournalAI.Backend.Services;

/// <summary>
/// Builds the JSON and Markdown representations of a set of entries. Shared by the
/// synchronous <c>ExportController</c> download path and the async export job so both
/// produce byte-for-byte the same content (and the JSON stays import-compatible).
/// </summary>
public static class ExportContentBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Serialize entries to the JSON export shape (round-trips through import).</summary>
    public static string BuildJson(IReadOnlyList<Entry> entries)
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
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    /// <summary>Render entries as a single Markdown document.</summary>
    public static string BuildMarkdown(IReadOnlyList<Entry> entries)
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
