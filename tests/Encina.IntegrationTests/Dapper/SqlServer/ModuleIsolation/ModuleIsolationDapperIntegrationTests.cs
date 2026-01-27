using System.Data;
using System.Data.Common;
using System.Globalization;
using Encina.Dapper.SqlServer.Modules;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.SqlServer.ModuleIsolation;

/// <summary>
/// Integration tests for module isolation support in Dapper SQL Server provider.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server supports real schemas. Module isolation uses proper schema.table syntax.
/// </para>
/// <para>
/// These tests focus on the ModuleAwareConnectionFactory, ModuleSchemaRegistry
/// validation logic, and SQL pattern validation for Dapper operations.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class ModuleIsolationDapperIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture = new();
    private SqlConnection _connection = null!;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        if (!_fixture.IsAvailable)
            return;

        _connection = (_fixture.CreateConnection() as SqlConnection)!;

        // Create schemas and tables
        await CreateSchemasAndTablesAsync(_connection);

        // Configure module isolation with real schemas
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
        _connection?.Dispose();
        await _fixture.DisposeAsync();
    }

    private static async Task CreateSchemasAndTablesAsync(SqlConnection connection)
    {
        // Create schemas
        const string schemasSql = """
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'orders')
                EXEC('CREATE SCHEMA orders');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'inventory')
                EXEC('CREATE SCHEMA inventory');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'shared')
                EXEC('CREATE SCHEMA shared');
            """;

        await using var schemasCmd = new SqlCommand(schemasSql, connection);
        await schemasCmd.ExecuteNonQueryAsync();

        // Create orders table
        const string ordersSql = """
            DROP TABLE IF EXISTS orders.Orders;
            CREATE TABLE orders.Orders (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
                CustomerName NVARCHAR(200) NOT NULL,
                Total DECIMAL(18,2) NOT NULL,
                Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                CreatedAtUtc DATETIME2 NOT NULL
            );
            """;

        // Create inventory table
        const string inventorySql = """
            DROP TABLE IF EXISTS inventory.InventoryItems;
            CREATE TABLE inventory.InventoryItems (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Sku NVARCHAR(50) NOT NULL UNIQUE,
                ProductName NVARCHAR(200) NOT NULL,
                QuantityInStock INT NOT NULL DEFAULT 0,
                ReorderThreshold INT NOT NULL DEFAULT 10,
                LastUpdatedAtUtc DATETIME2 NOT NULL
            );
            """;

        // Create shared table
        const string sharedSql = """
            DROP TABLE IF EXISTS shared.Lookups;
            CREATE TABLE shared.Lookups (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Code NVARCHAR(50) NOT NULL,
                DisplayName NVARCHAR(200) NOT NULL,
                Category NVARCHAR(100) NOT NULL,
                IsActive BIT NOT NULL DEFAULT 1,
                SortOrder INT NOT NULL DEFAULT 0
            );
            """;

        await using var ordersCmd = new SqlCommand(ordersSql, connection);
        await ordersCmd.ExecuteNonQueryAsync();

        await using var inventoryCmd = new SqlCommand(inventorySql, connection);
        await inventoryCmd.ExecuteNonQueryAsync();

        await using var sharedCmd = new SqlCommand(sharedSql, connection);
        await sharedCmd.ExecuteNonQueryAsync();
    }

    private SchemaValidatingConnection CreateSchemaValidatingConnection(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        var innerConnection = (_fixture.CreateConnection() as SqlConnection)!;
        return new SchemaValidatingConnection(innerConnection, _moduleContext, _schemaRegistry, _isolationOptions);
    }

    private ModuleAwareConnectionFactory CreateModuleAwareConnectionFactory(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        return new ModuleAwareConnectionFactory(
            () => (_fixture.CreateConnection() as SqlConnection)!,
            _moduleContext,
            _schemaRegistry,
            _isolationOptions);
    }

    #region ModuleAwareConnectionFactory Tests

    [SkippableFact]
    public void ModuleAwareConnectionFactory_CreatesSchemaValidatingConnection_WhenStrategyIsDevValidation()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Arrange
        var factory = CreateModuleAwareConnectionFactory("Orders");

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.ShouldBeOfType<SchemaValidatingConnection>();
    }

    [SkippableFact]
    public void ModuleAwareConnectionFactory_CreatesRegularConnection_WhenStrategyIsNotDevValidation()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Arrange - SchemaWithPermissions strategy doesn't wrap connections (DB handles validation)
        var noValidationOptions = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.SchemaWithPermissions
        };
        noValidationOptions.AddModuleSchema("Orders", "orders");

        var noValidationRegistry = new ModuleSchemaRegistry(noValidationOptions);
        _moduleContext.SetCurrentModule("Orders");

        var factory = new ModuleAwareConnectionFactory(
            () => (_fixture.CreateConnection() as SqlConnection)!,
            _moduleContext,
            noValidationRegistry,
            noValidationOptions);

        // Act
        var connection = factory.CreateConnection();

        // Assert - should be the original connection, not wrapped
        connection.ShouldBeOfType<SqlConnection>();
    }

    #endregion

    #region Schema Registry Validation Tests

    [SkippableFact]
    public void SchemaRegistry_ShouldAllowAccessToOwnSchema()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        _schemaRegistry.CanAccessSchema("Orders", "orders").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "inventory").ShouldBeTrue();
    }

    [SkippableFact]
    public void SchemaRegistry_ShouldAllowAccessToSharedSchema()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        _schemaRegistry.CanAccessSchema("Orders", "shared").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "shared").ShouldBeTrue();
    }

    [SkippableFact]
    public void SchemaRegistry_ShouldDenyAccessToOtherModuleSchemas()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        _schemaRegistry.CanAccessSchema("Orders", "inventory").ShouldBeFalse();
        _schemaRegistry.CanAccessSchema("Inventory", "orders").ShouldBeFalse();
    }

    #endregion

    #region SQL Validation Tests (schema.table style for SQL Server)

    [SkippableFact]
    public void SqlValidation_ValidQueryToOwnSchema_ShouldPass()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM orders.Orders WHERE Id = @Id");
        result.IsValid.ShouldBeTrue();
    }

    [SkippableFact]
    public void SqlValidation_QueryToSharedSchema_ShouldPass()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM shared.Lookups WHERE Category = @Category");
        result.IsValid.ShouldBeTrue();
    }

    [SkippableFact]
    public void SqlValidation_CrossModuleSchemaAccess_ShouldFail()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM inventory.InventoryItems WHERE Sku = @Sku");
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    #endregion

    #region SchemaValidatingConnection Tests

    [SkippableFact]
    public async Task SchemaValidatingConnection_CanExecuteValidQuery()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO orders.Orders (Id, OrderNumber, CustomerName, Total, Status, CreatedAtUtc)
            VALUES (@Id, @OrderNumber, @CustomerName, @Total, @Status, @CreatedAtUtc)";
        AddParameter(insertCmd, "@Id", Guid.NewGuid());
        AddParameter(insertCmd, "@OrderNumber", "ORD-001");
        AddParameter(insertCmd, "@CustomerName", "Test Customer");
        AddParameter(insertCmd, "@Total", 150.00m);
        AddParameter(insertCmd, "@Status", "Pending");
        AddParameter(insertCmd, "@CreatedAtUtc", DateTime.UtcNow);
        await insertCmd.ExecuteNonQueryAsync();

        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT COUNT(*) FROM orders.Orders";
        var count = await queryCmd.ExecuteScalarAsync();

        Convert.ToInt32(count, CultureInfo.InvariantCulture).ShouldBeGreaterThanOrEqualTo(1);
    }

    [SkippableFact]
    public async Task SchemaValidatingConnection_ThrowsOnCrossModuleTableAccess()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM inventory.InventoryItems";

        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await cmd.ExecuteReaderAsync();
        });
    }

    [SkippableFact]
    public async Task SchemaValidatingConnection_AllowsSharedTableAccess()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO shared.Lookups (Id, Code, DisplayName, Category, IsActive, SortOrder)
            VALUES (@Id, @Code, @DisplayName, @Category, @IsActive, @SortOrder)";
        AddParameter(insertCmd, "@Id", Guid.NewGuid());
        AddParameter(insertCmd, "@Code", "STATUS_ACTIVE");
        AddParameter(insertCmd, "@DisplayName", "Active");
        AddParameter(insertCmd, "@Category", "Status");
        AddParameter(insertCmd, "@IsActive", true);
        AddParameter(insertCmd, "@SortOrder", 1);
        await insertCmd.ExecuteNonQueryAsync();

        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT * FROM shared.Lookups";
        await using var reader = await queryCmd.ExecuteReaderAsync();

        reader.HasRows.ShouldBeTrue();
    }

    #endregion

    #region Module Context Tests

    [SkippableFact]
    public void ModuleContext_CanBeSwitched()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

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
