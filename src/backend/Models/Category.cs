using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Models;

public class Category
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}
