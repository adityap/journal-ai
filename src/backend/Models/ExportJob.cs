using System.ComponentModel.DataAnnotations;

namespace JournalAI.Models;

public class ExportJob
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Scope { get; set; } = null!; // JSON description
    public string Format { get; set; } = "json"; // json|md|zip
    public string Status { get; set; } = "queued"; // queued|running|completed|failed
    public string? DownloadUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
