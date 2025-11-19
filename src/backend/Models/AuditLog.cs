using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Models;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
