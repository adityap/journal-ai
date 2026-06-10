using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Data.Dtos;

/// <summary>
/// Request to unlock the caller's private entries for a session. The password is the
/// user's account password (per-entry passwords are deferred to v2).
/// </summary>
public class UnlockRequestDto
{
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Result of a successful unlock: an opaque session token the client sends back via the
/// <c>X-Unlock-Token</c> header to view private entries until <see cref="ExpiresAt"/>.
/// </summary>
public class UnlockResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
