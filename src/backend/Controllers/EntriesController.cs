using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using JournalAI.Backend.Data;
using JournalAI.Backend.Models;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public EntriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entries = _db.Entries.Where(e => e.UserId == userId).OrderByDescending(e => e.CreatedAt).Take(50).ToList();
        return Ok(entries);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Entry e)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        e.Id = Guid.NewGuid();
        e.UserId = userId.Value;
        e.CreatedAt = DateTime.UtcNow;
        e.UpdatedAt = e.CreatedAt;
        _db.Entries.Add(e);
        _db.SaveChanges();
        return CreatedAtAction(nameof(GetById), new { id = e.Id }, e);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entry = _db.Entries.Find(id);
        if (entry == null) return NotFound();
        if (entry.UserId != userId) return Forbid();
        return Ok(entry);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value ?? User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub)) return null;
        if (Guid.TryParse(sub, out var guid)) return guid;
        return null;
    }
}
