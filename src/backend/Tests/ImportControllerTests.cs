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
using Microsoft.AspNetCore.Http;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Tests for ImportController: ownership assignment, category scoping, confidentiality
/// defaulting, duplicate skipping, and payload limits.
/// </summary>
public class ImportControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private AppDbContext _dbContext = null!;
    private ImportController _controller = null!;
    private Guid _testUserId;

    public ImportControllerTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _testUserId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        _dbContext = new AppDbContext(_dbContextOptions);
        _dbContext.Users.Add(new User
        {
            Id = _testUserId, Email = "imp@example.com", PasswordHash = "hash",
            Timezone = "UTC", NoTrainingUse = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var entryLogger = new Mock<ILogger<EntryService>>();
        var logger = new Mock<ILogger<ImportController>>();
        _controller = new ImportController(_dbContext, new EntryService(entryLogger.Object), logger.Object);
        SetUser(_testUserId);
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    private void SetUser(Guid? userId)
    {
        var identity = userId.HasValue
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) })
            : new ClaimsIdentity();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Import_CreatesEntries_OwnedByCaller_AndMarkedImported()
    {
        var request = new ImportRequestDto
        {
            Entries = new()
            {
                new ImportEntryDto { Title = "First", BodyText = "alpha", Confidentiality = "public", CreatedAt = DateTime.UtcNow },
                new ImportEntryDto { Title = "Second", BodyText = "beta", Confidentiality = "private", CreatedAt = DateTime.UtcNow }
            }
        };

        var result = await _controller.ImportEntries(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var summary = Assert.IsType<ImportResultDto>(ok.Value);
        Assert.Equal(2, summary.Imported);
        Assert.Equal(0, summary.SkippedDuplicates);

        var stored = await _dbContext.Entries.Where(e => e.UserId == _testUserId).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.All(stored, e => Assert.Equal("import", e.Source));
        Assert.All(stored, e => Assert.False(string.IsNullOrEmpty(e.OriginalHash)));
    }

    [Fact]
    public async Task Import_IgnoresClientSuppliedIdAndUserId()
    {
        var otherUser = Guid.NewGuid();
        var request = new ImportRequestDto
        {
            // These fields don't exist on the DTO, but a malicious file could still try
            // to set ownership; the import always assigns the caller. Confirm directly.
            Entries = new() { new ImportEntryDto { Title = "Mine now", BodyText = "x", Confidentiality = "public" } }
        };

        await _controller.ImportEntries(request);

        var stored = await _dbContext.Entries.SingleAsync();
        Assert.Equal(_testUserId, stored.UserId);
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.NotEqual(otherUser, stored.UserId);
    }

    [Fact]
    public async Task Import_DropsForeignOrUnknownCategory()
    {
        var foreignCategory = Guid.NewGuid();
        var request = new ImportRequestDto
        {
            Entries = new() { new ImportEntryDto { Title = "Cat", BodyText = "x", Confidentiality = "public", CategoryId = foreignCategory } }
        };

        await _controller.ImportEntries(request);

        var stored = await _dbContext.Entries.SingleAsync();
        Assert.Null(stored.CategoryId);
    }

    [Fact]
    public async Task Import_KeepsOwnCategory()
    {
        var myCategory = new Category { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Work" };
        _dbContext.Categories.Add(myCategory);
        await _dbContext.SaveChangesAsync();

        var request = new ImportRequestDto
        {
            Entries = new() { new ImportEntryDto { Title = "Cat", BodyText = "x", Confidentiality = "public", CategoryId = myCategory.Id } }
        };

        await _controller.ImportEntries(request);

        var stored = await _dbContext.Entries.SingleAsync();
        Assert.Equal(myCategory.Id, stored.CategoryId);
    }

    [Fact]
    public async Task Import_DefaultsUnknownConfidentialityToPrivate()
    {
        var request = new ImportRequestDto
        {
            Entries = new()
            {
                new ImportEntryDto { Title = "No conf", BodyText = "x" },
                new ImportEntryDto { Title = "Bad conf", BodyText = "y", Confidentiality = "totally-secret" }
            }
        };

        await _controller.ImportEntries(request);

        var stored = await _dbContext.Entries.Where(e => e.UserId == _testUserId).ToListAsync();
        Assert.All(stored, e => Assert.Equal("private", e.Confidentiality));
    }

    [Fact]
    public async Task Import_SkipsDuplicates_OnReimport()
    {
        var created = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var request = new ImportRequestDto
        {
            Entries = new() { new ImportEntryDto { Title = "Dup", BodyText = "same", Confidentiality = "public", CreatedAt = created } }
        };

        var first = Assert.IsType<ImportResultDto>(Assert.IsType<OkObjectResult>(await _controller.ImportEntries(request)).Value);
        Assert.Equal(1, first.Imported);

        // Re-import the identical payload — should be a no-op.
        var second = Assert.IsType<ImportResultDto>(Assert.IsType<OkObjectResult>(await _controller.ImportEntries(request)).Value);
        Assert.Equal(0, second.Imported);
        Assert.Equal(1, second.SkippedDuplicates);

        Assert.Equal(1, await _dbContext.Entries.CountAsync(e => e.UserId == _testUserId));
    }

    [Fact]
    public async Task Import_SkipsDuplicatesWithinSameBatch()
    {
        var created = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var request = new ImportRequestDto
        {
            Entries = new()
            {
                new ImportEntryDto { Title = "Dup", BodyText = "same", Confidentiality = "public", CreatedAt = created },
                new ImportEntryDto { Title = "Dup", BodyText = "same", Confidentiality = "public", CreatedAt = created }
            }
        };

        var summary = Assert.IsType<ImportResultDto>(Assert.IsType<OkObjectResult>(await _controller.ImportEntries(request)).Value);
        Assert.Equal(1, summary.Imported);
        Assert.Equal(1, summary.SkippedDuplicates);
    }

    [Fact]
    public async Task Import_RecordsFailure_ForEmptyEntry()
    {
        var request = new ImportRequestDto
        {
            Entries = new() { new ImportEntryDto { Title = "  ", BodyText = "", Confidentiality = "public" } }
        };

        var summary = Assert.IsType<ImportResultDto>(Assert.IsType<OkObjectResult>(await _controller.ImportEntries(request)).Value);
        Assert.Equal(0, summary.Imported);
        Assert.Equal(1, summary.Failed);
        Assert.Single(summary.Errors);
    }

    [Fact]
    public async Task Import_EmptyPayload_Returns400()
    {
        var result = await _controller.ImportEntries(new ImportRequestDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Import_TooManyEntries_Returns400()
    {
        var request = new ImportRequestDto
        {
            Entries = Enumerable.Range(0, 5001)
                .Select(i => new ImportEntryDto { Title = $"E{i}", Confidentiality = "public" })
                .ToList()
        };

        var result = await _controller.ImportEntries(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Import_NoUserClaim_Returns401()
    {
        SetUser(null);
        var request = new ImportRequestDto
        {
            Entries = new() { new ImportEntryDto { Title = "x", Confidentiality = "public" } }
        };

        var result = await _controller.ImportEntries(request);
        Assert.IsType<UnauthorizedResult>(result);
    }
}
