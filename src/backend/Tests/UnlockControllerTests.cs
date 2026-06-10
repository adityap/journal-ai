using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Controllers;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Tests for the confidentiality/unlock feature: password-gated session issuance and
/// read-time redaction of private entries until unlocked.
/// </summary>
public class UnlockControllerTests : IAsyncLifetime
{
    private const string Password = "Secret123!@#";
    private readonly DbContextOptions<AppDbContext> _options;
    private AppDbContext _db = null!;
    private UnlockService _unlock = null!;
    private UnlockController _unlockController = null!;
    private EntriesController _entries = null!;
    private Guid _userId;
    private Guid _privateEntryId;
    private Guid _publicEntryId;

    public UnlockControllerTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _userId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        _db = new AppDbContext(_options);
        _db.Users.Add(new User
        {
            Id = _userId, Email = "lock@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 12),
            Timezone = "UTC", NoTrainingUse = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        _privateEntryId = Guid.NewGuid();
        _publicEntryId = Guid.NewGuid();
        _db.Entries.Add(new Entry
        {
            Id = _privateEntryId, UserId = _userId, Title = "Secret title", BodyText = "secret body",
            Tags = new[] { "private-tag" }, SentimentScore = 0.5m, SentimentLabel = "positive",
            Confidentiality = "private", CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        });
        _db.Entries.Add(new Entry
        {
            Id = _publicEntryId, UserId = _userId, Title = "Public title", BodyText = "public body",
            Confidentiality = "public", CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        });
        await _db.SaveChangesAsync();

        _unlock = new UnlockService(_db, new Mock<ILogger<UnlockService>>().Object);
        _unlockController = new UnlockController(_unlock, new Mock<ILogger<UnlockController>>().Object);
        _entries = new EntriesController(_db, new EntryService(new Mock<ILogger<EntryService>>().Object),
            new TfIdfService(), _unlock, new Mock<ILogger<EntriesController>>().Object);

        SetClaims(_unlockController, _userId);
        SetClaims(_entries, _userId);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private static void SetClaims(ControllerBase controller, Guid? userId, string? unlockToken = null)
    {
        var identity = userId.HasValue
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) })
            : new ClaimsIdentity();
        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        if (unlockToken != null)
            ctx.Request.Headers["X-Unlock-Token"] = unlockToken;
        controller.ControllerContext = new ControllerContext { HttpContext = ctx };
    }

    private async Task<string> UnlockAndGetTokenAsync()
    {
        var result = await _unlockController.Unlock(new UnlockRequestDto { Password = Password });
        var dto = Assert.IsType<UnlockResponseDto>(Assert.IsType<OkObjectResult>(result).Value);
        return dto.Token;
    }

    // --- unlock endpoint ---

    [Fact]
    public async Task Unlock_CorrectPassword_ReturnsTokenAndExpiry()
    {
        var result = await _unlockController.Unlock(new UnlockRequestDto { Password = Password });
        var dto = Assert.IsType<UnlockResponseDto>(Assert.IsType<OkObjectResult>(result).Value);

        Assert.False(string.IsNullOrWhiteSpace(dto.Token));
        Assert.True(dto.ExpiresAt > DateTime.UtcNow);

        // The DB stores only a hash, never the raw token.
        var stored = await _db.UnlockSessions.SingleAsync();
        Assert.NotEqual(dto.Token, stored.SessionToken);
        Assert.Equal(HashHex(dto.Token), stored.SessionToken);
        Assert.Null(stored.EntryId); // account-wide
    }

    [Fact]
    public async Task Unlock_WrongPassword_Returns401_AndCreatesNoSession()
    {
        var result = await _unlockController.Unlock(new UnlockRequestDto { Password = "wrong" });
        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, await _db.UnlockSessions.CountAsync());
    }

    [Fact]
    public async Task Unlock_MissingPassword_Returns400()
    {
        var result = await _unlockController.Unlock(new UnlockRequestDto { Password = "" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Unlock_NoClaim_Returns401()
    {
        SetClaims(_unlockController, null);
        var result = await _unlockController.Unlock(new UnlockRequestDto { Password = Password });
        Assert.IsType<UnauthorizedResult>(result);
    }

    // --- read gating ---

    [Fact]
    public async Task List_PrivateEntry_IsLockedAndRedacted_WhenNotUnlocked()
    {
        var result = await _entries.ListEntries(null, null, null, null);
        var paged = Assert.IsType<PagedResult<EntryResponseDto>>(Assert.IsType<OkObjectResult>(result).Value);

        var priv = paged.Items.Single(i => i.Id == _privateEntryId);
        Assert.True(priv.Locked);
        Assert.Null(priv.Title);
        Assert.Null(priv.BodyText);
        Assert.Null(priv.Tags);
        Assert.Null(priv.SentimentScore);
        Assert.Equal("private", priv.Confidentiality); // metadata still present

        var pub = paged.Items.Single(i => i.Id == _publicEntryId);
        Assert.False(pub.Locked);
        Assert.Equal("Public title", pub.Title);
    }

    [Fact]
    public async Task List_PrivateEntry_IsRevealed_WithValidUnlockToken()
    {
        var token = await UnlockAndGetTokenAsync();
        SetClaims(_entries, _userId, token);

        var result = await _entries.ListEntries(null, null, null, null);
        var paged = Assert.IsType<PagedResult<EntryResponseDto>>(Assert.IsType<OkObjectResult>(result).Value);

        var priv = paged.Items.Single(i => i.Id == _privateEntryId);
        Assert.False(priv.Locked);
        Assert.Equal("Secret title", priv.Title);
        Assert.Equal("secret body", priv.BodyText);
    }

    [Fact]
    public async Task GetEntry_PrivateEntry_IsLocked_WhenNotUnlocked()
    {
        var result = await _entries.GetEntry(_privateEntryId);
        var dto = Assert.IsType<EntryResponseDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(dto.Locked);
        Assert.Null(dto.BodyText);
    }

    [Fact]
    public async Task GetEntry_PrivateEntry_IsRevealed_WithValidUnlockToken()
    {
        var token = await UnlockAndGetTokenAsync();
        SetClaims(_entries, _userId, token);

        var result = await _entries.GetEntry(_privateEntryId);
        var dto = Assert.IsType<EntryResponseDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.False(dto.Locked);
        Assert.Equal("secret body", dto.BodyText);
    }

    [Fact]
    public async Task Graph_RedactsPrivateNodeTitles_WhenNotUnlocked()
    {
        var result = await _entries.GetSimilarityGraph();
        var graph = Assert.IsType<SimilarityGraphDto>(Assert.IsType<OkObjectResult>(result).Value);

        var privNode = graph.Nodes.Single(n => n.Id == _privateEntryId);
        Assert.Null(privNode.Title);
        var pubNode = graph.Nodes.Single(n => n.Id == _publicEntryId);
        Assert.Equal("Public title", pubNode.Title);
    }

    // --- session lifecycle ---

    [Fact]
    public async Task ExpiredSession_DoesNotUnlock()
    {
        var raw = "expired-token-value";
        _db.UnlockSessions.Add(new UnlockSession
        {
            Id = Guid.NewGuid(), UserId = _userId, EntryId = null,
            SessionToken = HashHex(raw), ExpiresAt = DateTime.UtcNow.AddMinutes(-1), CreatedAt = DateTime.UtcNow.AddHours(-2)
        });
        await _db.SaveChangesAsync();

        Assert.False(await _unlock.IsUnlockedAsync(_userId, raw));
    }

    [Fact]
    public async Task WrongToken_DoesNotUnlock()
    {
        await UnlockAndGetTokenAsync(); // a valid session exists
        Assert.False(await _unlock.IsUnlockedAsync(_userId, "not-the-token"));
    }

    [Fact]
    public async Task RevokeAll_RemovesSessions_SoEntriesRelock()
    {
        var token = await UnlockAndGetTokenAsync();
        Assert.True(await _unlock.IsUnlockedAsync(_userId, token));

        await _unlock.RevokeAllAsync(_userId);

        Assert.False(await _unlock.IsUnlockedAsync(_userId, token));
        Assert.Equal(0, await _db.UnlockSessions.CountAsync());
    }

    private static string HashHex(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
}
