using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Controllers;
using JournalAI.Backend.Data;
using JournalAI.Backend.Data.Dtos;
using JournalAI.Backend.Models;
using JournalAI.Backend.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JournalAI.Backend.Tests;

/// <summary>
/// Integration tests for EntriesController
/// Tests REST API endpoints with database interactions, JWT claims, and immutability enforcement
/// </summary>
public class EntriesControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private AppDbContext _dbContext = null!;
    private EntriesController _controller = null!;
    private EntryService _service = null!;
    private Guid _testUserId;
    private User _testUser = null!;

    public EntriesControllerTests()
    {
        // Use in-memory database for tests
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _testUserId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        _dbContext = new AppDbContext(_dbContextOptions);
        
        // Create test user
        _testUser = new User
        {
            Id = _testUserId,
            Email = "test@example.com",
            PasswordHash = "hash",
            Timezone = "UTC",
            NoTrainingUse = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(_testUser);
        await _dbContext.SaveChangesAsync();

        // Setup service and controller
        var mockLogger = new Mock<ILogger<EntryService>>();
        _service = new EntryService(mockLogger.Object);

        var mockControllerLogger = new Mock<ILogger<EntriesController>>();
        var unlock = new UnlockService(_dbContext, new Mock<ILogger<UnlockService>>().Object);
        _controller = new EntriesController(_dbContext, _service, new TfIdfService(), unlock, mockControllerLogger.Object);

        // Setup controller context with user claim.
        // The real JWT middleware maps the token's "sub" claim to ClaimTypes.NameIdentifier,
        // which is what EntriesController reads — mirror that here.
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    #region POST - Create Entry Tests

    /// <summary>
    /// Test: POST /api/v1/entries creates entry successfully and returns 201 Created
    /// </summary>
    [Fact]
    public async Task CreateEntry_WithValidData_Returns201Created()
    {
        // Arrange
        var createDto = new CreateEntryDto
        {
            Title = "Test Entry",
            BodyText = "Test content",
            Confidentiality = "public",
            SentimentScore = 0.5m
        };

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetEntry), createdResult.ActionName);
        Assert.Equal(201, createdResult.StatusCode);

        var responseDto = Assert.IsType<EntryResponseDto>(createdResult.Value);
        Assert.NotNull(responseDto);
        Assert.Equal("Test Entry", responseDto.Title);
        Assert.Equal("public", responseDto.Confidentiality);
    }

    /// <summary>
    /// Test: client-supplied sentiment is persisted end-to-end (regression guard).
    /// Sentiment is computed client-side; the API must store the supplied score/label/model
    /// and return them, and they must round-trip from the database.
    /// </summary>
    [Fact]
    public async Task CreateEntry_WithSentiment_PersistsSentimentFields()
    {
        // Arrange
        var createDto = new CreateEntryDto
        {
            Title = "Great Day",
            BodyText = "I love this, it's amazing and wonderful!",
            Confidentiality = "public",
            SentimentScore = 0.8m,
            SentimentLabel = "positive",
            SentimentModel = "simple-keyword-analysis"
        };

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert: returned response carries the sentiment
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var responseDto = Assert.IsType<EntryResponseDto>(createdResult.Value);
        Assert.Equal(0.8m, responseDto.SentimentScore);
        Assert.Equal("positive", responseDto.SentimentLabel);

        // Assert: it actually round-trips from the database (not just echoed back)
        var stored = await _dbContext.Entries.FindAsync(responseDto.Id);
        Assert.NotNull(stored);
        Assert.Equal(0.8m, stored!.SentimentScore);
        Assert.Equal("positive", stored.SentimentLabel);
        Assert.Equal("simple-keyword-analysis", stored.SentimentModel);
    }

    /// <summary>
    /// Test: POST with missing confidentiality returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateEntry_MissingConfidentiality_Returns400BadRequest()
    {
        // Arrange
        var createDto = new CreateEntryDto
        {
            Title = "Test Entry",
            BodyText = "Content",
            Confidentiality = null
        };

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test: POST with empty content (both title and body) returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateEntry_EmptyContent_Returns400BadRequest()
    {
        // Arrange
        var createDto = new CreateEntryDto
        {
            Title = null,
            BodyText = null,
            Confidentiality = "public"
        };

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test: POST creates entry with read_only_after timestamp set to end-of-day
    /// </summary>
    [Fact]
    public async Task CreateEntry_SetsReadOnlyAfterCorrectly()
    {
        // Arrange
        var createDto = new CreateEntryDto
        {
            Title = "Test",
            BodyText = "Content",
            Confidentiality = "public"
        };

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var responseDto = Assert.IsType<EntryResponseDto>(createdResult.Value);
        
        // ReadOnlyAfter should be set
        Assert.NotNull(responseDto.ReadOnlyAfter);
        Assert.True(responseDto.ReadOnlyAfter > DateTime.UtcNow);
    }

    #endregion

    #region GET - List & Retrieve Entry Tests

    /// <summary>
    /// Test: GET /api/v1/entries lists entries with default pagination (20 per page)
    /// </summary>
    [Fact]
    public async Task ListEntries_ReturnsPagedResult()
    {
        // Arrange - create 5 test entries
        for (int i = 0; i < 5; i++)
        {
            var entry = new Entry
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Title = $"Entry {i}",
                BodyText = $"Content {i}",
                Type = "text",
                Confidentiality = "public",
                CreatedAt = DateTime.UtcNow.AddHours(-i),
                ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
            };
            _dbContext.Entries.Add(entry);
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ListEntries(null, null, null, null, 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<EntryResponseDto>>(okResult.Value);
        Assert.Equal(5, pagedResult.TotalCount);
        Assert.Equal(5, pagedResult.Items.Count);
        Assert.Equal(1, pagedResult.Page);
        Assert.Equal(20, pagedResult.PerPage);
    }

    /// <summary>
    /// Test: GET entries ordered by CreatedAt DESC (newest first)
    /// </summary>
    [Fact]
    public async Task ListEntries_OrderedByCreatedAtDescending()
    {
        // Arrange
        var entries = new List<Entry>();
        for (int i = 0; i < 3; i++)
        {
            entries.Add(new Entry
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Title = $"Entry {i}",
                BodyText = $"Content {i}",
                Type = "text",
                Confidentiality = "public",
                CreatedAt = DateTime.UtcNow.AddHours(-i),
                ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
            });
        }
        _dbContext.Entries.AddRange(entries);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ListEntries(null, null, null, null, 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<EntryResponseDto>>(okResult.Value);
        
        // First item should be most recent (highest CreatedAt)
        Assert.True(pagedResult.Items[0].CreatedAt >= pagedResult.Items[1].CreatedAt);
    }

    /// <summary>
    /// Test: GET /api/v1/entries/{id} returns 200 with entry data
    /// </summary>
    [Fact]
    public async Task GetEntry_WithValidId_Returns200WithEntry()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Test Entry",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetEntry(entry.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseDto = Assert.IsType<EntryResponseDto>(okResult.Value);
        Assert.Equal(entry.Id.ToString(), responseDto.Id.ToString());
        Assert.Equal("Test Entry", responseDto.Title);
    }

    /// <summary>
    /// Test: GET with non-existent entry ID returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetEntry_WithInvalidId_Returns404NotFound()
    {
        // Act
        var result = await _controller.GetEntry(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Test: GET entry from different user returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetEntry_FromDifferentUser_Returns403Forbidden()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            Title = "Other User Entry",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetEntry(entry.Id);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    #endregion

    #region PATCH - Update Entry Tests

    /// <summary>
    /// Test: PATCH before read_only_after boundary succeeds with 200 OK
    /// </summary>
    [Fact]
    public async Task UpdateEntry_BeforeBoundary_Returns200Success()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Original",
            BodyText = "Original Content",
            Type = "text",
            Confidentiality = "public",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(1), // Boundary is in future
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateEntryDto
        {
            Title = "Updated",
            BodyText = "Updated Content"
        };

        // Act
        var result = await _controller.UpdateEntry(entry.Id, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseDto = Assert.IsType<EntryResponseDto>(okResult.Value);
        Assert.Equal("Updated", responseDto.Title);
        Assert.Equal("Updated Content", responseDto.BodyText);
    }

    /// <summary>
    /// Test: PATCH after read_only_after boundary returns 403 Forbidden with ENTRY_IMMUTABLE
    /// </summary>
    [Fact]
    public async Task UpdateEntry_AfterBoundary_Returns403Immutable()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Original",
            BodyText = "Original Content",
            Type = "text",
            Confidentiality = "public",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(-1), // Boundary is in past
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateEntryDto
        {
            Title = "Updated",
            BodyText = "Updated Content"
        };

        // Act
        var result = await _controller.UpdateEntry(entry.Id, updateDto);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Test: PATCH does not update confidentiality (only Title, BodyText, CategoryId, Tags)
    /// </summary>
    [Fact]
    public async Task UpdateEntry_DoesNotUpdateConfidentiality()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Original",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateEntryDto
        {
            Title = "Updated"
        };

        // Act
        await _controller.UpdateEntry(entry.Id, updateDto);
        var updatedEntry = await _dbContext.Entries.FindAsync(entry.Id);

        // Assert
        Assert.NotNull(updatedEntry);
        Assert.Equal("public", updatedEntry.Confidentiality); // Should not change
    }

    #endregion

    #region DELETE - Remove Entry Tests

    /// <summary>
    /// Test: DELETE before read_only_after boundary succeeds with 204 NoContent
    /// </summary>
    [Fact]
    public async Task DeleteEntry_BeforeBoundary_Returns204NoContent()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "To Delete",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(1) // Boundary in future
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteEntry(entry.Id);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);

        // Verify entry is deleted
        var deletedEntry = await _dbContext.Entries.FindAsync(entry.Id);
        Assert.Null(deletedEntry);
    }

    /// <summary>
    /// Test: DELETE after read_only_after boundary returns 403 Forbidden with ENTRY_IMMUTABLE
    /// </summary>
    [Fact]
    public async Task DeleteEntry_AfterBoundary_Returns403Immutable()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "To Delete",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(-1) // Boundary in past
        };
        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteEntry(entry.Id);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);

        // Verify entry still exists
        var stillExists = await _dbContext.Entries.FindAsync(entry.Id);
        Assert.NotNull(stillExists);
    }

    /// <summary>
    /// Test: DELETE non-existent entry returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task DeleteEntry_WithInvalidId_Returns404NotFound()
    {
        // Act
        var result = await _controller.DeleteEntry(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    #endregion

    #region Filter & Pagination Tests

    /// <summary>
    /// Test: GET entries with pagination page=2, perPage=2 returns correct items
    /// </summary>
    [Fact]
    public async Task ListEntries_WithPagination_ReturnCorrectPage()
    {
        // Arrange - create 5 entries
        for (int i = 0; i < 5; i++)
        {
            var entry = new Entry
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Title = $"Entry {i}",
                BodyText = $"Content {i}",
                Type = "text",
                Confidentiality = "public",
                CreatedAt = DateTime.UtcNow.AddHours(-i),
                ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
            };
            _dbContext.Entries.Add(entry);
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ListEntries(null, null, null, null, 2, 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<EntryResponseDto>>(okResult.Value);
        Assert.Equal(5, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Page);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(3, pagedResult.TotalPages);
    }

    /// <summary>
    /// Test: GET entries filtered by category returns only entries with that category
    /// </summary>
    [Fact]
    public async Task ListEntries_FilterByCategory_ReturnsFilteredEntries()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            UserId = _testUserId,
            Name = "Work",
            Color = "#FF0000"
        };
        _dbContext.Categories.Add(category);

        var entry1 = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Work Entry",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        };

        var entry2 = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Personal Entry",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            CategoryId = null, // No category
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        };

        _dbContext.Entries.AddRange(entry1, entry2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ListEntries(null, null, categoryId, null, 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<EntryResponseDto>>(okResult.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("Work Entry", pagedResult.Items[0].Title);
    }

    /// <summary>
    /// Test: GET entries filtered by tag returns only entries with that tag
    /// </summary>
    [Fact]
    public async Task ListEntries_FilterByTag_ReturnsFilteredEntries()
    {
        // Arrange
        var entry1 = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Tagged Entry",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            Tags = new[] { "important", "review" },
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        };

        var entry2 = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Untagged Entry",
            BodyText = "Content",
            Type = "text",
            Confidentiality = "public",
            Tags = new[] { "archived" },
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
        };

        _dbContext.Entries.AddRange(entry1, entry2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ListEntries(null, null, null, "important", 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<EntryResponseDto>>(okResult.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("Tagged Entry", pagedResult.Items[0].Title);
    }

    /// <summary>
    /// Test: ?q= performs a case-insensitive substring search over title and body.
    /// </summary>
    [Fact]
    public async Task ListEntries_SearchByQuery_MatchesTitleAndBodyCaseInsensitively()
    {
        _dbContext.Entries.AddRange(
            new Entry
            {
                Id = Guid.NewGuid(), UserId = _testUserId, Title = "Kyoto trip",
                BodyText = "temples and gardens", Type = "text", Confidentiality = "public",
                CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
            },
            new Entry
            {
                Id = Guid.NewGuid(), UserId = _testUserId, Title = "Grocery list",
                BodyText = "Visited the KYOTO market", Type = "text", Confidentiality = "public",
                CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
            },
            new Entry
            {
                Id = Guid.NewGuid(), UserId = _testUserId, Title = "Unrelated",
                BodyText = "nothing here", Type = "text", Confidentiality = "public",
                CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.UtcNow.AddDays(1)
            });
        await _dbContext.SaveChangesAsync();

        // Lower-case query should match both the title hit and the body hit (case-insensitive).
        var result = await _controller.ListEntries(null, null, null, null, 1, 20, "kyoto");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<EntryResponseDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.DoesNotContain(pagedResult.Items, e => e.Title == "Unrelated");
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Test: Missing JWT claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task CreateEntry_WithoutJWTClaim_Returns401Unauthorized()
    {
        // Arrange
        var controllerNoClaim = new EntriesController(_dbContext, _service, new TfIdfService(),
            new UnlockService(_dbContext, new Mock<ILogger<UnlockService>>().Object),
            new Mock<ILogger<EntriesController>>().Object);
        controllerNoClaim.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } // No claims
        };

        var createDto = new CreateEntryDto
        {
            Title = "Test",
            BodyText = "Content",
            Confidentiality = "public"
        };

        // Act
        var result = await controllerNoClaim.CreateEntry(createDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    /// <summary>
    /// Test: Out-of-range sentiment score returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateEntry_InvalidSentimentScore_Returns400BadRequest()
    {
        // Arrange
        var createDto = new CreateEntryDto
        {
            Title = "Test",
            BodyText = "Content",
            Confidentiality = "public",
            SentimentScore = 2.5m // Out of range
        };

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    #endregion
}
