namespace Encina.Modules.Isolation;

/// <summary>
/// Defines the strategy for enforcing module isolation at the database level.
/// </summary>
/// <remarks>
/// <para>
/// Module isolation ensures that modules cannot directly access each other's data,
/// forcing communication through well-defined interfaces (commands, queries, events).
/// </para>
/// <para>
/// The strategy chosen affects both development-time validation and production behavior:
/// <list type="bullet">
/// <item><description><see cref="DevelopmentValidationOnly"/>: Validates SQL queries without real DB permissions (default)</description></item>
/// <item><description><see cref="SchemaWithPermissions"/>: Uses real database users with limited schema permissions</description></item>
/// <item><description><see cref="ConnectionPerModule"/>: Each module uses a separate connection string</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.ModuleIsolation.Strategy = ModuleIsolationStrategy.SchemaWithPermissions;
/// });
/// </code>
/// </example>
public enum ModuleIsolationStrategy
{
    /// <summary>
    /// Development-time validation only. No real database users or permissions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the default strategy, suitable for development and testing environments.
    /// </para>
    /// <para>
    /// An interceptor validates SQL statements against module boundaries at runtime,
    /// throwing <see cref="ModuleIsolationViolationException"/> if a module attempts
    /// to access schemas it doesn't own.
    /// </para>
    /// <para>
    /// This strategy provides fast feedback without requiring database configuration.
    /// </para>
    /// </remarks>
    DevelopmentValidationOnly = 0,

    /// <summary>
    /// Real database users with schema-level permissions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each module has a dedicated database user with permissions limited to its own schema.
    /// Shared schemas (lookup tables, etc.) are granted SELECT-only access to all module users.
    /// </para>
    /// <para>
    /// This strategy provides defense-in-depth: even if application code has a bug,
    /// the database rejects unauthorized access attempts.
    /// </para>
    /// <para>
    /// Requires running permission setup scripts to create users and grant permissions.
    /// Use <see cref="ModuleIsolationOptions.GeneratePermissionScripts"/> to generate these scripts.
    /// </para>
    /// </remarks>
    SchemaWithPermissions = 1,

    /// <summary>
    /// Each module uses a completely separate connection string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provides the strongest isolation by using different database connections per module.
    /// Each module's connection string points to a user with permissions for that module's schema only.
    /// </para>
    /// <para>
    /// This strategy is useful when preparing for microservice extraction,
    /// as it mirrors the architecture where each service has its own database credentials.
    /// </para>
    /// <para>
    /// Connection strings are resolved via a module connection string provider
    /// based on the current module context.
    /// </para>
    /// </remarks>
    ConnectionPerModule = 2
}
