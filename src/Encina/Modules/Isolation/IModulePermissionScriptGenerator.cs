namespace Encina.Modules.Isolation;

/// <summary>
/// Generates database permission scripts for module isolation.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface generate provider-specific SQL scripts
/// for setting up database-level module isolation. Each provider (SQL Server,
/// PostgreSQL, etc.) has its own implementation with dialect-specific syntax.
/// </para>
/// <para>
/// The generated scripts are idempotent - they can be run multiple times
/// without causing errors (using IF NOT EXISTS, CREATE OR ALTER, etc.).
/// </para>
/// <para>
/// Scripts are typically generated during development/deployment and executed
/// by DBAs or migration tools, not at application runtime.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Generate all permission scripts
/// var scripts = generator.GenerateAllScripts(options);
/// foreach (var script in scripts)
/// {
///     Console.WriteLine($"-- {script.Name}");
///     Console.WriteLine(script.Content);
/// }
///
/// // Or generate specific script types
/// var schemaScript = generator.GenerateSchemaCreationScript(options);
/// var grantScript = generator.GenerateGrantPermissionsScript(options);
/// </code>
/// </example>
public interface IModulePermissionScriptGenerator
{
    /// <summary>
    /// Gets the database provider name (e.g., "SqlServer", "PostgreSql").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Generates a script to create schemas for all configured modules.
    /// </summary>
    /// <param name="options">The module isolation configuration.</param>
    /// <returns>A script that creates all required schemas.</returns>
    /// <remarks>
    /// <para>
    /// The script uses idempotent statements (CREATE SCHEMA IF NOT EXISTS)
    /// so it can be run safely multiple times.
    /// </para>
    /// <para>
    /// Creates schemas for:
    /// <list type="bullet">
    /// <item><description>Each module's own schema</description></item>
    /// <item><description>All shared schemas</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    PermissionScript GenerateSchemaCreationScript(ModuleIsolationOptions options);

    /// <summary>
    /// Generates a script to create database users for all configured modules.
    /// </summary>
    /// <param name="options">The module isolation configuration.</param>
    /// <returns>A script that creates all required database users.</returns>
    /// <remarks>
    /// <para>
    /// The script creates a dedicated database user for each module that has
    /// a <see cref="ModuleSchemaOptions.DatabaseUser"/> configured.
    /// </para>
    /// <para>
    /// For SQL Server, this creates both a LOGIN and a USER.
    /// For PostgreSQL, this creates a ROLE with LOGIN capability.
    /// </para>
    /// <para>
    /// Passwords are generated as placeholders (e.g., '{{Orders_Password}}')
    /// that should be replaced with actual values before execution.
    /// </para>
    /// </remarks>
    PermissionScript GenerateUserCreationScript(ModuleIsolationOptions options);

    /// <summary>
    /// Generates a script to grant permissions to module users.
    /// </summary>
    /// <param name="options">The module isolation configuration.</param>
    /// <returns>A script that grants all required permissions.</returns>
    /// <remarks>
    /// <para>
    /// Grants the following permissions:
    /// <list type="bullet">
    /// <item><description>Full access (SELECT, INSERT, UPDATE, DELETE) on the module's own schema</description></item>
    /// <item><description>SELECT-only on shared schemas</description></item>
    /// <item><description>SELECT-only on additional allowed schemas</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Also grants EXECUTE on stored procedures and functions where applicable.
    /// </para>
    /// </remarks>
    PermissionScript GenerateGrantPermissionsScript(ModuleIsolationOptions options);

    /// <summary>
    /// Generates a script to revoke all permissions from module users.
    /// </summary>
    /// <param name="options">The module isolation configuration.</param>
    /// <returns>A script that revokes all permissions.</returns>
    /// <remarks>
    /// <para>
    /// This script is useful for cleanup or when reconfiguring permissions.
    /// It revokes all previously granted permissions without dropping users.
    /// </para>
    /// <para>
    /// Execute this before <see cref="GenerateGrantPermissionsScript"/> when
    /// changing permission configurations to ensure clean state.
    /// </para>
    /// </remarks>
    PermissionScript GenerateRevokePermissionsScript(ModuleIsolationOptions options);

    /// <summary>
    /// Generates all permission scripts in the correct execution order.
    /// </summary>
    /// <param name="options">The module isolation configuration.</param>
    /// <returns>
    /// An enumerable of scripts in the order they should be executed:
    /// schemas, users, grants.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The scripts are returned in dependency order:
    /// <list type="number">
    /// <item><description>Schema creation (required before users can be granted schema access)</description></item>
    /// <item><description>User creation (required before permissions can be granted)</description></item>
    /// <item><description>Permission grants (depends on schemas and users existing)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    IEnumerable<PermissionScript> GenerateAllScripts(ModuleIsolationOptions options);

    /// <summary>
    /// Generates a script to grant permissions for a single module.
    /// </summary>
    /// <param name="moduleOptions">The module's schema options.</param>
    /// <param name="sharedSchemas">The shared schemas to grant SELECT access to.</param>
    /// <returns>A script that grants permissions for the specified module.</returns>
    /// <remarks>
    /// Useful when adding a new module to an existing system without
    /// regenerating all permissions.
    /// </remarks>
    PermissionScript GenerateModulePermissionsScript(
        ModuleSchemaOptions moduleOptions,
        IEnumerable<string> sharedSchemas);
}

/// <summary>
/// Represents a generated permission SQL script.
/// </summary>
/// <param name="Name">A descriptive name for the script (e.g., "001_create_schemas.sql").</param>
/// <param name="Description">A human-readable description of what the script does.</param>
/// <param name="Content">The SQL script content.</param>
/// <param name="Order">The execution order (lower numbers execute first).</param>
public readonly record struct PermissionScript(
    string Name,
    string Description,
    string Content,
    int Order)
{
    /// <summary>
    /// Creates a schema creation script.
    /// </summary>
    public static PermissionScript ForSchemaCreation(string content)
        => new("001_create_schemas.sql", "Creates database schemas for modules", content, 1);

    /// <summary>
    /// Creates a user creation script.
    /// </summary>
    public static PermissionScript ForUserCreation(string content)
        => new("002_create_users.sql", "Creates database users/logins for modules", content, 2);

    /// <summary>
    /// Creates a grant permissions script.
    /// </summary>
    public static PermissionScript ForGrantPermissions(string content)
        => new("003_grant_permissions.sql", "Grants schema permissions to module users", content, 3);

    /// <summary>
    /// Creates a revoke permissions script.
    /// </summary>
    public static PermissionScript ForRevokePermissions(string content)
        => new("000_revoke_permissions.sql", "Revokes all module permissions (cleanup)", content, 0);

    /// <summary>
    /// Creates a module-specific permissions script.
    /// </summary>
    public static PermissionScript ForModule(string moduleName, string content)
        => new($"module_{moduleName.ToLowerInvariant()}_permissions.sql",
               $"Permissions for {moduleName} module",
               content,
               10);
}
