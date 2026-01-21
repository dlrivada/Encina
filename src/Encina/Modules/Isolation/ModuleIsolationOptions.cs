using System.Collections.Immutable;

namespace Encina.Modules.Isolation;

/// <summary>
/// Configuration options for module isolation at the database level.
/// </summary>
/// <remarks>
/// <para>
/// Module isolation enforces bounded context boundaries at the database level,
/// preventing modules from directly accessing each other's data.
/// </para>
/// <para>
/// This configuration controls:
/// <list type="bullet">
/// <item><description>The isolation strategy (validation-only, permissions, or connection-per-module)</description></item>
/// <item><description>Shared schemas accessible to all modules</description></item>
/// <item><description>Permission script generation settings</description></item>
/// <item><description>Per-module schema configurations</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseModuleIsolation = true;
///     config.ModuleIsolation.Strategy = ModuleIsolationStrategy.SchemaWithPermissions;
///     config.ModuleIsolation.SharedSchemas = ["shared", "lookup"];
///     config.ModuleIsolation.GeneratePermissionScripts = true;
///     config.ModuleIsolation.PermissionScriptsOutputPath = "./scripts/permissions";
///
///     config.ModuleIsolation.AddModuleSchema(new ModuleSchemaOptions
///     {
///         ModuleName = "Orders",
///         SchemaName = "orders",
///         DatabaseUser = "orders_user"
///     });
///
///     config.ModuleIsolation.AddModuleSchema(new ModuleSchemaOptions
///     {
///         ModuleName = "Payments",
///         SchemaName = "payments",
///         DatabaseUser = "payments_user"
///     });
/// });
/// </code>
/// </example>
public sealed class ModuleIsolationOptions
{
    private readonly List<ModuleSchemaOptions> _moduleSchemas = [];
    private readonly HashSet<string> _sharedSchemas = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the isolation strategy.
    /// Default is <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/> validates SQL
    /// without real database permissions - ideal for development.
    /// </para>
    /// <para>
    /// <see cref="ModuleIsolationStrategy.SchemaWithPermissions"/> uses real database
    /// users with limited permissions - recommended for production.
    /// </para>
    /// <para>
    /// <see cref="ModuleIsolationStrategy.ConnectionPerModule"/> uses separate connection
    /// strings per module - useful for microservice preparation.
    /// </para>
    /// </remarks>
    public ModuleIsolationStrategy Strategy { get; set; } = ModuleIsolationStrategy.DevelopmentValidationOnly;

    /// <summary>
    /// Gets or sets the schemas that are shared across all modules.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared schemas contain lookup tables, reference data, or other read-only data
    /// that multiple modules need to access (e.g., Countries, Currencies, Statuses).
    /// </para>
    /// <para>
    /// All modules automatically receive SELECT-only access to shared schemas.
    /// No module can INSERT, UPDATE, or DELETE from shared schemas unless explicitly configured.
    /// </para>
    /// <para>
    /// Common patterns for shared schemas:
    /// <list type="bullet">
    /// <item><description>"shared" or "common" - application-wide reference data</description></item>
    /// <item><description>"lookup" - lookup tables and enumerations</description></item>
    /// <item><description>"dbo" - default schema for legacy shared tables</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public IReadOnlySet<string> SharedSchemas => _sharedSchemas;

    /// <summary>
    /// Gets the configured module schema options.
    /// </summary>
    public IReadOnlyList<ModuleSchemaOptions> ModuleSchemas => _moduleSchemas;

    /// <summary>
    /// Gets or sets whether to generate permission SQL scripts.
    /// Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, permission scripts are generated during application startup
    /// or can be generated on-demand using <see cref="IModulePermissionScriptGenerator"/>.
    /// </para>
    /// <para>
    /// Scripts include:
    /// <list type="bullet">
    /// <item><description>Schema creation (CREATE SCHEMA IF NOT EXISTS)</description></item>
    /// <item><description>User creation (CREATE USER/LOGIN)</description></item>
    /// <item><description>Permission grants (GRANT SELECT/INSERT/UPDATE/DELETE)</description></item>
    /// <item><description>Permission revokes for cleanup</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool GeneratePermissionScripts { get; set; }

