using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Models;

public class Media
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}
