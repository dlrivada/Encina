namespace Encina.Sharding.Migrations;

/// <summary>
/// Describes a schema difference for a single table between two shards.
/// </summary>
/// <param name="TableName">The name of the table that differs.</param>
/// <param name="DiffType">The kind of difference (missing, extra, or modified).</param>
/// <param name="ColumnDiffs">
/// Optional details about individual column differences when <see cref="DiffType"/>
/// is <see cref="TableDiffType.Modified"/>. <see langword="null"/> for
/// <see cref="TableDiffType.Missing"/> and <see cref="TableDiffType.Extra"/> diffs.
/// </param>
public sealed record TableDiff(
    string TableName,
    TableDiffType DiffType,
    IReadOnlyList<string>? ColumnDiffs = null)
{
    /// <summary>Gets the table name.</summary>
    public string TableName { get; } = !string.IsNullOrWhiteSpace(TableName)
        ? TableName
        : throw new ArgumentException("Table name cannot be null or whitespace.", nameof(TableName));
}
