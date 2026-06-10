using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JournalAI.Backend.Models;

/// <summary>
/// Unlock session model for time-limited access to a user's private entries.
/// The user unlocks with their account password; the session grants read access to
/// all of their private entries until it expires (1-hour TTL) or is revoked on logout.
/// </summary>
public class UnlockSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional entry scope. Null means an account-wide session (the MVP behavior:
    /// one unlock covers all of the user's private entries). Reserved non-null for a
    /// future per-entry unlock mode.
    /// </summary>
    public Guid? EntryId { get; set; }

    /// <summary>
    /// SHA-256 hash (hex) of the opaque session token. The raw token is returned to
    /// the client once and never persisted, so a DB leak does not grant unlock access.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; } // Session TTL: 1 hour

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(EntryId))]
    public virtual Entry? Entry { get; set; }
}
