using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JournalAI.Backend.Controllers;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos.Auth;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
// Use fully-qualified BCrypt to avoid type/namespace ambiguity when referencing the
// compiled main assembly from tests.

namespace JournalAI.Backend.Tests;

public class AuthControllerTests : IDisposable
{
    private readonly Mock<ILogger<AuthService>> _mockAuthLogger;
    private readonly Mock<ILogger<AuthController>> _mockControllerLogger;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockAuthLogger = new Mock<ILogger<AuthService>>();
        _mockControllerLogger = new Mock<ILogger<AuthController>>();
        _mockConfig = new Mock<IConfiguration>();

        _mockConfig.Setup(x => x["Jwt:Secret"]).Returns("this-is-a-super-secret-key-for-testing-jwt-tokens");
        _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("journal-ai");
        _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("journal-ai-users");
        _mockConfig.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("1440");

        _authService = new AuthService(_context, _mockConfig.Object, _mockAuthLogger.Object);
        _authController = new AuthController(_authService, _mockControllerLogger.Object);
    }

    [Fact]
    public async Task Register_WithValidCredentials_Returns201Created()
    {
        var registerDto = new RegisterDto
        {
            Email = "controller@example.com",
            Password = "ValidPassword123",
            Timezone = "UTC"
        };

        var result = await _authController.Register(registerDto);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var tokenDto = Assert.IsType<TokenDto>(createdResult.Value);
        Assert.NotNull(tokenDto.AccessToken);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400BadRequest()
    {
        var registerDto = new RegisterDto
        {
            Email = "weak@example.com",
            Password = "short",
            Timezone = "UTC"
        };

        var result = await _authController.Register(registerDto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        var registerDto1 = new RegisterDto
        {
            Email = "dup@example.com",
            Password = "Password123",
            Timezone = "UTC"
        };

        var registerDto2 = new RegisterDto
        {
            Email = "dup@example.com",
            Password = "Different123",
            Timezone = "UTC"
        };

        await _authController.Register(registerDto1);
        var result = await _authController.Register(registerDto2);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        var password = "LoginPassword123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "login@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
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

        var result = await _authController.Login(loginDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "wrong@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123", 12),
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

        var result = await _authController.Login(loginDto);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public void Logout_WhenAuthenticated_Returns200Ok()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim("sub", userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext();
        _authController.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        var result = _authController.Logout();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void GetCurrentUser_WhenAuthenticated_Returns200Ok()
    {
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("email", "me@example.com"),
            new Claim("timezone", "UTC")
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext();
        _authController.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        var result = _authController.GetCurrentUser();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void GetCurrentUser_WhenNotAuthenticated_Returns401Unauthorized()
    {
        var claims = new Claim[0];
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext();
        _authController.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        var result = _authController.GetCurrentUser();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public void ValidateToken_WithValidToken_Returns200Ok()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "validate@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123", 12),
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow
        };
        var tokenDto = _authService.GenerateJWT(user);

        var result = _authController.ValidateToken(tokenDto.AccessToken);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_Returns401Unauthorized()
    {
        var invalidToken = "invalid.token.here";

        var result = _authController.ValidateToken(invalidToken);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public void ValidateToken_WithMissingToken_Returns400BadRequest()
    {
        var emptyToken = "";

        var result = _authController.ValidateToken(emptyToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
