using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using JournalAI.Backend.Controllers;
using JournalAI.Backend.Services;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using System.Security.Claims;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Integration tests for MediaController
/// Tests media upload, download, and deletion endpoints
/// </summary>
public class MediaControllerTests : IDisposable
{
    private readonly Mock<MediaService> _mediaServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<MediaController>> _loggerMock;
    private readonly MediaController _controller;

    public MediaControllerTests()
    {
        // Setup mocks
        _mediaServiceMock = new Mock<MediaService>(MockBehavior.Loose,
            It.IsAny<AppDbContext>(),
            It.IsAny<S3Service>(),
            It.IsAny<ILogger<MediaService>>()
        );
        _configurationMock = new Mock<IConfiguration>(MockBehavior.Loose);
        _loggerMock = new Mock<ILogger<MediaController>>(MockBehavior.Loose);

        _controller = new MediaController(_mediaServiceMock.Object, _configurationMock.Object, _loggerMock.Object);

        // Setup controller context with user claims
        SetupControllerWithUser();
    }

    #region Test Helpers

    private void SetupControllerWithUser()
    {
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private Guid GetControllerUserId()
    {
        var claim = _controller.User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }

    #endregion

    #region Initiate Upload Tests

    [Fact]
    public async Task InitiateUpload_WithValidInput_ReturnsOkWithPresignedUrl()
    {
        // Arrange
        var userId = GetControllerUserId();
        var request = new InitiateUploadDto
        {
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024 * 1024
        };
        var presignedUrl = new PresignedUrlDto
        {
            Url = "https://s3.example.com/presigned",
            StorageKey = $"users/{userId}/test.jpg",
            ExpirationMinutes = 15
        };

        _configurationMock
            .Setup(c => c["S3:BucketName"])
            .Returns("test-bucket");

        _mediaServiceMock
            .Setup(s => s.InitiateUploadAsync(userId, request.FileName, request.MimeType, request.FileSize, "test-bucket"))
            .ReturnsAsync(presignedUrl);

        // Act
        var result = await _controller.InitiateUpload(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedUrl = Assert.IsType<PresignedUrlDto>(okResult.Value);
        Assert.Equal(presignedUrl.Url, returnedUrl.Url);
    }

    [Fact]
    public async Task InitiateUpload_WithMissingFileName_ReturnsBadRequest()
    {
        // Arrange
        var request = new InitiateUploadDto
        {
            FileName = "",
            MimeType = "image/jpeg",
            FileSize = 1024
        };

        // Act
        var result = await _controller.InitiateUpload(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task InitiateUpload_WithInvalidMimeType_ReturnsBadRequest()
    {
        // Arrange
        var userId = GetControllerUserId();
        var request = new InitiateUploadDto
        {
            FileName = "test.exe",
            MimeType = "application/exe",
            FileSize = 1024
        };

        _configurationMock
            .Setup(c => c["S3:BucketName"])
            .Returns("test-bucket");

        _mediaServiceMock
            .Setup(s => s.InitiateUploadAsync(userId, request.FileName, request.MimeType, request.FileSize, "test-bucket"))
            .ThrowsAsync(new InvalidOperationException("MIME type 'application/exe' is not allowed"));

        // Act
        var result = await _controller.InitiateUpload(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    #endregion

    #region Complete Upload Tests

    [Fact]
    public async Task CompleteUpload_WithValidInput_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaId = Guid.NewGuid();
        var request = new CompleteUploadDto
        {
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024 * 1024,
            StorageKey = $"users/{userId}/test.jpg"
        };
        var mediaResponse = new MediaResponseDto
        {
            Id = mediaId,
            Url = "s3://bucket/key",
            MimeType = "image/jpeg",
            Size = 1024 * 1024,
            CreatedAt = DateTime.UtcNow
        };

        _configurationMock
            .Setup(c => c["S3:BucketName"])
            .Returns("test-bucket");

        _mediaServiceMock
            .Setup(s => s.FinalizeUploadAsync(userId, request, "test-bucket"))
            .ReturnsAsync(mediaResponse);

        // Act
        var result = await _controller.CompleteUpload(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(MediaController.GetMedia), createdResult.ActionName);
        var returnedMedia = Assert.IsType<MediaResponseDto>(createdResult.Value);
        Assert.Equal(mediaId, returnedMedia.Id);
    }

    [Fact]
    public async Task CompleteUpload_WithMissingStorageKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new CompleteUploadDto
        {
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            StorageKey = ""
        };

        // Act
        var result = await _controller.CompleteUpload(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    #endregion

    #region Get Media Tests

    [Fact]
    public async Task GetMedia_WithValidId_ReturnsOkWithMedia()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaId = Guid.NewGuid();
        var mediaResponse = new MediaResponseDto
        {
            Id = mediaId,
            Url = "s3://bucket/key",
            MimeType = "image/jpeg",
            Size = 1024,
            CreatedAt = DateTime.UtcNow
        };

        _mediaServiceMock
            .Setup(s => s.GetMediaAsync(mediaId, userId))
            .ReturnsAsync(mediaResponse);

        // Act
        var result = await _controller.GetMedia(mediaId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedMedia = Assert.IsType<MediaResponseDto>(okResult.Value);
        Assert.Equal(mediaId, returnedMedia.Id);
    }

    [Fact]
    public async Task GetMedia_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaId = Guid.NewGuid();

        _mediaServiceMock
            .Setup(s => s.GetMediaAsync(mediaId, userId))
            .ReturnsAsync((MediaResponseDto?)null);

        // Act
        var result = await _controller.GetMedia(mediaId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    #endregion

    #region List Media Tests

    [Fact]
    public async Task ListMedia_WithValidUser_ReturnsOkWithMediaList()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaList = new List<MediaResponseDto>
        {
            new MediaResponseDto { Id = Guid.NewGuid(), MimeType = "image/jpeg", Size = 1024 },
            new MediaResponseDto { Id = Guid.NewGuid(), MimeType = "image/png", Size = 2048 }
        };

        _mediaServiceMock
            .Setup(s => s.GetUserMediaAsync(userId, null))
            .ReturnsAsync(mediaList);

        // Act
        var result = await _controller.ListMedia(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedList = Assert.IsType<List<MediaResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedList.Count);
    }

    [Fact]
    public async Task ListMedia_FilteredByEntry_ReturnsOkWithFilteredList()
    {
        // Arrange
        var userId = GetControllerUserId();
        var entryId = Guid.NewGuid();
        var mediaList = new List<MediaResponseDto>
        {
            new MediaResponseDto { Id = Guid.NewGuid(), EntryId = entryId, MimeType = "image/jpeg", Size = 1024 }
        };

        _mediaServiceMock
            .Setup(s => s.GetUserMediaAsync(userId, entryId))
            .ReturnsAsync(mediaList);

        // Act
        var result = await _controller.ListMedia(entryId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedList = Assert.IsType<List<MediaResponseDto>>(okResult.Value);
        Assert.Single(returnedList);
        Assert.Equal(entryId, returnedList[0].EntryId);
    }

    #endregion

    #region Delete Media Tests

    [Fact]
    public async Task DeleteMedia_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaId = Guid.NewGuid();

        _configurationMock
            .Setup(c => c["S3:BucketName"])
            .Returns("test-bucket");

        _mediaServiceMock
            .Setup(s => s.DeleteMediaAsync(mediaId, userId, "test-bucket"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteMedia(mediaId);

        // Assert
        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContent.StatusCode);
    }

    [Fact]
    public async Task DeleteMedia_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaId = Guid.NewGuid();

        _configurationMock
            .Setup(c => c["S3:BucketName"])
            .Returns("test-bucket");

        _mediaServiceMock
            .Setup(s => s.DeleteMediaAsync(mediaId, userId, "test-bucket"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteMedia(mediaId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    #endregion

    #region Associate With Entry Tests

    [Fact]
    public async Task AssociateWithEntry_WithValidIds_ReturnsOk()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        _mediaServiceMock
            .Setup(s => s.AssociateWithEntryAsync(mediaId, entryId, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AssociateWithEntry(mediaId, entryId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task AssociateWithEntry_WithInvalidIds_ReturnsNotFound()
    {
        // Arrange
        var userId = GetControllerUserId();
        var mediaId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        _mediaServiceMock
            .Setup(s => s.AssociateWithEntryAsync(mediaId, entryId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AssociateWithEntry(mediaId, entryId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    #endregion

    public void Dispose()
    {
        // No resources to clean up for mock-based tests
    }
}
