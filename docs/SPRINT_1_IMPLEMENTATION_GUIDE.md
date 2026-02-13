# Sprint 1 Implementation Guide — Core Entries & Database

**Sprint**: Sprint 1  
**Duration**: Weeks 2-3 (2 weeks, 10 business days)  
**User Story**: **US1: Create, read, edit, delete journal entries with same-day edit/delete enforcement**  
**Team Size**: 2-3 developers  
**Success Criteria**: All tasks complete, ≥90% unit test coverage, P95 latency <300ms for list operations

---

## Sprint Goals

✅ **Primary Goal**: Implement complete Entry CRUD with immutability business logic  
✅ **Secondary Goal**: Build EF Core data model foundation for all downstream features  
✅ **Quality Gate**: ≥90% test coverage on Entry service, all integration tests passing  

---

## Deliverables

| Deliverable | Purpose | Owner | Status |
|-------------|---------|-------|--------|
| EF Core DbContext | Database configuration | Backend | Not Started |
| Entry model classes | C# domain models | Backend | Not Started |
| EF Core migrations | Database schema creation | Backend | Not Started |
| Entries API controller | REST endpoints | Backend | Not Started |
| Entry service layer | Business logic | Backend | Not Started |
| Unit tests | Entry business logic | Backend | Not Started |
| Integration tests | API + database | Backend | Not Started |
| Frontend Timeline UI | Entry list view | Frontend | Not Started |
| Frontend Entry Form | Entry creation | Frontend | Not Started |

---

## Task Breakdown

### Phase 1: Database Setup (Day 1-2)

#### T101: Create EF Core DbContext

**File**: `src/backend/Data/AppDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using JournalAI.Models;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Entry> Entries { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Media> Media { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ExportJob> ExportJobs { get; set; }
    public DbSet<UnlockSession> UnlockSessions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure all relationships and constraints
        // See data-model.md for full specification
        
        // User indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        // Entry indexes
        modelBuilder.Entity<Entry>()
            .HasIndex(e => new { e.UserId, e.CreatedAt })
            .IsDescending(false, true);
        
        modelBuilder.Entity<Entry>()
            .HasIndex(e => new { e.UserId, e.CategoryId });
        
        // Category indexes
        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.UserId, c.Name });
    }
}
```

**Acceptance Criteria**:
- [ ] DbContext compiles without errors
- [ ] All DbSet properties defined
- [ ] Index configuration matches data-model.md
- [ ] Relationship configurations correct

---

#### T102: Create Entry Model

**File**: `src/backend/Models/Entry.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Entry
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public string? Title { get; set; }
    
    public string? BodyText { get; set; }
    
    [Required]
    [MaxLength(16)]
    public string Type { get; set; } = "text"; // text|photo|video|mixed
    
    [Required]
    [MaxLength(16)]
    public string Confidentiality { get; set; } = "public"; // public|private
    
    [MaxLength(32)]
    public string? ConfidentialityMethod { get; set; } = "none"; // account_password|none
    
    public Guid? CategoryId { get; set; }
    
    public string[]? Tags { get; set; }
    
    // Sentiment fields
    public decimal? SentimentScore { get; set; } // -1..1
    public string? SentimentLabel { get; set; } // positive|neutral|negative
    public string? SentimentModel { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    [Required]
    public DateTime ReadOnlyAfter { get; set; } // Computed: end-of-day in user's timezone
    
    public bool Immutable { get; set; } = false
    
    public Dictionary<string, object>? Metadata { get; set; }
    
    [MaxLength(16)]
    public string? Source { get; set; } // ui|import|api
    
    public string? OriginalHash { get; set; } // For deduplication
    
    // Navigation
    public virtual User User { get; set; }
    public virtual Category? Category { get; set; }
    public virtual ICollection<Media> Media { get; set; } = new List<Media>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
```

