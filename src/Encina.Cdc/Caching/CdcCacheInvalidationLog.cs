using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.Caching;

/// <summary>
/// High-performance structured logging for CDC-driven cache invalidation.
/// Uses <see cref="LoggerMessageAttribute"/> source generators.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class CdcCacheInvalidationLog
{
    [LoggerMessage(
        EventId = 150,
        Level = LogLevel.Debug,
        Message = "Invalidating cache for table '{TableName}' resolved to entity type '{EntityType}' with pattern '{Pattern}'")]
    public static partial void InvalidatingCache(
        ILogger logger, string tableName, string entityType, string pattern);

    [LoggerMessage(
        EventId = 151,
        Level = LogLevel.Debug,
        Message = "Cache invalidation completed for entity type '{EntityType}'")]
    public static partial void CacheInvalidated(
        ILogger logger, string entityType);

    [LoggerMessage(
        EventId = 152,
        Level = LogLevel.Debug,
        Message = "Broadcasting cache invalidation pattern '{Pattern}' on channel '{Channel}'")]
    public static partial void BroadcastingInvalidation(
        ILogger logger, string pattern, string channel);

    [LoggerMessage(
        EventId = 153,
        Level = LogLevel.Debug,
        Message = "Skipping cache invalidation for table '{TableName}' - not in configured table filter")]
    public static partial void TableFilteredOut(
        ILogger logger, string tableName);

    [LoggerMessage(
        EventId = 154,
        Level = LogLevel.Warning,
        Message = "Cache invalidation failed for table '{TableName}'")]
    public static partial void CacheInvalidationFailed(
        ILogger logger, Exception exception, string tableName);

    [LoggerMessage(
        EventId = 155,
        Level = LogLevel.Warning,
        Message = "PubSub broadcast failed for cache invalidation pattern '{Pattern}'")]
    public static partial void PubSubBroadcastFailed(
        ILogger logger, Exception exception, string pattern);

    // =========================================================================
    // Cache Invalidation Subscriber (EventIds 160-165)
    // =========================================================================

    [LoggerMessage(
        EventId = 160,
        Level = LogLevel.Information,
        Message = "Cache invalidation subscriber starting on channel '{Channel}'")]
    public static partial void SubscriberStarting(
        ILogger logger, string channel);

    [LoggerMessage(
        EventId = 161,
        Level = LogLevel.Information,
        Message = "Cache invalidation subscriber started on channel '{Channel}'")]
    public static partial void SubscriberStarted(
        ILogger logger, string channel);

    [LoggerMessage(
        EventId = 162,
        Level = LogLevel.Debug,
        Message = "Received cache invalidation pattern '{Pattern}' from channel '{Channel}'")]
    public static partial void SubscriberReceivedInvalidation(
        ILogger logger, string pattern, string channel);

    [LoggerMessage(
        EventId = 163,
        Level = LogLevel.Debug,
        Message = "Subscriber invalidated local cache with pattern '{Pattern}'")]
    public static partial void SubscriberInvalidatedCache(
        ILogger logger, string pattern);

    [LoggerMessage(
        EventId = 164,
        Level = LogLevel.Warning,
        Message = "Subscriber failed to invalidate local cache with pattern '{Pattern}'")]
    public static partial void SubscriberInvalidationFailed(
        ILogger logger, Exception exception, string pattern);

    [LoggerMessage(
        EventId = 165,
        Level = LogLevel.Information,
        Message = "Cache invalidation subscriber stopped on channel '{Channel}'")]
    public static partial void SubscriberStopped(
        ILogger logger, string channel);
}
