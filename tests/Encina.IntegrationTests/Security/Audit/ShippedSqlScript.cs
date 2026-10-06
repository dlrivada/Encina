using System.Text.RegularExpressions;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Reads the DDL scripts that ship in a provider package (<c>src/{package}/Scripts</c>) so an
/// integration test creates its table from the same script an application would run, instead of
/// a copy that can drift from it.
/// </summary>
internal static partial class ShippedSqlScript
{
    /// <summary>The operation audit table script; the same file name in all six relational packages.</summary>
    public const string OperationAuditEntries = "028_CreateOperationAuditEntriesTable.sql";

    /// <summary>
    /// Reads <c>src/{package}/Scripts/{fileName}</c> from the repository that contains the test assembly.
    /// </summary>
    /// <param name="package">The provider package folder, for example <c>Encina.ADO.SqlServer</c>.</param>
    /// <param name="fileName">The script file name.</param>
    /// <returns>The script text.</returns>
    public static string Read(string package, string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Encina.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new FileNotFoundException("Encina.slnx was not found above the test assembly.");
        }

        return File.ReadAllText(Path.Combine(directory.FullName, "src", package, "Scripts", fileName));
    }

    /// <summary>
    /// Splits a SQL Server script on its <c>GO</c> batch separator lines.
    /// </summary>
    /// <param name="script">The script text.</param>
    /// <returns>The non-empty batches in order.</returns>
    public static IReadOnlyList<string> SplitBatches(string script)
    {
        return BatchSeparator()
            .Split(script)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToList();
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex BatchSeparator();
}
