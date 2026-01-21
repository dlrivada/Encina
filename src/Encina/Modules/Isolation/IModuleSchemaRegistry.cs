namespace Encina.Modules.Isolation;

/// <summary>
/// Tracks module-to-schema mappings and provides schema access validation.
/// </summary>
/// <remarks>
/// <para>
/// This registry centralizes the knowledge of which schemas each module can access.
/// It combines information from:
/// <list type="bullet">
/// <item><description>The module's own schema (<see cref="ModuleSchemaOptions.SchemaName"/>)</description></item>
/// <item><description>Shared schemas (<see cref="ModuleIsolationOptions.SharedSchemas"/>)</description></item>
/// <item><description>Additional allowed schemas (<see cref="ModuleSchemaOptions.AdditionalAllowedSchemas"/>)</description></item>
/// </list>
/// </para>
/// <para>
/// The registry is typically registered as a singleton and populated during application startup
/// from the <see cref="ModuleIsolationOptions"/> configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if a module can access a schema
/// if (!registry.CanAccessSchema("Orders", "payments"))
/// {
///     throw new ModuleIsolationViolationException("Orders", ["payments"], registry.GetAllowedSchemas("Orders"));
/// }
///
/// // Get all schemas a module can access
/// var allowedSchemas = registry.GetAllowedSchemas("Orders");
/// // Returns: ["orders", "shared", "lookup"] (own + shared schemas)
/// </code>
/// </example>
public interface IModuleSchemaRegistry
{
    /// <summary>
    /// Gets all allowed schemas for a module.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <returns>
    /// A read-only set of schema names the module can access,
    /// including its own schema, shared schemas, and additional allowed schemas.
    /// Returns an empty set if the module is not registered.
    /// </returns>
    IReadOnlySet<string> GetAllowedSchemas(string moduleName);

    /// <summary>
    /// Gets the schema options for a module.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <returns>
    /// The module's schema options, or <c>null</c> if the module is not registered.
    /// </returns>
    ModuleSchemaOptions? GetModuleOptions(string moduleName);

    /// <summary>
    /// Determines whether a module can access a specific schema.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="schemaName">The name of the schema to check.</param>
    /// <returns>
    /// <c>true</c> if the module can access the schema; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// A module can access a schema if any of the following is true:
    /// <list type="bullet">
    /// <item><description>The schema is the module's own schema</description></item>
    /// <item><description>The schema is in the shared schemas list</description></item>
    /// <item><description>The schema is in the module's additional allowed schemas</description></item>
    /// </list>
    /// </remarks>
    bool CanAccessSchema(string moduleName, string schemaName);

    /// <summary>
    /// Gets all registered modules.
    /// </summary>
    /// <returns>
    /// An enumerable of all registered module names.
    /// </returns>
    IEnumerable<string> GetRegisteredModules();

    /// <summary>
    /// Gets the shared schemas accessible to all modules.
    /// </summary>
    /// <returns>
    /// A read-only set of schema names that all modules can access.
    /// </returns>
    IReadOnlySet<string> GetSharedSchemas();

    /// <summary>
    /// Determines whether a module is registered in the registry.
    /// </summary>
    /// <param name="moduleName">The name of the module to check.</param>
    /// <returns>
    /// <c>true</c> if the module is registered; otherwise, <c>false</c>.
    /// </returns>
    bool IsModuleRegistered(string moduleName);

    /// <summary>
    /// Validates a SQL statement against a module's allowed schemas.
    /// </summary>
    /// <param name="moduleName">The name of the module executing the SQL.</param>
    /// <param name="sql">The SQL statement to validate.</param>
    /// <returns>
    /// A validation result indicating whether the SQL accesses only allowed schemas.
    /// </returns>
    SchemaAccessValidationResult ValidateSqlAccess(string moduleName, string sql);
}

/// <summary>
/// Result of validating SQL schema access.
/// </summary>
/// <param name="IsValid">Whether all accessed schemas are allowed.</param>
/// <param name="AccessedSchemas">The schemas referenced in the SQL.</param>
/// <param name="UnauthorizedSchemas">The schemas that are not allowed for the module.</param>
/// <param name="AllowedSchemas">The schemas the module is allowed to access.</param>
public readonly record struct SchemaAccessValidationResult(
    bool IsValid,
    IReadOnlySet<string> AccessedSchemas,
    IReadOnlySet<string> UnauthorizedSchemas,
    IReadOnlySet<string> AllowedSchemas)
{
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static SchemaAccessValidationResult Success(
        IReadOnlySet<string> accessedSchemas,
        IReadOnlySet<string> allowedSchemas)
        => new(true, accessedSchemas, ImmutableHashSetCompat.Empty, allowedSchemas);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static SchemaAccessValidationResult Failure(
        IReadOnlySet<string> accessedSchemas,
        IReadOnlySet<string> unauthorizedSchemas,
        IReadOnlySet<string> allowedSchemas)
        => new(false, accessedSchemas, unauthorizedSchemas, allowedSchemas);
}

/// <summary>
/// Compatibility helper for ImmutableHashSet.
/// </summary>
internal static class ImmutableHashSetCompat
{
    public static IReadOnlySet<string> Empty { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
