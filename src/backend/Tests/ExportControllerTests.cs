using Xunit;
using Moq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Controllers;
using JournalAI.Backend.Data;
using JournalAI.Backend.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Tests for ExportController: per-user scoping, format handling, and content.
/// </summary>
public class ExportControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private AppDbContext _dbContext = null!;
    private ExportController _controller = null!;
    private Guid _testUserId;

    public ExportControllerTests()
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
            Id = _testUserId, Email = "exp@example.com", PasswordHash = "hash",
            Timezone = "UTC", NoTrainingUse = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        // Two entries for this user, one for someone else (must be excluded).
        _dbContext.Entries.Add(new Entry
        {
            Id = Guid.NewGuid(), UserId = _testUserId, Title = "Mine A", BodyText = "alpha",
            Confidentiality = "public", CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        });
        _dbContext.Entries.Add(new Entry
        {
            Id = Guid.NewGuid(), UserId = _testUserId, Title = "Mine B", BodyText = "beta",
            Confidentiality = "private", CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        });
        _dbContext.Entries.Add(new Entry
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Not mine", BodyText = "gamma",
            Confidentiality = "public", CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        });
        await _dbContext.SaveChangesAsync();

        var logger = new Mock<ILogger<ExportController>>();
        _controller = new ExportController(_dbContext, logger.Object);
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
        };
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    [Fact]
    public async Task ExportEntries_Json_ReturnsOnlyOwnEntries()
    {
        var result = await _controller.ExportEntries("json");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/json", file.ContentType);
        Assert.EndsWith(".json", file.FileDownloadName);

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(file.FileContents));
        Assert.Equal(2, doc.RootElement.GetProperty("count").GetInt32());
        var titles = doc.RootElement.GetProperty("entries").EnumerateArray()
            .Select(e => e.GetProperty("Title").GetString()).ToList();
        Assert.Contains("Mine A", titles);
        Assert.Contains("Mine B", titles);
        Assert.DoesNotContain("Not mine", titles);
    }

    [Fact]
    public async Task ExportEntries_Markdown_ReturnsMarkdownWithEntries()
    {
        var result = await _controller.ExportEntries("markdown");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/markdown", file.ContentType);
        Assert.EndsWith(".md", file.FileDownloadName);

        var text = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("# Journal Export", text);
        Assert.Contains("## Mine A", text);
        Assert.Contains("## Mine B", text);
        Assert.DoesNotContain("Not mine", text);
    }

    [Fact]
    public async Task ExportEntries_UnsupportedFormat_Returns400()
    {
        var result = await _controller.ExportEntries("pdf");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportEntries_NoUserClaim_Returns401()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var result = await _controller.ExportEntries("json");
        Assert.IsType<UnauthorizedResult>(result);
    }
}