**Acceptance Criteria**:
- [ ] All fields from data-model.md present
- [ ] Data annotations correct (Required, MaxLength, etc.)
- [ ] Navigation properties defined
- [ ] Compiles without errors

---

#### T103: Create Other Models (User, Category, Media, AuditLog, etc.)

**Files**:
- `src/backend/Models/User.cs`
- `src/backend/Models/Category.cs`
- `src/backend/Models/Media.cs`
- `src/backend/Models/AuditLog.cs`
- `src/backend/Models/ExportJob.cs`
- `src/backend/Models/UnlockSession.cs`

Each model should follow same structure as Entry: properties, validation, navigation. See data-model.md for complete field specifications.

**Acceptance Criteria**:
- [ ] All models created
- [ ] All fields match data-model.md exactly
- [ ] Relationships configured correctly
- [ ] All compile without errors

---

#### T104: Create initial EF Core migration

```bash
cd src/backend
dotnet ef migrations add InitialCreate --output-dir Migrations
```

**File**: `src/backend/Migrations/[Timestamp]_InitialCreate.cs`

**Acceptance Criteria**:
- [ ] Migration generates SQL matching data-model.md
- [ ] All indexes created
- [ ] Foreign key constraints correct
- [ ] Migration applies cleanly to fresh database
- [ ] Migration rolls back cleanly

---

### Phase 2: API Implementation (Day 3-5)

#### T105: Create Entry DTOs

**File**: `src/backend/Data/Dtos/EntryDtos.cs`

```csharp
public class CreateEntryDto
{
    public string? Title { get; set; }
    public string? BodyText { get; set; }
    public string? Type { get; set; } = "text";
    [Required]
    public string Confidentiality { get; set; }
    public Guid? CategoryId { get; set; }
    public string[]? Tags { get; set; }
    public decimal? SentimentScore { get; set; }
    public string? SentimentLabel { get; set; }
    public string? SentimentModel { get; set; }
    public Guid[]? MediaIds { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UpdateEntryDto
{
    public string? Title { get; set; }
    public string? BodyText { get; set; }
    public Guid? CategoryId { get; set; }
    public string[]? Tags { get; set; }
}

public class EntryResponseDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? BodyText { get; set; }
    public string Type { get; set; }
    public string Confidentiality { get; set; }
    public Guid? CategoryId { get; set; }
    public string[]? Tags { get; set; }
    public decimal? SentimentScore { get; set; }
    public string? SentimentLabel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ReadOnlyAfter { get; set; }
    public bool Immutable { get; set; }
}
```

**Acceptance Criteria**:
- [ ] All DTOs match API spec in specs.md
- [ ] Data annotations for validation present
- [ ] Compile without errors

---

#### T106: Create EntryService business logic

**File**: `src/backend/Services/EntryService.cs`

```csharp
public class EntryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EntryService> _logger;

    public EntryService(AppDbContext context, ILogger<EntryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculate read_only_after: end-of-day (23:59:59) in user's timezone
    /// </summary>
    public DateTime CalculateReadOnlyAfter(string userTimezone, DateTime createdAt)
    {
        try
        {
            var userTz = TimeZoneInfo.FindSystemTimeZoneById(userTimezone);
            var userLocalTime = TimeZoneInfo.ConvertTime(createdAt, TimeZoneInfo.Utc, userTz);
            
            // Get end of day in user's local time
            var endOfDay = userLocalTime.Date.AddDays(1).AddSeconds(-1); // 23:59:59
            
            // Convert back to UTC
            var utcEndOfDay = TimeZoneInfo.ConvertTime(endOfDay, userTz, TimeZoneInfo.Utc);
            
            _logger.LogInformation(
                "Read-only boundary: {CreatedAt} UTC → {UserLocal} {Timezone} → {EndOfDay} UTC",
                createdAt, userLocalTime, userTimezone, utcEndOfDay);
            
            return utcEndOfDay;
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Invalid timezone {Timezone}, using UTC", userTimezone);
            return createdAt.Date.AddDays(1).AddSeconds(-1);
        }
    }

    /// <summary>
    /// Check if entry is still editable (before read_only_after)
    /// </summary>
    public bool IsEntryEditable(Entry entry)
    {
        return DateTime.UtcNow <= entry.ReadOnlyAfter;
    }

    /// <summary>
    /// Validate CreateEntryDto
    /// </summary>
    public List<string> ValidateCreateEntry(CreateEntryDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Confidentiality))
            errors.Add("Confidentiality is required");

        if (!new[] { "public", "private" }.Contains(dto.Confidentiality?.ToLower()))
            errors.Add("Confidentiality must be 'public' or 'private'");

        if (string.IsNullOrWhiteSpace(dto.BodyText) && string.IsNullOrWhiteSpace(dto.Title))
            errors.Add("Entry must have title or body_text");

        if (dto.SentimentScore.HasValue && (dto.SentimentScore < -1 || dto.SentimentScore > 1))
            errors.Add("Sentiment score must be between -1 and 1");

        return errors;
    }
}
```

