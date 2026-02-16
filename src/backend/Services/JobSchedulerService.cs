using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JournalAI.Backend.Services;

/// <summary>
/// Hangfire background job scheduler configuration
/// Manages background job setup, retry policies, and job triggers
/// </summary>
public class JobSchedulerService
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JobSchedulerService> _logger;

    public JobSchedulerService(
        IBackgroundJobClient jobClient,
        IRecurringJobManager recurringJobManager,
        IConfiguration configuration,
        ILogger<JobSchedulerService> logger)
    {
        _jobClient = jobClient;
        _recurringJobManager = recurringJobManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Schedules thumbnail generation for a media file
    /// Queued immediately with automatic retry on failure
    /// </summary>
    public string ScheduleThumbnailGeneration(Guid mediaId, string bucketName)
    {
        try
        {
            var jobId = _jobClient.Enqueue<ThumbnailGenerationJob>(
                job => job.GenerateThumbnailAsync(mediaId, bucketName)
            );

            _logger.LogInformation("Scheduled thumbnail generation: JobId={JobId}, MediaId={MediaId}", 
                jobId, mediaId);

            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule thumbnail generation for media {MediaId}", mediaId);
            throw;
        }
    }

    /// <summary>
    /// Schedules a delayed background job with automatic retry
    /// </summary>
    public string ScheduleJob<T>(
        System.Linq.Expressions.Expression<Func<T, Task>> methodCall,
        TimeSpan delay) where T : class
    {
        try
        {
            var jobId = _jobClient.Schedule(methodCall, delay);
            _logger.LogInformation("Scheduled job: JobId={JobId}, Delay={Delay}ms", jobId, delay.TotalMilliseconds);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule job");
            throw;
        }
    }

    /// <summary>
    /// Registers a recurring job
    /// </summary>
    public void RegisterRecurringJob<T>(
        string jobId,
        System.Linq.Expressions.Expression<Func<T, Task>> methodCall,
        string cronExpression) where T : class
    {
        try
        {
            _recurringJobManager.AddOrUpdate(jobId, methodCall, cronExpression);
            _logger.LogInformation("Registered recurring job: JobId={JobId}, Cron={Cron}", jobId, cronExpression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register recurring job {JobId}", jobId);
            throw;
        }
    }

    /// <summary>
    /// Removes a recurring job
    /// </summary>
    public void RemoveRecurringJob(string jobId)
    {
        try
        {
            _recurringJobManager.RemoveIfExists(jobId);
            _logger.LogInformation("Removed recurring job: JobId={JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove recurring job {JobId}", jobId);
            throw;
        }
    }
}
