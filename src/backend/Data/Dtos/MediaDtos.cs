namespace JournalAI.Backend.Data.Dtos;

/// <summary>
/// DTO for initiating a media upload (multipart or presigned URL request)
/// </summary>
public class InitiateUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid? EntryId { get; set; } // Optional: associate with entry during upload
}

/// <summary>
/// DTO for completing a media upload
/// </summary>
public class CompleteUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageKey { get; set; } = string.Empty; // S3 key where file was stored
    public Guid? EntryId { get; set; } // Optional: associate with entry
}

/// <summary>
/// DTO for returning media metadata to clients
/// </summary>
public class MediaResponseDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid? EntryId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for presigned URL response
/// </summary>
public class PresignedUrlDto
{
    public string Url { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
}
