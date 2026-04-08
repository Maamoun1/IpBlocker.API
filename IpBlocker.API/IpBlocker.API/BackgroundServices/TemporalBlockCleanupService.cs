using IpBlocker.API.Repositories;

namespace IpBlocker.API.BackgroundServices;

/// <summary>
/// Hosted background service that automatically removes expired temporal blocks.
/// Runs every 5 minutes for the lifetime of the application.
///
/// DI LIFETIME NOTE:
/// BackgroundService is a Singleton by nature.
/// ITemporalBlockRepository is also Singleton — safe to inject directly.
/// If you needed a Scoped dependency, inject IServiceScopeFactory and create
/// a scope manually inside ExecuteAsync.
/// </summary>
public class TemporalBlockCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    private readonly ITemporalBlockRepository _temporalRepo;
    private readonly ILogger<TemporalBlockCleanupService> _logger;

    public TemporalBlockCleanupService(
        ITemporalBlockRepository temporalRepo,
        ILogger<TemporalBlockCleanupService> logger)
    {
        _temporalRepo = temporalRepo;
        _logger = logger;
    }

    /// <summary>
    /// Main loop using PeriodicTimer (recommended since .NET 6).
    /// WHY PeriodicTimer over Task.Delay?
    /// - No drift: cleanup time is NOT added to the interval.
    /// - WaitForNextTickAsync returns false on cancellation — clean shutdown.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "TemporalBlockCleanupService started. Sweeping every {Interval} minutes.",
            CleanupInterval.TotalMinutes);

        using var timer = new PeriodicTimer(CleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync();
        }

        _logger.LogInformation("TemporalBlockCleanupService stopped.");
    }

    private Task RunCleanupAsync()
    {
        try
        {
            var expired = _temporalRepo.GetExpired().ToList();

            if (expired.Count == 0)
            {
                _logger.LogDebug("Cleanup sweep: no expired temporal blocks found.");
                return Task.CompletedTask;
            }

            _logger.LogInformation(
                "Cleanup sweep: removing {Count} expired temporal block(s).", expired.Count);

            var removed = 0;
            foreach (var block in expired)
            {
                if (_temporalRepo.TryRemove(block.CountryCode))
                {
                    removed++;
                    _logger.LogInformation(
                        "Removed expired temporal block: {CountryCode} (expired {ExpiresAt}).",
                        block.CountryCode, block.ExpiresAt);
                }
            }

            _logger.LogInformation("Cleanup sweep complete. Removed {Removed}/{Total}.",
                removed, expired.Count);
        }
        catch (Exception ex)
        {
            // NEVER let exceptions escape ExecuteAsync — it kills the background service permanently.
            _logger.LogError(ex, "Unhandled error during temporal block cleanup sweep.");
        }

        return Task.CompletedTask;
    }
}
