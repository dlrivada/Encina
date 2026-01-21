using System.Collections.Immutable;

namespace Encina.Modules.Isolation;

/// <summary>
/// Configuration options for a module's database schema isolation.
/// </summary>
/// <remarks>
/// <para>
/// Each module that participates in database isolation must define its schema configuration.
/// This includes the schema name, optional database user, and any additional schemas
/// the module is allowed to access.
/// </para>
/// <para>
/// The module name is used to identify the module in isolation validation and
/// permission script generation. It should match the module's <see cref="IModule.Name"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ModuleSchemaOptions
/// {
///     ModuleName = "Orders",
///     SchemaName = "orders",
///     DatabaseUser = "orders_user",
///     AdditionalAllowedSchemas = ["inventory"] // Can read inventory schema
/// };
/// </code>
/// </example>
public sealed class ModuleSchemaOptions
{
    /// <summary>
    /// Gets or sets the name of the module.
    /// </summary>
    /// <remarks>
    /// This should match the <see cref="IModule.Name"/> property of the corresponding module.
    /// Used for module identification in logging, diagnostics, and error messages.
    /// </remarks>
    public required string ModuleName { get; init; }

    /// <summary>
    /// Gets or sets the database schema name owned by this module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The module has full access (SELECT, INSERT, UPDATE, DELETE) to tables in this schema.
    /// </para>
    /// <para>
    /// For SQL Server, this is the schema prefix (e.g., "orders" for "orders.Orders" table).
    /// For PostgreSQL, this maps to a PostgreSQL schema.
    /// For MongoDB, this typically maps to a collection prefix or separate database.
    /// </para>
    /// </remarks>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Gets or sets the database user for this module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required when using <see cref="ModuleIsolationStrategy.SchemaWithPermissions"/>
    /// or <see cref="ModuleIsolationStrategy.ConnectionPerModule"/>.
    /// </para>
    /// <para>
    /// This user will be granted full permissions on <see cref="SchemaName"/>
    /// and SELECT-only on shared schemas.
    /// </para>
    /// <para>
    /// If null, the module uses the default connection credentials (suitable for
    /// <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>).
    /// </para>
    /// </remarks>
    public string? DatabaseUser { get; init; }

    /// <summary>
    /// Gets the additional schemas this module is allowed to access.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These schemas are granted SELECT-only access (read-only).
    /// Use this for cross-module reads that don't go through the messaging system.
    /// </para>
    /// <para>
    /// Note: Shared schemas configured in <see cref="ModuleIsolationOptions.SharedSchemas"/>
    /// are automatically available to all modules and don't need to be listed here.
    /// </para>
    /// <para>
    /// Use sparingly - prefer messaging for cross-module communication.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> AdditionalAllowedSchemas { get; init; } = [];

    /// <summary>
    /// Creates a copy of this instance with the specified additional allowed schemas.
    /// </summary>
    /// <param name="schemas">The schemas to add to the allowed list.</param>
    /// <returns>A new instance with the combined additional allowed schemas.</returns>
    public ModuleSchemaOptions WithAdditionalAllowedSchemas(params string[] schemas)
    {
        var combined = AdditionalAllowedSchemas.Concat(schemas).Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();
        return new ModuleSchemaOptions
        {
            ModuleName = ModuleName,
            SchemaName = SchemaName,
            DatabaseUser = DatabaseUser,
            AdditionalAllowedSchemas = combined
        };
    }

    /// <summary>
    /// Gets all schemas this module is allowed to access (own schema + additional).
    /// </summary>
    /// <returns>An enumerable of all allowed schema names.</returns>
    /// <remarks>
    /// This does not include shared schemas, which are managed by <see cref="IModuleSchemaRegistry"/>.
    /// </remarks>
    public IEnumerable<string> GetAllowedSchemas()
    {
        yield return SchemaName;
        foreach (var schema in AdditionalAllowedSchemas)
        {
            yield return schema;
        }
    }
}
