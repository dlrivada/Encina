using System.Data;
using System.Data.Common;
using System.Globalization;
using Encina.ADO.MySQL.Modules;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using MySqlConnector;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.MySQL.ModuleIsolation;

/// <summary>
/// Integration tests for module isolation support in ADO.NET MySQL provider.
/// Tests schema boundary enforcement and cross-module access prevention.
/// </summary>
/// <remarks>
/// MySQL uses table prefixes instead of schemas within a database for module isolation.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("ADO-MySQL")]
public class ModuleIsolationADOIntegrationTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;

    public ModuleIsolationADOIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    private MySqlConnection _connection = null!;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public async Task InitializeAsync()
    {
        _connection = (_fixture.CreateConnection() as MySqlConnection)!;
        await ModuleIsolationSchema.CreateAllModuleSchemasAsync(_connection);

        // Configure module isolation with table prefixes (MySQL doesn't support schemas within a database)
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

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await ModuleIsolationSchema.ClearModuleIsolationDataAsync(_connection);
        }
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private SchemaValidatingConnection CreateSchemaValidatingConnection(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        var innerConnection = (_fixture.CreateConnection() as MySqlConnection)!;
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

    #region SQL Validation Tests (using table prefixes for MySQL)

    [Fact]
    public void SqlValidation_ValidQueryToOwnTable_ShouldPass()
    {

        // MySQL uses table prefixes: orders_Orders instead of orders.Orders
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM orders_Orders WHERE Id = @Id");

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_QueryToSharedTable_ShouldPass()
    {

        // MySQL uses table prefixes: shared_Lookups instead of shared.Lookups
        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM shared_Lookups WHERE Category = @Category");

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SqlValidation_CrossModuleTableAccess_ShouldFail()
    {

        // MySQL uses table prefixes: inventory_InventoryItems instead of inventory.InventoryItems
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
        // Connection is already open from CreateConnection() in fixture

        var order = new OrdersModuleEntity
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerName = "Test Customer",
            Total = 150.00m,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Insert using the validating connection (MySQL uses table prefix)
        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO orders_Orders (Id, OrderNumber, CustomerName, Total, Status, CreatedAtUtc)
            VALUES (@Id, @OrderNumber, @CustomerName, @Total, @Status, @CreatedAtUtc)";
        AddParameter(insertCmd, "@Id", order.Id.ToString());
        AddParameter(insertCmd, "@OrderNumber", order.OrderNumber);
        AddParameter(insertCmd, "@CustomerName", order.CustomerName);
        AddParameter(insertCmd, "@Total", order.Total);
        AddParameter(insertCmd, "@Status", order.Status);
        AddParameter(insertCmd, "@CreatedAtUtc", order.CreatedAtUtc);
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
        // Connection is already open from CreateConnection() in fixture

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
        // Connection is already open from CreateConnection() in fixture

        // Insert a shared lookup first (MySQL uses table prefix)
        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO shared_Lookups (Id, Code, DisplayName, Category, IsActive, SortOrder)
            VALUES (@Id, @Code, @DisplayName, @Category, @IsActive, @SortOrder)";
        AddParameter(insertCmd, "@Id", Guid.NewGuid().ToString());
        AddParameter(insertCmd, "@Code", "STATUS_ACTIVE");
        AddParameter(insertCmd, "@DisplayName", "Active");
        AddParameter(insertCmd, "@Category", "Status");
        AddParameter(insertCmd, "@IsActive", true);
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
