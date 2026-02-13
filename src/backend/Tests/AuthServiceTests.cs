using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos.Auth;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using System;
using System.Threading.Tasks;
using BCrypt.Net;

namespace JournalAI.Backend.Tests;

public class AuthServiceTests
{
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AppDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockConfig = new Mock<IConfiguration>();

        _mockConfig.Setup(x => x["Jwt:Secret"]).Returns("this-is-a-super-secret-key-for-testing-jwt-tokens");
        _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("journal-ai");
        _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("journal-ai-users");
        _mockConfig.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("1440");

        _authService = new AuthService(_context, _mockConfig.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidCredentials_ReturnsTokenDto()
    {
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Password123",
            Timezone = "UTC"
        };

        var result = await _authService.RegisterAsync(registerDto);

        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.NotNull(result.User);
        Assert.Equal("test@example.com", result.User.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsException()
    {
        var email = "duplicate@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.HashPassword("OldPassword123", 12),
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Email = email,
            Password = "NewPassword123",
            Timezone = "UTC"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.RegisterAsync(registerDto)
        );
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenDto()
    {
        var password = "LoginPassword123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "login@example.com",
            PasswordHash = BCrypt.HashPassword(password, 12),
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "login@example.com",
            Password = password
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.Equal("login@example.com", result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "wrong@example.com",
            PasswordHash = BCrypt.HashPassword("CorrectPassword123", 12),
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "wrong@example.com",
            Password = "WrongPassword123"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _authService.LoginAsync(loginDto)
        );
    }

    [Fact]
    public void GenerateJWT_CreatesValidToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "jwttest@example.com",
            PasswordHash = BCrypt.HashPassword("Password123", 12),
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow
        };

        var tokenDto = _authService.GenerateJWT(user);

        Assert.NotNull(tokenDto);
        Assert.NotNull(tokenDto.AccessToken);
        Assert.True(tokenDto.ExpiresIn > 0);
        Assert.Equal("Bearer", tokenDto.TokenType);
    }

    [Fact]
    public void ValidateJWT_WithValidToken_ReturnsTrue()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "valid@example.com",
            PasswordHash = BCrypt.HashPassword("Password123", 12),
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow
        };
        var tokenDto = _authService.GenerateJWT(user);

        var result = _authService.ValidateJWT(tokenDto.AccessToken);

        Assert.True(result);
    }

    [Fact]
    public void ValidateJWT_WithInvalidToken_ReturnsFalse()
    {
        var invalidToken = "invalid.token.here";

        var result = _authService.ValidateJWT(invalidToken);

        Assert.False(result);
    }

    [Fact]
    public void ExtractUserIdFromToken_WithValidToken_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "extract@example.com",
            PasswordHash = BCrypt.HashPassword("Password123", 12),
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow
        };
        var tokenDto = _authService.GenerateJWT(user);

        var extractedId = _authService.ExtractUserIdFromToken(tokenDto.AccessToken);

        Assert.Equal(userId, extractedId);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
