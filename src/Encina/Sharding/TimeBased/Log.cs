using Microsoft.Extensions.Logging;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// High-performance log messages for time-based sharding lifecycle operations.
/// </summary>
internal static partial class Log
{
    // TierTransitionScheduler: EventIds 1-29

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Time-based sharding scheduler started. Check interval: {CheckInterval}, Transitions: {TransitionCount}")]
    public static partial void SchedulerStarted(ILogger logger, TimeSpan checkInterval, int transitionCount);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Time-based sharding scheduler stopped")]
    public static partial void SchedulerStopped(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug,
        Message = "Time-based sharding scheduler is disabled (Enabled = false)")]
    public static partial void SchedulerDisabled(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug,
        Message = "Starting tier transition check cycle")]
    public static partial void TransitionCheckStarted(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information,
        Message = "Transitioning shard '{ShardId}' from {FromTier} to {ToTier}")]
    public static partial void TransitioningTier(ILogger logger, string shardId, ShardTier fromTier, ShardTier toTier);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information,
        Message = "Successfully transitioned shard '{ShardId}' to {NewTier}")]
    public static partial void TransitionSucceeded(ILogger logger, string shardId, ShardTier newTier);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning,
        Message = "Tier transition failed for shard '{ShardId}': {ErrorMessage}")]
    public static partial void TransitionFailed(ILogger logger, string shardId, string errorMessage);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information,
        Message = "Tier transition check completed. Transitioned: {SuccessCount}, Failed: {FailureCount}")]
    public static partial void TransitionCheckCompleted(ILogger logger, int successCount, int failureCount);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error,
        Message = "Unhandled error during tier transition check cycle")]
    public static partial void TransitionCheckError(ILogger logger, Exception exception);

    // Auto-shard creation: EventIds 30-49

    [LoggerMessage(EventId = 30, Level = LogLevel.Information,
        Message = "Auto-creating shard '{ShardId}' for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}")]
    public static partial void AutoCreatingShard(ILogger logger, string shardId, DateOnly periodStart, DateOnly periodEnd);

    [LoggerMessage(EventId = 31, Level = LogLevel.Information,
        Message = "Successfully auto-created shard '{ShardId}'")]
    public static partial void AutoCreateSucceeded(ILogger logger, string shardId);

    [LoggerMessage(EventId = 32, Level = LogLevel.Warning,
        Message = "Auto-shard creation skipped for '{ShardId}': shard already exists")]
    public static partial void AutoCreateSkippedAlreadyExists(ILogger logger, string shardId);

    [LoggerMessage(EventId = 33, Level = LogLevel.Error,
        Message = "Auto-shard creation failed for '{ShardId}'")]
    public static partial void AutoCreateFailed(ILogger logger, Exception exception, string shardId);

    [LoggerMessage(EventId = 34, Level = LogLevel.Warning,
        Message = "Auto-shard creation skipped: no ConnectionStringTemplate configured")]
    public static partial void AutoCreateSkippedNoTemplate(ILogger logger);

    [LoggerMessage(EventId = 35, Level = LogLevel.Debug,
        Message = "Auto-shard creation not needed: next period shard '{ShardId}' already exists")]
    public static partial void AutoCreateNotNeeded(ILogger logger, string shardId);
}
