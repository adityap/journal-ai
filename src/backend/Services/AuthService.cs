using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos.Auth;
using JournalAI.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace JournalAI.Backend.Services;

/// <summary>
/// Authentication service for user registration, login, and JWT token generation
/// Implements bcrypt password hashing (12 rounds) and JWT token creation
/// </summary>
public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    public async Task<TokenDto> RegisterAsync(RegisterDto dto)
    {
        // Validate email uniqueness
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already registered");
        }

        // Hash password with bcrypt (12 rounds per constitution)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = passwordHash,
            Timezone = dto.Timezone ?? "UTC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            NoTrainingUse = true, // Default: entries cannot be used for training
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered: {UserId} ({Email})", user.Id, user.Email);

        // Generate JWT token
        var token = GenerateJWT(user);
        return token;
    }

    /// <summary>
    /// Login user and return JWT token
    /// </summary>
    public async Task<TokenDto> LoginAsync(LoginDto dto)
    {
        // Find user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login attempt with invalid password for user: {UserId}", user.Id);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        _logger.LogInformation("User logged in: {UserId} ({Email})", user.Id, user.Email);

        // Generate JWT token
        var token = GenerateJWT(user);
        return token;
    }

    /// <summary>
    /// Generate JWT token for a user
    /// Token includes user ID as 'sub' claim and expires in 24 hours
    /// </summary>
    public TokenDto GenerateJWT(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("JWT secret not configured");
        }

        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "journal-ai";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "journal-ai-users";
        var jwtExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440"); // Default 24 hours

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("timezone", user.Timezone),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogDebug("JWT token generated for user: {UserId}, expires in {ExpiryMinutes} minutes", user.Id, jwtExpiryMinutes);

        return new TokenDto
        {
            AccessToken = tokenString,
            ExpiresIn = jwtExpiryMinutes * 60, // Convert to seconds
            TokenType = "Bearer",
            User = new TokenUserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = "User",
                Timezone = user.Timezone,
                CreatedAt = user.CreatedAt,
            }
        };
    }

    /// <summary>
    /// Validate a JWT token (for testing/verification)
    /// </summary>
    public bool ValidateJWT(string token)
    {
        try
        {
            var jwtSecret = _configuration["Jwt:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                return false;
            }

            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "journal-ai";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "journal-ai-users";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return validatedToken is JwtSecurityToken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("JWT validation failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Extract user ID from JWT token
    /// </summary>
    public Guid? ExtractUserIdFromToken(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();

            // First try to read the token without validation to extract claims
            if (tokenHandler.CanReadToken(token))
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to extract user ID from token: {Message}", ex.Message);
            return null;
        }
    }
}
