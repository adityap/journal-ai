using Xunit;
using Moq;
using JournalAI.Backend.Services;
using JournalAI.Backend.Data;
using JournalAI.Backend.Models;
using JournalAI.Backend.Data.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Amazon.S3;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Unit tests for MediaService
/// Tests media CRUD, validation, and S3 operations
/// </summary>
public class MediaServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<S3Service> _s3ServiceMock;
    private readonly Mock<ILogger<MediaService>> _loggerMock;
    private readonly MediaService _mediaService;

    public MediaServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"MediaServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);

        // Setup mocks
        _s3ServiceMock = new Mock<S3Service>(MockBehavior.Loose,
            It.IsAny<IAmazonS3>(),
            It.IsAny<IConfiguration>(),
            It.IsAny<ILogger<S3Service>>()
        );
        _loggerMock = new Mock<ILogger<MediaService>>(MockBehavior.Loose);

        _mediaService = new MediaService(_context, _s3ServiceMock.Object, _loggerMock.Object);

        // Seed test data
        SeedTestData();
    }

    #region Test Helpers

    private void SeedTestData()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    private User GetTestUser()
    {
        return _context.Users.First();
    }

    private void AssertNoErrors()
    {
        Assert.Empty(_context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
            .Select(e => e.Entity)
            .OfType<object>()
            .Where(e => e == null));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateMediaUpload_WithValidMimeType_Succeeds()
    {
        // Arrange
        var mimeType = "image/jpeg";
        var fileSize = 1024 * 1024; // 1 MB

        // Act & Assert
        _mediaService.ValidateMediaUpload(mimeType, fileSize);
    }

    [Theory]
    [InlineData("image/jpeg", 1024)]
    [InlineData("image/png", 5242880)]
    [InlineData("video/mp4", 52428800)]
    [InlineData("application/pdf", 1024)]
    public void ValidateMediaUpload_WithAllowedTypes_Succeeds(string mimeType, long fileSize)
    {
        // Act & Assert
        _mediaService.ValidateMediaUpload(mimeType, fileSize);
    }

    [Fact]
    public void ValidateMediaUpload_WithDisallowedMimeType_Throws()
    {
        // Arrange
        var mimeType = "application/exe";
        var fileSize = 1024;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _mediaService.ValidateMediaUpload(mimeType, fileSize)
        );
        Assert.Contains("MIME type", ex.Message);
    }

    [Fact]
    public void ValidateMediaUpload_WithFileTooLarge_Throws()
    {
        // Arrange
        var mimeType = "image/jpeg";
        var fileSize = 101 * 1024 * 1024; // 101 MB (exceeds 100 MB limit)

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _mediaService.ValidateMediaUpload(mimeType, fileSize)
        );
        Assert.Contains("exceeds maximum", ex.Message);
    }

    #endregion

    #region Initiate Upload Tests

    [Fact]
    public async Task InitiateUploadAsync_WithValidInput_ReturnsPresignedUrl()
    {
        // Arrange
        var userId = GetTestUser().Id;
        var fileName = "test.jpg";
        var mimeType = "image/jpeg";
        var fileSize = 1024 * 1024;
        var bucketName = "test-bucket";

        _s3ServiceMock
            .Setup(s => s.GenerateStorageKey(userId, fileName))
            .Returns($"users/{userId}/test.jpg");

        _s3ServiceMock
            .Setup(s => s.GeneratePresignedUploadUrlAsync(bucketName, It.IsAny<string>(), 15))
            .ReturnsAsync("https://s3.example.com/presigned-url");

        // Act
        var result = await _mediaService.InitiateUploadAsync(userId, fileName, mimeType, fileSize, bucketName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://s3.example.com/presigned-url", result.Url);
        Assert.NotEmpty(result.StorageKey);
        Assert.Equal(15, result.ExpirationMinutes);
    }

    [Fact]
    public async Task InitiateUploadAsync_WithInvalidMimeType_Throws()
    {
        // Arrange
        var userId = GetTestUser().Id;
        var fileName = "test.exe";
        var mimeType = "application/exe";
        var fileSize = 1024;
        var bucketName = "test-bucket";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mediaService.InitiateUploadAsync(userId, fileName, mimeType, fileSize, bucketName)
        );
    }

    #endregion

    #region Finalize Upload Tests

    [Fact]
    public async Task FinalizeUploadAsync_WithValidInput_CreatesMediaRecord()
    {
        // Arrange
        var userId = GetTestUser().Id;
        var storageKey = $"users/{userId}/test-{Guid.NewGuid()}.jpg";
        var request = new CompleteUploadDto
        {
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024 * 1024,
            StorageKey = storageKey,
            EntryId = null
        };
        var bucketName = "test-bucket";

        // Mock ObjectExistsAsync - use ReturnsAsync instead of Setup
        _s3ServiceMock
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mediaService.FinalizeUploadAsync(userId, request, bucketName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.MimeType, result.MimeType);
        Assert.Equal(request.FileSize, result.Size);
        Assert.Equal(userId, _context.Media.First().UserId);
    }

    [Fact]
    public async Task FinalizeUploadAsync_WithMissingFile_Throws()
    {
        // Arrange
        var userId = GetTestUser().Id;
        var storageKey = $"users/{userId}/missing.jpg";
        var request = new CompleteUploadDto
        {
            FileName = "missing.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            StorageKey = storageKey
        };
        var bucketName = "test-bucket";

        _s3ServiceMock
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mediaService.FinalizeUploadAsync(userId, request, bucketName)
        );
        Assert.Contains("not found", ex.Message);
    }

    #endregion

    #region Get Media Tests

    [Fact]
    public async Task GetMediaAsync_WithValidId_ReturnsMedia()
    {
        // Arrange
        var user = GetTestUser();
        var media = new Media
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Url = "s3://bucket/key",
            MimeType = "image/jpeg",
            Size = 1024,
            StorageKey = "key",
            CreatedAt = DateTime.UtcNow
        };
        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mediaService.GetMediaAsync(media.Id, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(media.Id, result.Id);
        Assert.Equal(media.MimeType, result.MimeType);
    }

    [Fact]
    public async Task GetMediaAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var user = GetTestUser();
        var wrongUserId = Guid.NewGuid();
        var media = new Media
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Url = "s3://bucket/key",
            MimeType = "image/jpeg",
            Size = 1024,
            StorageKey = "key",
            CreatedAt = DateTime.UtcNow
        };
        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mediaService.GetMediaAsync(media.Id, wrongUserId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Delete Media Tests

    [Fact]
    public async Task DeleteMediaAsync_WithValidId_DeletesMedia()
    {
        // Arrange
        var user = GetTestUser();
        var media = new Media
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Url = "s3://bucket/key",
            MimeType = "image/jpeg",
            Size = 1024,
            StorageKey = "key",
            CreatedAt = DateTime.UtcNow
        };
        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        _s3ServiceMock
            .Setup(s => s.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mediaService.DeleteMediaAsync(media.Id, user.Id, "bucket");

        // Assert
        Assert.True(result);
        Assert.Empty(_context.Media);
    }

    [Fact]
    public async Task DeleteMediaAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var user = GetTestUser();
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _mediaService.DeleteMediaAsync(invalidId, user.Id, "bucket");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Associate With Entry Tests

    [Fact]
    public async Task AssociateWithEntryAsync_WithValidIds_AssociatesMedia()
    {
        // Arrange
        var user = GetTestUser();
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Test Entry",
            BodyText = "Test content",
            Confidentiality = "private",
            CreatedAt = DateTime.UtcNow
        };
        var media = new Media
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Url = "s3://bucket/key",
            MimeType = "image/jpeg",
            Size = 1024,
            StorageKey = "key",
            CreatedAt = DateTime.UtcNow
        };

        _context.Entries.Add(entry);
        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mediaService.AssociateWithEntryAsync(media.Id, entry.Id, user.Id);

        // Assert
        Assert.True(result);
        var updatedMedia = _context.Media.First(m => m.Id == media.Id);
        Assert.Equal(entry.Id, updatedMedia.EntryId);
    }

    [Fact]
    public async Task AssociateWithEntryAsync_WithInvalidEntryId_ReturnsFalse()
    {
        // Arrange
        var user = GetTestUser();
        var media = new Media
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Url = "s3://bucket/key",
            MimeType = "image/jpeg",
            Size = 1024,
            StorageKey = "key",
            CreatedAt = DateTime.UtcNow
        };
        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mediaService.AssociateWithEntryAsync(media.Id, Guid.NewGuid(), user.Id);

        // Assert
        Assert.False(result);
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}
