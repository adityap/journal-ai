using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(AppDbContext context, ILogger<CategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List the current user's categories with entry counts
    /// GET /api/v1/categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ListCategories()
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var categories = await _context.Categories
            .Where(c => c.UserId == userGuid)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                ParentId = c.ParentId,
                CreatedAt = c.CreatedAt,
                EntryCount = c.Entries.Count()
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Get a single category
    /// GET /api/v1/categories/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();
        if (category.UserId != userGuid)
            return Forbid();

        return Ok(MapToDto(category, await CountEntries(id)));
    }

    /// <summary>
    /// Create a category. Names are unique per user.
    /// POST /api/v1/categories
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest(new { errors = new[] { "Name is required" } });

        var nameExists = await _context.Categories
            .AnyAsync(c => c.UserId == userGuid && c.Name == name);
        if (nameExists)
            return Conflict(new { message = $"A category named '{name}' already exists" });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userGuid,
            Name = name,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? "#000000" : dto.Color!,
            ParentId = dto.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category created: {CategoryId} by user {UserId}", category.Id, userGuid);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToDto(category, 0));
    }

    /// <summary>
    /// Update a category (partial). Names remain unique per user.
    /// PATCH /api/v1/categories/{id}
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();
        if (category.UserId != userGuid)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var name = dto.Name.Trim();
            var clash = await _context.Categories
                .AnyAsync(c => c.UserId == userGuid && c.Name == name && c.Id != id);
            if (clash)
                return Conflict(new { message = $"A category named '{name}' already exists" });
            category.Name = name;
        }

        if (!string.IsNullOrWhiteSpace(dto.Color))
            category.Color = dto.Color;

        if (dto.ParentId.HasValue)
            category.ParentId = dto.ParentId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Category updated: {CategoryId} by user {UserId}", id, userGuid);
        return Ok(MapToDto(category, await CountEntries(id)));
    }

    /// <summary>
    /// Delete a category. Entries referencing it have their category cleared
    /// (FK is configured ON DELETE SET NULL), they are not deleted.
    /// DELETE /api/v1/categories/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (!TryGetUserId(out var userGuid))
            return Unauthorized();

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();
        if (category.UserId != userGuid)
            return Forbid();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category deleted: {CategoryId} by user {UserId}", id, userGuid);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userGuid)
    {
        userGuid = Guid.Empty;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out userGuid);
    }

    private Task<int> CountEntries(Guid categoryId) =>
        _context.Entries.CountAsync(e => e.CategoryId == categoryId);

    private static CategoryResponseDto MapToDto(Category c, int entryCount) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Color = c.Color,
        ParentId = c.ParentId,
        CreatedAt = c.CreatedAt,
        EntryCount = entryCount
    };
}
