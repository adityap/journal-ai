using Xunit;
using Moq;
using System.Text;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Controllers;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Tests for ExportController: per-user scoping, format handling, and async export jobs.
/// </summary>
public class ExportControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private AppDbContext _dbContext = null!;
    private ExportController _controller = null!;
    private Mock<IBackgroundJobClient> _jobClient = null!;
    private Mock<S3Service> _s3 = null!;
    private IConfiguration _config = null!;
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
        _jobClient = new Mock<IBackgroundJobClient>();
        _s3 = new Mock<S3Service>(
            new Mock<Amazon.S3.IAmazonS3>().Object,
            new ConfigurationBuilder().Build(),
            new Mock<ILogger<S3Service>>().Object);
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["S3:BucketName"] = "test-bucket" })
            .Build();

        _controller = new ExportController(_dbContext, _jobClient.Object, _s3.Object, _config, logger.Object);
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

    // --- async export jobs ---

    [Fact]
    public async Task CreateJob_QueuesJob_AndEnqueuesWork()
    {
        var result = await _controller.CreateJob(new CreateExportJobDto { Format = "zip" });

        var accepted = Assert.IsType<AcceptedAtActionResult>(result);
        var dto = Assert.IsType<ExportJobDto>(accepted.Value);
        Assert.Equal("zip", dto.Format);
        Assert.Equal("queued", dto.Status);
        Assert.False(dto.Ready);

        var stored = await _dbContext.ExportJobs.SingleAsync();
        Assert.Equal(_testUserId, stored.UserId);
        _jobClient.Verify(c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task CreateJob_UnsupportedFormat_Returns400()
    {
        var result = await _controller.CreateJob(new CreateExportJobDto { Format = "pdf" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ListJobs_ReturnsOnlyOwnJobs()
    {
        _dbContext.ExportJobs.Add(new ExportJob { Id = Guid.NewGuid(), UserId = _testUserId, Format = "zip", Status = "queued", CreatedAt = DateTime.UtcNow });
        _dbContext.ExportJobs.Add(new ExportJob { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Format = "zip", Status = "queued", CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.ListJobs();
        var jobs = Assert.IsType<List<ExportJobDto>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(jobs);
    }

    [Fact]
    public async Task GetJob_OtherUsersJob_Returns404()
    {
        var foreign = new ExportJob { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Format = "zip", Status = "queued", CreatedAt = DateTime.UtcNow };
        _dbContext.ExportJobs.Add(foreign);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetJob(foreign.Id);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DownloadJob_NotReady_Returns409()
    {
        var job = new ExportJob { Id = Guid.NewGuid(), UserId = _testUserId, Format = "zip", Status = "running", CreatedAt = DateTime.UtcNow };
        _dbContext.ExportJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.DownloadJob(job.Id);
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DownloadJob_Completed_StreamsArtifactFromBlobStore()
    {
        var job = new ExportJob
        {
            Id = Guid.NewGuid(), UserId = _testUserId, Format = "zip", Status = "completed",
            DownloadUrl = $"exports/{_testUserId}/job.zip", CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
        };
        _dbContext.ExportJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var payload = new byte[] { 1, 2, 3, 4 };
        _s3.Setup(s => s.DownloadObjectAsync("test-bucket", job.DownloadUrl!)).ReturnsAsync(payload);

        var result = await _controller.DownloadJob(job.Id);
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal(payload, file.FileContents);
    }
}
