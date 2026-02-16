using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace JournalAI.Backend.Services;

/// <summary>
/// Service for S3-compatible blob storage (MinIO dev, AWS S3 prod)
/// Handles presigned URL generation, object operations, and deletions
/// </summary>
public class S3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3Service> logger)
    {
        _s3Client = s3Client;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates a presigned URL for downloading media from S3
    /// </summary>
    public async Task<string> GeneratePresignedDownloadUrlAsync(
        string bucketName,
        string key,
        int expirationMinutes = 60)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Verb = HttpVerb.GET
            };

            var url = _s3Client.GetPreSignedURL(request);
            _logger.LogInformation("Generated presigned download URL for {Key} with {Minutes} min expiration", key, expirationMinutes);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned download URL for {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Generates a presigned URL for uploading media to S3
    /// </summary>
    public async Task<string> GeneratePresignedUploadUrlAsync(
        string bucketName,
        string key,
        int expirationMinutes = 60)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Verb = HttpVerb.PUT
            };

            var url = _s3Client.GetPreSignedURL(request);
            _logger.LogInformation("Generated presigned upload URL for {Key} with {Minutes} min expiration", key, expirationMinutes);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned upload URL for {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Deletes an object from S3
    /// </summary>
    public async Task<bool> DeleteObjectAsync(string bucketName, string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("Deleted object {Key} from bucket {Bucket}", key, bucketName);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
                   response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object {Key} from bucket {Bucket}", key, bucketName);
            throw;
        }
    }

    /// <summary>
    /// Checks if an object exists in S3
    /// </summary>
    public async Task<bool> ObjectExistsAsync(string bucketName, string key)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Object {Key} does not exist in bucket {Bucket}", key, bucketName);
            return false;
        }
    }

    /// <summary>
    /// Generates a storage key (path) for media files
    /// Format: users/{userId}/{guid}/{filename}
    /// </summary>
    public string GenerateStorageKey(Guid userId, string originalFileName)
    {
        var fileExtension = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        return $"users/{userId}/{fileName}";
    }

    /// <summary>
    /// Downloads an object from S3 as a byte array
    /// </summary>
    public async Task<byte[]> DownloadObjectAsync(string bucketName, string key)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            using (var response = await _s3Client.GetObjectAsync(request))
            using (var memoryStream = new MemoryStream())
            {
                await response.ResponseStream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                _logger.LogInformation("Downloaded object {Key} from bucket {Bucket} ({Bytes} bytes)", key, bucketName, bytes.Length);
                return bytes;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download object {Key} from bucket {Bucket}", key, bucketName);
            throw;
        }
    }

    /// <summary>
    /// Uploads a byte array as an object to S3
    /// </summary>
    public async Task<bool> PutObjectAsync(string bucketName, string key, byte[] data, string contentType = "application/octet-stream")
    {
        try
        {
            using (var memoryStream = new MemoryStream(data))
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = memoryStream,
                    ContentType = contentType
                };

                var response = await _s3Client.PutObjectAsync(request);
                _logger.LogInformation("Uploaded object {Key} to bucket {Bucket} ({Bytes} bytes)", key, bucketName, data.Length);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object {Key} to bucket {Bucket}", key, bucketName);
            throw;
        }
    }
}
