using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Backend.Models;

/// <summary>
/// Media model for photos, videos, and attachments
/// Stores metadata about media files in blob storage (MinIO/S3)
/// </summary>
public class Media
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public Guid? EntryId { get; set; } // Nullable: media can exist before assignment

    [Required]
    public string Url { get; set; } = string.Empty; // S3/MinIO URL

    [Required]
    [StringLength(64)]
    public string MimeType { get; set; } = string.Empty; // image/jpeg, video/mp4, etc.

    [Required]
    public long Size { get; set; } // In bytes

    [StringLength(512)]
    public string? ThumbnailUrl { get; set; }

    [Required]
    [StringLength(512)]
    public string StorageKey { get; set; } = string.Empty; // S3 object key for deletion

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(EntryId))]
    public virtual Entry? Entry { get; set; }
}