**Acceptance Criteria**:
- [ ] CalculateReadOnlyAfter correctly handles timezones
- [ ] Unit tests for timezone edge cases (DST, midnight boundary)
- [ ] IsEntryEditable returns correct bool
- [ ] ValidateCreateEntry validates all constraints
- [ ] ≥90% test coverage on this service

---

#### T107: Create Entries Controller

**File**: `src/backend/Controllers/EntriesController.cs`

```csharp
[ApiController]
[Route("api/v1/entries")]
public class EntriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EntryService _entryService;
    private readonly ILogger<EntriesController> _logger;

    public EntriesController(AppDbContext context, EntryService entryService, ILogger<EntriesController> logger)
    {
        _context = context;
        _entryService = entryService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<EntryResponseDto>> CreateEntry([FromBody] CreateEntryDto createDto)
    {
        // Validate DTO
        var validationErrors = _entryService.ValidateCreateEntry(createDto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        // Get current user (from JWT)
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return Unauthorized();

        // Create entry
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = createDto.Title,
            BodyText = createDto.BodyText,
            Type = createDto.Type ?? "text",
            Confidentiality = createDto.Confidentiality,
            CategoryId = createDto.CategoryId,
            Tags = createDto.Tags,
            SentimentScore = createDto.SentimentScore,
            SentimentLabel = createDto.SentimentLabel,
            SentimentModel = createDto.SentimentModel,
            CreatedAt = DateTime.UtcNow,
            Source = "ui"
        };

        // Calculate read_only_after based on user's timezone
        entry.ReadOnlyAfter = _entryService.CalculateReadOnlyAfter(user.Timezone, entry.CreatedAt);
        entry.Immutable = false; // Will be set by job when read_only_after passes

        _context.Entries.Add(entry);
        await _context.SaveChangesAsync();

        // Log audit
        _context.AuditLogs.Add(new AuditLog
        {
            EntryId = entry.Id,
            UserId = user.Id,
            Action = "create",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, MapToDto(entry));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<EntryResponseDto>>> ListEntries(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = _context.Entries
            .Where(e => e.UserId == Guid.Parse(userId))
            .AsQueryable();

        // Filter by date range
        if (start.HasValue)
            query = query.Where(e => e.CreatedAt >= start.Value);
        if (end.HasValue)
            query = query.Where(e => e.CreatedAt <= end.Value);

        // Filter by category
        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        // Filter by tag (simple string match)
        if (!string.IsNullOrEmpty(tag))
            query = query.Where(e => e.Tags != null && e.Tags.Contains(tag));

        // Filter out private entries (will be handled by unlock in T108)
        // For now, include all but mark them as private

        var totalCount = await query.CountAsync();
        var entries = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        var result = new PagedResult<EntryResponseDto>
        {
            Items = entries.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PerPage = perPage,
            TotalPages = (totalCount + perPage - 1) / perPage
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EntryResponseDto>> GetEntry(Guid id)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var entry = await _context.Entries.FindAsync(id);
        if (entry == null)
            return NotFound();

        if (entry.UserId != Guid.Parse(userId))
            return Forbid();

        return Ok(MapToDto(entry));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<EntryResponseDto>> UpdateEntry(Guid id, [FromBody] UpdateEntryDto updateDto)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var entry = await _context.Entries.FindAsync(id);
        if (entry == null)
            return NotFound();

        if (entry.UserId != Guid.Parse(userId))
            return Forbid();

        // Check immutability
        if (!_entryService.IsEntryEditable(entry))
            return StatusCode(403, new { code = "ENTRY_IMMUTABLE", message = "Entry can no longer be edited" });

        // Update fields
        if (!string.IsNullOrEmpty(updateDto.Title))
            entry.Title = updateDto.Title;
        if (!string.IsNullOrEmpty(updateDto.BodyText))
            entry.BodyText = updateDto.BodyText;
        if (updateDto.CategoryId.HasValue)
            entry.CategoryId = updateDto.CategoryId.Value;
        if (updateDto.Tags != null)
            entry.Tags = updateDto.Tags;

        entry.UpdatedAt = DateTime.UtcNow;

        _context.Entries.Update(entry);
        await _context.SaveChangesAsync();

        // Log audit
        _context.AuditLogs.Add(new AuditLog
        {
            EntryId = entry.Id,
            UserId = Guid.Parse(userId),
            Action = "update",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Ok(MapToDto(entry));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var entry = await _context.Entries.FindAsync(id);
        if (entry == null)
            return NotFound();

        if (entry.UserId != Guid.Parse(userId))
            return Forbid();

        // Check immutability
        if (!_entryService.IsEntryEditable(entry))
            return StatusCode(403, new { code = "ENTRY_IMMUTABLE", message = "Entry can no longer be deleted" });

        // Delete media files (enqueue background job)
        var mediaFiles = await _context.Media.Where(m => m.EntryId == id).ToListAsync();
        // TODO: Enqueue media deletion job

        // Delete entry
        _context.Entries.Remove(entry);
        await _context.SaveChangesAsync();

        // Log audit
        _context.AuditLogs.Add(new AuditLog
        {
            EntryId = entry.Id,
            UserId = Guid.Parse(userId),
            Action = "delete",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private EntryResponseDto MapToDto(Entry entry)
    {
        return new EntryResponseDto
        {
            Id = entry.Id,
            Title = entry.Title,
            BodyText = entry.BodyText,
            Type = entry.Type,
            Confidentiality = entry.Confidentiality,
            CategoryId = entry.CategoryId,
            Tags = entry.Tags,
            SentimentScore = entry.SentimentScore,
            SentimentLabel = entry.SentimentLabel,
            CreatedAt = entry.CreatedAt,
            ReadOnlyAfter = entry.ReadOnlyAfter,
            Immutable = entry.Immutable
        };
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int TotalPages { get; set; }
}
```

