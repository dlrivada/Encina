namespace Encina.Sharding.Migrations;

/// <summary>
/// Describes a single column in a database table schema.
/// </summary>
/// <param name="Name">The column name.</param>
/// <param name="DataType">The provider-specific data type string (e.g., <c>"nvarchar(255)"</c>, <c>"integer"</c>).</param>
/// <param name="IsNullable">Whether the column allows NULL values.</param>
/// <param name="DefaultValue">The column's default value expression, or <see langword="null"/> if none.</param>
public sealed record ColumnSchema(
    string Name,
    string DataType,
    bool IsNullable,
    string? DefaultValue = null)
{
    /// <summary>Gets the column name.</summary>
    public string Name { get; } = !string.IsNullOrWhiteSpace(Name)
        ? Name
        : throw new ArgumentException("Column name cannot be null or whitespace.", nameof(Name));

    /// <summary>Gets the data type.</summary>
    public string DataType { get; } = !string.IsNullOrWhiteSpace(DataType)
        ? DataType
        : throw new ArgumentException("Data type cannot be null or whitespace.", nameof(DataType));
}
