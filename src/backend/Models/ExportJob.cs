using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Backend.Models;

/// <summary>
/// Export job model for async data exports
/// Tracks background jobs for exporting entries in multiple formats
/// </summary>
public class ExportJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Scope { get; set; } // JSON string: Query filters (dateRange, categoryIds, tags)

    [Required]
    [StringLength(16)]
    public string Format { get; set; } = "json"; // json|markdown|zip

    [Required]
    [StringLength(16)]
    public string Status { get; set; } = "queued"; // queued|running|completed|failed

    [StringLength(1024)]
    public string? DownloadUrl { get; set; } // S3 URL for completed export

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "text")]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
