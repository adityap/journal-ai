using System.IO.Compression;
using System.Text;
using JournalAI.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Services;

/// <summary>
/// Background job that produces an export artifact for an <c>ExportJob</c> and stores it
/// in the blob store. Text formats (json/markdown) yield a single file; <c>zip</c> bundles
/// <c>entries.json</c>, <c>entries.md</c>, and the user's media fetched from S3. The job
/// row is the source of truth for status — it is marked completed/failed here rather than
/// relying on Hangfire's own retry bookkeeping.
/// </summary>
public class ExportGenerationJob
{
    private readonly AppDbContext _context;
    private readonly S3Service _s3;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExportGenerationJob> _logger;

    public ExportGenerationJob(AppDbContext context, S3Service s3, IConfiguration configuration, ILogger<ExportGenerationJob> logger)
    {
        _context = context;
        _s3 = s3;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task GenerateAsync(Guid jobId)
    {
        var job = await _context.ExportJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null)
        {
            _logger.LogWarning("Export job {JobId} not found", jobId);
            return;
        }

        try
        {
            job.Status = "running";
            await _context.SaveChangesAsync();

            var entries = await _context.Entries
                .Where(e => e.UserId == job.UserId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var bucket = _configuration["S3:BucketName"] ?? "journal-ai";
            byte[] artifact;
            string contentType;
            string extension;

            switch (job.Format)
            {
                case "json":
                    artifact = Encoding.UTF8.GetBytes(ExportContentBuilder.BuildJson(entries));
                    contentType = "application/json";
                    extension = "json";
                    break;
                case "markdown":
                    artifact = Encoding.UTF8.GetBytes(ExportContentBuilder.BuildMarkdown(entries));
                    contentType = "text/markdown";
                    extension = "md";
                    break;
                default: // zip
                    artifact = await BuildZipAsync(job.UserId, entries, bucket);
                    contentType = "application/zip";
                    extension = "zip";
                    break;
            }

            // Store the artifact in the blob store; the download endpoint streams it back
            // (we keep the object key in DownloadUrl rather than exposing a public URL).
            var key = $"exports/{job.UserId}/{job.Id}.{extension}";
            await _s3.PutObjectAsync(bucket, key, artifact, contentType);

            job.DownloadUrl = key;
            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Export job {JobId} completed: {Format}, {Bytes} bytes", job.Id, job.Format, artifact.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export job {JobId} failed", jobId);
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            // Don't rethrow: the job row already records the failure, and we don't want
            // Hangfire to retry and flip the status back to running.
        }
    }

    /// <summary>
    /// Build a ZIP containing entries.json, entries.md, and a media/ folder. Media that
    /// can't be fetched is skipped (logged) so one missing object doesn't fail the export.
    /// </summary>
    private async Task<byte[]> BuildZipAsync(Guid userId, IReadOnlyList<Models.Entry> entries, string bucket)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteTextEntryAsync(zip, "entries.json", ExportContentBuilder.BuildJson(entries));
            await WriteTextEntryAsync(zip, "entries.md", ExportContentBuilder.BuildMarkdown(entries));

            var media = await _context.Media.Where(m => m.UserId == userId).ToListAsync();
            foreach (var m in media)
            {
                try
                {
                    var bytes = await _s3.DownloadObjectAsync(bucket, m.StorageKey);
                    var ext = Path.GetExtension(m.StorageKey);
                    var name = $"media/{m.EntryId?.ToString() ?? "unassigned"}/{m.Id}{ext}";
                    var zipEntry = zip.CreateEntry(name, CompressionLevel.Optimal);
                    await using var entryStream = zipEntry.Open();
                    await entryStream.WriteAsync(bytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping media {MediaId} in export for user {UserId}", m.Id, userId);
                }
            }
        }
        return ms.ToArray();
    }

    private static async Task WriteTextEntryAsync(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        await stream.WriteAsync(bytes);
    }
}
