using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Data.Dtos;

/// <summary>
/// Request to start an asynchronous export. <c>zip</c> bundles entries plus their media;
/// <c>json</c>/<c>markdown</c> produce a single text file (handy for large journals where
/// the synchronous download would be slow).
/// </summary>
public class CreateExportJobDto
{
    [StringLength(16)]
    public string Format { get; set; } = "zip"; // json|markdown|zip
}

/// <summary>
/// Status of an export job. <see cref="Ready"/> is true once the artifact can be
/// downloaded from <c>GET /api/v1/exports/jobs/{id}/download</c>.
/// </summary>
public class ExportJobDto
{
    public Guid Id { get; set; }

    public string Format { get; set; } = "zip";

    public string Status { get; set; } = "queued"; // queued|running|completed|failed

    public bool Ready { get; set; }

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