    /// <summary>
    /// Gets or sets the output path for generated permission scripts.
    /// Default is <c>null</c> (scripts are not written to disk).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, permission scripts are written to this directory with filenames
    /// like "001_create_schemas.sql", "002_create_users.sql", "003_grant_permissions.sql".
    /// </para>
    /// <para>
    /// The path can be absolute or relative to the application's working directory.
    /// The directory is created if it doesn't exist.
    /// </para>
    /// </remarks>
    public string? PermissionScriptsOutputPath { get; set; }

    /// <summary>
    /// Adds schemas to the shared schemas collection.
    /// </summary>
    /// <param name="schemas">The schema names to add.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ModuleIsolationOptions AddSharedSchemas(params string[] schemas)
    {
        foreach (var schema in schemas)
        {
            if (!string.IsNullOrWhiteSpace(schema))
            {
                _sharedSchemas.Add(schema);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds a module schema configuration.
    /// </summary>
    /// <param name="options">The module schema options to add.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a module with the same name is already configured.
    /// </exception>
    public ModuleIsolationOptions AddModuleSchema(ModuleSchemaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_moduleSchemas.Any(m => m.ModuleName.Equals(options.ModuleName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Module '{options.ModuleName}' is already configured. Each module can only have one schema configuration.");
        }

        _moduleSchemas.Add(options);
        return this;
    }

    /// <summary>
    /// Adds a module schema configuration using a fluent builder action.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="schemaName">The database schema name.</param>
    /// <param name="configure">Optional action to configure additional options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ModuleIsolationOptions AddModuleSchema(
        string moduleName,
        string schemaName,
        Action<ModuleSchemaOptionsBuilder>? configure = null)
    {
        var builder = new ModuleSchemaOptionsBuilder(moduleName, schemaName);
        configure?.Invoke(builder);
        return AddModuleSchema(builder.Build());
    }

    /// <summary>
    /// Gets the schema options for a specific module.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <returns>The schema options, or <c>null</c> if the module is not configured.</returns>
    public ModuleSchemaOptions? GetModuleSchema(string moduleName)
    {
        return _moduleSchemas.FirstOrDefault(
            m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether a module has isolation configuration.
    /// </summary>
    /// <param name="moduleName">The name of the module to check.</param>
    /// <returns><c>true</c> if the module has isolation configuration; otherwise, <c>false</c>.</returns>
    public bool HasModuleSchema(string moduleName)
    {
        return _moduleSchemas.Any(
            m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Builder for creating <see cref="ModuleSchemaOptions"/> instances.
/// </summary>
public sealed class ModuleSchemaOptionsBuilder
{
    private readonly string _moduleName;
    private readonly string _schemaName;
    private string? _databaseUser;
    private readonly List<string> _additionalAllowedSchemas = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSchemaOptionsBuilder"/> class.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="schemaName">The database schema name.</param>
    public ModuleSchemaOptionsBuilder(string moduleName, string schemaName)
    {
        _moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    /// Sets the database user for this module.
    /// </summary>
    /// <param name="user">The database user name.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ModuleSchemaOptionsBuilder WithDatabaseUser(string user)
    {
        _databaseUser = user;
        return this;
    }

    /// <summary>
    /// Adds additional schemas that this module is allowed to access.
    /// </summary>
    /// <param name="schemas">The schema names to allow.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ModuleSchemaOptionsBuilder WithAdditionalAllowedSchemas(params string[] schemas)
    {
        _additionalAllowedSchemas.AddRange(schemas);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ModuleSchemaOptions"/> instance.
    /// </summary>
    /// <returns>A new <see cref="ModuleSchemaOptions"/> instance.</returns>
    public ModuleSchemaOptions Build()
    {
        return new ModuleSchemaOptions
        {
            ModuleName = _moduleName,
            SchemaName = _schemaName,
            DatabaseUser = _databaseUser,
            AdditionalAllowedSchemas = _additionalAllowedSchemas.Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray()
        };
    }
}
