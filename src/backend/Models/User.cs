using System.ComponentModel.DataAnnotations;

namespace JournalAI.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    public string? Name { get; set; }
    public string Timezone { get; set; } = "UTC";
    public bool NoTrainingUse { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
