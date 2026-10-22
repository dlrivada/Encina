using System.Text.RegularExpressions;

namespace Encina.Messaging;

/// <summary>
/// Validates and sanitizes SQL identifiers (table names, column names) to prevent SQL injection.
/// This helper ensures that dynamically constructed SQL queries using table names are safe.
/// </summary>
public static partial class SqlIdentifierValidator
{
    /// <summary>
    /// Maximum length for SQL identifiers per SQL Server standard (128 characters).
    /// </summary>
    private const int MaxSqlIdentifierLength = 128;

    private const string TooLongMessage =
        $"Table name exceeds maximum length of {MaxSqlIdentifierLength} characters.";

    // The name is not echoed: it may carry control characters that would forge log lines.
    private const string InvalidCharactersMessage =
        "Table name contains invalid characters. " +
        "Only letters, digits, underscores, and an optional schema prefix are allowed.";

    // Valid SQL identifier: one or two dot-separated parts (optional schema, then name). Each part is
    // either bare or fully bracketed, starts with an ASCII letter or underscore and continues with ASCII
    // letters, digits or underscores. \A and \z anchor the whole input ($ would accept a trailing newline).
    [GeneratedRegex(
        @"\A(?:(?:[a-zA-Z_][a-zA-Z0-9_]*|\[[a-zA-Z_][a-zA-Z0-9_]*\])\.)?(?:[a-zA-Z_][a-zA-Z0-9_]*|\[[a-zA-Z_][a-zA-Z0-9_]*\])\z",
        RegexOptions.CultureInvariant)]
    private static partial Regex ValidIdentifierRegex();

    /// <summary>
    /// Validates that a table name is a safe SQL identifier.
    /// Throws <see cref="ArgumentException"/> if the name contains potentially dangerous characters.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>The validated table name.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the table name is null, empty, whitespace, longer than 128 characters, or not a valid identifier.
    /// </exception>
    /// <remarks>
    /// The name is one identifier, or a schema identifier and a name identifier separated by a dot.
    /// Each part is either bare (<c>Users</c>) or fully bracketed (<c>[Users]</c>), so mixed forms such as
    /// <c>dbo.[Users]</c> are accepted. A part starts with an ASCII letter or an underscore and continues
    /// with ASCII letters, digits or underscores. Everything else is rejected:
    /// <list type="bullet">
    /// <item>Control characters anywhere, including a trailing newline</item>
    /// <item>Half-delimited, empty or nested brackets</item>
    /// <item>Backtick and double-quote delimiters (the stores quote names themselves)</item>
    /// <item>Non-ASCII letters, spaces and punctuation</item>
    /// <item>More than two parts, or an empty part</item>
    /// </list>
    /// </remarks>
    public static string ValidateTableName(string tableName, string paramName = "tableName")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName, paramName);

        var problem = FindProblem(tableName);
        if (problem is not null)
        {
            throw new ArgumentException(problem, paramName);
        }

        return tableName;
    }

    /// <summary>
    /// Checks if a table name is a valid SQL identifier without throwing.
    /// </summary>
    /// <param name="tableName">The table name to check.</param>
    /// <returns>True if the table name is valid, false otherwise.</returns>
    /// <remarks>
    /// Applies the same rules as <see cref="ValidateTableName"/>.
    /// </remarks>
    public static bool IsValidTableName(string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return false;

        return FindProblem(tableName) is null;
    }

    private static string? FindProblem(string tableName)
    {
        if (tableName.Length > MaxSqlIdentifierLength)
            return TooLongMessage;

        // Defence in depth (#2028): the anchored regex already excludes control characters, but this
        // explicit check keeps their rejection independent of the pattern.
        foreach (var c in tableName)
        {
            if (char.IsControl(c))
                return InvalidCharactersMessage;
        }

        return ValidIdentifierRegex().IsMatch(tableName) ? null : InvalidCharactersMessage;
    }
}
