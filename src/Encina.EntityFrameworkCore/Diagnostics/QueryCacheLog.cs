using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Diagnostics;

/// <summary>
/// High-performance logging methods for query cache operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 2400-2499 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class QueryCacheLog
{
    /// <summary>
    /// Logs that a cache lookup is being performed for the specified entity type and query hash.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity being looked up.</param>
    /// <param name="queryHash">The hash identifying the cached query.</param>
    [LoggerMessage(
        EventId = 2400,
        Level = LogLevel.Debug,
        Message = "Cache lookup for {EntityType} (QueryHash: {QueryHash})")]
    public static partial void CacheLookup(
        ILogger logger,
        string entityType,
        string queryHash);

    /// <summary>
    /// Logs that a cache hit occurred for the specified entity type and query hash.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity found in cache.</param>
    /// <param name="queryHash">The hash identifying the cached query.</param>
    [LoggerMessage(
        EventId = 2401,
        Level = LogLevel.Debug,
        Message = "Cache hit for {EntityType} (QueryHash: {QueryHash})")]
    public static partial void CacheHit(
        ILogger logger,
        string entityType,
        string queryHash);

    /// <summary>
    /// Logs that a cache miss occurred for the specified entity type and query hash.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity not found in cache.</param>
    /// <param name="queryHash">The hash identifying the query that missed the cache.</param>
    [LoggerMessage(
        EventId = 2402,
        Level = LogLevel.Debug,
        Message = "Cache miss for {EntityType} (QueryHash: {QueryHash})")]
    public static partial void CacheMiss(
        ILogger logger,
        string entityType,
        string queryHash);

    /// <summary>
    /// Logs that the cache is being populated for the specified entity type and query hash.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity being cached.</param>
    /// <param name="queryHash">The hash identifying the query being cached.</param>
    [LoggerMessage(
        EventId = 2403,
        Level = LogLevel.Debug,
        Message = "Populating cache for {EntityType} (QueryHash: {QueryHash})")]
    public static partial void CachePopulating(
        ILogger logger,
        string entityType,
        string queryHash);

    /// <summary>
    /// Logs that a cache entry was evicted for the specified entity type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity whose cache was evicted.</param>
    /// <param name="reason">The reason for the cache eviction.</param>
    [LoggerMessage(
        EventId = 2404,
        Level = LogLevel.Debug,
        Message = "Cache eviction for {EntityType} (Reason: {Reason})")]
    public static partial void CacheEviction(
        ILogger logger,
        string entityType,
        string reason);

    /// <summary>
    /// Logs that a cache operation failed for the specified entity type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity involved in the failed cache operation.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    [LoggerMessage(
        EventId = 2405,
        Level = LogLevel.Warning,
        Message = "Cache operation failed for {EntityType}: {ErrorMessage}")]
    public static partial void CacheOperationFailed(
        ILogger logger,
        string entityType,
        string errorMessage);
}
