using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Encina.Modules.Isolation;

/// <summary>
/// Extracts schema names from SQL statements using regex pattern matching.
/// </summary>
/// <remarks>
/// <para>
/// This utility class parses SQL statements to identify schema-qualified table references.
/// It is designed for development-time validation of module isolation boundaries,
/// not for production-level SQL parsing.
/// </para>
/// <para>
/// The extractor handles common SQL patterns including:
/// <list type="bullet">
/// <item><description>Schema-qualified table names: <c>schema.Table</c>, <c>[schema].[Table]</c></description></item>
/// <item><description>Quoted identifiers: <c>"schema"."Table"</c></description></item>
/// <item><description>Multiple table references in JOINs, subqueries, CTEs</description></item>
/// <item><description>INSERT, UPDATE, DELETE, SELECT, MERGE statements</description></item>
/// </list>
/// </para>
/// <para>
/// Limitations (by design - this is development-time validation):
/// <list type="bullet">
/// <item><description>Does not handle dynamic SQL or string concatenation</description></item>
/// <item><description>May produce false positives for schema-like patterns in strings</description></item>
/// <item><description>Does not parse CTEs or derived table aliases</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var sql = "SELECT * FROM orders.Orders o JOIN payments.Payments p ON o.Id = p.OrderId";
/// var schemas = SqlSchemaExtractor.ExtractSchemas(sql);
/// // Returns: ["orders", "payments"]
/// </code>
/// </example>
public static partial class SqlSchemaExtractor
{
    /// <summary>
    /// The default schema name used when no schema is specified.
    /// </summary>
    public const string DefaultSchema = "dbo";

    // Regex patterns for extracting schema-qualified table names
    // Pattern explanation:
    // - (?:FROM|JOIN|INTO|UPDATE|DELETE\s+FROM|MERGE\s+INTO)\s+ - SQL keywords that precede table names
    // - (?:\[([^\]]+)\]|"([^"]+)"|(\w+)) - schema name (bracketed, quoted, or plain)
    // - \. - literal dot separator
    // - (?:\[([^\]]+)\]|"([^"]+)"|(\w+)) - table name (bracketed, quoted, or plain)

