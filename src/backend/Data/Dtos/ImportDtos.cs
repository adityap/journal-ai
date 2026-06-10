namespace JournalAI.Backend.Data.Dtos;

/// <summary>
/// DTO for importing entries. Mirrors the JSON export shape: the file produced by
/// <c>GET /api/v1/exports?format=json</c> is an object with an <c>entries</c> array,
/// so it can be re-submitted here directly for a round-trip.
/// </summary>
public class ImportRequestDto
{
    public List<ImportEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// A single entry in an import payload. Only content fields are honored — any
/// <c>id</c>/<c>userId</c> present in the file is intentionally ignored, and ownership
/// is always assigned to the calling user. Treat every field as untrusted input.
/// </summary>
public class ImportEntryDto
{
    public string? Title { get; set; }

    public string? BodyText { get; set; }

    public string? Confidentiality { get; set; }

    public Guid? CategoryId { get; set; }

    public string[]? Tags { get; set; }

    public decimal? SentimentScore { get; set; }

    public string? SentimentLabel { get; set; }

    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Summary of an import run. Reports how many entries were created, how many were
/// skipped as duplicates of existing entries, and any per-entry validation errors.
/// </summary>
public class ImportResultDto
{
    public int Imported { get; set; }

    public int SkippedDuplicates { get; set; }

    public int Failed { get; set; }

    public List<string> Errors { get; set; } = new();
}
