using JournalAI.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace JournalAI.Backend.Services;

/// <summary>
/// Background job for generating image thumbnails
/// Triggered asynchronously after media upload completion
/// Supports retry logic with exponential backoff via Hangfire
/// </summary>
public class ThumbnailGenerationJob
{
    private readonly AppDbContext _context;
    private readonly S3Service _s3Service;
    private readonly MediaService _mediaService;
    private readonly ILogger<ThumbnailGenerationJob> _logger;
    private const int ThumbnailWidth = 200;
    private const int ThumbnailHeight = 200;
    private const int ThumbnailQuality = 75;

    public ThumbnailGenerationJob(
        AppDbContext context,
        S3Service s3Service,
        MediaService mediaService,
        ILogger<ThumbnailGenerationJob> logger)
    {
        _context = context;
        _s3Service = s3Service;
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a thumbnail for an image media file
    /// Called as a Hangfire background job with automatic retry
    /// </summary>
    public async Task GenerateThumbnailAsync(Guid mediaId, string bucketName)
    {
        try
        {
            // Retrieve media from database
            var media = await _context.Media.FirstOrDefaultAsync(m => m.Id == mediaId);
            if (media == null)
            {
                _logger.LogWarning("Media {MediaId} not found for thumbnail generation", mediaId);
                return;
            }

            // Only process image types
            if (!IsImageMimeType(media.MimeType))
            {
                _logger.LogInformation("Skipping thumbnail for non-image media {MediaId} ({MimeType})", mediaId, media.MimeType);
                return;
            }

            // Download original image from S3
            var imageData = await DownloadImageFromS3Async(bucketName, media.StorageKey);
            if (imageData == null || imageData.Length == 0)
            {
                _logger.LogError("Failed to download image for media {MediaId}", mediaId);
                return;
            }

            // Generate thumbnail
            var thumbnailData = GenerateThumbnail(imageData, media.MimeType);
            if (thumbnailData == null || thumbnailData.Length == 0)
            {
                _logger.LogError("Failed to generate thumbnail for media {MediaId}", mediaId);
                return;
            }

            // Upload thumbnail to S3
            var thumbnailKey = GenerateThumbnailKey(media.StorageKey);
            var uploaded = await UploadThumbnailToS3Async(bucketName, thumbnailKey, thumbnailData);

            if (!uploaded)
            {
                _logger.LogError("Failed to upload thumbnail for media {MediaId}", mediaId);
                return;
            }

            // Update Media record with thumbnail URL
            var thumbnailUrl = $"s3://{bucketName}/{thumbnailKey}";
            var updated = await _mediaService.UpdateThumbnailAsync(mediaId, thumbnailUrl, media.UserId);

            if (updated)
            {
                _logger.LogInformation("Successfully generated thumbnail for media {MediaId}", mediaId);
            }
            else
            {
                _logger.LogError("Failed to update Media record with thumbnail URL for {MediaId}", mediaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during thumbnail generation for media {MediaId}", mediaId);
            throw; // Hangfire will retry on exception
        }
    }

    /// <summary>
    /// Downloads image from S3
    /// </summary>
    private async Task<byte[]?> DownloadImageFromS3Async(string bucketName, string key)
    {
        try
        {
            var imageData = await _s3Service.DownloadObjectAsync(bucketName, key);
            _logger.LogInformation("Downloaded image from S3: {Bucket}/{Key} ({Size} bytes)", bucketName, key, imageData.Length);
            return imageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download image from S3: {Bucket}/{Key}", bucketName, key);
            return null;
        }
    }

    /// <summary>
    /// Generates thumbnail from image data
    /// </summary>
    private byte[]? GenerateThumbnail(byte[] imageData, string mimeType)
    {
        try
        {
            using var image = Image.Load(imageData);
            
            // Calculate aspect ratio and resize to fit in ThumbnailWidth x ThumbnailHeight
            var aspectRatio = (double)image.Width / image.Height;
            int newWidth, newHeight;

            if (aspectRatio > 1)
            {
                // Wider than tall
                newWidth = ThumbnailWidth;
                newHeight = (int)(ThumbnailWidth / aspectRatio);
            }
            else
            {
                // Taller than wide
                newHeight = ThumbnailHeight;
                newWidth = (int)(ThumbnailHeight * aspectRatio);
            }

            image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));

            // Save to memory stream
            using var ms = new MemoryStream();
            
            // Use original format or default to JPEG
            var format = GetImageFormat(mimeType);
            if (format != null)
            {
                image.Save(ms, format);
            }
            else
            {
                // Default to JPEG
                image.SaveAsJpeg(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = ThumbnailQuality });
            }

            _logger.LogInformation("Generated thumbnail: {OriginalSize} -> {ThumbnailSize}", 
                imageData.Length, ms.Length);
            
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail from image data");
            return null;
        }
    }

    /// <summary>
    /// Uploads thumbnail to S3
    /// </summary>
    private async Task<bool> UploadThumbnailToS3Async(string bucketName, string key, byte[] thumbnailData)
    {
        try
        {
            var uploaded = await _s3Service.PutObjectAsync(bucketName, key, thumbnailData, "image/jpeg");
            if (uploaded)
            {
                _logger.LogInformation("Uploaded thumbnail to S3: {Bucket}/{Key} ({Size} bytes)", 
                    bucketName, key, thumbnailData.Length);
            }
            return uploaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload thumbnail to S3: {Bucket}/{Key}", bucketName, key);
            return false;
        }
    }

    /// <summary>
    /// Generates thumbnail S3 key from original key
    /// </summary>
    private string GenerateThumbnailKey(string originalKey)
    {
        // Insert '_thumb' before file extension
        // E.g., users/userid/uuid.jpg -> users/userid/uuid_thumb.jpg
        var lastDot = originalKey.LastIndexOf('.');
        if (lastDot > 0)
        {
            return originalKey.Insert(lastDot, "_thumb");
        }
        return originalKey + "_thumb";
    }

    /// <summary>
    /// Checks if MIME type is an image
    /// </summary>
    private bool IsImageMimeType(string mimeType)
    {
        var imageMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        return imageMimeTypes.Contains(mimeType);
    }

    /// <summary>
    /// Gets ImageSharp format from MIME type
    /// </summary>
    private SixLabors.ImageSharp.Formats.IImageFormat? GetImageFormat(string mimeType)
    {
        return mimeType switch
        {
            "image/jpeg" => SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance,
            "image/png" => SixLabors.ImageSharp.Formats.Png.PngFormat.Instance,
            "image/gif" => SixLabors.ImageSharp.Formats.Gif.GifFormat.Instance,
            // WebP format requires separate NuGet package, fall back to JPEG
            "image/webp" => null,
            _ => null
        };
    }
}
