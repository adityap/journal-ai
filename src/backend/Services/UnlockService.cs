using System.Security.Cryptography;
using System.Text;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Services;

/// <summary>
/// Issues and validates account-wide unlock sessions that grant read access to a user's
/// private entries. The account password is verified with bcrypt; on success an opaque
/// token is returned to the client while only its SHA-256 hash is stored, so a database
/// leak cannot be replayed as an unlock. Sessions expire after <see cref="SessionTtl"/>
/// or when revoked (e.g. on logout).
/// </summary>
public class UnlockService
{
    public static readonly TimeSpan SessionTtl = TimeSpan.FromHours(1);

    private readonly AppDbContext _context;
    private readonly ILogger<UnlockService> _logger;

    public UnlockService(AppDbContext context, ILogger<UnlockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Verify the account password and, if correct, create an account-wide unlock session.
    /// Returns the raw token and its expiry, or null if the password is wrong / user missing.
    /// Only the token's hash is persisted; the raw token is returned here and nowhere stored.
    /// </summary>
    public async Task<UnlockResponseDto?> CreateSessionAsync(Guid userId, string password)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Failed unlock attempt for user {UserId}", userId);
            return null;
        }

        var rawToken = GenerateRawToken();
        var session = new UnlockSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntryId = null, // account-wide
            SessionToken = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.Add(SessionTtl),
            CreatedAt = DateTime.UtcNow
        };

        _context.UnlockSessions.Add(session);
        await _context.SaveChangesAsync();

        return new UnlockResponseDto { Token = rawToken, ExpiresAt = session.ExpiresAt };
    }

    /// <summary>
    /// True if the presented raw token maps to a live (non-expired) session for this user.
    /// </summary>
    public async Task<bool> IsUnlockedAsync(Guid userId, string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        var hash = HashToken(rawToken);
        var now = DateTime.UtcNow;
        return await _context.UnlockSessions
            .AnyAsync(s => s.UserId == userId && s.SessionToken == hash && s.ExpiresAt > now);
    }

    /// <summary>
    /// Revoke every unlock session for a user (called on logout). Also a convenient place
    /// to clear out any of their expired rows.
    /// </summary>
    public async Task RevokeAllAsync(Guid userId)
    {
        var sessions = await _context.UnlockSessions.Where(s => s.UserId == userId).ToListAsync();
        if (sessions.Count == 0)
            return;

        _context.UnlockSessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
    }

    private static string GenerateRawToken()
    {
        // 256 bits of entropy, URL-safe so it travels cleanly in a header.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
