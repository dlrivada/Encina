using System.Text.RegularExpressions;

namespace Encina.Security.Sanitization.Sanitizers;

/// <summary>
/// Provides defense-in-depth sanitization for SQL contexts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Important:</b> Parameterized queries are always the preferred defense against
/// SQL injection. This sanitizer provides an additional layer of protection for scenarios
/// where parameterization is not possible (e.g., dynamic column names, ORDER BY clauses).
/// </para>
/// </remarks>
internal static partial class SqlSanitizer
{
    /// <summary>
    /// Sanitizes input for safe use in SQL contexts.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>The sanitized string with SQL-dangerous patterns neutralized.</returns>
    internal static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // 1. Escape single quotes (SQL string delimiter): ' → ''
        var result = input.Replace("'", "''", StringComparison.Ordinal);

        // 2-5. Removing one dangerous sequence can join the characters around it into a new one
        // (for example "*;/" becomes "*/"), so repeat the removal until the string is stable.
        // Every change shortens the string, so the loop always terminates. Quote escaping stays
        // outside the loop so doubled quotes are never doubled again.
        string previous;
        do
        {
            previous = result;
            result = RemoveDangerousSequences(previous);
        }
        while (!string.Equals(result, previous, StringComparison.Ordinal));

        return result;
    }

    private static string RemoveDangerousSequences(string value)
    {
        // Single-line comment markers: --
        var result = value.Replace("--", string.Empty, StringComparison.Ordinal);

        // Multi-line comment sequences: /* ... */ and unclosed markers
        result = BlockCommentPattern().Replace(result, string.Empty);
        result = result.Replace("/*", string.Empty, StringComparison.Ordinal);
        result = result.Replace("*/", string.Empty, StringComparison.Ordinal);

        // Semicolons that could terminate statements
        result = result.Replace(";", string.Empty, StringComparison.Ordinal);

        // xp_ extended stored procedure calls (SQL Server), also inside identifiers
        return ExtendedProcPattern().Replace(result, string.Empty);
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex BlockCommentPattern();

    [GeneratedRegex(@"xp_\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExtendedProcPattern();
}
