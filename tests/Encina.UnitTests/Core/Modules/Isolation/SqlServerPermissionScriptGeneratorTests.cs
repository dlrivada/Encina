using Encina.Modules.Isolation;
using VerifyXunit;

namespace Encina.UnitTests.Core.Modules.Isolation;

/// <summary>
/// Unit tests for <see cref="SqlServerPermissionScriptGenerator"/>.
/// Uses snapshot testing to verify generated SQL scripts.
/// </summary>
public class SqlServerPermissionScriptGeneratorTests
{
    private readonly SqlServerPermissionScriptGenerator _generator = new();

    #region ProviderName

    [Fact]
    public void ProviderName_ShouldBeSqlServer()
    {
        _generator.ProviderName.ShouldBe("SqlServer");
    }

    #endregion

    #region GenerateSchemaCreationScript

    [Fact]
    public Task GenerateSchemaCreationScript_SingleModule_ShouldGenerateCorrectScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders"
        });

        // Act
        var script = _generator.GenerateSchemaCreationScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public Task GenerateSchemaCreationScript_MultipleModulesWithSharedSchemas_ShouldGenerateCorrectScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup");
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders"
        });
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Payments",
            SchemaName = "payments"
        });

        // Act
        var script = _generator.GenerateSchemaCreationScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public void GenerateSchemaCreationScript_ShouldHaveCorrectOrder()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Test", SchemaName = "test" });

        // Act
        var script = _generator.GenerateSchemaCreationScript(options);

        // Assert
        script.Order.ShouldBe(1);
        script.Name.ShouldBe("001_create_schemas.sql");
    }

    [Fact]
    public void GenerateSchemaCreationScript_NullOptions_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _generator.GenerateSchemaCreationScript(null!));
    }

    #endregion

    #region GenerateUserCreationScript

    [Fact]
    public Task GenerateUserCreationScript_ModuleWithDatabaseUser_ShouldGenerateCorrectScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user"
        });

        // Act
        var script = _generator.GenerateUserCreationScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public Task GenerateUserCreationScript_MultipleModulesWithUsers_ShouldGenerateCorrectScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user"
        });
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Payments",
            SchemaName = "payments",
            DatabaseUser = "payments_user"
        });

        // Act
        var script = _generator.GenerateUserCreationScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public Task GenerateUserCreationScript_NoModulesWithUsers_ShouldGenerateEmptyScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders"
            // No DatabaseUser
        });

        // Act
        var script = _generator.GenerateUserCreationScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public void GenerateUserCreationScript_ShouldHaveCorrectOrder()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        var script = _generator.GenerateUserCreationScript(options);

        // Assert
        script.Order.ShouldBe(2);
        script.Name.ShouldBe("002_create_users.sql");
    }

    [Fact]
    public void GenerateUserCreationScript_NullOptions_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _generator.GenerateUserCreationScript(null!));
    }

    #endregion

    #region GenerateGrantPermissionsScript

    [Fact]
    public Task GenerateGrantPermissionsScript_FullConfiguration_ShouldGenerateCorrectScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup");
        options.AddModuleSchema("Orders", "orders", builder =>
            builder.WithDatabaseUser("orders_user")
                   .WithAdditionalAllowedSchemas("audit"));

        // Act
        var script = _generator.GenerateGrantPermissionsScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public Task GenerateGrantPermissionsScript_NoModulesWithUsers_ShouldGenerateEmptyScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders"
        });

        // Act
        var script = _generator.GenerateGrantPermissionsScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public void GenerateGrantPermissionsScript_ShouldHaveCorrectOrder()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        var script = _generator.GenerateGrantPermissionsScript(options);

        // Assert
        script.Order.ShouldBe(3);
        script.Name.ShouldBe("003_grant_permissions.sql");
    }

    [Fact]
    public void GenerateGrantPermissionsScript_NullOptions_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _generator.GenerateGrantPermissionsScript(null!));
    }

    #endregion

    #region GenerateRevokePermissionsScript

    [Fact]
    public Task GenerateRevokePermissionsScript_FullConfiguration_ShouldGenerateCorrectScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup");
        options.AddModuleSchema("Orders", "orders", builder =>
            builder.WithDatabaseUser("orders_user")
                   .WithAdditionalAllowedSchemas("audit"));

        // Act
        var script = _generator.GenerateRevokePermissionsScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public Task GenerateRevokePermissionsScript_NoModulesWithUsers_ShouldGenerateEmptyScript()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        var script = _generator.GenerateRevokePermissionsScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public void GenerateRevokePermissionsScript_ShouldHaveCorrectOrder()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        var script = _generator.GenerateRevokePermissionsScript(options);

        // Assert
        script.Order.ShouldBe(0);
        script.Name.ShouldBe("000_revoke_permissions.sql");
    }

    [Fact]
    public void GenerateRevokePermissionsScript_NullOptions_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _generator.GenerateRevokePermissionsScript(null!));
    }

    #endregion

    #region GenerateAllScripts

    [Fact]
    public void GenerateAllScripts_ShouldReturnThreeScriptsInOrder()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user"
        });

        // Act
        var scripts = _generator.GenerateAllScripts(options).ToList();

        // Assert
        scripts.Count.ShouldBe(3);
        scripts[0].Order.ShouldBe(1);
        scripts[1].Order.ShouldBe(2);
        scripts[2].Order.ShouldBe(3);
    }

    [Fact]
    public void GenerateAllScripts_NullOptions_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _generator.GenerateAllScripts(null!).ToList());
    }

    #endregion

    #region GenerateModulePermissionsScript

    [Fact]
    public Task GenerateModulePermissionsScript_FullConfiguration_ShouldGenerateCorrectScript()
    {
        // Arrange
        var moduleOptions = new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user",
            AdditionalAllowedSchemas = ["audit"]
        };

        var sharedSchemas = new[] { "shared", "lookup" };

        // Act
        var script = _generator.GenerateModulePermissionsScript(moduleOptions, sharedSchemas);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public Task GenerateModulePermissionsScript_NoDatabaseUser_ShouldGenerateMinimalScript()
    {
        // Arrange
        var moduleOptions = new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders"
        };

        var sharedSchemas = Array.Empty<string>();

        // Act
        var script = _generator.GenerateModulePermissionsScript(moduleOptions, sharedSchemas);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public void GenerateModulePermissionsScript_NullModuleOptions_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            _generator.GenerateModulePermissionsScript(null!, Array.Empty<string>()));
    }

    [Fact]
    public void GenerateModulePermissionsScript_NullSharedSchemas_ShouldThrow()
    {
        var moduleOptions = new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders"
        };

        Should.Throw<ArgumentNullException>(() =>
            _generator.GenerateModulePermissionsScript(moduleOptions, null!));
    }

    #endregion

    #region SQL Escaping

    [Fact]
    public Task GenerateSchemaCreationScript_SchemaWithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "My]Module",
            SchemaName = "my]schema"
        });

        // Act
        var script = _generator.GenerateSchemaCreationScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    [Fact]
    public Task GenerateUserCreationScript_UserWithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Test'Module",
            SchemaName = "test",
            DatabaseUser = "test']user"
        });

        // Act
        var script = _generator.GenerateUserCreationScript(options);

        // Assert
        return Verifier.Verify(script.Content);
    }

    #endregion
}
