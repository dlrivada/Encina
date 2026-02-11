using Encina.EntityFrameworkCore.Modules;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.ModuleIsolation;

/// <summary>
/// PostgreSQL-specific integration tests for EF Core module isolation support.
/// Tests the ModuleSchemaValidationInterceptor and module-aware DbContext configuration.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL supports real schemas, so module isolation uses schema-qualified table names
/// (e.g., orders."ModuleOrders", inventory."ModuleInventoryItems").
/// </para>
/// <para>
/// These tests verify that:
/// <list type="bullet">
/// <item><description>ModuleSchemaValidationInterceptor validates SQL against module boundaries</description></item>
/// <item><description>Cross-module queries are blocked in development validation mode</description></item>
/// <item><description>Shared schemas are accessible from all modules</description></item>
/// <item><description>Module context switching works correctly</description></item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class ModuleIsolationEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public ModuleIsolationEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        await CreateSchemasAndTablesAsync();

        // Configure module isolation with real PostgreSQL schemas
        _isolationOptions = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly
        };
        _isolationOptions.AddSharedSchemas("shared");
        _isolationOptions.AddModuleSchema("Orders", "orders");
        _isolationOptions.AddModuleSchema("Inventory", "inventory");

        _schemaRegistry = new ModuleSchemaRegistry(_isolationOptions);
        _moduleContext = new TestModuleExecutionContext();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        await CleanupSchemasAsync();
    }

    private async Task CreateSchemasAndTablesAsync()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Create schemas and tables
        const string sql = """
            -- Create schemas if they don't exist
            CREATE SCHEMA IF NOT EXISTS orders;
            CREATE SCHEMA IF NOT EXISTS inventory;
            CREATE SCHEMA IF NOT EXISTS shared;

            -- Drop existing tables
            DROP TABLE IF EXISTS orders."ModuleOrders";
            DROP TABLE IF EXISTS inventory."ModuleInventoryItems";
            DROP TABLE IF EXISTS shared."ModuleLookups";

            -- Create orders table
            CREATE TABLE orders."ModuleOrders" (
                "Id" UUID PRIMARY KEY,
                "OrderNumber" VARCHAR(50) NOT NULL UNIQUE,
                "CustomerName" VARCHAR(200) NOT NULL,
                "Total" DECIMAL(18,2) NOT NULL,
                "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
                "CreatedAtUtc" TIMESTAMPTZ NOT NULL
            );

            -- Create inventory table
            CREATE TABLE inventory."ModuleInventoryItems" (
                "Id" UUID PRIMARY KEY,
                "Sku" VARCHAR(50) NOT NULL UNIQUE,
                "ProductName" VARCHAR(200) NOT NULL,
                "QuantityInStock" INT NOT NULL DEFAULT 0,
                "ReorderThreshold" INT NOT NULL DEFAULT 10,
                "LastUpdatedAtUtc" TIMESTAMPTZ NOT NULL
            );

            -- Create shared table
            CREATE TABLE shared."ModuleLookups" (
                "Id" UUID PRIMARY KEY,
                "Code" VARCHAR(50) NOT NULL,
                "DisplayName" VARCHAR(100) NOT NULL,
                "Category" VARCHAR(50) NOT NULL,
                "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
                "SortOrder" INT NOT NULL DEFAULT 0
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CleanupSchemasAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                DROP TABLE IF EXISTS orders."ModuleOrders";
                DROP TABLE IF EXISTS inventory."ModuleInventoryItems";
                DROP TABLE IF EXISTS shared."ModuleLookups";
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private ModuleTestDbContext CreateDbContextWithInterceptor(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);

        var interceptor = new ModuleSchemaValidationInterceptor(
            _moduleContext,
            _schemaRegistry,
            _isolationOptions,
            NullLogger<ModuleSchemaValidationInterceptor>.Instance);

        var optionsBuilder = new DbContextOptionsBuilder<ModuleTestDbContext>();
        optionsBuilder.UseNpgsql(_fixture.ConnectionString);
        optionsBuilder.AddInterceptors(interceptor);

        return new ModuleTestDbContext(optionsBuilder.Options, usePostgreSql: true);
    }

    #region Schema Registry Validation Tests

    [Fact]
    public void SchemaRegistry_ShouldAllowAccessToOwnSchema()
    {

        // Assert
        _schemaRegistry.CanAccessSchema("Orders", "orders").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "inventory").ShouldBeTrue();
    }

    [Fact]
    public void SchemaRegistry_ShouldAllowAccessToSharedSchema()
    {

        // Assert
        _schemaRegistry.CanAccessSchema("Orders", "shared").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "shared").ShouldBeTrue();
    }

    [Fact]
    public void SchemaRegistry_ShouldDenyAccessToOtherModuleSchemas()
    {

        // Assert
        _schemaRegistry.CanAccessSchema("Orders", "inventory").ShouldBeFalse();
        _schemaRegistry.CanAccessSchema("Inventory", "orders").ShouldBeFalse();
    }

    #endregion

    #region SQL Validation Tests (schema-qualified for PostgreSQL)

    [Fact]
    public void SqlValidation_ValidQueryToOwnSchemaTable_ShouldPass()
    {

        // PostgreSQL uses schema-qualified names: orders."ModuleOrders"
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM orders.\"ModuleOrders\" WHERE \"Id\" = @Id");

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_QueryToSharedSchemaTable_ShouldPass()
    {

        // PostgreSQL uses schema-qualified names: shared."ModuleLookups"
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM shared.\"ModuleLookups\" WHERE \"Category\" = @Category");

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_CrossModuleSchemaAccess_ShouldFail()
    {

        // PostgreSQL uses schema-qualified names: inventory."ModuleInventoryItems"
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM inventory.\"ModuleInventoryItems\" WHERE \"Sku\" = @Sku");

        // Assert
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    #endregion

    #region ModuleSchemaValidationInterceptor Tests

    [Fact]
    public async Task Interceptor_CanExecuteValidQuery()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");

        // Insert
        var order = new ModuleOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{Guid.NewGuid():N}".Substring(0, 15),
            CustomerName = "Test Customer",
            Total = 150.00m,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        context.ModuleOrders.Add(order);
        await context.SaveChangesAsync();

        // Act - Query
        var count = await context.ModuleOrders.CountAsync();

        // Assert
        count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Interceptor_ThrowsOnCrossModuleTableAccess()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");

        // Act & Assert - Trying to access inventory from Orders module context
        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await context.Database.ExecuteSqlRawAsync("SELECT * FROM inventory.\"ModuleInventoryItems\"");
        });
    }

    [Fact]
    public async Task Interceptor_AllowsSharedTableAccess()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");

        // Insert a shared lookup
        var lookup = new ModuleLookup
        {
            Id = Guid.NewGuid(),
            Code = "STATUS_ACTIVE",
            DisplayName = "Active",
            Category = "Status",
            IsActive = true,
            SortOrder = 1
        };
        context.ModuleLookups.Add(lookup);
        await context.SaveChangesAsync();

        // Act - Query shared table from Orders module
        var lookups = await context.ModuleLookups.ToListAsync();

        // Assert
        lookups.ShouldNotBeEmpty();
    }

    #endregion

    #region Module Context Tests

    [Fact]
    public void ModuleContext_CanBeSwitched()
    {

        // Arrange & Act
        _moduleContext.SetCurrentModule("Orders");
        var orders = _moduleContext.CurrentModule;

        _moduleContext.SetCurrentModule("Inventory");
        var inventory = _moduleContext.CurrentModule;

        _moduleContext.ClearCurrentModule();
        var cleared = _moduleContext.CurrentModule;

        // Assert
        orders.ShouldBe("Orders");
        inventory.ShouldBe("Inventory");
        cleared.ShouldBeNull();
    }

    [Fact]
    public void ModuleContext_CreateScope_SetsAndClearsModule()
    {

        // Arrange & Act
        string? moduleInScope;
        using (_moduleContext.CreateScope("Orders"))
        {
            moduleInScope = _moduleContext.CurrentModule;
        }
        var moduleAfterScope = _moduleContext.CurrentModule;

        // Assert
        moduleInScope.ShouldBe("Orders");
        moduleAfterScope.ShouldBeNull();
    }

    #endregion

    #region Additional Allowed Schemas Tests

    [Fact]
    public void SchemaRegistry_WithAdditionalAllowedSchemas_ShouldAllowAccess()
    {

        // Arrange - Create options with additional allowed schemas
        var options = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly
        };
        options.AddSharedSchemas("shared");
        options.AddModuleSchema("Orders", "orders");
        options.AddModuleSchema("Reporting", "reporting", builder =>
            builder.WithAdditionalAllowedSchemas("orders", "inventory"));

        var registry = new ModuleSchemaRegistry(options);

        // Act & Assert - Reporting can access its own, shared, and additional schemas
        registry.CanAccessSchema("Reporting", "reporting").ShouldBeTrue();
        registry.CanAccessSchema("Reporting", "shared").ShouldBeTrue();
        registry.CanAccessSchema("Reporting", "orders").ShouldBeTrue();
        registry.CanAccessSchema("Reporting", "inventory").ShouldBeTrue();

        // But Orders cannot access reporting
        registry.CanAccessSchema("Orders", "reporting").ShouldBeFalse();
    }

    [Fact]
    public void SqlValidation_WithAdditionalAllowedSchemas_JoinQuery_ShouldPass()
    {

        // Arrange - Create options with additional allowed schemas
        var options = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly
        };
        options.AddSharedSchemas("shared");
        options.AddModuleSchema("Reporting", "reporting", builder =>
            builder.WithAdditionalAllowedSchemas("orders", "inventory"));

        var registry = new ModuleSchemaRegistry(options);

        // Act - Reporting module queries across orders and inventory (PostgreSQL syntax, no table aliases)
        // Note: SqlSchemaExtractor may detect table aliases as schema references.
        var sql = """
            SELECT orders."ModuleOrders"."OrderNumber", inventory."ModuleInventoryItems"."ProductName"
            FROM orders."ModuleOrders"
            INNER JOIN inventory."ModuleInventoryItems" ON orders."ModuleOrders"."Id" = inventory."ModuleInventoryItems"."Id"
            """;
        var result = registry.ValidateSqlAccess("Reporting", sql);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Exception Details Tests

    [Fact]
    public async Task Interceptor_CrossModuleAccess_ExceptionContainsModuleName()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");

        // Act & Assert
        var exception = await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await context.Database.ExecuteSqlRawAsync("SELECT * FROM inventory.\"ModuleInventoryItems\"");
        });

        exception.ModuleName.ShouldBe("Orders");
        exception.UnauthorizedSchemas.ShouldContain("inventory");
    }

    [Fact]
    public async Task Interceptor_CrossModuleAccess_ExceptionMessageIsDescriptive()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");

        // Act & Assert
        var exception = await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await context.Database.ExecuteSqlRawAsync("SELECT * FROM inventory.\"ModuleInventoryItems\"");
        });

        exception.Message.ShouldContain("Orders");
        exception.Message.ShouldContain("inventory");
    }

    #endregion

    #region Cross-Module Query Detection Tests

    [Fact]
    public void SqlValidation_JoinQueryAcrossModules_ShouldFail()
    {

        // Arrange - Join between orders and inventory (PostgreSQL syntax)
        var sql = """
            SELECT o."OrderNumber", i."ProductName"
            FROM orders."ModuleOrders" o
            JOIN inventory."ModuleInventoryItems" i ON o."Id" = i."Id"
            """;

        // Act
        var result = _schemaRegistry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    [Fact]
    public void SqlValidation_SubqueryAcrossModules_ShouldFail()
    {

        // Arrange - Subquery referencing inventory from orders (PostgreSQL syntax)
        var sql = """
            SELECT * FROM orders."ModuleOrders"
            WHERE "Id" IN (SELECT "Id" FROM inventory."ModuleInventoryItems" WHERE "QuantityInStock" > 0)
            """;

        // Act
        var result = _schemaRegistry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    [Fact]
    public void SqlValidation_JoinWithSharedTable_ShouldPass()
    {

        // Arrange - Join between own schema and shared (PostgreSQL syntax, no table aliases)
        // Note: SqlSchemaExtractor may detect table aliases as schema references.
        var sql = """
            SELECT orders."ModuleOrders"."OrderNumber", shared."ModuleLookups"."DisplayName"
            FROM orders."ModuleOrders"
            INNER JOIN shared."ModuleLookups" ON orders."ModuleOrders"."Status" = shared."ModuleLookups"."Code"
            """;

        // Act
        var result = _schemaRegistry.ValidateSqlAccess("Orders", sql);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Interceptor_InsertToOwnSchema_ShouldSucceed()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");
        var orderId = Guid.NewGuid();

        // Act (PostgreSQL syntax)
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO orders.\"ModuleOrders\" (\"Id\", \"OrderNumber\", \"CustomerName\", \"Total\", \"Status\", \"CreatedAtUtc\") " +
            "VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            orderId, $"ORD-{Guid.NewGuid():N}"[..15], "Test", 100m, "Pending", DateTime.UtcNow);

        // Assert - should not throw
        var count = await context.ModuleOrders.CountAsync();
        count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Interceptor_UpdateInOwnSchema_ShouldSucceed()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");
        var order = new ModuleOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..15],
            CustomerName = "Test Customer",
            Total = 100m,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        context.ModuleOrders.Add(order);
        await context.SaveChangesAsync();

        // Act (PostgreSQL syntax)
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE orders.\"ModuleOrders\" SET \"Status\" = {0} WHERE \"Id\" = {1}",
            "Completed", order.Id);

        // Assert - Use AsNoTracking to get fresh data from database
        var updated = await context.ModuleOrders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        updated.Status.ShouldBe("Completed");
    }

    [Fact]
    public async Task Interceptor_DeleteFromOwnSchema_ShouldSucceed()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");
        var order = new ModuleOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..15],
            CustomerName = "Test Customer",
            Total = 100m,
            Status = "ToDelete",
            CreatedAtUtc = DateTime.UtcNow
        };
        context.ModuleOrders.Add(order);
        await context.SaveChangesAsync();

        // Act (PostgreSQL syntax)
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM orders.\"ModuleOrders\" WHERE \"Id\" = {0}", order.Id);

        // Assert
        var exists = await context.ModuleOrders.AnyAsync(o => o.Id == order.Id);
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task Interceptor_InsertToCrossModuleSchema_ShouldThrow()
    {

        // Arrange
        await using var context = CreateDbContextWithInterceptor("Orders");

        // Act & Assert (PostgreSQL syntax)
        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO inventory.\"ModuleInventoryItems\" (\"Id\", \"Sku\", \"ProductName\", \"QuantityInStock\", \"ReorderThreshold\", \"LastUpdatedAtUtc\") " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                Guid.NewGuid(), "SKU-001", "Test Product", 10, 5, DateTime.UtcNow);
        });
    }

    #endregion

    #region Module Context Switching Behavior Tests

    [Fact]
    public async Task Interceptor_SameQueryAllowedOrBlockedByContext()
    {

        // The same query should succeed for Inventory but fail for Orders (PostgreSQL syntax)
        const string sql = "SELECT * FROM inventory.\"ModuleInventoryItems\"";

        // Act 1 - Inventory module can access inventory
        await using var inventoryContext = CreateDbContextWithInterceptor("Inventory");
        await inventoryContext.Database.ExecuteSqlRawAsync(sql);

        // Act 2 - Orders module cannot access inventory
        await using var ordersContext = CreateDbContextWithInterceptor("Orders");
        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await ordersContext.Database.ExecuteSqlRawAsync(sql);
        });
    }

    [Fact]
    public async Task Interceptor_ContextScopeChangesValidation()
    {

        // Arrange
        var interceptor = new ModuleSchemaValidationInterceptor(
            _moduleContext,
            _schemaRegistry,
            _isolationOptions,
            NullLogger<ModuleSchemaValidationInterceptor>.Instance);

        var optionsBuilder = new DbContextOptionsBuilder<ModuleTestDbContext>();
        optionsBuilder.UseNpgsql(_fixture.ConnectionString);
        optionsBuilder.AddInterceptors(interceptor);

        // Act & Assert - Change context mid-stream using scope
        await using var context = new ModuleTestDbContext(optionsBuilder.Options, usePostgreSql: true);

        // With Orders context - inventory query should fail
        using (_moduleContext.CreateScope("Orders"))
        {
            await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            {
                await context.Database.ExecuteSqlRawAsync("SELECT * FROM inventory.\"ModuleInventoryItems\"");
            });
        }

        // With Inventory context - inventory query should succeed
        using (_moduleContext.CreateScope("Inventory"))
        {
            await context.Database.ExecuteSqlRawAsync("SELECT * FROM inventory.\"ModuleInventoryItems\"");
        }
    }

    [Fact]
    public async Task Interceptor_NoModuleContext_BypassesValidation()
    {

        // Arrange - Create interceptor but don't set module context
        var noModuleContext = new TestModuleExecutionContext();
        // Intentionally NOT setting any module

        var interceptor = new ModuleSchemaValidationInterceptor(
            noModuleContext,
            _schemaRegistry,
            _isolationOptions,
            NullLogger<ModuleSchemaValidationInterceptor>.Instance);

        var optionsBuilder = new DbContextOptionsBuilder<ModuleTestDbContext>();
        optionsBuilder.UseNpgsql(_fixture.ConnectionString);
        optionsBuilder.AddInterceptors(interceptor);

        await using var context = new ModuleTestDbContext(optionsBuilder.Options, usePostgreSql: true);

        // Act - Query any schema (should bypass validation when no module context)
        // This represents infrastructure queries that don't belong to any module
        await context.Database.ExecuteSqlRawAsync("SELECT * FROM orders.\"ModuleOrders\"");
        await context.Database.ExecuteSqlRawAsync("SELECT * FROM inventory.\"ModuleInventoryItems\"");
        await context.Database.ExecuteSqlRawAsync("SELECT * FROM shared.\"ModuleLookups\"");

        // Assert - No exceptions thrown means validation was bypassed
    }

    #endregion
}

#region Test DbContext and Entities

internal sealed class ModuleTestDbContext : DbContext
{
    private readonly bool _usePostgreSql;

    public ModuleTestDbContext(DbContextOptions<ModuleTestDbContext> options, bool usePostgreSql = false)
        : base(options)
    {
        _usePostgreSql = usePostgreSql;
    }

    public DbSet<ModuleOrder> ModuleOrders => Set<ModuleOrder>();
    public DbSet<ModuleInventoryItem> ModuleInventoryItems => Set<ModuleInventoryItem>();
    public DbSet<ModuleLookup> ModuleLookups => Set<ModuleLookup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (_usePostgreSql)
        {
            // PostgreSQL uses schema-qualified names
            modelBuilder.Entity<ModuleOrder>(entity =>
            {
                entity.ToTable("ModuleOrders", "orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<ModuleInventoryItem>(entity =>
            {
                entity.ToTable("ModuleInventoryItems", "inventory");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<ModuleLookup>(entity =>
            {
                entity.ToTable("ModuleLookups", "shared");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });
        }
    }
}

internal class ModuleOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; }
}

internal class ModuleInventoryItem
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; } = 10;
    public DateTime LastUpdatedAtUtc { get; set; }
}

internal class ModuleLookup
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

#endregion

#region Test Module Execution Context

internal sealed class TestModuleExecutionContext : IModuleExecutionContext
{
    private string? _currentModule;

    public string? CurrentModule => _currentModule;

    public void SetModule(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        _currentModule = moduleName;
    }

    public void ClearModule()
    {
        _currentModule = null;
    }

    public IDisposable CreateScope(string moduleName)
    {
        SetModule(moduleName);
        return new ModuleScope(this);
    }

    private sealed class ModuleScope(TestModuleExecutionContext context) : IDisposable
    {
        public void Dispose() => context.ClearModule();
    }

    // Test helper methods
    public void SetCurrentModule(string? moduleName)
    {
        if (moduleName is null)
            ClearModule();
        else
            SetModule(moduleName);
    }

    public void ClearCurrentModule() => ClearModule();
}

#endregion
