using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Backend.Models;

/// <summary>
/// Audit log model for tracking changes
/// Records all create, update, delete operations on entries
/// Supports privacy audit and compliance requirements
/// </summary>
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EntryId { get; set; }

    public Guid? UserId { get; set; } // Nullable: can be null if user deleted

    [Required]
    [StringLength(16)]
    public string Action { get; set; } = string.Empty; // create|update|delete

    [StringLength(45)]
    public string? ActorIp { get; set; } // IPv4 or IPv6

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "jsonb")]
    public string? Diff { get; set; } // JSON string: what changed (encrypted)

    // Navigation properties
    [ForeignKey(nameof(EntryId))]
    public virtual Entry Entry { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
