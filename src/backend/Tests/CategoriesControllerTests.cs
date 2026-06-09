using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Controllers;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Integration tests for CategoriesController: CRUD, per-user ownership,
/// unique-name enforcement, and entry-category clearing on delete.
/// </summary>
public class CategoriesControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private AppDbContext _dbContext = null!;
    private CategoriesController _controller = null!;
    private Guid _testUserId;

    public CategoriesControllerTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _testUserId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        _dbContext = new AppDbContext(_dbContextOptions);

        _dbContext.Users.Add(new User
        {
            Id = _testUserId,
            Email = "cat@example.com",
            PasswordHash = "hash",
            Timezone = "UTC",
            NoTrainingUse = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var logger = new Mock<ILogger<CategoriesController>>();
        _controller = new CategoriesController(_dbContext, logger.Object);

        // The real JWT middleware maps the "sub" claim to ClaimTypes.NameIdentifier.
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    [Fact]
    public async Task CreateCategory_WithValidData_Returns201AndPersists()
    {
        var result = await _controller.CreateCategory(new CreateCategoryDto { Name = "Work", Color = "#ff0000" });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        var dto = Assert.IsType<CategoryResponseDto>(created.Value);
        Assert.Equal("Work", dto.Name);
        Assert.Equal("#ff0000", dto.Color);

        var stored = await _dbContext.Categories.FindAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal(_testUserId, stored!.UserId);
    }

    [Fact]
    public async Task CreateCategory_TrimsNameAndDefaultsColor()
    {
        var result = await _controller.CreateCategory(new CreateCategoryDto { Name = "  Personal  " });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<CategoryResponseDto>(created.Value);
        Assert.Equal("Personal", dto.Name);
        Assert.Equal("#000000", dto.Color);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns409()
    {
        await _controller.CreateCategory(new CreateCategoryDto { Name = "Work" });

        var result = await _controller.CreateCategory(new CreateCategoryDto { Name = "Work" });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task ListCategories_ReturnsOnlyCurrentUserWithEntryCounts()
    {
        var work = new Category { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Work", CreatedAt = DateTime.UtcNow };
        _dbContext.Categories.Add(work);
        // A category owned by a different user must not appear.
        _dbContext.Categories.Add(new Category { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Other", CreatedAt = DateTime.UtcNow });
        _dbContext.Entries.Add(new Entry
        {
            Id = Guid.NewGuid(), UserId = _testUserId, Confidentiality = "public",
            CategoryId = work.Id, CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.ListCategories();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<CategoryResponseDto>>(ok.Value);
        var items = new List<CategoryResponseDto>(list);
        Assert.Single(items);
        Assert.Equal("Work", items[0].Name);
        Assert.Equal(1, items[0].EntryCount);
    }

    [Fact]
    public async Task GetCategory_FromDifferentUser_Returns403()
    {
        var foreign = new Category { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Foreign", CreatedAt = DateTime.UtcNow };
        _dbContext.Categories.Add(foreign);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetCategory(foreign.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetCategory_Missing_Returns404()
    {
        var result = await _controller.GetCategory(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateCategory_RenamesCategory()
    {
        var cat = new Category { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Old", CreatedAt = DateTime.UtcNow };
        _dbContext.Categories.Add(cat);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.UpdateCategory(cat.Id, new UpdateCategoryDto { Name = "New" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CategoryResponseDto>(ok.Value);
        Assert.Equal("New", dto.Name);
        Assert.Equal("New", (await _dbContext.Categories.FindAsync(cat.Id))!.Name);
    }

    [Fact]
    public async Task UpdateCategory_ToExistingName_Returns409()
    {
        _dbContext.Categories.Add(new Category { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Work", CreatedAt = DateTime.UtcNow });
        var personal = new Category { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Personal", CreatedAt = DateTime.UtcNow };
        _dbContext.Categories.Add(personal);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.UpdateCategory(personal.Id, new UpdateCategoryDto { Name = "Work" });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_RemovesCategory_AndClearsEntryReference()
    {
        var cat = new Category { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Temp", CreatedAt = DateTime.UtcNow };
        _dbContext.Categories.Add(cat);
        var entry = new Entry
        {
            Id = Guid.NewGuid(), UserId = _testUserId, Confidentiality = "public",
            CategoryId = cat.Id, CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.DeleteCategory(cat.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await _dbContext.Categories.FindAsync(cat.Id));
        // Entry survives; its category reference is cleared.
        var survivingEntry = await _dbContext.Entries.FindAsync(entry.Id);
        Assert.NotNull(survivingEntry);
        Assert.Null(survivingEntry!.CategoryId);
    }
}