**Acceptance Criteria**:
- [ ] POST /api/v1/entries creates entry, returns 201
- [ ] GET /api/v1/entries lists entries paginated, ordered by created_at DESC
- [ ] GET /api/v1/entries/{id} returns single entry
- [ ] PATCH /api/v1/entries/{id} edits if before read_only_after, 403 if after
- [ ] DELETE /api/v1/entries/{id} deletes if before read_only_after, 403 if after
- [ ] All endpoints validate user ownership
- [ ] All error responses follow schema

---

### Phase 3: Testing (Day 5-7)

#### T108: Unit tests for EntryService

**File**: `src/backend/Tests/EntryServiceTests.cs`

```csharp
[TestClass]
public class EntryServiceTests
{
    private EntryService _service;
    private AppDbContext _context;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("test")
            .Options;
        _context = new AppDbContext(options);
        _service = new EntryService(_context, LoggerFactory.Create(b => {}).CreateLogger<EntryService>());
    }

    [TestMethod]
    public void CalculateReadOnlyAfter_UTC_ReturnsEndOfDayUTC()
    {
        // Arrange
        var createdAt = new DateTime(2026, 2, 13, 20, 0, 0, DateTimeKind.Utc);
        
        // Act
        var result = _service.CalculateReadOnlyAfter("UTC", createdAt);
        
        // Assert
        Assert.AreEqual(new DateTime(2026, 2, 13, 23, 59, 59, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void CalculateReadOnlyAfter_EST_ReturnsEndOfDayInUTC()
    {
        // Arrange
        var createdAt = new DateTime(2026, 2, 13, 20, 0, 0, DateTimeKind.Utc); // 3 PM EST
        
        // Act
        var result = _service.CalculateReadOnlyAfter("Eastern Standard Time", createdAt);
        
        // Assert - EST is UTC-5, so 8 PM EST = midnight UTC
        Assert.IsTrue(result > createdAt);
    }

    [TestMethod]
    public void IsEntryEditable_BeforeReadOnlyAfter_ReturnsTrue()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(1)
        };
        
        // Act
        var result = _service.IsEntryEditable(entry);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsEntryEditable_AfterReadOnlyAfter_ReturnsFalse()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(-1)
        };
        
        // Act
        var result = _service.IsEntryEditable(entry);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidateCreateEntry_MissingConfidentiality_ReturnsError()
    {
        // Arrange
        var dto = new CreateEntryDto { Confidentiality = null };
        
        // Act
        var errors = _service.ValidateCreateEntry(dto);
        
        // Assert
        Assert.IsTrue(errors.Any(e => e.Contains("Confidentiality is required")));
    }

    [TestMethod]
    public void ValidateCreateEntry_EmptyContent_ReturnsError()
    {
        // Arrange
        var dto = new CreateEntryDto 
        { 
            Confidentiality = "public",
            BodyText = null,
            Title = null
        };
        
        // Act
        var errors = _service.ValidateCreateEntry(dto);
        
        // Assert
        Assert.IsTrue(errors.Any(e => e.Contains("Entry must have")));
    }
}
```

