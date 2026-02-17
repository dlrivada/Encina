using Encina.EntityFrameworkCore.Modules;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.DbContexts;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Encina.TestInfrastructure.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for Module Isolation integration tests with EF Core.
/// Provides infrastructure for testing the <see cref="ModuleSchemaValidationInterceptor"/>
/// across different database providers.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <remarks>
/// <para>
/// This base class provides:
/// <list type="bullet">
/// <item><description>Helper methods for creating <see cref="IModuleSchemaRegistry"/> instances</description></item>
/// <item><description>Helper methods for creating <see cref="IModuleExecutionContext"/> scopes</description></item>
/// <item><description>Methods for creating DbContext with <see cref="ModuleSchemaValidationInterceptor"/></description></item>
/// <item><description>Standard test methods for module isolation validation</description></item>
/// </list>
/// </para>
/// <para>
/// The interceptor is configured with <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>
/// to enable SQL validation without requiring real database permissions.
/// </para>
/// </remarks>
public abstract class ModuleIsolationEFTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class, IEFCoreFixture
{
    /// <summary>
    /// Standard schema names used in module isolation tests.
    /// </summary>
    protected static class SchemaNames
    {
        /// <summary>Module A's schema (orders).</summary>
        public const string ModuleA = "orders";

        /// <summary>Module B's schema (inventory).</summary>
        public const string ModuleB = "inventory";

        /// <summary>Shared schema accessible to all modules.</summary>
        public const string Shared = "shared";
    }

    /// <summary>
    /// Standard module names used in module isolation tests.
    /// </summary>
    protected static class ModuleNames
    {
        /// <summary>Module A (Orders).</summary>
        public const string ModuleA = "Orders";

        /// <summary>Module B (Inventory).</summary>
        public const string ModuleB = "Inventory";
    }

    private readonly ModuleExecutionContext _moduleContext;
    private IModuleSchemaRegistry? _schemaRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleIsolationEFTestsBase{TFixture}"/> class.
    /// </summary>
    protected ModuleIsolationEFTestsBase()
    {
        _moduleContext = new ModuleExecutionContext();
    }

    /// <summary>
    /// Gets the EF Core fixture instance.
    /// </summary>
    protected abstract TFixture Fixture { get; }

    /// <summary>
    /// Gets the provider name (e.g., "SqlServer", "PostgreSQL").
    /// </summary>
    protected string ProviderName => Fixture.ProviderName;

    /// <summary>
    /// Gets whether the provider supports schemas.
    /// SQLite does not support schemas natively.
    /// </summary>
    protected virtual bool SupportsSchemas => Fixture.ProviderName != "Sqlite";

    /// <summary>
    /// Gets the module execution context for setting the current module.
    /// </summary>
    protected IModuleExecutionContext ModuleContext => _moduleContext;

    /// <summary>
    /// Gets or creates the module schema registry configured for tests.
    /// </summary>
    protected IModuleSchemaRegistry SchemaRegistry => _schemaRegistry ??= CreateDefaultSchemaRegistry();

    #region IAsyncLifetime

    /// <inheritdoc />
    public virtual async ValueTask InitializeAsync()
    {
        if (!Fixture.IsAvailable)
        {
            return;
        }

        await ClearTestDataAsync();
        await CreateModuleSchemasAsync();
    }

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        // Clear module context to avoid leaking state between tests
        _moduleContext.ClearModule();

