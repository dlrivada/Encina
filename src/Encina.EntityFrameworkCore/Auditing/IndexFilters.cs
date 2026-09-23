namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Builds the SQL of the filtered indexes declared by the audit entity configurations.
/// </summary>
/// <remarks>
/// <para>
/// A filter is raw SQL that EF Core copies into <c>CREATE INDEX ... WHERE</c>. An unquoted identifier such as
/// <c>UserId IS NOT NULL</c> is folded to lower case by PostgreSQL (<c>userid</c>), which does not match the
/// PascalCase column EF Core creates, so the whole schema creation failed and the audit tables never existed (#1128).
/// </para>
/// <para>
/// The column is therefore written as an ANSI delimited identifier (<c>"UserId"</c>), which PostgreSQL accepts and
/// SQL Server accepts with <c>QUOTED_IDENTIFIER ON</c> (the SqlClient default and a requirement of filtered indexes).
/// MySQL has no filtered indexes, so its provider does not emit the filter.
/// </para>
/// </remarks>
internal static class IndexFilters
{
    /// <summary>
    /// Returns the filter <c>"<paramref name="column"/>" IS NOT NULL</c>.
    /// </summary>
    /// <param name="column">The column name as mapped by EF Core.</param>
    /// <returns>The filter SQL.</returns>
    public static string IsNotNull(string column) => $"\"{column}\" IS NOT NULL";
}