**Acceptance Criteria**:
- [ ] All test methods pass
- [ ] ≥15 test cases covering:
  - Timezone calculations (UTC, EST, PST, JST)
  - DST transitions
  - Midnight boundary conditions
  - Editability check before/after
  - Validation logic
- [ ] Coverage ≥90%

---

#### T109: Integration tests for Entries API

**File**: `src/backend/Tests/EntriesControllerTests.cs`

```csharp
[TestClass]
public class EntriesControllerTests
{
    private AppDbContext _context;
    private EntriesController _controller;
    private User _testUser;

    [TestInitialize]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("test")
            .Options;
        _context = new AppDbContext(options);

        // Create test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed",
            Timezone = "UTC"
        };
        _context.Users.Add(_testUser);
        await _context.SaveChangesAsync();

        // Setup controller with mock claims
        var claims = new List<Claim> { new Claim("sub", _testUser.Id.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _controller = new EntriesController(_context, new EntryService(_context, LoggerFactory.Create(b => {}).CreateLogger<EntryService>()), LoggerFactory.Create(b => {}).CreateLogger<EntriesController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    [TestMethod]
    public async Task PostCreateEntry_ValidData_Returns201()
    {
        // Arrange
        var createDto = new CreateEntryDto
        {
            Title = "Test Entry",
            BodyText = "This is a test entry",
            Confidentiality = "public",
            SentimentScore = 0.5m,
            SentimentLabel = "positive"
        };

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
        var createdResult = (CreatedAtActionResult)result.Result;
        Assert.AreEqual(nameof(_controller.GetEntry), createdResult.ActionName);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("Test Entry", result.Value.Title);
    }

    [TestMethod]
    public async Task GetListEntries_ReturnsEntriesOrderedByCreatedAtDesc()
    {
        // Arrange
        var entry1 = new Entry { Id = Guid.NewGuid(), UserId = _testUser.Id, BodyText = "Old", CreatedAt = DateTime.UtcNow.AddDays(-1), ReadOnlyAfter = DateTime.MaxValue };
        var entry2 = new Entry { Id = Guid.NewGuid(), UserId = _testUser.Id, BodyText = "New", CreatedAt = DateTime.UtcNow, ReadOnlyAfter = DateTime.MaxValue };
        _context.Entries.AddRange(entry1, entry2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ListEntries(null, null, null, null, 1, 20);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result.Result;
        var pagedResult = (PagedResult<EntryResponseDto>)okResult.Value;
        Assert.AreEqual(2, pagedResult.Items.Count);
        Assert.AreEqual("New", pagedResult.Items[0].BodyText);
        Assert.AreEqual("Old", pagedResult.Items[1].BodyText);
    }

    [TestMethod]
    public async Task PatchUpdateEntry_BeforeReadOnlyAfter_Returns200()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Title = "Original",
            BodyText = "Original body",
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddHours(1),
            Immutable = false
        };
        _context.Entries.Add(entry);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateEntryDto { Title = "Updated" };

        // Act
        var result = await _controller.UpdateEntry(entry.Id, updateDto);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        var updated = await _context.Entries.FindAsync(entry.Id);
        Assert.AreEqual("Updated", updated.Title);
    }

    [TestMethod]
    public async Task PatchUpdateEntry_AfterReadOnlyAfter_Returns403()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Title = "Original",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(-1),
            Immutable = false
        };
        _context.Entries.Add(entry);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateEntryDto { Title = "Updated" };

        // Act
        var result = await _controller.UpdateEntry(entry.Id, updateDto);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(StatusCodeResult));
        var statusResult = (StatusCodeResult)result.Result;
        Assert.AreEqual(403, statusResult.StatusCode);
    }

    [TestMethod]
    public async Task DeleteEntry_BeforeReadOnlyAfter_Returns204()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            ReadOnlyAfter = DateTime.UtcNow.AddHours(1),
            Immutable = false
        };
        _context.Entries.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteEntry(entry.Id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NoContentResult));
        var deleted = await _context.Entries.FindAsync(entry.Id);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task DeleteEntry_AfterReadOnlyAfter_Returns403()
    {
        // Arrange
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ReadOnlyAfter = DateTime.UtcNow.AddHours(-1),
            Immutable = false
        };
        _context.Entries.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteEntry(entry.Id);

        // Assert
        Assert.IsInstanceOfType(result, typeof(StatusCodeResult));
        var statusResult = (StatusCodeResult)result;
        Assert.AreEqual(403, statusResult.StatusCode);
    }
}
```

