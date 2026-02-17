using System.Data;
using System.Data.Common;
using System.Globalization;
using Encina.Dapper.MySQL.Modules;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.MySQL.ModuleIsolation;

/// <summary>
/// Integration tests for module isolation support in Dapper MySQL provider.
/// </summary>
/// <remarks>
/// <para>
/// MySQL does not support schemas in the same way as SQL Server or PostgreSQL.
/// Module isolation in MySQL uses database prefixes or separate databases
/// to simulate schema boundaries.
/// </para>
/// <para>
/// These tests focus on the ModuleAwareConnectionFactory, ModuleSchemaRegistry
/// validation logic, and SQL pattern validation for Dapper operations.
/// </para>
/// </remarks>
[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
public class ModuleIsolationDapperIntegrationTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;
    private MySqlConnection _connection = null!;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public ModuleIsolationDapperIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        _connection = (_fixture.CreateConnection() as MySqlConnection)!;

        // Create tables with prefix naming convention
        await CreatePrefixedTablesAsync(_connection);

        // Configure module isolation with prefixes (MySQL uses prefix naming like SQLite)
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
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private static async Task CreatePrefixedTablesAsync(MySqlConnection connection)
    {
        // Create orders table with prefix
        const string ordersSql = """
            DROP TABLE IF EXISTS orders_Orders;
            CREATE TABLE orders_Orders (
                Id CHAR(36) PRIMARY KEY,
                OrderNumber VARCHAR(50) NOT NULL UNIQUE,
                CustomerName VARCHAR(200) NOT NULL,
                Total DECIMAL(18,2) NOT NULL,
                Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                CreatedAtUtc DATETIME(6) NOT NULL
            );
            """;

        // Create inventory table with prefix
        const string inventorySql = """
            DROP TABLE IF EXISTS inventory_InventoryItems;
            CREATE TABLE inventory_InventoryItems (
                Id CHAR(36) PRIMARY KEY,
                Sku VARCHAR(50) NOT NULL UNIQUE,
                ProductName VARCHAR(200) NOT NULL,
                QuantityInStock INT NOT NULL DEFAULT 0,
                ReorderThreshold INT NOT NULL DEFAULT 10,
                LastUpdatedAtUtc DATETIME(6) NOT NULL
            );
            """;

        // Create shared table with prefix
        const string sharedSql = """
            DROP TABLE IF EXISTS shared_Lookups;
            CREATE TABLE shared_Lookups (
                Id CHAR(36) PRIMARY KEY,
                Code VARCHAR(50) NOT NULL,
                DisplayName VARCHAR(200) NOT NULL,
                Category VARCHAR(100) NOT NULL,
                IsActive TINYINT(1) NOT NULL DEFAULT 1,
                SortOrder INT NOT NULL DEFAULT 0
            );
            """;

        await using var ordersCmd = new MySqlCommand(ordersSql, connection);
        await ordersCmd.ExecuteNonQueryAsync();

        await using var inventoryCmd = new MySqlCommand(inventorySql, connection);
        await inventoryCmd.ExecuteNonQueryAsync();

        await using var sharedCmd = new MySqlCommand(sharedSql, connection);
        await sharedCmd.ExecuteNonQueryAsync();
    }

    private SchemaValidatingConnection CreateSchemaValidatingConnection(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        var innerConnection = (_fixture.CreateConnection() as MySqlConnection)!;
        return new SchemaValidatingConnection(innerConnection, _moduleContext, _schemaRegistry, _isolationOptions);
    }

    private ModuleAwareConnectionFactory CreateModuleAwareConnectionFactory(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        return new ModuleAwareConnectionFactory(
            () => (_fixture.CreateConnection() as MySqlConnection)!,
            _moduleContext,
            _schemaRegistry,
            _isolationOptions);
    }

    #region ModuleAwareConnectionFactory Tests

    [Fact]
    public void ModuleAwareConnectionFactory_CreatesSchemaValidatingConnection_WhenStrategyIsDevValidation()
    {

        // Arrange
        var factory = CreateModuleAwareConnectionFactory("Orders");

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.ShouldBeOfType<SchemaValidatingConnection>();
    }

    [Fact]
    public void ModuleAwareConnectionFactory_CreatesRegularConnection_WhenStrategyIsNotDevValidation()
    {

        // Arrange - SchemaWithPermissions strategy doesn't wrap connections (DB handles validation)
        var noValidationOptions = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.SchemaWithPermissions
        };
        noValidationOptions.AddModuleSchema("Orders", "orders");

        var noValidationRegistry = new ModuleSchemaRegistry(noValidationOptions);
        _moduleContext.SetCurrentModule("Orders");

        var factory = new ModuleAwareConnectionFactory(
            () => (_fixture.CreateConnection() as MySqlConnection)!,
            _moduleContext,
            noValidationRegistry,
            noValidationOptions);

        // Act
        var connection = factory.CreateConnection();

        // Assert - should be the original connection, not wrapped
        connection.ShouldBeOfType<MySqlConnection>();
    }

    #endregion

    #region Schema Registry Validation Tests

    [Fact]
    public void SchemaRegistry_ShouldAllowAccessToOwnSchema()
    {

        _schemaRegistry.CanAccessSchema("Orders", "orders").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "inventory").ShouldBeTrue();
    }

    [Fact]
    public void SchemaRegistry_ShouldAllowAccessToSharedSchema()
    {

        _schemaRegistry.CanAccessSchema("Orders", "shared").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "shared").ShouldBeTrue();
    }

    [Fact]
    public void SchemaRegistry_ShouldDenyAccessToOtherModuleSchemas()
    {

        _schemaRegistry.CanAccessSchema("Orders", "inventory").ShouldBeFalse();
        _schemaRegistry.CanAccessSchema("Inventory", "orders").ShouldBeFalse();
    }

    #endregion

    #region SQL Validation Tests (table prefix style for MySQL)

    [Fact]
    public void SqlValidation_ValidQueryToOwnPrefixedTable_ShouldPass()
    {

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM orders_Orders WHERE Id = @Id");
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_QueryToSharedPrefixedTable_ShouldPass()
    {

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM shared_Lookups WHERE Category = @Category");
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_CrossModulePrefixedTableAccess_ShouldFail()
    {

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM inventory_InventoryItems WHERE Sku = @Sku");
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    #endregion

    #region SchemaValidatingConnection Tests

    [Fact]
    public async Task SchemaValidatingConnection_CanExecuteValidQuery()
    {

        await using var connection = CreateSchemaValidatingConnection("Orders");
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO orders_Orders (Id, OrderNumber, CustomerName, Total, Status, CreatedAtUtc)
            VALUES (@Id, @OrderNumber, @CustomerName, @Total, @Status, @CreatedAtUtc)";
        AddParameter(insertCmd, "@Id", Guid.NewGuid().ToString());
        AddParameter(insertCmd, "@OrderNumber", "ORD-001");
        AddParameter(insertCmd, "@CustomerName", "Test Customer");
        AddParameter(insertCmd, "@Total", 150.00m);
        AddParameter(insertCmd, "@Status", "Pending");
        AddParameter(insertCmd, "@CreatedAtUtc", DateTime.UtcNow);
        await insertCmd.ExecuteNonQueryAsync();

        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT COUNT(*) FROM orders_Orders";
        var count = await queryCmd.ExecuteScalarAsync();

        Convert.ToInt32(count, CultureInfo.InvariantCulture).ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SchemaValidatingConnection_ThrowsOnCrossModuleTableAccess()
    {

        await using var connection = CreateSchemaValidatingConnection("Orders");
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

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

        await using var connection = CreateSchemaValidatingConnection("Orders");
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

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

        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT * FROM shared_Lookups";
        await using var reader = await queryCmd.ExecuteReaderAsync();

        reader.HasRows.ShouldBeTrue();
    }

    #endregion

    #region Module Context Tests

    [Fact]
    public void ModuleContext_CanBeSwitched()
    {

        _moduleContext.SetCurrentModule("Orders");
        var orders = _moduleContext.CurrentModule;

        _moduleContext.SetCurrentModule("Inventory");
        var inventory = _moduleContext.CurrentModule;

        _moduleContext.ClearCurrentModule();
        var cleared = _moduleContext.CurrentModule;

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
