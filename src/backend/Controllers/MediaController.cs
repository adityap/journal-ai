using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Services;
using System.Security.Claims;

namespace JournalAI.Backend.Controllers;

/// <summary>
/// Controller for managing media uploads, storage, and deletions
/// Implements secure presigned URL workflows for client-side uploads
/// </summary>
[ApiController]
[Route("api/v1/media")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly MediaService _mediaService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        MediaService mediaService,
        IConfiguration configuration,
        ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a media upload by generating a presigned URL
    /// Client uses this URL to upload file directly to S3/MinIO
    /// </summary>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(PresignedUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InitiateUpload([FromBody] InitiateUploadDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest(new { error = "FileName is required" });
        }

        if (string.IsNullOrWhiteSpace(request.MimeType))
        {
            return BadRequest(new { error = "MimeType is required" });
        }

        if (request.FileSize <= 0)
        {
            return BadRequest(new { error = "FileSize must be greater than 0" });
        }

        try
        {
            var userId = GetUserId();
            var bucketName = _configuration["S3:BucketName"] ?? "journal-ai";

            var presignedUrl = await _mediaService.InitiateUploadAsync(
                userId,
                request.FileName,
                request.MimeType,
                request.FileSize,
                bucketName);

            _logger.LogInformation("User {UserId} initiated upload for {FileName}", userId, request.FileName);
            return Ok(presignedUrl);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Upload validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate upload");
            return StatusCode(500, new { error = "Failed to initiate upload" });
        }
    }

    /// <summary>
    /// Completes a media upload after client finishes uploading to S3/MinIO
    /// Creates Media record and triggers thumbnail generation
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(MediaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteUpload([FromBody] CompleteUploadDto request)
    {
        if (string.IsNullOrWhiteSpace(request.StorageKey))
        {
            return BadRequest(new { error = "StorageKey is required" });
        }

        if (request.FileSize <= 0)
        {
            return BadRequest(new { error = "FileSize must be greater than 0" });
        }

        try
        {
            var userId = GetUserId();
            var bucketName = _configuration["S3:BucketName"] ?? "journal-ai";

            var media = await _mediaService.FinalizeUploadAsync(userId, request, bucketName);

            _logger.LogInformation("User {UserId} completed upload: {MediaId}", userId, media.Id);
            return CreatedAtAction(nameof(GetMedia), new { id = media.Id }, media);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Upload completion validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete upload");
            return StatusCode(500, new { error = "Failed to complete upload" });
        }
    }

    /// <summary>
    /// Retrieves media by ID (ownership verified)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MediaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedia(Guid id)
    {
        var userId = GetUserId();
        var media = await _mediaService.GetMediaAsync(id, userId);

        if (media == null)
        {
            return NotFound(new { error = "Media not found" });
        }

        return Ok(media);
    }

    /// <summary>
    /// Lists all media for authenticated user
    /// Optionally filters by entry ID
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MediaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMedia([FromQuery] Guid? entryId = null)
    {
        var userId = GetUserId();
        var mediaList = await _mediaService.GetUserMediaAsync(userId, entryId);
        return Ok(mediaList);
    }

    /// <summary>
    /// Deletes media and removes from S3
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedia(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var bucketName = _configuration["S3:BucketName"] ?? "journal-ai";

            var deleted = await _mediaService.DeleteMediaAsync(id, userId, bucketName);

            if (!deleted)
            {
                return NotFound(new { error = "Media not found" });
            }

            _logger.LogInformation("User {UserId} deleted media {MediaId}", userId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media {MediaId}", id);
            return StatusCode(500, new { error = "Failed to delete media" });
        }
    }

    /// <summary>
    /// Associates media with an entry
    /// </summary>
    [HttpPost("{mediaId}/associate-entry/{entryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssociateWithEntry(Guid mediaId, Guid entryId)
    {
        try
        {
            var userId = GetUserId();
            var associated = await _mediaService.AssociateWithEntryAsync(mediaId, entryId, userId);

            if (!associated)
            {
                return NotFound(new { error = "Media or entry not found" });
            }

            _logger.LogInformation("User {UserId} associated media {MediaId} with entry {EntryId}", userId, mediaId, entryId);
            return Ok(new { message = "Media associated with entry" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to associate media with entry");
            return StatusCode(500, new { error = "Failed to associate media with entry" });
        }
    }

    /// <summary>
    /// Helper: Extract user ID from JWT claims
    /// </summary>
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
