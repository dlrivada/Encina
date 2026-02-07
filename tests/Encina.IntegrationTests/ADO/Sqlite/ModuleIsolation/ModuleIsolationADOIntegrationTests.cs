using System.Data;
using System.Data.Common;
using System.Globalization;
using Encina.ADO.Sqlite.Modules;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.Sqlite.ModuleIsolation;

/// <summary>
/// Integration tests for module isolation support in ADO.NET SQLite provider.
/// </summary>
/// <remarks>
/// <para>
/// SQLite does not support schemas natively. Module isolation in SQLite uses
/// table name prefixes to simulate schema boundaries.
/// </para>
/// <para>
/// These tests focus on the ModuleSchemaRegistry validation logic and SQL
/// pattern validation rather than actual database schema enforcement.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("ADO-Sqlite")]
public class ModuleIsolationADOIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private SqliteConnection _connection = null!;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public ModuleIsolationADOIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _connection = (_fixture.CreateConnection() as SqliteConnection)!;

        // Create tables with prefix naming convention
        await CreatePrefixedTablesAsync(_connection);

        // Configure module isolation with prefixes (SQLite doesn't support real schemas)
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

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task CreatePrefixedTablesAsync(SqliteConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        // Create orders table with prefix
        const string ordersSql = """
            DROP TABLE IF EXISTS orders_Orders;
            CREATE TABLE orders_Orders (
                Id TEXT PRIMARY KEY,
                OrderNumber TEXT NOT NULL UNIQUE,
                CustomerName TEXT NOT NULL,
                Total REAL NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Pending',
                CreatedAtUtc TEXT NOT NULL
            );
            """;

        // Create inventory table with prefix
        const string inventorySql = """
            DROP TABLE IF EXISTS inventory_InventoryItems;
            CREATE TABLE inventory_InventoryItems (
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
            DROP TABLE IF EXISTS shared_Lookups;
            CREATE TABLE shared_Lookups (
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

    private SchemaValidatingConnection CreateSchemaValidatingConnection(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        // Create a NEW independent connection to the same shared in-memory DB.
        // SchemaValidatingConnection will dispose the inner connection on Dispose(),
        // so we must NOT give it the fixture's shared connection.
        var innerConnection = new SqliteConnection(_fixture.ConnectionString);
        innerConnection.Open();
        return new SchemaValidatingConnection(innerConnection, _moduleContext, _schemaRegistry, _isolationOptions);
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

    #region SQL Validation Tests (table prefix style for SQLite)

    [Fact]
    public void SqlValidation_ValidQueryToOwnPrefixedTable_ShouldPass()
    {
        // SQLite uses table prefixes: orders_Orders
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM orders_Orders WHERE Id = @Id");

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_QueryToSharedPrefixedTable_ShouldPass()
    {
        // SQLite uses table prefixes: shared_Lookups
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM shared_Lookups WHERE Category = @Category");

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_CrossModulePrefixedTableAccess_ShouldFail()
    {
        // SQLite uses table prefixes: inventory_InventoryItems
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM inventory_InventoryItems WHERE Sku = @Sku");

        // Assert
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    #endregion

    #region SchemaValidatingConnection Tests

    [Fact]
    public async Task SchemaValidatingConnection_CanExecuteValidQuery()
    {
        // Arrange
        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        // Insert
        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO orders_Orders (Id, OrderNumber, CustomerName, Total, Status, CreatedAtUtc)
            VALUES (@Id, @OrderNumber, @CustomerName, @Total, @Status, @CreatedAtUtc)";
        AddParameter(insertCmd, "@Id", Guid.NewGuid().ToString());
        AddParameter(insertCmd, "@OrderNumber", "ORD-001");
        AddParameter(insertCmd, "@CustomerName", "Test Customer");
        AddParameter(insertCmd, "@Total", 150.00);
        AddParameter(insertCmd, "@Status", "Pending");
        AddParameter(insertCmd, "@CreatedAtUtc", DateTime.UtcNow.ToString("O"));
        await insertCmd.ExecuteNonQueryAsync();

        // Act - Query
        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT COUNT(*) FROM orders_Orders";
        var count = await queryCmd.ExecuteScalarAsync();

        // Assert
        Convert.ToInt32(count, CultureInfo.InvariantCulture).ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SchemaValidatingConnection_ThrowsOnCrossModuleTableAccess()
    {
        // Arrange
        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        // Act & Assert
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM inventory_InventoryItems";

        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await cmd.ExecuteReaderAsync();
        });
    }

    [Fact]
    public async Task SchemaValidatingConnection_AllowsSharedTableAccess()
    {
        // Arrange
        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        // Insert a shared lookup first
        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO shared_Lookups (Id, Code, DisplayName, Category, IsActive, SortOrder)
            VALUES (@Id, @Code, @DisplayName, @Category, @IsActive, @SortOrder)";
        AddParameter(insertCmd, "@Id", Guid.NewGuid().ToString());
        AddParameter(insertCmd, "@Code", "STATUS_ACTIVE");
        AddParameter(insertCmd, "@DisplayName", "Active");
        AddParameter(insertCmd, "@Category", "Status");
        AddParameter(insertCmd, "@IsActive", 1);
        AddParameter(insertCmd, "@SortOrder", 1);
        await insertCmd.ExecuteNonQueryAsync();

        // Act - Query shared table
        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT * FROM shared_Lookups";
        await using var reader = await queryCmd.ExecuteReaderAsync();

        // Assert
        reader.HasRows.ShouldBeTrue();
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

    #endregion

    #region Helper Methods

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        command.Parameters.Add(param);
    }

    #endregion
}

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
