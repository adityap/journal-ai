using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Backend.Models;

/// <summary>
/// User model
/// Represents a journal AI user account
/// Features: timezone-aware immutability, privacy controls, audit trail
/// </summary>
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Flag: user data marked as not for training.
    /// Always true, immutable. GDPR/privacy compliance.
    /// </summary>
    [Required]
    public bool NoTrainingUse { get; set; } = true;

    [Column(TypeName = "jsonb")]
    public string? Settings { get; set; } // JSON string for flexible user settings

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    public virtual ICollection<Media> Media { get; set; } = new List<Media>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<ExportJob> ExportJobs { get; set; } = new List<ExportJob>();
    public virtual ICollection<UnlockSession> UnlockSessions { get; set; } = new List<UnlockSession>();
}
