using Xunit;
using Moq;
using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Data;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Tests for the async export worker: artifact generation, ZIP bundling with media,
/// blob-store upload, and failure handling. S3 is mocked.
/// </summary>
public class ExportGenerationJobTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private AppDbContext _db = null!;
    private Mock<S3Service> _s3 = null!;
    private IConfiguration _config = null!;
    private ExportGenerationJob _job = null!;
    private Guid _userId;

    public ExportGenerationJobTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _userId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        _db = new AppDbContext(_options);
        _db.Entries.Add(new Entry
        {
            Id = Guid.NewGuid(), UserId = _userId, Title = "Entry one", BodyText = "hello",
            Confidentiality = "public", CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        });
        await _db.SaveChangesAsync();

        _s3 = new Mock<S3Service>(
            new Mock<Amazon.S3.IAmazonS3>().Object,
            new ConfigurationBuilder().Build(),
            new Mock<ILogger<S3Service>>().Object);
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["S3:BucketName"] = "test-bucket" })
            .Build();
        _job = new ExportGenerationJob(_db, _s3.Object, _config, new Mock<ILogger<ExportGenerationJob>>().Object);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<ExportJob> AddJobAsync(string format)
    {
        var job = new ExportJob { Id = Guid.NewGuid(), UserId = _userId, Format = format, Status = "queued", CreatedAt = DateTime.UtcNow };
        _db.ExportJobs.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    [Fact]
    public async Task Generate_Json_UploadsArtifactAndMarksCompleted()
    {
        byte[]? uploaded = null;
        _s3.Setup(s => s.PutObjectAsync("test-bucket", It.IsAny<string>(), It.IsAny<byte[]>(), "application/json"))
            .Callback<string, string, byte[], string>((_, _, data, _) => uploaded = data)
            .ReturnsAsync(true);

        var job = await AddJobAsync("json");
        await _job.GenerateAsync(job.Id);

        var updated = await _db.ExportJobs.FindAsync(job.Id);
        Assert.Equal("completed", updated!.Status);
        Assert.False(string.IsNullOrEmpty(updated.DownloadUrl));
        Assert.NotNull(updated.CompletedAt);
        Assert.Contains("Entry one", Encoding.UTF8.GetString(uploaded!));
    }

    [Fact]
    public async Task Generate_Zip_BundlesEntriesAndMedia()
    {
        // One media row for the user; its bytes come from the mocked S3 download.
        var media = new Media
        {
            Id = Guid.NewGuid(), UserId = _userId, MimeType = "image/png", Size = 3,
            Url = "s3://test-bucket/users/x/a.png", StorageKey = "users/x/a.png", CreatedAt = DateTime.UtcNow
        };
        _db.Media.Add(media);
        await _db.SaveChangesAsync();

        _s3.Setup(s => s.DownloadObjectAsync("test-bucket", media.StorageKey)).ReturnsAsync(new byte[] { 9, 9, 9 });
        byte[]? uploadedZip = null;
        _s3.Setup(s => s.PutObjectAsync("test-bucket", It.IsAny<string>(), It.IsAny<byte[]>(), "application/zip"))
            .Callback<string, string, byte[], string>((_, _, data, _) => uploadedZip = data)
            .ReturnsAsync(true);

        var job = await AddJobAsync("zip");
        await _job.GenerateAsync(job.Id);

        var updated = await _db.ExportJobs.FindAsync(job.Id);
        Assert.Equal("completed", updated!.Status);

        using var zip = new ZipArchive(new MemoryStream(uploadedZip!), ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("entries.json", names);
        Assert.Contains("entries.md", names);
        Assert.Contains(names, n => n.StartsWith("media/") && n.EndsWith(".png"));
    }

    [Fact]
    public async Task Generate_WhenUploadFails_MarksJobFailed()
    {
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("blob store down"));

        var job = await AddJobAsync("json");
        await _job.GenerateAsync(job.Id);

        var updated = await _db.ExportJobs.FindAsync(job.Id);
        Assert.Equal("failed", updated!.Status);
        Assert.Equal("blob store down", updated.ErrorMessage);
    }
}
