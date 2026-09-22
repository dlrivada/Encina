using Microsoft.Extensions.Logging;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// High-performance log messages for time-based sharding lifecycle operations.
/// </summary>
internal static partial class Log
{
    // TierTransitionScheduler: EventIds 147-155

    [LoggerMessage(EventId = 147, Level = LogLevel.Information,
        Message = "Time-based sharding scheduler started. Check interval: {CheckInterval}, Transitions: {TransitionCount}")]
    public static partial void SchedulerStarted(ILogger logger, TimeSpan checkInterval, int transitionCount);

    [LoggerMessage(EventId = 148, Level = LogLevel.Information,
        Message = "Time-based sharding scheduler stopped")]
    public static partial void SchedulerStopped(ILogger logger);

    [LoggerMessage(EventId = 149, Level = LogLevel.Debug,
        Message = "Time-based sharding scheduler is disabled (Enabled = false)")]
    public static partial void SchedulerDisabled(ILogger logger);

    [LoggerMessage(EventId = 150, Level = LogLevel.Debug,
        Message = "Starting tier transition check cycle")]
    public static partial void TransitionCheckStarted(ILogger logger);

    [LoggerMessage(EventId = 151, Level = LogLevel.Information,
        Message = "Transitioning shard '{ShardId}' from {FromTier} to {ToTier}")]
    public static partial void TransitioningTier(ILogger logger, string shardId, ShardTier fromTier, ShardTier toTier);

    [LoggerMessage(EventId = 152, Level = LogLevel.Information,
        Message = "Successfully transitioned shard '{ShardId}' to {NewTier}")]
    public static partial void TransitionSucceeded(ILogger logger, string shardId, ShardTier newTier);

    [LoggerMessage(EventId = 153, Level = LogLevel.Warning,
        Message = "Tier transition failed for shard '{ShardId}': {ErrorMessage}")]
    public static partial void TransitionFailed(ILogger logger, string shardId, string errorMessage);

    [LoggerMessage(EventId = 154, Level = LogLevel.Information,
        Message = "Tier transition check completed. Transitioned: {SuccessCount}, Failed: {FailureCount}")]
    public static partial void TransitionCheckCompleted(ILogger logger, int successCount, int failureCount);

    [LoggerMessage(EventId = 155, Level = LogLevel.Error,
        Message = "Unhandled error during tier transition check cycle")]
    public static partial void TransitionCheckError(ILogger logger, Exception exception);

    // Auto-shard creation: EventIds 156-161

    [LoggerMessage(EventId = 156, Level = LogLevel.Information,
        Message = "Auto-creating shard '{ShardId}' for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}")]
    public static partial void AutoCreatingShard(ILogger logger, string shardId, DateOnly periodStart, DateOnly periodEnd);

    [LoggerMessage(EventId = 157, Level = LogLevel.Information,
        Message = "Successfully auto-created shard '{ShardId}'")]
    public static partial void AutoCreateSucceeded(ILogger logger, string shardId);

    [LoggerMessage(EventId = 158, Level = LogLevel.Warning,
        Message = "Auto-shard creation skipped for '{ShardId}': shard already exists")]
    public static partial void AutoCreateSkippedAlreadyExists(ILogger logger, string shardId);

    [LoggerMessage(EventId = 159, Level = LogLevel.Error,
        Message = "Auto-shard creation failed for '{ShardId}'")]
    public static partial void AutoCreateFailed(ILogger logger, Exception exception, string shardId);

    [LoggerMessage(EventId = 160, Level = LogLevel.Warning,
        Message = "Auto-shard creation skipped: no ConnectionStringTemplate configured")]
    public static partial void AutoCreateSkippedNoTemplate(ILogger logger);

    [LoggerMessage(EventId = 161, Level = LogLevel.Debug,
        Message = "Auto-shard creation not needed: next period shard '{ShardId}' already exists")]
    public static partial void AutoCreateNotNeeded(ILogger logger, string shardId);
}
