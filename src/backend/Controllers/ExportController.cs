using System.Security.Claims;
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/v1/exports")]
[Authorize]
public class ExportController : ControllerBase
{
    private static readonly string[] ValidJobFormats = { "json", "markdown", "zip" };

    private readonly AppDbContext _context;
    private readonly IBackgroundJobClient _jobClient;
    private readonly S3Service _s3;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        AppDbContext context,
        IBackgroundJobClient jobClient,
        S3Service s3,
        IConfiguration configuration,
        ILogger<ExportController> logger)
    {
        _context = context;
        _jobClient = jobClient;
        _s3 = s3;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Export the current user's entries as a downloadable file.
    /// GET /api/v1/exports?format=json|markdown
    ///
    /// Synchronous, text-based export covering all of the caller's own entries
    /// (regardless of confidentiality — it is their own data). ZIP-with-media and
    /// background/large exports remain a planned enhancement on the export_jobs table.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ExportEntries([FromQuery] string format = "json")
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var normalized = (format ?? "json").Trim().ToLowerInvariant();
        if (normalized is not ("json" or "markdown" or "md"))
            return BadRequest(new { errors = new[] { "Unsupported format. Use 'json' or 'markdown'." } });

        var entries = await _context.Entries
            .Where(e => e.UserId == userGuid)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        if (normalized == "json")
        {
            var json = ExportContentBuilder.BuildJson(entries);
            _logger.LogInformation("Export (json) of {Count} entries by user {UserId}", entries.Count, userGuid);
            return File(Encoding.UTF8.GetBytes(json), "application/json", $"journal-export-{stamp}.json");
        }

        // markdown / md
        var markdown = ExportContentBuilder.BuildMarkdown(entries);
        _logger.LogInformation("Export (markdown) of {Count} entries by user {UserId}", entries.Count, userGuid);
        return File(Encoding.UTF8.GetBytes(markdown), "text/markdown", $"journal-export-{stamp}.md");
    }

    /// <summary>
    /// Start an asynchronous export job.
    /// POST /api/v1/exports/jobs  { "format": "zip" | "json" | "markdown" }
    ///
    /// Returns 202 with the queued job; a background worker builds the artifact (a ZIP
    /// bundles entries plus media) and stores it in the blob store. Poll
    /// <c>GET /api/v1/exports/jobs/{id}</c> and download from
    /// <c>GET /api/v1/exports/jobs/{id}/download</c> once <c>ready</c>.
    /// </summary>
    [HttpPost("jobs")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateJob([FromBody] CreateExportJobDto request)
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var format = (request?.Format ?? "zip").Trim().ToLowerInvariant();
        if (!ValidJobFormats.Contains(format))
            return BadRequest(new { errors = new[] { "Unsupported format. Use 'json', 'markdown', or 'zip'." } });

        var job = new ExportJob
        {
            Id = Guid.NewGuid(),
            UserId = userGuid,
            Format = format,
            Status = "queued",
            CreatedAt = DateTime.UtcNow
        };
        _context.ExportJobs.Add(job);
        await _context.SaveChangesAsync();

        _jobClient.Enqueue<ExportGenerationJob>(j => j.GenerateAsync(job.Id));
        _logger.LogInformation("Export job {JobId} ({Format}) queued by user {UserId}", job.Id, format, userGuid);

        return AcceptedAtAction(nameof(GetJob), new { id = job.Id }, ToDto(job));
    }

    /// <summary>List the caller's export jobs, most recent first.</summary>
    [HttpGet("jobs")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ListJobs()
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var jobs = await _context.ExportJobs
            .Where(j => j.UserId == userGuid)
            .OrderByDescending(j => j.CreatedAt)
            .Take(50)
            .ToListAsync();

        return Ok(jobs.Select(ToDto).ToList());
    }

    /// <summary>Get a single export job's status.</summary>
    [HttpGet("jobs/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetJob(Guid id)
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var job = await _context.ExportJobs.FirstOrDefaultAsync(j => j.Id == id && j.UserId == userGuid);
        if (job == null)
            return NotFound();

        return Ok(ToDto(job));
    }

    /// <summary>
    /// Stream the completed export artifact. The bytes are served through this
    /// authenticated endpoint (scoped to the owner) rather than via a public presigned URL.
    /// </summary>
    [HttpGet("jobs/{id}/download")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> DownloadJob(Guid id)
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var job = await _context.ExportJobs.FirstOrDefaultAsync(j => j.Id == id && j.UserId == userGuid);
        if (job == null)
            return NotFound();

        if (job.Status != "completed" || string.IsNullOrEmpty(job.DownloadUrl))
            return Conflict(new { errors = new[] { $"Export is not ready (status: {job.Status})." } });

        var bucket = _configuration["S3:BucketName"] ?? "journal-ai";
        var bytes = await _s3.DownloadObjectAsync(bucket, job.DownloadUrl);

        var (contentType, fileName) = job.Format switch
        {
            "json" => ("application/json", $"journal-export-{id}.json"),
            "markdown" => ("text/markdown", $"journal-export-{id}.md"),
            _ => ("application/zip", $"journal-export-{id}.zip"),
        };
        return File(bytes, contentType, fileName);
    }

    private bool TryGetUserId(out Guid userGuid)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userId, out userGuid);
    }

    private static ExportJobDto ToDto(ExportJob job) => new()
    {
        Id = job.Id,
        Format = job.Format,
        Status = job.Status,
        Ready = job.Status == "completed" && !string.IsNullOrEmpty(job.DownloadUrl),
        Error = job.ErrorMessage,
        CreatedAt = job.CreatedAt,
        CompletedAt = job.CompletedAt
    };
}
