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

        // 2. Remove single-line comment markers: --
        result = result.Replace("--", string.Empty, StringComparison.Ordinal);

        // 3. Remove multi-line comment sequences: /* ... */
        result = BlockCommentPattern().Replace(result, string.Empty);

        // 4. Remove semicolons that could terminate statements
        result = result.Replace(";", string.Empty, StringComparison.Ordinal);

        // 5. Neutralize xp_ extended stored procedure calls (SQL Server)
        result = ExtendedProcPattern().Replace(result, string.Empty);

        return result;
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex BlockCommentPattern();

    [GeneratedRegex(@"\bxp_\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExtendedProcPattern();
}
