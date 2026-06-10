using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Services;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/v1/unlock")]
[Authorize]
public class UnlockController : ControllerBase
{
    private readonly UnlockService _unlock;
    private readonly ILogger<UnlockController> _logger;

    public UnlockController(UnlockService unlock, ILogger<UnlockController> logger)
    {
        _unlock = unlock;
        _logger = logger;
    }

    /// <summary>
    /// Unlock the caller's private entries for a session.
    /// POST /api/v1/unlock  { "password": "..." }
    ///
    /// Verifies the account password and returns a short-lived opaque token. The client
    /// resends it via the <c>X-Unlock-Token</c> header on entry reads to see private
    /// entries; otherwise those entries come back redacted (locked). A wrong password
    /// returns 401 without revealing anything further.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Unlock([FromBody] UnlockRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        if (request == null || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { errors = new[] { "Password is required." } });

        var unlock = await _unlock.CreateSessionAsync(userGuid, request.Password);
        if (unlock == null)
            return Unauthorized(new { errors = new[] { "Incorrect password." } });

        // Unlock is an account-level event (not tied to a single entry, so it doesn't fit
        // the entry-scoped AuditLog table); record it via structured logging instead.
        _logger.LogInformation("Private entries unlocked for user {UserId} until {ExpiresAt}", userGuid, unlock.ExpiresAt);

        return Ok(unlock);
    }
}
