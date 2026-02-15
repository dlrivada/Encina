using System.Diagnostics.Metrics;

namespace Encina.Cdc.Caching.Diagnostics;

/// <summary>
/// Exposes CDC-driven cache invalidation metrics via the <c>Encina.Cdc.CacheInvalidation</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
/// <item><description><c>encina.cdc.cache.invalidations</c> — Total cache invalidations processed</description></item>
/// <item><description><c>encina.cdc.cache.broadcasts</c> — Total pub/sub broadcasts sent</description></item>
/// <item><description><c>encina.cdc.cache.errors</c> — Total cache invalidation errors</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use <c>table_name</c> and <c>operation</c> tags to identify the source
/// of the change event that triggered invalidation.
/// </para>
/// </remarks>
internal sealed class CacheInvalidationMetrics
{
    private static readonly Meter Meter = new("Encina.Cdc.CacheInvalidation", "1.0");

    private static readonly Counter<long> InvalidationsCounter = Meter.CreateCounter<long>(
        "encina.cdc.cache.invalidations",
        unit: "{invalidation}",
        description: "Total number of CDC-driven cache invalidations processed.");

    private static readonly Counter<long> BroadcastsCounter = Meter.CreateCounter<long>(
        "encina.cdc.cache.broadcasts",
        unit: "{broadcast}",
        description: "Total number of pub/sub cache invalidation broadcasts sent.");

    private static readonly Counter<long> ErrorsCounter = Meter.CreateCounter<long>(
        "encina.cdc.cache.errors",
        unit: "{error}",
        description: "Total number of cache invalidation errors encountered.");

    /// <summary>
    /// Records a successful cache invalidation.
    /// </summary>
    /// <param name="tableName">The table name that triggered invalidation.</param>
    /// <param name="operation">The change operation type (insert, update, delete).</param>
    internal static void RecordInvalidation(string tableName, string operation)
    {
        InvalidationsCounter.Add(1,
            new KeyValuePair<string, object?>("table_name", tableName),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Records a pub/sub broadcast sent.
    /// </summary>
    /// <param name="tableName">The table name that triggered the broadcast.</param>
    /// <param name="operation">The change operation type.</param>
    internal static void RecordBroadcast(string tableName, string operation)
    {
        BroadcastsCounter.Add(1,
            new KeyValuePair<string, object?>("table_name", tableName),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Records a cache invalidation error.
    /// </summary>
    /// <param name="tableName">The table name that caused the error.</param>
    /// <param name="operation">The change operation type.</param>
    /// <param name="errorType">The type of error (e.g., "cache_failure", "broadcast_failure").</param>
    internal static void RecordError(string tableName, string operation, string errorType)
    {
        ErrorsCounter.Add(1,
            new KeyValuePair<string, object?>("table_name", tableName),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("error_type", errorType));
    }
}
