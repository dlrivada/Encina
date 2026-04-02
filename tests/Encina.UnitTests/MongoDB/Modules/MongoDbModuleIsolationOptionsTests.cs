using Encina.MongoDB.Modules;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Modules;

/// <summary>
/// Unit tests for <see cref="MongoDbModuleIsolationOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class MongoDbModuleIsolationOptionsTests
{
    #region Default Values

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new MongoDbModuleIsolationOptions();

        options.EnableDatabasePerModule.ShouldBeTrue();
        options.DatabaseNamePattern.ShouldBe("{baseName}_{moduleName}");
        options.ThrowOnMissingModuleContext.ShouldBeFalse();
        options.LogWarningOnFallback.ShouldBeTrue();
        options.ModuleDatabaseMappings.ShouldBeEmpty();
    }

    #endregion

    #region Properties

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = false,
            DatabaseNamePattern = "module_{moduleName}",
            ThrowOnMissingModuleContext = true,
            LogWarningOnFallback = false
        };

        options.EnableDatabasePerModule.ShouldBeFalse();
        options.DatabaseNamePattern.ShouldBe("module_{moduleName}");
        options.ThrowOnMissingModuleContext.ShouldBeTrue();
        options.LogWarningOnFallback.ShouldBeFalse();
    }

    #endregion

    #region GetDatabaseName

    [Fact]
    public void GetDatabaseName_DefaultPattern_ReplacesPlaceholders()
    {
        var options = new MongoDbModuleIsolationOptions();

        var result = options.GetDatabaseName("MyApp", "Orders");

        result.ShouldBe("MyApp_orders");
    }

    [Fact]
    public void GetDatabaseName_CustomPattern_ReplacesPlaceholders()
    {
        var options = new MongoDbModuleIsolationOptions
        {
            DatabaseNamePattern = "module_{moduleName}"
        };

        var result = options.GetDatabaseName("MyApp", "Orders");

        result.ShouldBe("module_orders");
    }

    [Fact]
    public void GetDatabaseName_ExplicitMapping_TakesPrecedenceOverPattern()
    {
        var options = new MongoDbModuleIsolationOptions();
        options.ModuleDatabaseMappings["Orders"] = "production_orders_db";

        var result = options.GetDatabaseName("MyApp", "Orders");

        result.ShouldBe("production_orders_db");
    }

    [Fact]
    public void GetDatabaseName_ExplicitMapping_IsCaseInsensitive()
    {
        var options = new MongoDbModuleIsolationOptions();
        options.ModuleDatabaseMappings["ORDERS"] = "prod_orders";

        var result = options.GetDatabaseName("MyApp", "orders");

        result.ShouldBe("prod_orders");
    }

    [Fact]
    public void GetDatabaseName_NoExplicitMapping_FallsBackToPattern()
    {
        var options = new MongoDbModuleIsolationOptions();
        options.ModuleDatabaseMappings["Inventory"] = "inventory_db";

        var result = options.GetDatabaseName("MyApp", "Orders");

        result.ShouldBe("MyApp_orders");
    }

    [Fact]
    public void GetDatabaseName_NullBaseName_ThrowsArgumentException()
    {
        var options = new MongoDbModuleIsolationOptions();

        Should.Throw<ArgumentException>(() => options.GetDatabaseName(null!, "Orders"));
    }

    [Fact]
    public void GetDatabaseName_EmptyBaseName_ThrowsArgumentException()
    {
        var options = new MongoDbModuleIsolationOptions();

        Should.Throw<ArgumentException>(() => options.GetDatabaseName("", "Orders"));
    }

    [Fact]
    public void GetDatabaseName_NullModuleName_ThrowsArgumentException()
    {
        var options = new MongoDbModuleIsolationOptions();

        Should.Throw<ArgumentException>(() => options.GetDatabaseName("MyApp", null!));
    }

    [Fact]
    public void GetDatabaseName_EmptyModuleName_ThrowsArgumentException()
    {
        var options = new MongoDbModuleIsolationOptions();

        Should.Throw<ArgumentException>(() => options.GetDatabaseName("MyApp", ""));
    }

    [Fact]
    public void GetDatabaseName_ModuleNameIsLowered()
    {
        var options = new MongoDbModuleIsolationOptions();

        var result = options.GetDatabaseName("Base", "UPPERCASEMODULE");

        result.ShouldBe("Base_uppercasemodule");
    }

    #endregion

    #region ModuleDatabaseMappings

    [Fact]
    public void ModuleDatabaseMappings_AddMultiple_AllAccessible()
    {
        var options = new MongoDbModuleIsolationOptions();
        options.ModuleDatabaseMappings["Orders"] = "db_orders";
        options.ModuleDatabaseMappings["Inventory"] = "db_inventory";
        options.ModuleDatabaseMappings["Users"] = "db_users";

        options.ModuleDatabaseMappings.Count.ShouldBe(3);
        options.GetDatabaseName("Base", "Orders").ShouldBe("db_orders");
        options.GetDatabaseName("Base", "Inventory").ShouldBe("db_inventory");
        options.GetDatabaseName("Base", "Users").ShouldBe("db_users");
    }

    #endregion
}
