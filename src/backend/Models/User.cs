using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Timezone { get; set; }
    public bool NoTraining { get; set; } = true;
}
