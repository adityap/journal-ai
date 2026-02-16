using JournalAI.Backend.Data;
using JournalAI.Backend.Models;
using JournalAI.Backend.Data.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Services;

/// <summary>
/// Service for managing media metadata and lifecycle
/// Coordinates with S3Service for storage operations
/// </summary>
public class MediaService
{
    private readonly AppDbContext _context;
    private readonly S3Service _s3Service;
    private readonly JobSchedulerService _jobScheduler;
    private readonly ILogger<MediaService> _logger;
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "video/mp4",
        "video/webm",
        "application/pdf"
    };

    public MediaService(
        AppDbContext context,
        S3Service s3Service,
        JobSchedulerService jobScheduler,
        ILogger<MediaService> logger)
    {
        _context = context;
        _s3Service = s3Service;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    /// <summary>
    /// Validates file before upload (MIME type, size)
    /// </summary>
    public virtual void ValidateMediaUpload(string mimeType, long fileSize)
    {
        if (!AllowedMimeTypes.Contains(mimeType))
        {
            throw new InvalidOperationException($"MIME type '{mimeType}' is not allowed. Allowed types: {string.Join(", ", AllowedMimeTypes)}");
        }

        if (fileSize > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size {fileSize} bytes exceeds maximum of {MaxFileSizeBytes} bytes");
        }
    }

    /// <summary>
    /// Creates a presigned URL for upload
    /// </summary>
    public virtual async Task<PresignedUrlDto> InitiateUploadAsync(
        Guid userId,
        string originalFileName,
        string mimeType,
        long fileSize,
        string bucketName)
    {
        // Validate before generating presigned URL
        ValidateMediaUpload(mimeType, fileSize);

        // Generate storage key
        var storageKey = _s3Service.GenerateStorageKey(userId, originalFileName);

        // Generate presigned upload URL (15 min expiration)
        var presignedUrl = await _s3Service.GeneratePresignedUploadUrlAsync(bucketName, storageKey, 15);

        _logger.LogInformation("Initiated upload for user {UserId} with storage key {StorageKey}", userId, storageKey);

        return new PresignedUrlDto
        {
            Url = presignedUrl,
            StorageKey = storageKey,
            ExpirationMinutes = 15
        };
    }

    /// <summary>
    /// Creates a Media record after successful S3 upload
    /// </summary>
    public virtual async Task<MediaResponseDto> FinalizeUploadAsync(
        Guid userId,
        CompleteUploadDto dto,
        string bucketName)
    {
        // Validate media
        ValidateMediaUpload(dto.MimeType, dto.FileSize);

        // Verify file exists in S3
        var exists = await _s3Service.ObjectExistsAsync(bucketName, dto.StorageKey);
        if (!exists)
        {
            throw new InvalidOperationException($"File not found in storage at key '{dto.StorageKey}'");
        }

        // Create Media record
        var media = new Media
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntryId = dto.EntryId,
            Url = $"s3://{bucketName}/{dto.StorageKey}", // Store S3 URI
            MimeType = dto.MimeType,
            Size = dto.FileSize,
            StorageKey = dto.StorageKey,
            CreatedAt = DateTime.UtcNow
        };

        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Finalized upload for user {UserId}: media {MediaId}", userId, media.Id);

        // Schedule thumbnail generation for images
        if (IsImageType(dto.MimeType))
        {
            try
            {
                _jobScheduler.ScheduleThumbnailGeneration(media.Id, bucketName);
                _logger.LogInformation("Scheduled thumbnail generation for media {MediaId}", media.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule thumbnail generation for media {MediaId}", media.Id);
                // Don't fail the upload if thumbnail scheduling fails
            }
        }

        return MapToResponseDto(media);
    }

    /// <summary>
    /// Retrieves media by ID (with ownership verification)
    /// </summary>
    public virtual async Task<MediaResponseDto?> GetMediaAsync(Guid mediaId, Guid userId)
    {
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == mediaId && m.UserId == userId);

        if (media == null)
        {
            _logger.LogWarning("Media {MediaId} not found or not owned by user {UserId}", mediaId, userId);
            return null;
        }

        return MapToResponseDto(media);
    }

    /// <summary>
    /// Retrieves all media for a user, optionally filtered by entry
    /// </summary>
    public virtual async Task<List<MediaResponseDto>> GetUserMediaAsync(Guid userId, Guid? entryId = null)
    {
        var query = _context.Media.Where(m => m.UserId == userId);

        if (entryId.HasValue)
        {
            query = query.Where(m => m.EntryId == entryId);
        }

        var mediaList = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
        return mediaList.Select(MapToResponseDto).ToList();
    }

    /// <summary>
    /// Deletes media and removes from S3
    /// </summary>
    public virtual async Task<bool> DeleteMediaAsync(Guid mediaId, Guid userId, string bucketName)
    {
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == mediaId && m.UserId == userId);

        if (media == null)
        {
            _logger.LogWarning("Media {MediaId} not found or not owned by user {UserId}", mediaId, userId);
            return false;
        }

        // Delete from S3
        var deleted = await _s3Service.DeleteObjectAsync(bucketName, media.StorageKey);

        if (!deleted)
        {
            _logger.LogWarning("Failed to delete media from S3: {StorageKey}", media.StorageKey);
            // Continue with database deletion even if S3 deletion fails
        }

        // Delete thumbnail if exists
        if (!string.IsNullOrEmpty(media.ThumbnailUrl))
        {
            try
            {
                var thumbnailKey = ExtractStorageKeyFromUrl(media.ThumbnailUrl);
                if (thumbnailKey != null)
                {
                    await _s3Service.DeleteObjectAsync(bucketName, thumbnailKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete thumbnail for media {MediaId}", mediaId);
            }
        }

        // Delete from database
        _context.Media.Remove(media);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted media {MediaId} for user {UserId}", mediaId, userId);
        return true;
    }

    /// <summary>
    /// Associates media with an entry
    /// </summary>
    public virtual async Task<bool> AssociateWithEntryAsync(Guid mediaId, Guid entryId, Guid userId)
    {
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == mediaId && m.UserId == userId);

        if (media == null)
        {
            return false;
        }

        // Verify entry exists and belongs to user
        var entry = await _context.Entries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

        if (entry == null)
        {
            return false;
        }

        media.EntryId = entryId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Associated media {MediaId} with entry {EntryId}", mediaId, entryId);
        return true;
    }

    /// <summary>
    /// Updates thumbnail URL after generation
    /// </summary>
    public virtual async Task<bool> UpdateThumbnailAsync(Guid mediaId, string thumbnailUrl, Guid userId)
    {
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == mediaId && m.UserId == userId);

        if (media == null)
        {
            return false;
        }

        media.ThumbnailUrl = thumbnailUrl;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated thumbnail for media {MediaId}", mediaId);
        return true;
    }

    /// <summary>
    /// Helper: Checks if MIME type is an image (for thumbnail generation)
    /// </summary>
    private bool IsImageType(string mimeType)
    {
        var imageTypes = new[] 
        { 
            "image/jpeg", 
            "image/png", 
            "image/gif", 
            "image/webp" 
        };
        return imageTypes.Contains(mimeType.ToLower());
    }

    /// <summary>
    /// Helper: Maps Media entity to DTO
    /// </summary>
    private MediaResponseDto MapToResponseDto(Media media)
    {
        return new MediaResponseDto
        {
            Id = media.Id,
            Url = media.Url,
            MimeType = media.MimeType,
            Size = media.Size,
            ThumbnailUrl = media.ThumbnailUrl,
            EntryId = media.EntryId,
            CreatedAt = media.CreatedAt
        };
    }

    /// <summary>
    /// Helper: Extracts storage key from S3 URL
    /// </summary>
    private string? ExtractStorageKeyFromUrl(string url)
    {
        if (url.StartsWith("s3://"))
        {
            // Format: s3://bucket/key
            var parts = url.Substring(5).Split('/', 2);
            return parts.Length > 1 ? parts[1] : null;
        }
        return null;
    }
}
