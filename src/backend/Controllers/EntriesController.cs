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

    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] Entry updated)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entry = _db.Entries.Find(id);
        if (entry == null) return NotFound();
        if (entry.UserId != userId) return Forbid();

        // Only allow same-day edits
        if (!IsSameLocalDay(entry.CreatedAt, userId.Value))
            return BadRequest(new { error = "Entries can only be edited on the day they were created." });

        // Apply allowed changes
        entry.Title = updated.Title ?? entry.Title;
        entry.Body = updated.Body ?? entry.Body;
        entry.IsPrivate = updated.IsPrivate;
        entry.UpdatedAt = DateTime.UtcNow;

        _db.Entries.Update(entry);
        _db.SaveChanges();
        return Ok(entry);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entry = _db.Entries.Find(id);
        if (entry == null) return NotFound();
        if (entry.UserId != userId) return Forbid();

        // Only allow same-day deletes
        if (!IsSameLocalDay(entry.CreatedAt, userId.Value))
            return BadRequest(new { error = "Entries can only be deleted on the day they were created." });

        _db.Entries.Remove(entry);
        _db.SaveChanges();
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value ?? User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub)) return null;
        if (Guid.TryParse(sub, out var guid)) return guid;
        return null;
    }

    private bool IsSameLocalDay(DateTime utcTime, Guid userId)
    {
        var user = _db.Users.Find(userId);
        var tz = user?.Timezone;
        DateTime userLocal;
        try
        {
            if (!string.IsNullOrEmpty(tz))
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                userLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), tzInfo);
            }
            else
            {
                userLocal = utcTime.ToLocalTime();
            }
        }
        catch
        {
            // If timezone lookup fails, fall back to UTC local comparison
            userLocal = utcTime.ToLocalTime();
        }

        var nowLocal = DateTime.UtcNow;
        try
        {
            if (!string.IsNullOrEmpty(tz))
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
            }
            else
            {
                nowLocal = DateTime.UtcNow.ToLocalTime();
            }
        }
        catch
        {
            nowLocal = DateTime.UtcNow.ToLocalTime();
        }

        return userLocal.Date == nowLocal.Date;
    }
}