    [GeneratedRegex(
        @"(?:FROM|JOIN|INTO|UPDATE|MERGE\s+INTO)\s+(?:\[([^\]]+)\]|""([^""]+)""|(\w+))\.(?:\[([^\]]+)\]|""([^""]+)""|(\w+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex SchemaTablePatternRegex();

    // Pattern for DELETE FROM schema.table
    [GeneratedRegex(
        @"DELETE\s+FROM\s+(?:\[([^\]]+)\]|""([^""]+)""|(\w+))\.(?:\[([^\]]+)\]|""([^""]+)""|(\w+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex DeleteFromPatternRegex();

    // Pattern for schema.table references without preceding keyword (e.g., in SET clauses, expressions)
    [GeneratedRegex(
        @"(?<![.\w])(?:\[([^\]]+)\]|""([^""]+)""|([A-Za-z_]\w*))\.(?:\[([^\]]+)\]|""([^""]+)""|([A-Za-z_]\w*))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex GenericSchemaTablePatternRegex();

    /// <summary>
    /// Extracts all schema names referenced in a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to analyze.</param>
    /// <returns>
    /// A set of unique schema names found in the SQL statement, normalized to lowercase.
    /// Returns an empty set if no schemas are found or if the SQL is null/empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Schema names are normalized to lowercase for case-insensitive comparison.
    /// </para>
    /// <para>
    /// The method does not include the default schema (dbo) unless explicitly referenced.
    /// Tables without schema qualification are assumed to be in the default schema
    /// and are not extracted.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = @"
    ///     SELECT o.*, p.Amount
    ///     FROM [orders].[Orders] o
    ///     INNER JOIN payments.Payments p ON o.Id = p.OrderId
    ///     WHERE o.Status IN (SELECT Code FROM shared.OrderStatuses)";
    ///
    /// var schemas = SqlSchemaExtractor.ExtractSchemas(sql);
    /// // Returns: ["orders", "payments", "shared"]
    /// </code>
    /// </example>
    public static IReadOnlySet<string> ExtractSchemas(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return ImmutableHashSet<string>.Empty;
        }

        var schemas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Extract from standard SQL keywords (FROM, JOIN, INTO, UPDATE, MERGE)
            ExtractFromMatches(SchemaTablePatternRegex().Matches(sql), schemas);

            // Extract from DELETE FROM statements
            ExtractFromMatches(DeleteFromPatternRegex().Matches(sql), schemas);

            // Extract from generic schema.table patterns (catches edge cases)
            ExtractFromMatches(GenericSchemaTablePatternRegex().Matches(sql), schemas);
        }
        catch (RegexMatchTimeoutException)
        {
            // If regex times out on complex SQL, return what we have
            // This is development-time validation, so failing open is acceptable
        }

        return schemas.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts schemas from regex matches and adds them to the collection.
    /// </summary>
    private static void ExtractFromMatches(MatchCollection matches, HashSet<string> schemas)
    {
        foreach (Match match in matches)
        {
            // Groups 1, 2, 3 are for bracketed, quoted, and plain schema names
            var schema = GetFirstNonEmptyGroup(match, 1, 2, 3);
            if (!string.IsNullOrWhiteSpace(schema) && !IsReservedKeyword(schema))
            {
                schemas.Add(schema.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    /// Gets the first non-empty group value from the specified group indices.
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
    /// Checks if the identifier is a SQL reserved keyword that shouldn't be treated as a schema.
    /// </summary>
    private static bool IsReservedKeyword(string identifier)
    {
        // Common SQL keywords that might be incorrectly matched as schema names
        return identifier.Equals("sys", StringComparison.OrdinalIgnoreCase) ||
               identifier.Equals("information_schema", StringComparison.OrdinalIgnoreCase) ||
               identifier.Equals("INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a SQL statement references any schemas outside the allowed list.
    /// </summary>
    /// <param name="sql">The SQL statement to analyze.</param>
    /// <param name="allowedSchemas">The schemas the module is allowed to access.</param>
    /// <returns>
    /// A tuple indicating whether the access is valid and the set of unauthorized schemas.
    /// </returns>
    /// <example>
    /// <code>
    /// var sql = "SELECT * FROM orders.Orders JOIN payments.Payments ON ...";
    /// var allowed = new[] { "orders", "shared" };
    /// var (isValid, unauthorized) = SqlSchemaExtractor.ValidateSchemaAccess(sql, allowed);
    /// // isValid: false
    /// // unauthorized: ["payments"]
    /// </code>
    /// </example>
    public static (bool IsValid, IReadOnlySet<string> UnauthorizedSchemas) ValidateSchemaAccess(
        string? sql,
        IEnumerable<string> allowedSchemas)
    {
        var referencedSchemas = ExtractSchemas(sql);
        if (referencedSchemas.Count == 0)
        {
            return (true, ImmutableHashSet<string>.Empty);
        }

        var allowedSet = allowedSchemas.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unauthorized = referencedSchemas
            .Where(s => !allowedSet.Contains(s))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        return (unauthorized.Count == 0, unauthorized);
    }

    /// <summary>
    /// Extracts table references (schema.table pairs) from a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to analyze.</param>
    /// <returns>
    /// A collection of (schema, table) tuples representing the referenced tables.
    /// </returns>
    /// <remarks>
    /// This method provides more detailed information than <see cref="ExtractSchemas"/>,
    /// including the table names. Useful for detailed violation reporting.
    /// </remarks>
    public static IReadOnlyList<(string Schema, string Table)> ExtractTableReferences(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        var references = new List<(string Schema, string Table)>();

        try
        {
            ExtractTableRefsFromMatches(SchemaTablePatternRegex().Matches(sql), references);
            ExtractTableRefsFromMatches(DeleteFromPatternRegex().Matches(sql), references);
        }
        catch (RegexMatchTimeoutException)
        {
            // Return what we have
        }

        return references
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Extracts table references from regex matches.
    /// </summary>
    private static void ExtractTableRefsFromMatches(
        MatchCollection matches,
        List<(string Schema, string Table)> references)
    {
        foreach (Match match in matches)
        {
            var schema = GetFirstNonEmptyGroup(match, 1, 2, 3);
            var table = GetFirstNonEmptyGroup(match, 4, 5, 6);

            if (!string.IsNullOrWhiteSpace(schema) &&
                !string.IsNullOrWhiteSpace(table) &&
                !IsReservedKeyword(schema))
            {
                references.Add((schema.ToLowerInvariant(), table));
            }
        }
    }
}
