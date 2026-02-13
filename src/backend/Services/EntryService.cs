using JournalAI.Backend.Data.Dtos;

namespace JournalAI.Backend.Services;

/// <summary>
/// Entry business logic service
/// Handles immutability calculations, validation, and timezone conversions
/// </summary>
public class EntryService
{
    private readonly ILogger<EntryService> _logger;

    public EntryService(ILogger<EntryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate read_only_after: end-of-day (23:59:59) in user's timezone
    /// Converts to UTC for storage
    /// </summary>
    /// <param name="userTimezone">IANA timezone identifier (e.g., "America/New_York")</param>
    /// <param name="createdAt">UTC creation time</param>
    /// <returns>UTC time when entry becomes immutable (end-of-day in user's local timezone)</returns>
    public DateTime CalculateReadOnlyAfter(string userTimezone, DateTime createdAt)
    {
        try
        {
            var userTz = TimeZoneInfo.FindSystemTimeZoneById(userTimezone);
            var userLocalTime = TimeZoneInfo.ConvertTime(createdAt, TimeZoneInfo.Utc, userTz);

            // Get end of day in user's local time (23:59:59)
            var endOfDay = userLocalTime.Date.AddDays(1).AddSeconds(-1);

            // Convert back to UTC
            var utcEndOfDay = TimeZoneInfo.ConvertTime(endOfDay, userTz, TimeZoneInfo.Utc);

            _logger.LogInformation(
                "Read-only boundary calculated: {CreatedAt} UTC → {UserLocal} {Timezone} → {EndOfDay} UTC",
                createdAt, userLocalTime, userTimezone, utcEndOfDay);

            return utcEndOfDay;
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invalid timezone {Timezone}, using UTC", userTimezone);
            // Fallback: use UTC end-of-day
            return createdAt.Date.AddDays(1).AddSeconds(-1);
        }
    }

    /// <summary>
    /// Check if entry is still editable (before read_only_after boundary)
    /// </summary>
    public bool IsEntryEditable(Models.Entry entry)
    {
        return DateTime.UtcNow <= entry.ReadOnlyAfter;
    }

    /// <summary>
    /// Validate CreateEntryDto
    /// Ensures required fields are present and valid
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
