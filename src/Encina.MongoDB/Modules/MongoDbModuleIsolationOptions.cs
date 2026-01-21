using System.Diagnostics.CodeAnalysis;

namespace Encina.MongoDB.Modules;

/// <summary>
/// Configuration options for MongoDB module isolation support.
/// </summary>
/// <remarks>
/// <para>
/// These options control how module isolation is applied to MongoDB operations.
/// Unlike SQL databases which use schemas, MongoDB uses a database-per-module
/// strategy for isolation.
/// </para>
/// <para>
/// Configure these options when calling <c>AddEncinaMongoDBWithModuleIsolation()</c>:
/// </para>
/// <code>
/// services.AddEncinaMongoDBWithModuleIsolation(config =>
/// {
///     config.UseOutbox = true;
/// }, isolation =>
/// {
///     isolation.EnableDatabasePerModule = true;
///     isolation.DatabaseNamePattern = "{baseName}_{moduleName}";
/// });
/// </code>
/// </remarks>
public sealed class MongoDbModuleIsolationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable database-per-module isolation.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// <para>
    /// When enabled, each module uses a separate MongoDB database.
    /// The database name is determined by <see cref="DatabaseNamePattern"/>.
    /// </para>
    /// <para>
    /// When disabled, all modules share a single database and module isolation
    /// must be enforced at the application level (e.g., through collection naming).
    /// </para>
    /// </remarks>
    public bool EnableDatabasePerModule { get; set; } = true;

    /// <summary>
    /// Gets or sets the pattern for generating module-specific database names.
    /// </summary>
    /// <value>The default is <c>"{baseName}_{moduleName}"</c>.</value>
    /// <remarks>
    /// <para>
    /// Placeholders:
    /// <list type="bullet">
    /// <item><c>{baseName}</c> - The base database name from <see cref="EncinaMongoDbOptions.DatabaseName"/></item>
    /// <item><c>{moduleName}</c> - The current module's name (lowercase)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Only used when <see cref="EnableDatabasePerModule"/> is <c>true</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default pattern: "MyApp_orders" for module "Orders"
    /// options.DatabaseNamePattern = "{baseName}_{moduleName}";
    ///
    /// // Custom pattern: "module_orders"
    /// options.DatabaseNamePattern = "module_{moduleName}";
    /// </code>
    /// </example>
    public string DatabaseNamePattern { get; set; } = "{baseName}_{moduleName}";

    /// <summary>
    /// Gets or sets a value indicating whether to throw an exception
    /// when no module context is available for operations that require it.
    /// </summary>
    /// <value>The default is <c>false</c>.</value>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, operations without a current module context will
    /// throw an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// When <c>false</c> (default), operations will use the base database,
    /// which is suitable for shared infrastructure operations.
    /// </para>
    /// </remarks>
    public bool ThrowOnMissingModuleContext { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to log a warning
    /// when falling back to the base database due to missing module context.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// This helps identify operations that might unintentionally bypass
    /// module isolation during development.
    /// </remarks>
    public bool LogWarningOnFallback { get; set; } = true;

    /// <summary>
    /// Gets the module-to-database mapping for explicit configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to override the default database name pattern for specific modules.
    /// If a module is not in this dictionary, the <see cref="DatabaseNamePattern"/>
    /// will be used to generate its database name.
    /// </para>
    /// <para>
    /// Keys are module names (case-insensitive), values are explicit database names.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ModuleDatabaseMappings["Orders"] = "production_orders_db";
    /// options.ModuleDatabaseMappings["Inventory"] = "production_inventory_db";
    /// </code>
    /// </example>
    public Dictionary<string, string> ModuleDatabaseMappings { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Generates the database name for a specific module.
    /// </summary>
    /// <param name="baseName">The base database name.</param>
    /// <param name="moduleName">The module name.</param>
    /// <returns>The database name for the module.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="baseName"/> or <paramref name="moduleName"/> is null or empty.
    /// </exception>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "MongoDB database names conventionally use lowercase. This is intentional.")]
    public string GetDatabaseName(string baseName, string moduleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseName);
        ArgumentException.ThrowIfNullOrEmpty(moduleName);

        // Check for explicit mapping first
        if (ModuleDatabaseMappings.TryGetValue(moduleName, out var explicitDbName))
        {
            return explicitDbName;
        }

        // Use pattern (lowercase is conventional for MongoDB database names)
        return DatabaseNamePattern
            .Replace("{baseName}", baseName, StringComparison.Ordinal)
            .Replace("{moduleName}", moduleName.ToLowerInvariant(), StringComparison.Ordinal);
    }
}
