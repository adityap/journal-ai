using System.ComponentModel.DataAnnotations;

namespace JournalAI.Models;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; }
    public Guid? EntryId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = null!; // create|update|delete|unlock
    public string? ActorIp { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public byte[]? Diff { get; set; }
}
