namespace SimpleMediator.Caching;

/// <summary>
/// Specifies the priority of a cached item.
/// </summary>
/// <remarks>
/// <para>
/// Cache priority determines the order in which items are evicted when the cache
/// reaches its capacity limit. Items with lower priority are evicted first.
/// </para>
/// <para>
/// Priority levels are used as hints to the cache provider. Not all providers
/// support priority-based eviction. In those cases, the priority is ignored.
/// </para>
/// </remarks>
public enum CachePriority
{
    /// <summary>
    /// Lowest priority. Items are evicted first during memory pressure.
    /// Use for items that are cheap to regenerate.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority. Default priority level for cached items.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority. Items are retained longer during memory pressure.
    /// Use for items that are expensive to regenerate.
    /// </summary>
    High = 2,

    /// <summary>
    /// Items should never be evicted due to memory pressure.
    /// Use sparingly for critical data that must always be available.
    /// </summary>
    NeverRemove = 3
}
