using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Models;

public class Entry
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public string? Title { get; set; }
    public string? BodyText { get; set; }
    public string Type { get; set; } = "text"; // text|photo|video|mixed
    public string Confidentiality { get; set; } = "public"; // public|private
    public string? ConfidentialityMethod { get; set; }
    public string? ConfidentialityHint { get; set; }
    public string? ConfidentialityHash { get; set; }

    public Guid? CategoryId { get; set; }

    [Column(TypeName = "text[]")]
    public string[]? Tags { get; set; }

    public decimal? SentimentScore { get; set; }
    public string? SentimentLabel { get; set; }
    public string? SentimentModel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime ReadOnlyAfter { get; set; }
    public bool Immutable { get; set; }

    public string? Metadata { get; set; }
    public string? Source { get; set; }
    public string? OriginalHash { get; set; }
}
