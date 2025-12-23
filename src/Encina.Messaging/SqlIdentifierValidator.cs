using System.Text.RegularExpressions;

namespace Encina.Messaging;

/// <summary>
/// Validates and sanitizes SQL identifiers (table names, column names) to prevent SQL injection.
/// This helper ensures that dynamically constructed SQL queries using table names are safe.
/// </summary>
public static partial class SqlIdentifierValidator
{
    // Valid SQL identifier: starts with letter or underscore, contains only letters, digits, underscores
    // Also allows schema-qualified names like "dbo.TableName" or "[schema].[table]"
    [GeneratedRegex(@"^(\[?[a-zA-Z_][a-zA-Z0-9_]*\]?\.)?(\[?[a-zA-Z_][a-zA-Z0-9_]*\]?)$", RegexOptions.Compiled)]
    private static partial Regex ValidIdentifierRegex();

    /// <summary>
    /// Validates that a table name is a safe SQL identifier.
    /// Throws <see cref="ArgumentException"/> if the name contains potentially dangerous characters.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>The validated table name.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the table name is null, empty, whitespace, or contains invalid characters.
    /// </exception>
    /// <remarks>
    /// This method prevents SQL injection by ensuring table names only contain:
    /// <list type="bullet">
    /// <item>Letters (a-z, A-Z)</item>
    /// <item>Digits (0-9, but not at the start)</item>
    /// <item>Underscores (_)</item>
    /// <item>Optional schema prefix (schema.table or [schema].[table])</item>
    /// <item>Optional bracket delimiters ([])</item>
    /// </list>
    /// </remarks>
    public static string ValidateTableName(string tableName, string paramName = "tableName")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName, paramName);

        if (tableName.Length > 128)
        {
            throw new ArgumentException(
                "Table name exceeds maximum length of 128 characters.",
                paramName);
        }

        if (!ValidIdentifierRegex().IsMatch(tableName))
        {
            throw new ArgumentException(
                $"Table name '{tableName}' contains invalid characters. " +
                "Only letters, digits, underscores, and optional schema prefix are allowed.",
                paramName);
        }

        return tableName;
    }

    /// <summary>
    /// Checks if a table name is a valid SQL identifier without throwing.
    /// </summary>
    /// <param name="tableName">The table name to check.</param>
    /// <returns>True if the table name is valid, false otherwise.</returns>
    public static bool IsValidTableName(string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return false;

        if (tableName.Length > 128)
            return false;

        return ValidIdentifierRegex().IsMatch(tableName);
    }
}