        if (Fixture.IsAvailable)
        {
            await ClearTestDataAsync();
        }
    }

    #endregion

    #region Schema Registry Creation

    /// <summary>
    /// Creates the default module schema registry for tests.
    /// </summary>
    /// <returns>A configured <see cref="IModuleSchemaRegistry"/>.</returns>
    protected virtual IModuleSchemaRegistry CreateDefaultSchemaRegistry()
    {
        return CreateSchemaRegistry(
            sharedSchemas: [SchemaNames.Shared],
            moduleSchemas: [
                (ModuleNames.ModuleA, SchemaNames.ModuleA),
                (ModuleNames.ModuleB, SchemaNames.ModuleB)
            ]);
    }

    /// <summary>
    /// Creates a module schema registry with the specified configuration.
    /// </summary>
    /// <param name="sharedSchemas">Schemas accessible to all modules.</param>
    /// <param name="moduleSchemas">Module-to-schema mappings.</param>
    /// <returns>A configured <see cref="IModuleSchemaRegistry"/>.</returns>
    protected IModuleSchemaRegistry CreateSchemaRegistry(
        IEnumerable<string>? sharedSchemas = null,
        IEnumerable<(string ModuleName, string SchemaName)>? moduleSchemas = null)
    {
        var options = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly
        };

        if (sharedSchemas is not null)
        {
            options.AddSharedSchemas(sharedSchemas.ToArray());
        }

        if (moduleSchemas is not null)
        {
            foreach (var (moduleName, schemaName) in moduleSchemas)
            {
                options.AddModuleSchema(moduleName, schemaName);
            }
        }

        return new ModuleSchemaRegistry(options);
    }

    /// <summary>
    /// Creates module isolation options with the default test configuration.
    /// </summary>
    /// <returns>Configured <see cref="ModuleIsolationOptions"/>.</returns>
    protected virtual ModuleIsolationOptions CreateDefaultModuleIsolationOptions()
    {
        var options = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly
        };

        options.AddSharedSchemas(SchemaNames.Shared);
        options.AddModuleSchema(ModuleNames.ModuleA, SchemaNames.ModuleA);
        options.AddModuleSchema(ModuleNames.ModuleB, SchemaNames.ModuleB);

        return options;
    }

    #endregion

    #region DbContext Creation with Interceptor

    /// <summary>
    /// Creates a <see cref="ModuleIsolationTestDbContext"/> with the
    /// <see cref="ModuleSchemaValidationInterceptor"/> attached.
    /// </summary>
    /// <returns>A DbContext configured for module isolation testing.</returns>
    protected ModuleIsolationTestDbContext CreateDbContextWithInterceptor()
    {
        return CreateDbContextWithInterceptor(SchemaRegistry, _moduleContext);
    }

    /// <summary>
    /// Creates a <see cref="ModuleIsolationTestDbContext"/> with a custom schema registry.
    /// </summary>
    /// <param name="schemaRegistry">The schema registry to use for validation.</param>
    /// <returns>A DbContext configured for module isolation testing.</returns>
    protected ModuleIsolationTestDbContext CreateDbContextWithInterceptor(IModuleSchemaRegistry schemaRegistry)
    {
        return CreateDbContextWithInterceptor(schemaRegistry, _moduleContext);
    }

    /// <summary>
    /// Creates a <see cref="ModuleIsolationTestDbContext"/> with custom dependencies.
    /// </summary>
    /// <param name="schemaRegistry">The schema registry to use for validation.</param>
    /// <param name="moduleContext">The module execution context.</param>
    /// <param name="options">Optional module isolation options.</param>
    /// <param name="logger">Optional logger for the interceptor.</param>
    /// <returns>A DbContext configured for module isolation testing.</returns>
    protected ModuleIsolationTestDbContext CreateDbContextWithInterceptor(
        IModuleSchemaRegistry schemaRegistry,
        IModuleExecutionContext moduleContext,
        ModuleIsolationOptions? options = null,
        ILogger<ModuleSchemaValidationInterceptor>? logger = null)
    {
        options ??= CreateDefaultModuleIsolationOptions();
        logger ??= NullLogger<ModuleSchemaValidationInterceptor>.Instance;

        var interceptor = new ModuleSchemaValidationInterceptor(
            moduleContext,
            schemaRegistry,
            options,
            logger);

        var dbContextOptions = CreateDbContextOptionsWithInterceptor(interceptor);

        return new ModuleIsolationTestDbContext(dbContextOptions);
    }

    /// <summary>
    /// Creates DbContext options with the interceptor attached.
    /// </summary>
    /// <param name="interceptor">The module schema validation interceptor.</param>
    /// <returns>Configured DbContext options.</returns>
    protected abstract DbContextOptions<ModuleIsolationTestDbContext> CreateDbContextOptionsWithInterceptor(
        ModuleSchemaValidationInterceptor interceptor);

    #endregion

    #region Module Scope Helpers

    /// <summary>
    /// Creates a scope that sets the current module context.
    /// The module context is automatically cleared when the scope is disposed.
    /// </summary>
    /// <param name="moduleName">The name of the module to set as current.</param>
    /// <returns>A disposable scope that clears the module on disposal.</returns>
    protected IDisposable CreateModuleScope(string moduleName)
    {
        return _moduleContext.CreateScope(moduleName);
    }

    /// <summary>
    /// Creates a scope for Module A (Orders).
    /// </summary>
    /// <returns>A disposable scope for Module A.</returns>
    protected IDisposable CreateModuleAScope() => CreateModuleScope(ModuleNames.ModuleA);

    /// <summary>
    /// Creates a scope for Module B (Inventory).
    /// </summary>
    /// <returns>A disposable scope for Module B.</returns>
    protected IDisposable CreateModuleBScope() => CreateModuleScope(ModuleNames.ModuleB);

    #endregion

    #region Schema Creation (Provider-Specific)

    /// <summary>
    /// Creates the module schemas in the database.
    /// Override this method for provider-specific schema creation.
    /// </summary>
    protected abstract Task CreateModuleSchemasAsync();

    /// <summary>
    /// Clears all test data from the module schemas.
    /// Override this method for provider-specific data cleanup.
    /// </summary>
    protected abstract Task ClearTestDataAsync();

    #endregion

    #region Test Entity Helpers

    /// <summary>
    /// Creates a test entity for the Orders module.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <returns>A new <see cref="OrdersModuleEntity"/>.</returns>
    protected static OrdersModuleEntity CreateOrdersEntity(string? orderNumber = null)
    {
        return new OrdersModuleEntity
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber ?? $"ORD-{Guid.NewGuid():N}".Substring(0, 20),
            CustomerName = "Test Customer",
            Total = 100.00m,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test entity for the Inventory module.
    /// </summary>
    /// <param name="sku">The SKU.</param>
    /// <returns>A new <see cref="InventoryModuleEntity"/>.</returns>
    protected static InventoryModuleEntity CreateInventoryEntity(string? sku = null)
    {
        return new InventoryModuleEntity
        {
            Id = Guid.NewGuid(),
            Sku = sku ?? $"SKU-{Guid.NewGuid():N}".Substring(0, 20),
            ProductName = "Test Product",
            QuantityInStock = 100,
            ReorderThreshold = 10,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test entity for the Shared schema.
    /// </summary>
    /// <param name="code">The lookup code.</param>
    /// <returns>A new <see cref="SharedLookupEntity"/>.</returns>
    protected static SharedLookupEntity CreateSharedLookupEntity(string? code = null)
    {
        return new SharedLookupEntity
        {
            Id = Guid.NewGuid(),
            Code = code ?? $"CODE-{Guid.NewGuid():N}".Substring(0, 15),
            DisplayName = "Test Lookup",
            Category = "Test",
            IsActive = true,
            SortOrder = 0
        };
    }

    #endregion

    #region Shared Test Methods

    /// <summary>
    /// Verifies that a module can query entities in its own schema.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ModuleCanQueryOwnSchema()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        using (CreateModuleAScope())
        {
            // Insert an entity using Module A context
            var order = CreateOrdersEntity();
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        // Act - Query as Module A
        using (CreateModuleAScope())
        {
            var orders = await context.Orders.ToListAsync();

            // Assert
            orders.Count.ShouldBeGreaterThanOrEqualTo(1,
                $"[{ProviderName}] Module A should be able to query its own schema");
        }
    }

    /// <summary>
    /// Verifies that all modules can access shared schemas.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AllModulesCanAccessSharedSchema()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        // Insert shared lookup (can be done from any module)
        using (CreateModuleAScope())
        {
            var lookup = CreateSharedLookupEntity("TEST_SHARED");
            context.SharedLookups.Add(lookup);
            await context.SaveChangesAsync();
        }

        // Act & Assert - Both modules should be able to read shared data
        using (CreateModuleAScope())
        {
            var lookups = await context.SharedLookups.ToListAsync();
            lookups.Count.ShouldBeGreaterThanOrEqualTo(1,
                $"[{ProviderName}] Module A should be able to access shared schema");
        }

        using (CreateModuleBScope())
        {
            var lookups = await context.SharedLookups.ToListAsync();
            lookups.Count.ShouldBeGreaterThanOrEqualTo(1,
                $"[{ProviderName}] Module B should be able to access shared schema");
        }
    }

    /// <summary>
    /// Verifies that queries execute normally when no module context is set.
    /// Infrastructure queries bypass validation.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task QueriesWithoutModuleContextExecuteNormally()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        // Act - Query without setting module context
        // This should succeed (infrastructure queries bypass validation)
        _moduleContext.ClearModule();
        var orders = await context.Orders.ToListAsync();

        // Assert - No exception should be thrown
        orders.ShouldNotBeNull($"[{ProviderName}] Queries without module context should execute normally");
    }

    /// <summary>
    /// Verifies that cross-module schema access throws <see cref="ModuleIsolationViolationException"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CrossSchemaAccessThrowsModuleIsolationViolationException()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        // Act & Assert - Module A trying to access Module B's schema
        using (CreateModuleAScope())
        {
            await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            {
                await context.InventoryItems.ToListAsync();
            });
        }

        // Act & Assert - Module B trying to access Module A's schema
        using (CreateModuleBScope())
        {
            await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            {
                await context.Orders.ToListAsync();
            });
        }
    }

    /// <summary>
    /// Verifies that the <see cref="ModuleIsolationViolationException"/> contains correct details.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ViolationExceptionContainsCorrectDetails()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        // Act
        using (CreateModuleAScope())
        {
            var ex = await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            {
                await context.InventoryItems.ToListAsync();
            });

            // Assert
            ex.ModuleName.ShouldBe(ModuleNames.ModuleA,
                $"[{ProviderName}] Exception should contain the module name");
            ex.UnauthorizedSchemas.ShouldContain(SchemaNames.ModuleB,
                $"[{ProviderName}] Exception should contain unauthorized schemas");
            ex.AllowedSchemas.ShouldContain(SchemaNames.ModuleA,
                $"[{ProviderName}] Exception should contain allowed schemas");
            ex.AllowedSchemas.ShouldContain(SchemaNames.Shared,
                $"[{ProviderName}] Exception should contain shared schemas");
        }
    }

    /// <summary>
    /// Verifies that context switching changes validation behavior.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ContextSwitchingChangesValidationBehavior()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        await using var context = CreateDbContextWithInterceptor();

        // Insert data in both schemas
        _moduleContext.ClearModule(); // Infrastructure mode
        var order = CreateOrdersEntity();
        context.Orders.Add(order);
        var item = CreateInventoryEntity();
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        // Act & Assert - Same queries have different results based on context
        using (CreateModuleAScope())
        {
            // Orders should work
            var orders = await context.Orders.ToListAsync();
            orders.ShouldNotBeEmpty($"[{ProviderName}] Module A can access orders");

            // Inventory should fail
            await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            {
                await context.InventoryItems.ToListAsync();
            });
        }

        using (CreateModuleBScope())
        {
            // Inventory should work
            var items = await context.InventoryItems.ToListAsync();
            items.ShouldNotBeEmpty($"[{ProviderName}] Module B can access inventory");

            // Orders should fail
            await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            {
                await context.Orders.ToListAsync();
            });
        }
    }

    #endregion

    #region Registry Validation Tests (Provider-Agnostic)

    /// <summary>
    /// Verifies that the schema registry allows access to own schema.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SchemaRegistry_AllowsAccessToOwnSchema()
    {
        // Assert
        SchemaRegistry.CanAccessSchema(ModuleNames.ModuleA, SchemaNames.ModuleA).ShouldBeTrue(
            $"[{ProviderName}] Module A should access its own schema");
        SchemaRegistry.CanAccessSchema(ModuleNames.ModuleB, SchemaNames.ModuleB).ShouldBeTrue(
            $"[{ProviderName}] Module B should access its own schema");
    }

    /// <summary>
    /// Verifies that the schema registry allows access to shared schemas.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SchemaRegistry_AllowsAccessToSharedSchemas()
    {
        // Assert
        SchemaRegistry.CanAccessSchema(ModuleNames.ModuleA, SchemaNames.Shared).ShouldBeTrue(
            $"[{ProviderName}] Module A should access shared schema");
        SchemaRegistry.CanAccessSchema(ModuleNames.ModuleB, SchemaNames.Shared).ShouldBeTrue(
            $"[{ProviderName}] Module B should access shared schema");
    }

    /// <summary>
    /// Verifies that the schema registry denies access to other modules' schemas.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SchemaRegistry_DeniesAccessToOtherModuleSchemas()
    {
        // Assert
        SchemaRegistry.CanAccessSchema(ModuleNames.ModuleA, SchemaNames.ModuleB).ShouldBeFalse(
            $"[{ProviderName}] Module A should not access Module B's schema");
        SchemaRegistry.CanAccessSchema(ModuleNames.ModuleB, SchemaNames.ModuleA).ShouldBeFalse(
            $"[{ProviderName}] Module B should not access Module A's schema");
    }

    /// <summary>
    /// Verifies SQL validation with the schema registry.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SchemaRegistry_ValidateSqlAccess_WorksCorrectly()
    {
        // Valid query - Module A accessing its own schema
        var validResult = SchemaRegistry.ValidateSqlAccess(
            ModuleNames.ModuleA,
            "SELECT * FROM orders.Orders WHERE Id = @Id");
        validResult.IsValid.ShouldBeTrue($"[{ProviderName}] Valid query should pass");

        // Invalid query - Module A accessing Module B's schema
        var invalidResult = SchemaRegistry.ValidateSqlAccess(
            ModuleNames.ModuleA,
            "SELECT * FROM inventory.InventoryItems WHERE Sku = @Sku");
        invalidResult.IsValid.ShouldBeFalse($"[{ProviderName}] Cross-schema query should fail");
        invalidResult.UnauthorizedSchemas.ShouldContain(SchemaNames.ModuleB);
    }

    #endregion
}