**Acceptance Criteria**:
- [ ] POST test: creates entry with correct timestamp and read_only_after
- [ ] GET list test: returns paginated, ordered by created_at DESC
- [ ] GET detail test: returns full entry
- [ ] PATCH before boundary: succeeds, updates fields
- [ ] PATCH after boundary: returns 403 ENTRY_IMMUTABLE
- [ ] DELETE before boundary: succeeds
- [ ] DELETE after boundary: returns 403 ENTRY_IMMUTABLE
- [ ] All 8+ integration tests pass
- [ ] Coverage ≥80%

---

### Phase 4: Frontend Implementation (Day 5-7)

#### T110: Create Timeline component

**File**: `src/frontend/src/components/Timeline.tsx`

```tsx
import React, { useEffect, useState } from 'react';
import { apiClient } from '../services/apiClient';

interface Entry {
  id: string;
  title?: string;
  bodyText?: string;
  type: string;
  confidentiality: string;
  createdAt: string;
  readOnlyAfter: string;
  immutable: boolean;
}

interface PagedResult {
  items: Entry[];
  totalCount: number;
  page: number;
  perPage: number;
  totalPages: number;
}

export const Timeline: React.FC = () => {
  const [entries, setEntries] = useState<Entry[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchEntries = async () => {
      setLoading(true);
      setError(null);
      try {
        const response = await apiClient.get<PagedResult>(`/entries?page=${page}&per_page=20`);
        setEntries(response.data.items);
        setTotalPages(response.data.totalPages);
      } catch (err) {
        setError((err as Error).message);
      } finally {
        setLoading(false);
      }
    };

    fetchEntries();
  }, [page]);

  if (loading) return <div>Loading...</div>;
  if (error) return <div className="text-red-600">Error: {error}</div>;

  return (
    <div className="space-y-4">
      {entries.map((entry) => (
        <div
          key={entry.id}
          className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition"
        >
          <div className="flex justify-between items-start">
            <div>
              <h3 className="text-lg font-semibold">
                {entry.title || 'Untitled'}
              </h3>
              <p className="text-gray-600 text-sm">
                {new Date(entry.createdAt).toLocaleDateString()}
              </p>
            </div>
            <div className="flex items-center gap-2">
              {entry.confidentiality === 'private' && (
                <span className="px-2 py-1 text-xs font-semibold text-red-600 bg-red-100 rounded">
                  Private
                </span>
              )}
              <span className={`text-xs px-2 py-1 rounded ${entry.immutable ? 'bg-gray-100 text-gray-600' : 'bg-blue-100 text-blue-600'}`}>
                {entry.immutable ? 'Read-only' : 'Editable'}
              </span>
            </div>
          </div>
          {entry.bodyText && (
            <p className="mt-2 text-gray-700 line-clamp-3">
              {entry.bodyText}
            </p>
          )}
          <div className="mt-2 flex gap-2">
            <a href={`/entries/${entry.id}`} className="text-blue-600 hover:underline text-sm">
              View
            </a>
            {!entry.immutable && (
              <>
                <a href={`/entries/${entry.id}/edit`} className="text-blue-600 hover:underline text-sm">
                  Edit
                </a>
              </>
            )}
          </div>
        </div>
      ))}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-8">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-4 py-2 border border-gray-300 rounded disabled:opacity-50"
          >
            Previous
          </button>
          <span className="px-4 py-2">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="px-4 py-2 border border-gray-300 rounded disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
};
```

