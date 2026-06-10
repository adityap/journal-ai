using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Data.Dtos;

/// <summary>
/// DTO for creating a new entry
/// </summary>
public class CreateEntryDto
{
    public string? Title { get; set; }

    public string? BodyText { get; set; }

    [StringLength(16)]
    public string? Type { get; set; } = "text";

    [Required]
    [StringLength(16)]
    public string Confidentiality { get; set; } = null!;

    public Guid? CategoryId { get; set; }

    public string[]? Tags { get; set; }

    public decimal? SentimentScore { get; set; }

    public string? SentimentLabel { get; set; }

    public string? SentimentModel { get; set; }

    public Guid[]? MediaIds { get; set; }

    public Dictionary<string, object>? Metadata { get; set; } // JSON object for metadata

    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// DTO for updating an entry
/// Only editable fields before immutability boundary
/// </summary>
public class UpdateEntryDto
{
    public string? Title { get; set; }

    public string? BodyText { get; set; }

    public Guid? CategoryId { get; set; }

    public string[]? Tags { get; set; }
}

/// <summary>
/// DTO for entry responses
/// </summary>
public class EntryResponseDto
{
    public Guid Id { get; set; }

    public string? Title { get; set; }

    public string? BodyText { get; set; }

    public string Type { get; set; } = null!;

    public string Confidentiality { get; set; } = null!;

    public Guid? CategoryId { get; set; }

    public string[]? Tags { get; set; }

    public decimal? SentimentScore { get; set; }

    public string? SentimentLabel { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ReadOnlyAfter { get; set; }

    public bool Immutable { get; set; }

    /// <summary>
    /// True when this is a private entry the caller has not unlocked. Locked entries are
    /// returned with content fields (title, body, tags, sentiment) redacted to null; the
    /// caller can call POST /api/v1/unlock to reveal them.
    /// </summary>
    public bool Locked { get; set; }
}

/// <summary>
/// Paged result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PerPage { get; set; }

    public int TotalPages { get; set; }
}
