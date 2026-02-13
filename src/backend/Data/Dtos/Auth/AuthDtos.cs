using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Data.Dtos.Auth;

/// <summary>
/// Register request DTO
/// </summary>
public class RegisterDto
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(255)]
    public string Password { get; set; } = null!;

    [MaxLength(64)]
    public string? Timezone { get; set; } = "UTC";
}

/// <summary>
/// Login request DTO
/// </summary>
public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;
}

/// <summary>
/// Token response DTO
/// </summary>
public class TokenDto
{
    [Required]
    public string AccessToken { get; set; } = null!;

    [Required]
    public int ExpiresIn { get; set; } // Seconds

    [Required]
    public TokenUserDto User { get; set; } = null!;

    [Required]
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// User info in token response
/// </summary>
public class TokenUserDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [MaxLength(100)]
    public string? Name { get; set; }

    [Required]
    [MaxLength(64)]
    public string Timezone { get; set; } = "UTC";

    [Required]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Logout request DTO (empty body, just for consistency)
/// </summary>
public class LogoutDto
{
}

/// <summary>
/// Error response DTO
/// </summary>
public class ErrorDto
{
    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string Message { get; set; } = null!;

    public Dictionary<string, string[]>? Errors { get; set; }
}
