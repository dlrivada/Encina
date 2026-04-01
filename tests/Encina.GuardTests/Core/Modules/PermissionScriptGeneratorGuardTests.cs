using Encina.Modules.Isolation;

namespace Encina.GuardTests.Core.Modules;

/// <summary>
/// Guard clause tests for <see cref="PostgreSqlPermissionScriptGenerator"/>
/// and <see cref="SqlServerPermissionScriptGenerator"/>.
/// Verifies null parameter handling for all Generate* methods.
/// </summary>
public sealed class PermissionScriptGeneratorGuardTests
{
    private static readonly string[] SharedSchemaArray = ["shared"];
    private static readonly string[] EmptyStringArray = [];
    #region PostgreSQL Generator Guards

    /// <summary>
    /// Verifies that PostgreSQL GenerateSchemaCreationScript throws when options is null.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateSchemaCreationScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateSchemaCreationScript(null!));
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateUserCreationScript throws when options is null.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateUserCreationScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateUserCreationScript(null!));
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateGrantPermissionsScript throws when options is null.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateGrantPermissionsScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateGrantPermissionsScript(null!));
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateRevokePermissionsScript throws when options is null.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateRevokePermissionsScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateRevokePermissionsScript(null!));
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateAllScripts throws when options is null.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateAllScripts_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateAllScripts(null!).ToList());
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateModulePermissionsScript throws when moduleOptions is null.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateModulePermissionsScript_NullModuleOptions_ThrowsArgumentNullException()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateModulePermissionsScript(null!, SharedSchemaArray));
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateModulePermissionsScript throws when sharedSchemas is null.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateModulePermissionsScript_NullSharedSchemas_ThrowsArgumentNullException()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var moduleOptions = CreateTestModuleOptions();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateModulePermissionsScript(moduleOptions, null!));
    }

    /// <summary>
    /// Verifies that PostgreSQL ProviderName is correct.
    /// </summary>
    [Fact]
    public void PostgreSql_ProviderName_IsPostgreSql()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        generator.ProviderName.ShouldBe("PostgreSql");
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateSchemaCreationScript produces valid output for empty options.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateSchemaCreationScript_EmptyOptions_ProducesScript()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var options = new ModuleIsolationOptions();

        var script = generator.GenerateSchemaCreationScript(options);

        script.Content.ShouldNotBeNullOrWhiteSpace();
        script.Content.ShouldContain("PostgreSQL");
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateUserCreationScript handles no users gracefully.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateUserCreationScript_NoUsers_ProducesNotice()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });

        var script = generator.GenerateUserCreationScript(options);

        script.Content.ShouldContain("No module roles configured");
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateModulePermissionsScript handles no database user.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateModulePermissionsScript_NoDatabaseUser_ProducesNotice()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var moduleOptions = new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" };

        var script = generator.GenerateModulePermissionsScript(moduleOptions, EmptyStringArray);

        script.Content.ShouldContain("No database role configured");
    }

    /// <summary>
    /// Verifies that PostgreSQL GenerateAllScripts returns exactly 3 scripts.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateAllScripts_Returns3Scripts()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var options = CreateFullOptions();

        var scripts = generator.GenerateAllScripts(options).ToList();

        scripts.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies PostgreSQL generates correct permissions for module with shared schemas.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateGrantPermissionsScript_WithSharedSchemas_IncludesReadOnlyGrants()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var options = CreateFullOptions();

        var script = generator.GenerateGrantPermissionsScript(options);

        script.Content.ShouldContain("GRANT SELECT ON ALL TABLES IN SCHEMA");
        script.Content.ShouldContain("Read-only access to shared schema");
    }

    /// <summary>
    /// Verifies PostgreSQL revoke script includes shared schema revocations.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateRevokePermissionsScript_WithSharedSchemas_IncludesRevokes()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var options = CreateFullOptions();

        var script = generator.GenerateRevokePermissionsScript(options);

        script.Content.ShouldContain("REVOKE");
    }

    #endregion

    #region SQL Server Generator Guards

    /// <summary>
    /// Verifies that SQL Server GenerateSchemaCreationScript throws when options is null.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateSchemaCreationScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new SqlServerPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateSchemaCreationScript(null!));
    }

    /// <summary>
    /// Verifies that SQL Server GenerateUserCreationScript throws when options is null.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateUserCreationScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new SqlServerPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateUserCreationScript(null!));
    }

    /// <summary>
    /// Verifies that SQL Server GenerateGrantPermissionsScript throws when options is null.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateGrantPermissionsScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new SqlServerPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateGrantPermissionsScript(null!));
    }

    /// <summary>
    /// Verifies that SQL Server GenerateRevokePermissionsScript throws when options is null.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateRevokePermissionsScript_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new SqlServerPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateRevokePermissionsScript(null!));
    }

    /// <summary>
    /// Verifies that SQL Server GenerateAllScripts throws when options is null.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateAllScripts_NullOptions_ThrowsArgumentNullException()
    {
        var generator = new SqlServerPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateAllScripts(null!).ToList());
    }

    /// <summary>
    /// Verifies that SQL Server GenerateModulePermissionsScript throws when moduleOptions is null.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateModulePermissionsScript_NullModuleOptions_ThrowsArgumentNullException()
    {
        var generator = new SqlServerPermissionScriptGenerator();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateModulePermissionsScript(null!, SharedSchemaArray));
    }

    /// <summary>
    /// Verifies that SQL Server GenerateModulePermissionsScript throws when sharedSchemas is null.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateModulePermissionsScript_NullSharedSchemas_ThrowsArgumentNullException()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var moduleOptions = CreateTestModuleOptions();

        Should.Throw<ArgumentNullException>(() =>
            generator.GenerateModulePermissionsScript(moduleOptions, null!));
    }

    /// <summary>
    /// Verifies that SQL Server ProviderName is correct.
    /// </summary>
    [Fact]
    public void SqlServer_ProviderName_IsSqlServer()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        generator.ProviderName.ShouldBe("SqlServer");
    }

    /// <summary>
    /// Verifies that SQL Server GenerateSchemaCreationScript produces valid output for empty options.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateSchemaCreationScript_EmptyOptions_ProducesScript()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var options = new ModuleIsolationOptions();

        var script = generator.GenerateSchemaCreationScript(options);

        script.Content.ShouldNotBeNullOrWhiteSpace();
        script.Content.ShouldContain("SQL Server");
    }

    /// <summary>
    /// Verifies that SQL Server GenerateUserCreationScript handles no users gracefully.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateUserCreationScript_NoUsers_ProducesNotice()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });

        var script = generator.GenerateUserCreationScript(options);

        script.Content.ShouldContain("No module users configured");
    }

    /// <summary>
    /// Verifies that SQL Server GenerateModulePermissionsScript handles no database user.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateModulePermissionsScript_NoDatabaseUser_ProducesNotice()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var moduleOptions = new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" };

        var script = generator.GenerateModulePermissionsScript(moduleOptions, EmptyStringArray);

        script.Content.ShouldContain("No database user configured");
    }

    /// <summary>
    /// Verifies that SQL Server GenerateAllScripts returns exactly 3 scripts.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateAllScripts_Returns3Scripts()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var options = CreateFullOptions();

        var scripts = generator.GenerateAllScripts(options).ToList();

        scripts.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies SQL Server generates correct permissions for module with shared schemas.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateGrantPermissionsScript_WithSharedSchemas_IncludesGrants()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var options = CreateFullOptions();

        var script = generator.GenerateGrantPermissionsScript(options);

        script.Content.ShouldContain("GRANT SELECT ON SCHEMA");
        script.Content.ShouldContain("Read-only access to shared schema");
    }

    /// <summary>
    /// Verifies SQL Server revoke script includes shared schema revocations.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateRevokePermissionsScript_WithSharedSchemas_IncludesRevokes()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var options = CreateFullOptions();

        var script = generator.GenerateRevokePermissionsScript(options);

        script.Content.ShouldContain("REVOKE");
    }

    /// <summary>
    /// Verifies SQL Server generates login and user creation for modules with database users.
    /// </summary>
    [Fact]
    public void SqlServer_GenerateUserCreationScript_WithUser_CreatesLoginAndUser()
    {
        var generator = new SqlServerPermissionScriptGenerator();
        var options = CreateFullOptions();

        var script = generator.GenerateUserCreationScript(options);

        script.Content.ShouldContain("CREATE LOGIN");
        script.Content.ShouldContain("CREATE USER");
    }

    /// <summary>
    /// Verifies PostgreSQL module script with shared schemas includes ALTER DEFAULT PRIVILEGES.
    /// </summary>
    [Fact]
    public void PostgreSql_GenerateModulePermissionsScript_WithUser_IncludesAlterDefaultPrivileges()
    {
        var generator = new PostgreSqlPermissionScriptGenerator();
        var moduleOptions = CreateTestModuleOptions();

        var script = generator.GenerateModulePermissionsScript(moduleOptions, SharedSchemaArray);

        script.Content.ShouldContain("ALTER DEFAULT PRIVILEGES");
    }

    #endregion

    #region Test Helpers

    private static ModuleSchemaOptions CreateTestModuleOptions() =>
        new()
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user",
        };

    private static ModuleIsolationOptions CreateFullOptions()
    {
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup");
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user",
            AdditionalAllowedSchemas = ["extra"],
        });
        return options;
    }

    #endregion
}
