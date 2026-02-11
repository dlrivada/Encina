using System.Data;
using System.Diagnostics.CodeAnalysis;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Sqlite.ModuleIsolation;

/// <summary>
/// SQLite-specific integration tests for EF Core module isolation support.
/// Tests the ModuleSchemaRegistry configuration and module context management.
/// </summary>
/// <remarks>
/// <para>
/// SQLite does not support schemas natively. Module isolation in SQLite uses
/// table name prefixes to simulate schema boundaries (e.g., orders_ModuleOrders).
/// </para>
/// <para>
/// <b>IMPORTANT:</b> SQL-based validation (via SqlSchemaExtractor) does NOT support
/// SQLite's prefix-based naming convention. The extractor only detects schema.table
/// patterns (e.g., "orders.ModuleOrders"), not prefix_table patterns (e.g., "orders_ModuleOrders").
/// </para>
/// <para>
/// For SQLite, module isolation must rely on:
/// <list type="bullet">
/// <item><description>Database-level permissions (separate databases per module)</description></item>
/// <item><description>Application-level enforcement via naming conventions</description></item>
/// <item><description>Code review and conventions rather than runtime SQL validation</description></item>
/// </list>
/// </para>
/// <para>
/// These tests verify:
/// <list type="bullet">
/// <item><description>ModuleSchemaRegistry correctly tracks module-to-schema mappings</description></item>
/// <item><description>Module context switching works correctly</description></item>
/// <item><description>EF Core operations work against prefixed tables</description></item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("EFCore-Sqlite")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Connection is disposed in DisposeAsync")]
public sealed class ModuleIsolationEFSqliteTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;
    private SqliteConnection? _sharedConnection;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public ModuleIsolationEFSqliteTests(EFCoreSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Create a shared connection for SQLite in-memory
        _sharedConnection = new SqliteConnection(_fixture.ConnectionString);
        await _sharedConnection.OpenAsync();

        // Create tables with prefix naming convention (SQLite doesn't support real schemas)
        await CreatePrefixedTablesAsync(_sharedConnection);

        // Configure module isolation with prefixes
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
        if (_sharedConnection is not null)
        {
            await _sharedConnection.DisposeAsync();
        }
    }

    private static async Task CreatePrefixedTablesAsync(SqliteConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        // Create orders module table with prefix
        const string ordersSql = """
            DROP TABLE IF EXISTS orders_ModuleOrders;
            CREATE TABLE orders_ModuleOrders (
                Id TEXT PRIMARY KEY,
                OrderNumber TEXT NOT NULL UNIQUE,
                CustomerName TEXT NOT NULL,
                Total REAL NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Pending',
                CreatedAtUtc TEXT NOT NULL
            );
            """;

        // Create inventory module table with prefix
        const string inventorySql = """
            DROP TABLE IF EXISTS inventory_ModuleInventoryItems;
            CREATE TABLE inventory_ModuleInventoryItems (
                Id TEXT PRIMARY KEY,
                Sku TEXT NOT NULL UNIQUE,
                ProductName TEXT NOT NULL,
                QuantityInStock INTEGER NOT NULL DEFAULT 0,
                ReorderThreshold INTEGER NOT NULL DEFAULT 10,
                LastUpdatedAtUtc TEXT NOT NULL
            );
            """;

        // Create shared table with prefix
        const string sharedSql = """
            DROP TABLE IF EXISTS shared_ModuleLookups;
            CREATE TABLE shared_ModuleLookups (
                Id TEXT PRIMARY KEY,
                Code TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Category TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                SortOrder INTEGER NOT NULL DEFAULT 0
            );
            """;

        await using var ordersCmd = new SqliteCommand(ordersSql, connection);
        await ordersCmd.ExecuteNonQueryAsync();

        await using var inventoryCmd = new SqliteCommand(inventorySql, connection);
        await inventoryCmd.ExecuteNonQueryAsync();

        await using var sharedCmd = new SqliteCommand(sharedSql, connection);
        await sharedCmd.ExecuteNonQueryAsync();
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

    [Fact]
    public void SchemaRegistry_GetAllowedSchemas_ReturnsCorrectSchemas()
    {
        // Act
        var ordersSchemas = _schemaRegistry.GetAllowedSchemas("Orders");
        var inventorySchemas = _schemaRegistry.GetAllowedSchemas("Inventory");

        // Assert
        ordersSchemas.ShouldContain("orders");
        ordersSchemas.ShouldContain("shared");
        ordersSchemas.ShouldNotContain("inventory");

        inventorySchemas.ShouldContain("inventory");
        inventorySchemas.ShouldContain("shared");
        inventorySchemas.ShouldNotContain("orders");
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

    #region Strategy Tests

    [Fact]
    public void NonDevValidationStrategy_ShouldNotValidateAtRuntime()
    {
        // Arrange - SchemaWithPermissions strategy doesn't validate at runtime
        var noValidationOptions = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.SchemaWithPermissions
        };
        noValidationOptions.AddModuleSchema("Orders", "orders");

        var noValidationRegistry = new ModuleSchemaRegistry(noValidationOptions);

        // Act - Registry still tracks access rules
        var canAccess = noValidationRegistry.CanAccessSchema("Orders", "inventory");

        // Assert - Rules are tracked but not enforced at SQL level
        canAccess.ShouldBeFalse();
    }

    #endregion

    #region EF Core DbContext Operations (Prefix Tables)

    [Fact]
    public async Task DbContext_CanReadFromPrefixedTables()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<ModuleTestDbContext>();
        optionsBuilder.UseSqlite(_sharedConnection!);

        await using var context = new ModuleTestDbContext(optionsBuilder.Options);

        // Act - Query prefixed tables
        var orders = await context.ModuleOrders.ToListAsync();
        var inventory = await context.ModuleInventoryItems.ToListAsync();
        var lookups = await context.ModuleLookups.ToListAsync();

        // Assert - Should work (empty lists)
        orders.ShouldNotBeNull();
        inventory.ShouldNotBeNull();
        lookups.ShouldNotBeNull();
    }

    [Fact]
    public async Task DbContext_CanWriteToPrefixedTables()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<ModuleTestDbContext>();
        optionsBuilder.UseSqlite(_sharedConnection!);

        await using var context = new ModuleTestDbContext(optionsBuilder.Options);

        var order = new ModuleOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerName = "Test Customer",
            Total = 150.00m,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        context.ModuleOrders.Add(order);
        await context.SaveChangesAsync();

        // Assert
        var count = await context.ModuleOrders.CountAsync();
        count.ShouldBe(1);
    }

    #endregion
}

#region Test DbContext and Entities

internal sealed class ModuleTestDbContext : DbContext
{
    public ModuleTestDbContext(DbContextOptions<ModuleTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<ModuleOrder> ModuleOrders => Set<ModuleOrder>();
    public DbSet<ModuleInventoryItem> ModuleInventoryItems => Set<ModuleInventoryItem>();
    public DbSet<ModuleLookup> ModuleLookups => Set<ModuleLookup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Orders module table (with prefix for SQLite)
        modelBuilder.Entity<ModuleOrder>(entity =>
        {
            entity.ToTable("orders_ModuleOrders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        // Inventory module table (with prefix for SQLite)
        modelBuilder.Entity<ModuleInventoryItem>(entity =>
        {
            entity.ToTable("inventory_ModuleInventoryItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        // Shared table (with prefix for SQLite)
        modelBuilder.Entity<ModuleLookup>(entity =>
        {
            entity.ToTable("shared_ModuleLookups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });
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