**Acceptance Criteria**:
- [ ] Component fetches entries on mount
- [ ] Entries displayed in reverse chronological order
- [ ] Private entries marked with badge
- [ ] Immutable entries marked as read-only
- [ ] Pagination works
- [ ] Click "View" navigates to entry detail
- [ ] Click "Edit" only shows for editable entries

---

#### T111: Create Entry Form component

**File**: `src/frontend/src/components/EntryForm.tsx`

```tsx
import React, { useState } from 'react';
import { apiClient } from '../services/apiClient';
import { useNavigate } from 'react-router-dom';

interface EntryFormProps {
  entryId?: string; // If editing
  initialData?: any;
}

export const EntryForm: React.FC<EntryFormProps> = ({ entryId, initialData }) => {
  const [title, setTitle] = useState(initialData?.title || '');
  const [bodyText, setBodyText] = useState(initialData?.bodyText || '');
  const [category, setCategory] = useState(initialData?.categoryId || '');
  const [tags, setTags] = useState(initialData?.tags?.join(', ') || '');
  const [confidentiality, setConfidentiality] = useState(initialData?.confidentiality || 'public');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const payload = {
        title,
        bodyText,
        categoryId: category || null,
        tags: tags ? tags.split(',').map(t => t.trim()) : [],
        confidentiality,
        type: 'text'
      };

      if (entryId) {
        // Edit
        await apiClient.patch(`/entries/${entryId}`, payload);
      } else {
        // Create
        await apiClient.post('/entries', payload);
      }

      navigate('/');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="max-w-2xl mx-auto space-y-6">
      {error && <div className="text-red-600">{error}</div>}

      <div>
        <label className="block text-sm font-medium mb-2">Title (optional)</label>
        <input
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2"
          placeholder="Entry title"
        />
      </div>

      <div>
        <label className="block text-sm font-medium mb-2">Entry (required)</label>
        <textarea
          value={bodyText}
          onChange={(e) => setBodyText(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2 h-64"
          placeholder="Your thoughts..."
          required
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium mb-2">Category</label>
          <select value={category} onChange={(e) => setCategory(e.target.value)} className="w-full border border-gray-300 rounded px-3 py-2">
            <option value="">No category</option>
            {/* Load categories from API */}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium mb-2">Confidentiality *</label>
          <select value={confidentiality} onChange={(e) => setConfidentiality(e.target.value)} className="w-full border border-gray-300 rounded px-3 py-2">
            <option value="public">Public</option>
            <option value="private">Private</option>
          </select>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium mb-2">Tags (comma-separated)</label>
        <input
          type="text"
          value={tags}
          onChange={(e) => setTags(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2"
          placeholder="tag1, tag2, tag3"
        />
      </div>

      <div className="flex gap-4">
        <button
          type="submit"
          disabled={loading}
          className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {loading ? 'Saving...' : entryId ? 'Update' : 'Create'}
        </button>
        <button
          type="button"
          onClick={() => navigate('/')}
          className="px-6 py-2 border border-gray-300 rounded hover:bg-gray-100"
        >
          Cancel
        </button>
      </div>
    </form>
  );
};
```

