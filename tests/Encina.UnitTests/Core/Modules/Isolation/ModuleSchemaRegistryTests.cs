using Encina.Modules.Isolation;

namespace Encina.UnitTests.Core.Modules.Isolation;

/// <summary>
/// Unit tests for <see cref="ModuleSchemaRegistry"/>.
/// </summary>
public class ModuleSchemaRegistryTests
{
    #region GetAllowedSchemas

    [Fact]
    public void GetAllowedSchemas_RegisteredModule_ShouldReturnOwnSchemaAndSharedAndAdditional()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup");
        options.AddModuleSchema("Orders", "orders", builder =>
            builder.WithAdditionalAllowedSchemas("audit"));

        var registry = new ModuleSchemaRegistry(options);

        // Act
        var schemas = registry.GetAllowedSchemas("Orders");

        // Assert
        schemas.Count.ShouldBe(4);
        schemas.ShouldContain("orders");  // own schema
        schemas.ShouldContain("shared");  // shared
        schemas.ShouldContain("lookup");  // shared
        schemas.ShouldContain("audit");   // additional
    }

    [Fact]
    public void GetAllowedSchemas_UnregisteredModule_ShouldReturnOnlySharedSchemas()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup");
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });

        var registry = new ModuleSchemaRegistry(options);

        // Act
        var schemas = registry.GetAllowedSchemas("Unknown");

        // Assert
        schemas.Count.ShouldBe(2);
        schemas.ShouldContain("shared");
        schemas.ShouldContain("lookup");
    }

    [Fact]
    public void GetAllowedSchemas_NullModuleName_ShouldReturnEmpty()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var schemas = registry.GetAllowedSchemas(null!);

        // Assert
        schemas.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllowedSchemas_EmptyModuleName_ShouldReturnEmpty()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var schemas = registry.GetAllowedSchemas("");

        // Assert
        schemas.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllowedSchemas_ShouldBeCaseInsensitive()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var schemas = registry.GetAllowedSchemas("ORDERS");

        // Assert
        schemas.ShouldContain("orders");
    }

    #endregion

    #region GetModuleOptions

    [Fact]
    public void GetModuleOptions_RegisteredModule_ShouldReturnOptions()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            DatabaseUser = "orders_user"
        });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.GetModuleOptions("Orders");

        // Assert
        result.ShouldNotBeNull();
        result.ModuleName.ShouldBe("Orders");
        result.SchemaName.ShouldBe("orders");
        result.DatabaseUser.ShouldBe("orders_user");
    }

    [Fact]
    public void GetModuleOptions_UnregisteredModule_ShouldReturnNull()
    {
        // Arrange
        var registry = new ModuleSchemaRegistry();

        // Act
        var result = registry.GetModuleOptions("Unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModuleOptions_NullModuleName_ShouldReturnNull()
    {
        // Arrange
        var registry = new ModuleSchemaRegistry();

        // Act
        var result = registry.GetModuleOptions(null!);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region CanAccessSchema

    [Fact]
    public void CanAccessSchema_OwnSchema_ShouldReturnTrue()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.CanAccessSchema("Orders", "orders");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessSchema_SharedSchema_ShouldReturnTrue()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.CanAccessSchema("Orders", "shared");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessSchema_SharedSchemaWithoutModule_ShouldReturnTrue()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.CanAccessSchema(null!, "shared");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessSchema_AdditionalAllowedSchema_ShouldReturnTrue()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema("Orders", "orders", builder =>
            builder.WithAdditionalAllowedSchemas("audit"));
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.CanAccessSchema("Orders", "audit");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessSchema_UnauthorizedSchema_ShouldReturnFalse()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.CanAccessSchema("Orders", "payments");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAccessSchema_UnregisteredModule_ShouldReturnFalseForNonSharedSchema()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.CanAccessSchema("Unknown", "orders");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAccessSchema_NullOrWhitespaceSchema_ShouldReturnTrue()
    {
        // Arrange
        var registry = new ModuleSchemaRegistry();

        // Act & Assert
        registry.CanAccessSchema("Orders", null!).ShouldBeTrue();
        registry.CanAccessSchema("Orders", "").ShouldBeTrue();
        registry.CanAccessSchema("Orders", "  ").ShouldBeTrue();
    }

    [Fact]
    public void CanAccessSchema_ShouldBeCaseInsensitive()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.CanAccessSchema("ORDERS", "ORDERS");

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region GetRegisteredModules

    [Fact]
    public void GetRegisteredModules_ShouldReturnAllModuleNames()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Payments", SchemaName = "payments" });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var modules = registry.GetRegisteredModules().ToList();

        // Assert
        modules.Count.ShouldBe(2);
        modules.ShouldContain("Orders");
        modules.ShouldContain("Payments");
    }

    [Fact]
    public void GetRegisteredModules_EmptyRegistry_ShouldReturnEmpty()
    {
        // Arrange
        var registry = new ModuleSchemaRegistry();

        // Act
        var modules = registry.GetRegisteredModules().ToList();

        // Assert
        modules.ShouldBeEmpty();
    }

    #endregion

    #region GetSharedSchemas

    [Fact]
    public void GetSharedSchemas_ShouldReturnAllSharedSchemas()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup", "common");
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var schemas = registry.GetSharedSchemas();

        // Assert
        schemas.Count.ShouldBe(3);
        schemas.ShouldContain("shared");
        schemas.ShouldContain("lookup");
        schemas.ShouldContain("common");
    }

    [Fact]
    public void GetSharedSchemas_NoSharedSchemas_ShouldReturnEmpty()
    {
        // Arrange
        var registry = new ModuleSchemaRegistry();

        // Act
        var schemas = registry.GetSharedSchemas();

        // Assert
        schemas.ShouldBeEmpty();
    }

    #endregion

    #region IsModuleRegistered

    [Fact]
    public void IsModuleRegistered_RegisteredModule_ShouldReturnTrue()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Act
        var result = registry.IsModuleRegistered("Orders");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsModuleRegistered_UnregisteredModule_ShouldReturnFalse()
    {
        // Arrange
        var registry = new ModuleSchemaRegistry();

        // Act
        var result = registry.IsModuleRegistered("Unknown");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsModuleRegistered_NullOrWhitespace_ShouldReturnFalse()
    {
        // Arrange
        var registry = new ModuleSchemaRegistry();

        // Act & Assert
        registry.IsModuleRegistered(null!).ShouldBeFalse();
        registry.IsModuleRegistered("").ShouldBeFalse();
        registry.IsModuleRegistered("  ").ShouldBeFalse();
    }

    #endregion

    #region ValidateSqlAccess

    [Fact]
    public void ValidateSqlAccess_AllowedSchemas_ShouldReturnSuccess()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Use SQL without alias.column patterns to test schema validation specifically
        var sql = "SELECT * FROM orders.Orders";

        // Act
        var result = registry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.UnauthorizedSchemas.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateSqlAccess_UnauthorizedSchema_ShouldReturnFailure()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Use SQL without alias.column patterns
        var sql = "SELECT * FROM orders.Orders JOIN payments.Payments ON orders.Orders.Id = payments.Payments.OrderId";

        // Act
        var result = registry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("payments");
    }

    [Fact]
    public void ValidateSqlAccess_NoSchemas_ShouldReturnSuccess()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        var sql = "SELECT * FROM Orders";

        // Act
        var result = registry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSqlAccess_ResultShouldContainAccessedSchemas()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        // Use SQL without alias.column patterns
        var sql = "SELECT * FROM orders.Orders JOIN shared.Statuses ON orders.Orders.StatusId = shared.Statuses.Id";

        // Act
        var result = registry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.AccessedSchemas.ShouldContain("orders");
        result.AccessedSchemas.ShouldContain("shared");
    }

    [Fact]
    public void ValidateSqlAccess_ResultShouldContainAllowedSchemas()
    {
        // Arrange
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared");
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        var registry = new ModuleSchemaRegistry(options);

        var sql = "SELECT * FROM orders.Orders";

        // Act
        var result = registry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.AllowedSchemas.Count.ShouldBe(2);
        result.AllowedSchemas.ShouldContain("orders");
        result.AllowedSchemas.ShouldContain("shared");
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ModuleSchemaRegistry(null!));
    }

    [Fact]
    public void ParameterlessConstructor_ShouldCreateEmptyRegistry()
    {
        // Act
        var registry = new ModuleSchemaRegistry();

        // Assert
        registry.GetRegisteredModules().ShouldBeEmpty();
        registry.GetSharedSchemas().ShouldBeEmpty();
    }

    #endregion
}
