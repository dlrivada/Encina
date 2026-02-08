using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Encina.EntityFrameworkCore.Caching;

/// <summary>
/// Extracts table names from SQL statements for query cache invalidation.
/// </summary>
/// <remarks>
/// <para>
/// This utility parses SQL SELECT statements to identify referenced tables from
/// FROM and JOIN clauses. The extracted table names are used by the
/// <see cref="DefaultQueryCacheKeyGenerator"/> to map SQL tables to EF Core entity types,
/// enabling targeted cache invalidation when entities are modified.
/// </para>
/// <para>
/// The extractor handles provider-specific identifier quoting:
/// </para>
/// <list type="bullet">
/// <item><description>SQL Server: <c>[Schema].[Table]</c> or <c>[Table]</c></description></item>
/// <item><description>PostgreSQL: <c>"schema"."table"</c> or <c>"table"</c></description></item>
/// <item><description>MySQL: <c>`schema`.`table`</c> or <c>`table`</c></description></item>
/// <item><description>SQLite: <c>"table"</c> or plain <c>table</c></description></item>
/// </list>
/// <para>
/// Leverages similar regex-based patterns as <see cref="Encina.Modules.Isolation.SqlSchemaExtractor"/>
/// but focuses on table name extraction rather than schema validation.
/// </para>
/// </remarks>
internal static partial class SqlTableExtractor
{
    // Pattern for FROM clause table extraction:
    // Handles: FROM [table], FROM "table", FROM `table`, FROM schema.table, FROM table
    // Also handles schema-qualified: FROM [schema].[table], FROM "schema"."table", FROM `schema`.`table`
    //
    // Capture groups layout (per identifier):
    //   [bracketed] | "double-quoted" | `backtick-quoted` | plain_identifier
    //
    // For schema.table: groups 1-4 = schema, groups 5-8 = table
    // For plain table: groups 9-12 = table (no schema)
    [GeneratedRegex(
        @"FROM\s+" +
        @"(?:" +
            // Schema-qualified: schema.table
            @"(?:\[([^\]]+)\]|""([^""]+)""|`([^`]+)`|([A-Za-z_]\w*))" +
            @"\s*\.\s*" +
            @"(?:\[([^\]]+)\]|""([^""]+)""|`([^`]+)`|([A-Za-z_]\w*))" +
        @"|" +
            // Unqualified: table only
            @"(?:\[([^\]]+)\]|""([^""]+)""|`([^`]+)`|([A-Za-z_]\w*))" +
        @")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex FromClauseRegex();

    // Pattern for JOIN clause table extraction:
    // Handles: [INNER|LEFT|RIGHT|FULL|CROSS] JOIN [table], etc.
    // Same identifier quoting as FROM clause.
    [GeneratedRegex(
        @"(?:INNER\s+|LEFT\s+(?:OUTER\s+)?|RIGHT\s+(?:OUTER\s+)?|FULL\s+(?:OUTER\s+)?|CROSS\s+)?JOIN\s+" +
        @"(?:" +
            // Schema-qualified: schema.table
            @"(?:\[([^\]]+)\]|""([^""]+)""|`([^`]+)`|([A-Za-z_]\w*))" +
            @"\s*\.\s*" +
            @"(?:\[([^\]]+)\]|""([^""]+)""|`([^`]+)`|([A-Za-z_]\w*))" +
        @"|" +
            // Unqualified: table only
            @"(?:\[([^\]]+)\]|""([^""]+)""|`([^`]+)`|([A-Za-z_]\w*))" +
        @")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex JoinClauseRegex();

    /// <summary>
    /// Extracts all table names referenced in a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to analyze.</param>
    /// <returns>
    /// An ordered list of unique table names found in the SQL statement.
    /// The first element is the primary table from the FROM clause, followed by joined tables.
    /// Returns an empty list if no tables are found or if the SQL is null/empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Table names are returned without quoting characters and without schema prefixes.
    /// For schema-qualified references (e.g., <c>[dbo].[Orders]</c>), only the table name
    /// (<c>Orders</c>) is returned.
    /// </para>
    /// <para>
    /// The ordering is significant: the first table is considered the "primary" entity type
    /// for cache key generation purposes.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<string> ExtractTableNames(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        var tables = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Extract FROM clause tables first (these are the primary entities)
            ExtractTablesFromMatches(FromClauseRegex().Matches(sql), tables, seen);

            // Extract JOIN clause tables (secondary entities)
            ExtractTablesFromMatches(JoinClauseRegex().Matches(sql), tables, seen);
        }
        catch (RegexMatchTimeoutException)
        {
            // If regex times out on complex SQL, return what we have.
            // Cache key generation should be resilient - missing a table means
            // slightly broader invalidation, which is safe.
        }

        return tables;
    }

    /// <summary>
    /// Extracts table names from regex matches.
    /// </summary>
    /// <remarks>
    /// For schema-qualified matches (groups 1-4 = schema, 5-8 = table), extracts only the table name.
    /// For unqualified matches (groups 9-12 = table), extracts the table name directly.
    /// Filters out SQL keywords that might be false positives.
    /// </remarks>
    private static void ExtractTablesFromMatches(
        MatchCollection matches,
        List<string> tables,
        HashSet<string> seen)
    {
        foreach (Match match in matches)
        {
            // Try schema-qualified first: groups 5-8 contain the table name
            var tableName = GetFirstNonEmptyGroup(match, 5, 6, 7, 8);

            if (tableName is null)
            {
                // Try unqualified: groups 9-12 contain the table name
                tableName = GetFirstNonEmptyGroup(match, 9, 10, 11, 12);
            }

            if (tableName is not null && !IsSqlKeyword(tableName) && seen.Add(tableName))
            {
                tables.Add(tableName);
            }
        }
    }

    /// <summary>
    /// Gets the value of the first non-empty capture group from the specified indices.
    /// </summary>
    private static string? GetFirstNonEmptyGroup(Match match, params int[] groupIndices)
    {
        foreach (var index in groupIndices)
        {
            if (index < match.Groups.Count && match.Groups[index].Success)
            {
                var value = match.Groups[index].Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the identifier is a SQL keyword that should not be treated as a table name.
    /// </summary>
    private static bool IsSqlKeyword(string identifier) =>
        identifier.Equals("SELECT", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("FROM", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("WHERE", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("JOIN", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("AS", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("IN", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("NOT", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("NULL", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("INNER", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("LEFT", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("RIGHT", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("OUTER", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("CROSS", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("FULL", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("GROUP", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("ORDER", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("BY", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("HAVING", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("UNION", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("ALL", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("DISTINCT", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("TOP", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("LIMIT", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("OFFSET", StringComparison.OrdinalIgnoreCase) ||
        identifier.Equals("LATERAL", StringComparison.OrdinalIgnoreCase);
}