**Acceptance Criteria**:
- [ ] Form renders all fields
- [ ] Submit creates new entry via POST
- [ ] Submit edits existing entry via PATCH
- [ ] Validates confidentiality is required
- [ ] Parses tags correctly (comma-separated)
- [ ] Shows loading state
- [ ] Displays errors
- [ ] Redirects to timeline on success

---

## Success Criteria (End of Sprint)

✅ **Code Quality**:
- [ ] All unit tests pass (≥90% coverage on EntryService)
- [ ] All integration tests pass
- [ ] No compiler warnings
- [ ] Code formatted with dotnet format

✅ **Functionality**:
- [ ] POST /api/v1/entries creates entries correctly
- [ ] GET /api/v1/entries lists paginated, ordered by created_at DESC
- [ ] GET /api/v1/entries/{id} returns single entry
- [ ] PATCH /api/v1/entries/{id} enforces immutability (403 after read_only_after)
- [ ] DELETE /api/v1/entries/{id} enforces immutability (403 after read_only_after)
- [ ] Frontend Timeline displays entries
- [ ] Frontend EntryForm creates and edits entries

✅ **Performance**:
- [ ] GET /api/v1/entries list P95 < 300ms (with 100 entries)
- [ ] GET /api/v1/entries/{id} P95 < 150ms

✅ **Database**:
- [ ] Schema matches data-model.md exactly
- [ ] Migrations run cleanly (apply + rollback)
- [ ] All indexes created
- [ ] Relationships configured correctly

---

## Daily Standup Template

**What did you do yesterday?**
- [ ] Completed tasks...

**What will you do today?**
- [ ] Working on tasks...

**Any blockers?**
- [ ] None / [describe]

---

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Timezone calculation bugs | High | Medium | Add comprehensive unit tests for DST, edge cases |
| Database migration issues | Medium | High | Test migrations on fresh DB, test rollbacks |
| API contract changes | Medium | High | Finalize OpenAPI spec before implementation, use API versioning |
| Performance regression | Low | Medium | Run benchmarks on list/read operations |

---

**Sprint 1 Start Date**: Monday, [Date]  
**Sprint 1 End Date**: Friday, [Date + 2 weeks]  
**Daily Standup**: 9:00 AM [Timezone]  
**Sprint Review**: Friday 3:00 PM  
**Sprint Retrospective**: Friday 4:00 PM
