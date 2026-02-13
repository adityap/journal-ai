using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JournalAI.Backend.Data.Dtos.Auth;
using JournalAI.Backend.Services;

namespace JournalAI.Backend.Controllers;

/// <summary>
/// Authentication controller
/// Handles user registration, login, and token management
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorDto
            {
                Code = "VALIDATION_ERROR",
                Message = "Registration data is invalid",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .GroupBy(e => "registration")
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        try
        {
            // Validate email and password
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new ErrorDto
                {
                    Code = "INVALID_EMAIL",
                    Message = "Email is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            {
                return BadRequest(new ErrorDto
                {
                    Code = "WEAK_PASSWORD",
                    Message = "Password must be at least 8 characters"
                });
            }

            var token = await _authService.RegisterAsync(request);
            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return StatusCode(201, token);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
        {
            _logger.LogWarning("Registration failed: email already registered - {Email}", request.Email);
            return Conflict(new ErrorDto
            {
                Code = "EMAIL_EXISTS",
                Message = "Email is already registered"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error: {Email}", request.Email);
            return StatusCode(500, new ErrorDto
            {
                Code = "REGISTRATION_ERROR",
                Message = "An error occurred during registration"
            });
        }
    }

    /// <summary>
    /// Login to existing account
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorDto
            {
                Code = "VALIDATION_ERROR",
                Message = "Login data is invalid",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .GroupBy(e => "login")
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Unauthorized(new ErrorDto
                {
                    Code = "INVALID_CREDENTIALS",
                    Message = "Invalid email or password"
                });
            }

            var token = await _authService.LoginAsync(request);
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(token);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: invalid credentials - {Email}", request.Email);
            return Unauthorized(new ErrorDto
            {
                Code = "INVALID_CREDENTIALS",
                Message = "Invalid email or password"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error: {Email}", request.Email);
            return StatusCode(500, new ErrorDto
            {
                Code = "LOGIN_ERROR",
                Message = "An error occurred during login"
            });
        }
    }

    /// <summary>
    /// Logout (invalidates token on client side)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var userId = User.FindFirst("sub")?.Value;
        _logger.LogInformation("User logged out: {UserId}", userId);
        
        return Ok(new
        {
            message = "Logout successful"
        });
    }

    /// <summary>
    /// Get current authenticated user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;
        var timezone = User.FindFirst("timezone")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorDto
            {
                Code = "UNAUTHORIZED",
                Message = "Not authenticated"
            });
        }

        return Ok(new
        {
            id = userId,
            email = email,
            timezone = timezone ?? "UTC"
        });
    }

    /// <summary>
    /// Validate a JWT token
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public IActionResult ValidateToken([FromBody] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new ErrorDto
            {
                Code = "INVALID_TOKEN",
                Message = "Token is required"
            });
        }

        var isValid = _authService.ValidateJWT(token);
        if (!isValid)
        {
            return Unauthorized(new ErrorDto
            {
                Code = "INVALID_TOKEN",
                Message = "Token is invalid or expired"
            });
        }

        return Ok(new
        {
            valid = true,
            message = "Token is valid"
        });
    }
}

