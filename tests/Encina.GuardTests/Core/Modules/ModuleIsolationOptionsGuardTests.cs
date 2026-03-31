using Encina.Modules.Isolation;

namespace Encina.GuardTests.Core.Modules;

/// <summary>
/// Guard tests for <see cref="ModuleIsolationOptions"/> and <see cref="ModuleSchemaOptionsBuilder"/>
/// to verify null/whitespace parameter handling and duplicate detection.
/// </summary>
public class ModuleIsolationOptionsGuardTests
{
    // ---- AddSharedSchemas ----

    /// <summary>
    /// Verifies that AddSharedSchemas silently skips null/whitespace entries without throwing.
    /// </summary>
    [Fact]
    public void AddSharedSchemas_NullAndWhitespaceEntries_AreSkipped()
    {
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas(null!, "", "   ", "valid");
        options.SharedSchemas.Count.ShouldBe(1);
        options.SharedSchemas.ShouldContain("valid");
    }

    /// <summary>
    /// Verifies that AddSharedSchemas adds valid schema names correctly.
    /// </summary>
    [Fact]
    public void AddSharedSchemas_ValidSchemas_AddsToCollection()
    {
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "lookup");
        options.SharedSchemas.Count.ShouldBe(2);
        options.SharedSchemas.ShouldContain("shared");
        options.SharedSchemas.ShouldContain("lookup");
    }

    /// <summary>
    /// Verifies that AddSharedSchemas is case-insensitive (no duplicates).
    /// </summary>
    [Fact]
    public void AddSharedSchemas_DuplicateCaseInsensitive_NoDuplicate()
    {
        var options = new ModuleIsolationOptions();
        options.AddSharedSchemas("shared", "SHARED", "Shared");
        options.SharedSchemas.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies fluent chaining works for AddSharedSchemas.
    /// </summary>
    [Fact]
    public void AddSharedSchemas_ReturnsSameInstance_ForFluentChaining()
    {
        var options = new ModuleIsolationOptions();
        var result = options.AddSharedSchemas("test");
        result.ShouldBeSameAs(options);
    }

    // ---- AddModuleSchema (ModuleSchemaOptions) ----

    /// <summary>
    /// Verifies that AddModuleSchema throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void AddModuleSchema_NullOptions_ThrowsArgumentNullException()
    {
        var options = new ModuleIsolationOptions();
        var act = () => options.AddModuleSchema((ModuleSchemaOptions)null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that AddModuleSchema throws InvalidOperationException when a duplicate module is added.
    /// </summary>
    [Fact]
    public void AddModuleSchema_DuplicateModuleName_ThrowsInvalidOperationException()
    {
        var options = new ModuleIsolationOptions();
        var first = new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" };
        var duplicate = new ModuleSchemaOptions { ModuleName = "orders", SchemaName = "orders2" };

        options.AddModuleSchema(first);
        var act = () => options.AddModuleSchema(duplicate);
        Should.Throw<InvalidOperationException>(act);
    }

    /// <summary>
    /// Verifies that AddModuleSchema returns the same instance for fluent chaining.
    /// </summary>
    [Fact]
    public void AddModuleSchema_ValidOptions_ReturnsSameInstance()
    {
        var options = new ModuleIsolationOptions();
        var schema = new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" };
        var result = options.AddModuleSchema(schema);
        result.ShouldBeSameAs(options);
    }

    /// <summary>
    /// Verifies that AddModuleSchema correctly adds and exposes the module schema.
    /// </summary>
    [Fact]
    public void AddModuleSchema_Valid_AddsToModuleSchemas()
    {
        var options = new ModuleIsolationOptions();
        var schema = new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" };
        options.AddModuleSchema(schema);
        options.ModuleSchemas.Count.ShouldBe(1);
        options.ModuleSchemas[0].ModuleName.ShouldBe("Orders");
    }

    // ---- AddModuleSchema (fluent builder overload) ----

    /// <summary>
    /// Verifies that the fluent builder overload correctly creates and adds a module schema.
    /// </summary>
    [Fact]
    public void AddModuleSchema_FluentBuilder_AddsModuleSchema()
    {
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema("Payments", "payments", builder =>
            builder.WithDatabaseUser("payments_user")
                   .WithAdditionalAllowedSchemas("shared"));

        options.ModuleSchemas.Count.ShouldBe(1);
        options.ModuleSchemas[0].ModuleName.ShouldBe("Payments");
        options.ModuleSchemas[0].SchemaName.ShouldBe("payments");
        options.ModuleSchemas[0].DatabaseUser.ShouldBe("payments_user");
        options.ModuleSchemas[0].AdditionalAllowedSchemas.ShouldContain("shared");
    }

    /// <summary>
    /// Verifies that the fluent builder overload with duplicate module name throws.
    /// </summary>
    [Fact]
    public void AddModuleSchema_FluentBuilder_DuplicateName_ThrowsInvalidOperationException()
    {
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema("Orders", "orders");
        var act = () => options.AddModuleSchema("orders", "orders2");
        Should.Throw<InvalidOperationException>(act);
    }

    // ---- GetModuleSchema ----

    /// <summary>
    /// Verifies that GetModuleSchema returns null for a non-existent module.
    /// </summary>
    [Fact]
    public void GetModuleSchema_NonExistentModule_ReturnsNull()
    {
        var options = new ModuleIsolationOptions();
        options.GetModuleSchema("NonExistent").ShouldBeNull();
    }

    /// <summary>
    /// Verifies that GetModuleSchema is case-insensitive.
    /// </summary>
    [Fact]
    public void GetModuleSchema_CaseInsensitive_ReturnsSchema()
    {
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        options.GetModuleSchema("ORDERS").ShouldNotBeNull();
        options.GetModuleSchema("orders").ShouldNotBeNull();
    }

    // ---- HasModuleSchema ----

    /// <summary>
    /// Verifies that HasModuleSchema returns false for a non-existent module.
    /// </summary>
    [Fact]
    public void HasModuleSchema_NonExistentModule_ReturnsFalse()
    {
        var options = new ModuleIsolationOptions();
        options.HasModuleSchema("NonExistent").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that HasModuleSchema returns true for an existing module (case-insensitive).
    /// </summary>
    [Fact]
    public void HasModuleSchema_ExistingModule_ReturnsTrue()
    {
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema(new ModuleSchemaOptions { ModuleName = "Orders", SchemaName = "orders" });
        options.HasModuleSchema("Orders").ShouldBeTrue();
        options.HasModuleSchema("ORDERS").ShouldBeTrue();
    }

    // ---- ModuleSchemaOptionsBuilder ----

    /// <summary>
    /// Verifies that the builder throws ArgumentNullException when moduleName is null.
    /// </summary>
    [Fact]
    public void Builder_NullModuleName_ThrowsArgumentNullException()
    {
        var act = () => new ModuleSchemaOptionsBuilder(null!, "schema");
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("moduleName");
    }

    /// <summary>
    /// Verifies that the builder throws ArgumentNullException when schemaName is null.
    /// </summary>
    [Fact]
    public void Builder_NullSchemaName_ThrowsArgumentNullException()
    {
        var act = () => new ModuleSchemaOptionsBuilder("module", null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("schemaName");
    }

    /// <summary>
    /// Verifies that the builder produces correct ModuleSchemaOptions when fully configured.
    /// </summary>
    [Fact]
    public void Builder_FullyConfigured_BuildsCorrectOptions()
    {
        var builder = new ModuleSchemaOptionsBuilder("Orders", "orders")
            .WithDatabaseUser("orders_user")
            .WithAdditionalAllowedSchemas("shared", "lookup");

        var result = builder.Build();
        result.ModuleName.ShouldBe("Orders");
        result.SchemaName.ShouldBe("orders");
        result.DatabaseUser.ShouldBe("orders_user");
        result.AdditionalAllowedSchemas.Count.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that the builder deduplicates additional allowed schemas (case-insensitive).
    /// </summary>
    [Fact]
    public void Builder_DuplicateAllowedSchemas_Deduplicated()
    {
        var builder = new ModuleSchemaOptionsBuilder("Orders", "orders")
            .WithAdditionalAllowedSchemas("shared", "SHARED", "Shared");

        var result = builder.Build();
        result.AdditionalAllowedSchemas.Count.ShouldBe(1);
    }

    // ---- ModuleSchemaOptions.WithAdditionalAllowedSchemas ----

    /// <summary>
    /// Verifies that WithAdditionalAllowedSchemas creates a new instance with combined schemas.
    /// </summary>
    [Fact]
    public void SchemaOptions_WithAdditionalAllowedSchemas_CombinesAndDeduplicates()
    {
        var original = new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            AdditionalAllowedSchemas = ["shared"]
        };

        var extended = original.WithAdditionalAllowedSchemas("lookup", "SHARED");
        extended.AdditionalAllowedSchemas.Count.ShouldBe(2);
        extended.AdditionalAllowedSchemas.ShouldContain("shared");
        extended.AdditionalAllowedSchemas.ShouldContain("lookup");
    }

    /// <summary>
    /// Verifies that GetAllowedSchemas includes own schema plus additional ones.
    /// </summary>
    [Fact]
    public void SchemaOptions_GetAllowedSchemas_IncludesOwnAndAdditional()
    {
        var options = new ModuleSchemaOptions
        {
            ModuleName = "Orders",
            SchemaName = "orders",
            AdditionalAllowedSchemas = ["shared", "lookup"]
        };

        var allowed = options.GetAllowedSchemas().ToList();
        allowed.Count.ShouldBe(3);
        allowed[0].ShouldBe("orders");
        allowed.ShouldContain("shared");
        allowed.ShouldContain("lookup");
    }

    // ---- Default property values ----

    /// <summary>
    /// Verifies that default options have sensible defaults.
    /// </summary>
    [Fact]
    public void DefaultOptions_HaveExpectedDefaults()
    {
        var options = new ModuleIsolationOptions();
        options.Strategy.ShouldBe(ModuleIsolationStrategy.DevelopmentValidationOnly);
        options.SharedSchemas.Count.ShouldBe(0);
        options.ModuleSchemas.Count.ShouldBe(0);
        options.GeneratePermissionScripts.ShouldBeFalse();
        options.PermissionScriptsOutputPath.ShouldBeNull();
    }
}
