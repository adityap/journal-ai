using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Models;

public class Entry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsPrivate { get; set; } = false;
}
