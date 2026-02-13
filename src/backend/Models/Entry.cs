using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Backend.Models;

/// <summary>
/// Journal entry model
/// Core domain entity for journal AI application
/// Features: immutability after same day, confidentiality levels, sentiment tracking
/// </summary>
public class Entry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    [Column(TypeName = "text")]
    public string? BodyText { get; set; }

    [Required]
    [StringLength(16)]
    public string Type { get; set; } = "text"; // text|photo|video|mixed

    [Required]
    [StringLength(16)]
    public string Confidentiality { get; set; } = "public"; // public|private

    [StringLength(32)]
    public string? ConfidentialityMethod { get; set; } = "none"; // account_password|none

    public Guid? CategoryId { get; set; }

    [Column(TypeName = "text[]")]
    public string[]? Tags { get; set; }

    // Sentiment fields (computed client-side)
    public decimal? SentimentScore { get; set; } // -1..1
    [StringLength(32)]
    public string? SentimentLabel { get; set; } // positive|neutral|negative
    [StringLength(64)]
    public string? SentimentModel { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Computed: end-of-day (23:59:59) in user's timezone when entry becomes immutable
    /// Calculated as: end-of-day in user's local TZ, converted back to UTC
    /// </summary>
    [Required]
    public DateTime ReadOnlyAfter { get; set; }

    /// <summary>
    /// Immutability flag: set when ReadOnlyAfter has passed
    /// Used for quick queries and indexing
    /// </summary>
    public bool Immutable { get; set; } = false;

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; } // JSON string for flexible entry metadata

    [StringLength(16)]
    public string? Source { get; set; } = "ui"; // ui|import|api

    [StringLength(64)]
    public string? OriginalHash { get; set; } // For deduplication during import

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }

    public virtual ICollection<Media> Media { get; set; } = new List<Media>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<UnlockSession> UnlockSessions { get; set; } = new List<UnlockSession>();
}
