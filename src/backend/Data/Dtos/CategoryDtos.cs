using System.ComponentModel.DataAnnotations;

namespace JournalAI.Backend.Data.Dtos;

/// <summary>
/// DTO for creating a category
/// </summary>
public class CreateCategoryDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = null!;

    [StringLength(7)]
    public string? Color { get; set; }

    public Guid? ParentId { get; set; }
}

/// <summary>
/// DTO for updating a category (all fields optional)
/// </summary>
public class UpdateCategoryDto
{
    [StringLength(128)]
    public string? Name { get; set; }

    [StringLength(7)]
    public string? Color { get; set; }

    public Guid? ParentId { get; set; }
}

/// <summary>
/// DTO for category responses
/// </summary>
public class CategoryResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public Guid? ParentId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Number of entries currently assigned to this category.</summary>
    public int EntryCount { get; set; }
}
