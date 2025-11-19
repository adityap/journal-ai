using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Models;

public class ExportJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "pending";
}
