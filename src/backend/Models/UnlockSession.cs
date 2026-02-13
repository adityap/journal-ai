using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Backend.Models;

/// <summary>
/// Unlock session model for time-limited access to private entries
/// After entry becomes immutable, users must unlock via account password to view/edit
/// Session expires after 1 hour or on logout
/// </summary>
public class UnlockSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid EntryId { get; set; }

    [Required]
    [StringLength(255)]
    public string SessionToken { get; set; } = string.Empty; // JWT or secure token

    [Required]
    public DateTime ExpiresAt { get; set; } // Session TTL: 1 hour

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(EntryId))]
    public virtual Entry Entry { get; set; } = null!;
}
