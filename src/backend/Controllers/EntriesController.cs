using Microsoft.AspNetCore.Mvc;
using JournalAI.Backend.Data;
using JournalAI.Backend.Models;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public EntriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult GetAll()
    {
        var entries = _db.Entries.Take(50).ToList();
        return Ok(entries);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Entry e)
    {
        e.Id = Guid.NewGuid();
        e.CreatedAt = DateTime.UtcNow;
        e.UpdatedAt = e.CreatedAt;
        _db.Entries.Add(e);
        _db.SaveChanges();
        return CreatedAtAction(nameof(GetById), new { id = e.Id }, e);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var entry = _db.Entries.Find(id);
        if (entry == null) return NotFound();
        return Ok(entry);
    }
}
