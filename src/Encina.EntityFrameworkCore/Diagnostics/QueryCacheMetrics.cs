using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Exposes query caching metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.querycache.hits_total</c> (Counter) —
///   Total number of cache hits.</description></item>
///   <item><description><c>encina.querycache.misses_total</c> (Counter) —
///   Total number of cache misses.</description></item>
///   <item><description><c>encina.querycache.evictions_total</c> (Counter) —
///   Total number of cache evictions, tagged with <c>reason</c>.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class QueryCacheMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _hitsTotal;
    private readonly Counter<long> _missesTotal;
    private readonly Counter<long> _evictionsTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCacheMetrics"/> class,
    /// registering all query cache metric instruments.
    /// </summary>
    public QueryCacheMetrics()
    {
        _hitsTotal = Meter.CreateCounter<long>(
            "encina.querycache.hits_total",
            unit: "{hits}",
            description: "Total number of query cache hits.");

        _missesTotal = Meter.CreateCounter<long>(
            "encina.querycache.misses_total",
            unit: "{misses}",
            description: "Total number of query cache misses.");

        _evictionsTotal = Meter.CreateCounter<long>(
            "encina.querycache.evictions_total",
            unit: "{evictions}",
            description: "Total number of query cache evictions.");
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    /// <param name="entityType">The entity type of the cached query.</param>
    public void RecordHit(string entityType)
    {
        _hitsTotal.Add(1,
            new KeyValuePair<string, object?>("entity_type", entityType));
    }

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    /// <param name="entityType">The entity type of the missed query.</param>
    public void RecordMiss(string entityType)
    {
        _missesTotal.Add(1,
            new KeyValuePair<string, object?>("entity_type", entityType));
    }

    /// <summary>
    /// Records a cache eviction.
    /// </summary>
    /// <param name="reason">The eviction reason (ttl, invalidation, manual).</param>
    public void RecordEviction(string reason)
    {
        _evictionsTotal.Add(1,
            new KeyValuePair<string, object?>("reason", reason));
    }
}
