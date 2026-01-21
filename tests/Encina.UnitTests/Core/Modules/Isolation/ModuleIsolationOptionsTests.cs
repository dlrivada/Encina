using Encina.Modules.Isolation;

namespace Encina.UnitTests.Core.Modules.Isolation;

/// <summary>
/// Unit tests for <see cref="ModuleIsolationOptions"/>.
/// </summary>
public class ModuleIsolationOptionsTests
{
    [Fact]
    public void DefaultStrategy_ShouldBeDevelopmentValidationOnly()
    {
        // Arrange & Act
        var options = new ModuleIsolationOptions();

        // Assert
        options.Strategy.ShouldBe(ModuleIsolationStrategy.DevelopmentValidationOnly);
    }

    [Fact]
    public void SharedSchemas_ShouldBeEmptyByDefault()
    {
        // Arrange & Act
        var options = new ModuleIsolationOptions();

        // Assert
        options.SharedSchemas.ShouldBeEmpty();
    }

    [Fact]
    public void ModuleSchemas_ShouldBeEmptyByDefault()
    {
        // Arrange & Act
        var options = new ModuleIsolationOptions();

        // Assert
        options.ModuleSchemas.ShouldBeEmpty();
    }

    [Fact]
    public void GeneratePermissionScripts_ShouldBeFalseByDefault()
    {
        // Arrange & Act
        var options = new ModuleIsolationOptions();

        // Assert
        options.GeneratePermissionScripts.ShouldBeFalse();
    }

    [Fact]
    public void PermissionScriptsOutputPath_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var options = new ModuleIsolationOptions();

        // Assert
        options.PermissionScriptsOutputPath.ShouldBeNull();
    }

    [Fact]
    public void AddSharedSchemas_ShouldAddSchemasToCollection()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        options.AddSharedSchemas("shared", "lookup", "common");

        // Assert
        options.SharedSchemas.Count.ShouldBe(3);
        options.SharedSchemas.ShouldContain("shared");
        options.SharedSchemas.ShouldContain("lookup");
        options.SharedSchemas.ShouldContain("common");
    }

    [Fact]
    public void AddSharedSchemas_ShouldIgnoreNullOrWhitespace()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        options.AddSharedSchemas("shared", null!, "", "  ", "lookup");

        // Assert
        options.SharedSchemas.Count.ShouldBe(2);
        options.SharedSchemas.ShouldContain("shared");
        options.SharedSchemas.ShouldContain("lookup");
    }

    [Fact]
    public void AddSharedSchemas_ShouldReturnSameInstanceForFluent()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        var result = options.AddSharedSchemas("shared");

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddModuleSchema_ShouldAddModuleToCollection()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        var moduleSchema = new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user"
        };

        // Act
        options.AddModuleSchema(moduleSchema);

        // Assert
        options.ModuleSchemas.Count.ShouldBe(1);
        options.ModuleSchemas[0].ModuleName.ShouldBe("Orders");
        options.ModuleSchemas[0].SchemaName.ShouldBe("orders");
    }

    [Fact]
    public void AddModuleSchema_ShouldThrowWhenNull()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.AddModuleSchema(null!));
    }

    [Fact]
    public void AddModuleSchema_ShouldThrowWhenDuplicateModuleName()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders2" }));

        ex.Message.ShouldContain("Orders");
        ex.Message.ShouldContain("already configured");
    }

    [Fact]
    public void AddModuleSchema_ShouldBeCaseInsensitiveForDuplicateCheck()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "orders", SchemaName = "orders" });

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "ORDERS", SchemaName = "orders2" }));
    }

    [Fact]
    public void AddModuleSchema_WithBuilder_ShouldConfigureModule()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        options.AddModuleSchema("Orders", "orders", builder =>
        {
            builder.WithDatabaseUser("orders_user");
            builder.WithAdditionalAllowedSchemas("audit", "logging");
        });

        // Assert
        options.ModuleSchemas.Count.ShouldBe(1);
        var module = options.ModuleSchemas[0];
        module.ModuleName.ShouldBe("Orders");
        module.SchemaName.ShouldBe("orders");
        module.DatabaseUser.ShouldBe("orders_user");
        module.AdditionalAllowedSchemas.ShouldContain("audit");
        module.AdditionalAllowedSchemas.ShouldContain("logging");
    }

    [Fact]
    public void GetModuleSchema_ShouldReturnMatchingModule()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });

        // Act
        var result = options.GetModuleSchema("Orders");

        // Assert
        result.ShouldNotBeNull();
        result.ModuleName.ShouldBe("Orders");
    }

    [Fact]
    public void GetModuleSchema_ShouldBeCaseInsensitive()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });

        // Act
        var result = options.GetModuleSchema("ORDERS");

        // Assert
        result.ShouldNotBeNull();
        result.ModuleName.ShouldBe("Orders");
    }

    [Fact]
    public void GetModuleSchema_ShouldReturnNullForUnknownModule()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        var result = options.GetModuleSchema("Unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void HasModuleSchema_ShouldReturnTrueForKnownModule()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });

        // Act
        var result = options.HasModuleSchema("Orders");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasModuleSchema_ShouldReturnFalseForUnknownModule()
    {
        // Arrange
        var options = new ModuleIsolationOptions();

        // Act
        var result = options.HasModuleSchema("Unknown");

        // Assert
        result.ShouldBeFalse();
    }
}
