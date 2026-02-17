namespace Encina.Sharding.Migrations;

/// <summary>
/// Controls what information is retrieved during schema introspection.
/// </summary>
public sealed class SchemaIntrospectionOptions
{
    /// <summary>
    /// Gets or sets whether to include column details for each table.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool IncludeColumns { get; set; } = true;

    /// <summary>
    /// Gets or sets a table name filter. When set, only tables matching
    /// this prefix are included in the result.
    /// </summary>
    public string? TableNameFilter { get; set; }
}
