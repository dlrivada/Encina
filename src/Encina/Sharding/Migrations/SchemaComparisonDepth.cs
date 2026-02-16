namespace Encina.Sharding.Migrations;

/// <summary>
/// Specifies the depth of schema comparison when detecting drift between shards.
/// </summary>
/// <remarks>
/// <para>
/// Higher depth values increase comparison accuracy at the cost of additional
/// introspection queries and processing time.
/// </para>
/// </remarks>
public enum SchemaComparisonDepth
{
    /// <summary>
    /// Compare only table names — detects missing/extra tables but not structural
    /// differences within tables.
    /// </summary>
    TablesOnly,

    /// <summary>
    /// Compare table names and column definitions (name, type, nullability).
    /// This is the default depth.
    /// </summary>
    TablesAndColumns,

    /// <summary>
    /// Compare tables, columns, indexes, and constraints (primary keys, foreign keys,
    /// unique constraints). Provides the most thorough comparison.
    /// </summary>
    Full
}
