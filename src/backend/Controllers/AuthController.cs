using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JournalAI.Backend.Data;
using JournalAI.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public record RegisterRequest(string Username, string Password);
    public record LoginRequest(string Username, string Password);

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("username and password required");

        if (_db.Users.Any(u => u.Username == req.Username))
            return Conflict("username already exists");

        var user = new User { Username = req.Username };
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, req.Password);
        _db.Users.Add(user);
        _db.SaveChanges();

        return CreatedAtAction(null, new { id = user.Id }, new { user.Id, user.Username });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var user = _db.Users.SingleOrDefault(u => u.Username == req.Username);
        if (user == null) return Unauthorized();

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed) return Unauthorized();

        var token = GenerateToken(user);
        return Ok(new { token });
    }

    private string GenerateToken(User user)
    {
        var key = _config["Jwt:Key"] ?? "dev-please-change-this-key";
        var issuer = _config["Jwt:Issuer"] ?? "journalai";
        var audience = _config["Jwt:Audience"] ?? "journalai-users";

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
