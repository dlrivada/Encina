namespace Encina.Cdc.Caching;

/// <summary>
/// Resolves entity type names from CDC table names for cache key generation.
/// Supports explicit table-to-entity mappings and automatic schema stripping.
/// </summary>
/// <remarks>
/// <para>
/// CDC connectors typically report fully qualified table names (e.g., <c>"dbo.Orders"</c>,
/// <c>"public.products"</c>). This resolver translates those names into the entity type
/// names used in cache keys by <c>QueryCacheInterceptor</c>.
/// </para>
/// <para>
/// Resolution order:
/// <list type="number">
/// <item><description>Check explicit mappings (case-insensitive)</description></item>
/// <item><description>Strip schema prefix from qualified names (e.g., <c>"dbo.Orders"</c> → <c>"Orders"</c>)</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class CdcTableNameResolver
{
    /// <summary>
    /// Resolves the entity type name from a CDC table name.
    /// </summary>
    /// <param name="tableName">The CDC table name (e.g., <c>"dbo.Orders"</c>, <c>"Orders"</c>).</param>
    /// <param name="mappings">Optional explicit table-to-entity type mappings.</param>
    /// <returns>The resolved entity type name for use in cache key patterns.</returns>
    public static string ResolveEntityType(string tableName, Dictionary<string, string>? mappings)
    {
        // Check explicit mappings first (case-insensitive)
        if (mappings is not null)
        {
            foreach (var (key, value) in mappings)
            {
                if (string.Equals(key, tableName, StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }
        }

        // Strip schema prefix: "dbo.Orders" → "Orders", "public.products" → "products"
        var dotIndex = tableName.LastIndexOf('.');
        return dotIndex >= 0 ? tableName[(dotIndex + 1)..] : tableName;
    }
}
