namespace Encina.Sharding.Migrations;

/// <summary>
/// Describes the schema of a single database table.
/// </summary>
/// <param name="Name">The table name.</param>
/// <param name="Columns">The columns in this table.</param>
public sealed record TableSchema(
    string Name,
    IReadOnlyList<ColumnSchema> Columns)
{
    /// <summary>Gets the table name.</summary>
    public string Name { get; } = !string.IsNullOrWhiteSpace(Name)
        ? Name
        : throw new ArgumentException("Table name cannot be null or whitespace.", nameof(Name));
}
