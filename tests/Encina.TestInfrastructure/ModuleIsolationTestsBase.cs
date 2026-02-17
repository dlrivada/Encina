using System.Data;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Entities;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure;

/// <summary>
/// Abstract base class for module isolation integration tests.
/// Contains standard test methods for schema boundary enforcement that work
/// across ADO.NET, Dapper, and EF Core providers.
/// </summary>
/// <typeparam name="TFixture">The type of database fixture to use.</typeparam>
/// <remarks>
/// This base class provides:
/// <list type="bullet">
/// <item><description>Schema-scoped query execution tests</description></item>
/// <item><description>Cross-schema access prevention tests</description></item>
/// <item><description>ModuleSchemaRegistry validation tests</description></item>
/// <item><description>SchemaValidatingConnection interception tests</description></item>
/// </list>
/// <para>
/// Note: SQLite tests may skip or adapt tests since SQLite doesn't support schemas.
/// </para>
/// </remarks>
public abstract class ModuleIsolationTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    /// <summary>
    /// Gets the database fixture instance.
    /// </summary>
    protected abstract TFixture Fixture { get; }

    /// <summary>
    /// Creates a database connection for a specific module.
    /// </summary>
    /// <param name="moduleName">The module name (e.g., "Orders", "Inventory").</param>
    /// <returns>A database connection configured for the specified module's schema access.</returns>
    protected abstract IDbConnection CreateConnectionForModule(string moduleName);

    /// <summary>
    /// Gets the module schema registry for validation.
    /// </summary>
    protected abstract IModuleSchemaRegistry GetModuleSchemaRegistry();

    /// <summary>
    /// Inserts an entity into the Orders module schema.
    /// </summary>
    protected abstract Task InsertOrdersEntityAsync(IDbConnection connection, OrdersModuleEntity entity);

    /// <summary>
    /// Inserts an entity into the Inventory module schema.
    /// </summary>
    protected abstract Task InsertInventoryEntityAsync(IDbConnection connection, InventoryModuleEntity entity);

    /// <summary>
    /// Inserts an entity into the Shared schema.
    /// </summary>
    protected abstract Task InsertSharedLookupAsync(IDbConnection connection, SharedLookupEntity entity);

    /// <summary>
    /// Queries orders from the Orders module.
    /// </summary>
    protected abstract Task<List<OrdersModuleEntity>> QueryOrdersAsync(IDbConnection connection);

    /// <summary>
    /// Queries inventory from the Inventory module.
    /// </summary>
    protected abstract Task<List<InventoryModuleEntity>> QueryInventoryAsync(IDbConnection connection);

    /// <summary>
    /// Queries shared lookups (should be accessible from all modules).
    /// </summary>
    protected abstract Task<List<SharedLookupEntity>> QuerySharedLookupsAsync(IDbConnection connection);

    /// <summary>
    /// Attempts to execute a cross-schema query (should fail or return empty).
    /// </summary>
    /// <param name="connection">Connection for one module.</param>
    /// <param name="targetSchema">The schema being accessed (not allowed).</param>
    /// <returns>True if access was blocked; false if access was allowed.</returns>
    protected abstract Task<bool> AttemptCrossSchemaAccessAsync(IDbConnection connection, string targetSchema);

    /// <summary>
    /// Gets the provider-specific name for display in test output.
    /// </summary>
    protected abstract string ProviderName { get; }

    /// <summary>
    /// Gets whether the provider supports schemas (SQLite does not).
    /// </summary>
    protected abstract bool SupportsSchemas { get; }

    /// <summary>
    /// Clears all test data and recreates schemas.
    /// </summary>
    protected abstract Task ClearTestDataAsync();

    #region IAsyncLifetime

    /// <inheritdoc />
    public virtual async ValueTask InitializeAsync()
    {
        await ClearTestDataAsync();
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Schema-Scoped Query Tests

    [Fact]
    public async Task SchemaScopedQuery_OrdersModuleCanQueryOwnSchema()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        using var ordersConn = CreateConnectionForModule("Orders");
        var order = new OrdersModuleEntity
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerName = "Test Customer",
            Total = 150.00m,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        await InsertOrdersEntityAsync(ordersConn, order);

        // Act
        var orders = await QueryOrdersAsync(ordersConn);

        // Assert
        orders.Count.ShouldBe(1, $"[{ProviderName}] Orders module should see its own data");
        orders[0].OrderNumber.ShouldBe("ORD-001");
    }

    [Fact]
    public async Task SchemaScopedQuery_InventoryModuleCanQueryOwnSchema()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        using var inventoryConn = CreateConnectionForModule("Inventory");
        var item = new InventoryModuleEntity
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-001",
            ProductName = "Test Product",
            QuantityInStock = 100,
            ReorderThreshold = 10,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await InsertInventoryEntityAsync(inventoryConn, item);

        // Act
        var items = await QueryInventoryAsync(inventoryConn);

        // Assert
        items.Count.ShouldBe(1, $"[{ProviderName}] Inventory module should see its own data");
        items[0].Sku.ShouldBe("SKU-001");
    }

    #endregion

    #region Cross-Schema Access Prevention Tests

    [Fact]
    public async Task CrossSchemaAccess_OrdersModuleCannotAccessInventorySchema()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        using var inventoryConn = CreateConnectionForModule("Inventory");
        await InsertInventoryEntityAsync(inventoryConn, new InventoryModuleEntity
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-SECRET",
            ProductName = "Secret Product",
            QuantityInStock = 500,
            ReorderThreshold = 50,
            LastUpdatedAtUtc = DateTime.UtcNow
        });

        // Act - Orders module tries to access Inventory schema
        using var ordersConn = CreateConnectionForModule("Orders");
        var accessBlocked = await AttemptCrossSchemaAccessAsync(ordersConn, "inventory");

        // Assert
        accessBlocked.ShouldBeTrue($"[{ProviderName}] Orders module should not be able to access inventory schema");
    }

    [Fact]
    public async Task CrossSchemaAccess_InventoryModuleCannotAccessOrdersSchema()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange
        using var ordersConn = CreateConnectionForModule("Orders");
        await InsertOrdersEntityAsync(ordersConn, new OrdersModuleEntity
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-SECRET",
            CustomerName = "Secret Customer",
            Total = 9999.99m,
            Status = "Confidential",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act - Inventory module tries to access Orders schema
        using var inventoryConn = CreateConnectionForModule("Inventory");
        var accessBlocked = await AttemptCrossSchemaAccessAsync(inventoryConn, "orders");

        // Assert
        accessBlocked.ShouldBeTrue($"[{ProviderName}] Inventory module should not be able to access orders schema");
    }

    #endregion

    #region Shared Schema Access Tests

    [Fact]
    public async Task SharedSchemaAccess_AllModulesCanReadSharedLookups()
    {
        Assert.SkipUnless(SupportsSchemas, "Provider does not support schemas");

        // Arrange - Insert shared lookup
        using (var sharedConn = CreateConnectionForModule("Orders")) // Either module can access shared
        {
            await InsertSharedLookupAsync(sharedConn, new SharedLookupEntity
            {
                Id = Guid.NewGuid(),
                Code = "STATUS_ACTIVE",
                DisplayName = "Active",
                Category = "Status",
                IsActive = true,
                SortOrder = 1
            });
        }

        // Act - Both modules should be able to read shared lookups
        using var ordersConn = CreateConnectionForModule("Orders");
        using var inventoryConn = CreateConnectionForModule("Inventory");

        var ordersLookups = await QuerySharedLookupsAsync(ordersConn);
        var inventoryLookups = await QuerySharedLookupsAsync(inventoryConn);

        // Assert
        ordersLookups.Count.ShouldBe(1, $"[{ProviderName}] Orders module should see shared lookups");
        inventoryLookups.Count.ShouldBe(1, $"[{ProviderName}] Inventory module should see shared lookups");
    }

    #endregion

    #region ModuleSchemaRegistry Validation Tests

    [Fact]
    public void ModuleSchemaRegistry_ShouldAllowAccessToOwnSchema()
    {
        // Arrange
        var registry = GetModuleSchemaRegistry();

        // Assert
        registry.CanAccessSchema("Orders", "orders").ShouldBeTrue($"[{ProviderName}] Orders should access orders schema");
        registry.CanAccessSchema("Inventory", "inventory").ShouldBeTrue($"[{ProviderName}] Inventory should access inventory schema");
    }

    [Fact]
    public void ModuleSchemaRegistry_ShouldAllowAccessToSharedSchemas()
    {
        // Arrange
        var registry = GetModuleSchemaRegistry();

        // Assert
        registry.CanAccessSchema("Orders", "shared").ShouldBeTrue($"[{ProviderName}] Orders should access shared schema");
        registry.CanAccessSchema("Inventory", "shared").ShouldBeTrue($"[{ProviderName}] Inventory should access shared schema");
    }

    [Fact]
    public void ModuleSchemaRegistry_ShouldDenyAccessToOtherModuleSchemas()
    {
        // Arrange
        var registry = GetModuleSchemaRegistry();

        // Assert
        registry.CanAccessSchema("Orders", "inventory").ShouldBeFalse($"[{ProviderName}] Orders should not access inventory schema");
        registry.CanAccessSchema("Inventory", "orders").ShouldBeFalse($"[{ProviderName}] Inventory should not access orders schema");
    }

    [Fact]
    public void ModuleSchemaRegistry_ShouldReturnCorrectAllowedSchemas()
    {
        // Arrange
        var registry = GetModuleSchemaRegistry();

        // Act
        var ordersSchemas = registry.GetAllowedSchemas("Orders");
        var inventorySchemas = registry.GetAllowedSchemas("Inventory");

        // Assert
        ordersSchemas.ShouldContain("orders", $"[{ProviderName}] Orders allowed schemas should contain 'orders'");
        ordersSchemas.ShouldContain("shared", $"[{ProviderName}] Orders allowed schemas should contain 'shared'");
        ordersSchemas.ShouldNotContain("inventory", $"[{ProviderName}] Orders allowed schemas should not contain 'inventory'");

        inventorySchemas.ShouldContain("inventory", $"[{ProviderName}] Inventory allowed schemas should contain 'inventory'");
        inventorySchemas.ShouldContain("shared", $"[{ProviderName}] Inventory allowed schemas should contain 'shared'");
        inventorySchemas.ShouldNotContain("orders", $"[{ProviderName}] Inventory allowed schemas should not contain 'orders'");
    }

    #endregion

    #region SQL Validation Tests

    [Fact]
    public void SqlValidation_ValidQueryShouldPass()
    {
        // Arrange
        var registry = GetModuleSchemaRegistry();

        // Act
        var result = registry.ValidateSqlAccess("Orders", "SELECT * FROM orders.Orders WHERE Id = @Id");

        // Assert
        result.IsValid.ShouldBeTrue($"[{ProviderName}] Valid query should pass validation");
    }

    [Fact]
    public void SqlValidation_CrossSchemaQueryShouldFail()
    {
        // Arrange
        var registry = GetModuleSchemaRegistry();

        // Act
        var result = registry.ValidateSqlAccess("Orders", "SELECT * FROM inventory.InventoryItems WHERE Sku = @Sku");

        // Assert
        result.IsValid.ShouldBeFalse($"[{ProviderName}] Cross-schema query should fail validation");
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    #endregion
}
