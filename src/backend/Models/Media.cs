using System.ComponentModel.DataAnnotations;

namespace JournalAI.Models;

public class Media
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? EntryId { get; set; }
    public string Url { get; set; } = null!;
    public string? Mime { get; set; }
    public long? Size { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string StorageKey { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
