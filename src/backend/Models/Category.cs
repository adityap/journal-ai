using System.ComponentModel.DataAnnotations;

namespace JournalAI.Models;

public class Category
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public Guid? ParentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
