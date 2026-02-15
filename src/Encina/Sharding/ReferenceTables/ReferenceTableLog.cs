using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// High-performance structured logging for reference table replication operations.
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class ReferenceTableLog
{
    [LoggerMessage(
        EventId = 750,
        Level = LogLevel.Information,
        Message = "Reference table '{EntityType}' replicated: {RowsSynced} rows to {ShardCount} shards in {DurationMs}ms")]
    public static partial void ReplicationCompleted(
        ILogger logger,
        string entityType,
        int rowsSynced,
        int shardCount,
        double durationMs);

    [LoggerMessage(
        EventId = 751,
        Level = LogLevel.Warning,
        Message = "Reference table '{EntityType}' replication partially failed: {SuccessCount}/{TotalCount} shards succeeded, {FailedCount} failed")]
    public static partial void ReplicationPartialFailure(
        ILogger logger,
        string entityType,
        int successCount,
        int totalCount,
        int failedCount);

    [LoggerMessage(
        EventId = 752,
        Level = LogLevel.Error,
        Message = "Reference table '{EntityType}' replication failed: {ErrorMessage}")]
    public static partial void ReplicationFailed(
        ILogger logger,
        string entityType,
        string errorMessage);

    [LoggerMessage(
        EventId = 753,
        Level = LogLevel.Debug,
        Message = "No changes detected for reference table '{EntityType}' — hash unchanged ({Hash})")]
    public static partial void NoChangesDetected(
        ILogger logger,
        string entityType,
        string hash);

    [LoggerMessage(
        EventId = 754,
        Level = LogLevel.Information,
        Message = "Change detected for reference table '{EntityType}' — hash changed from '{PreviousHash}' to '{CurrentHash}', triggering replication")]
    public static partial void ChangeDetected(
        ILogger logger,
        string entityType,
        string previousHash,
        string currentHash);
}
