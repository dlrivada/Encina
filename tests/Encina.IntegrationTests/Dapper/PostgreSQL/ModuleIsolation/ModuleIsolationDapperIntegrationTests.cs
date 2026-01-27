using System.Data;
using System.Data.Common;
using System.Globalization;
using Encina.Dapper.PostgreSQL.Modules;
using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.PostgreSQL.ModuleIsolation;

/// <summary>
/// Integration tests for module isolation support in Dapper PostgreSQL provider.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL supports real schemas. Module isolation uses proper schema.table syntax.
/// </para>
/// <para>
/// These tests focus on the ModuleAwareConnectionFactory, ModuleSchemaRegistry
/// validation logic, and SQL pattern validation for Dapper operations.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public class ModuleIsolationDapperIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture = new();
    private NpgsqlConnection _connection = null!;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        if (!_fixture.IsAvailable)
            return;

        _connection = (_fixture.CreateConnection() as NpgsqlConnection)!;

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

    private static async Task CreateSchemasAndTablesAsync(NpgsqlConnection connection)
    {
        // Create schemas
        const string schemasSql = """
            CREATE SCHEMA IF NOT EXISTS orders;
            CREATE SCHEMA IF NOT EXISTS inventory;
            CREATE SCHEMA IF NOT EXISTS shared;
            """;

        await using var schemasCmd = new NpgsqlCommand(schemasSql, connection);
        await schemasCmd.ExecuteNonQueryAsync();

        // Create orders table
        const string ordersSql = """
            DROP TABLE IF EXISTS orders.orders CASCADE;
            CREATE TABLE orders.orders (
                id UUID PRIMARY KEY,
                order_number VARCHAR(50) NOT NULL UNIQUE,
                customer_name VARCHAR(200) NOT NULL,
                total DECIMAL(18,2) NOT NULL,
                status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                created_at_utc TIMESTAMP NOT NULL
            );
            """;

        // Create inventory table
        const string inventorySql = """
            DROP TABLE IF EXISTS inventory.inventory_items CASCADE;
            CREATE TABLE inventory.inventory_items (
                id UUID PRIMARY KEY,
                sku VARCHAR(50) NOT NULL UNIQUE,
                product_name VARCHAR(200) NOT NULL,
                quantity_in_stock INT NOT NULL DEFAULT 0,
                reorder_threshold INT NOT NULL DEFAULT 10,
                last_updated_at_utc TIMESTAMP NOT NULL
            );
            """;

        // Create shared table
        const string sharedSql = """
            DROP TABLE IF EXISTS shared.lookups CASCADE;
            CREATE TABLE shared.lookups (
                id UUID PRIMARY KEY,
                code VARCHAR(50) NOT NULL,
                display_name VARCHAR(200) NOT NULL,
                category VARCHAR(100) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                sort_order INT NOT NULL DEFAULT 0
            );
            """;

        await using var ordersCmd = new NpgsqlCommand(ordersSql, connection);
        await ordersCmd.ExecuteNonQueryAsync();

        await using var inventoryCmd = new NpgsqlCommand(inventorySql, connection);
        await inventoryCmd.ExecuteNonQueryAsync();

        await using var sharedCmd = new NpgsqlCommand(sharedSql, connection);
        await sharedCmd.ExecuteNonQueryAsync();
    }

    private SchemaValidatingConnection CreateSchemaValidatingConnection(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        var innerConnection = (_fixture.CreateConnection() as NpgsqlConnection)!;
        return new SchemaValidatingConnection(innerConnection, _moduleContext, _schemaRegistry, _isolationOptions);
    }

    private ModuleAwareConnectionFactory CreateModuleAwareConnectionFactory(string moduleName)
    {
        _moduleContext.SetCurrentModule(moduleName);
        return new ModuleAwareConnectionFactory(
            () => (_fixture.CreateConnection() as NpgsqlConnection)!,
            _moduleContext,
            _schemaRegistry,
            _isolationOptions);
    }

    #region ModuleAwareConnectionFactory Tests

    [SkippableFact]
    public void ModuleAwareConnectionFactory_CreatesSchemaValidatingConnection_WhenStrategyIsDevValidation()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange - SchemaWithPermissions strategy doesn't wrap connections (DB handles validation)
        var noValidationOptions = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.SchemaWithPermissions
        };
        noValidationOptions.AddModuleSchema("Orders", "orders");

        var noValidationRegistry = new ModuleSchemaRegistry(noValidationOptions);
        _moduleContext.SetCurrentModule("Orders");

        var factory = new ModuleAwareConnectionFactory(
            () => (_fixture.CreateConnection() as NpgsqlConnection)!,
            _moduleContext,
            noValidationRegistry,
            noValidationOptions);

        // Act
        var connection = factory.CreateConnection();

        // Assert - should be the original connection, not wrapped
        connection.ShouldBeOfType<NpgsqlConnection>();
    }

    #endregion

    #region Schema Registry Validation Tests

    [SkippableFact]
    public void SchemaRegistry_ShouldAllowAccessToOwnSchema()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        _schemaRegistry.CanAccessSchema("Orders", "orders").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "inventory").ShouldBeTrue();
    }

    [SkippableFact]
    public void SchemaRegistry_ShouldAllowAccessToSharedSchema()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        _schemaRegistry.CanAccessSchema("Orders", "shared").ShouldBeTrue();
        _schemaRegistry.CanAccessSchema("Inventory", "shared").ShouldBeTrue();
    }

    [SkippableFact]
    public void SchemaRegistry_ShouldDenyAccessToOtherModuleSchemas()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        _schemaRegistry.CanAccessSchema("Orders", "inventory").ShouldBeFalse();
        _schemaRegistry.CanAccessSchema("Inventory", "orders").ShouldBeFalse();
    }

    #endregion

    #region SQL Validation Tests (schema.table style for PostgreSQL)

    [SkippableFact]
    public void SqlValidation_ValidQueryToOwnSchema_ShouldPass()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM orders.orders WHERE id = @Id");
        result.IsValid.ShouldBeTrue();
    }

    [SkippableFact]
    public void SqlValidation_QueryToSharedSchema_ShouldPass()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM shared.lookups WHERE category = @Category");
        result.IsValid.ShouldBeTrue();
    }

    [SkippableFact]
    public void SqlValidation_CrossModuleSchemaAccess_ShouldFail()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        var result = _schemaRegistry.ValidateSqlAccess("Orders", "SELECT * FROM inventory.inventory_items WHERE sku = @Sku");
        result.IsValid.ShouldBeFalse();
        result.UnauthorizedSchemas.ShouldContain("inventory");
    }

    #endregion

    #region SchemaValidatingConnection Tests

    [SkippableFact]
    public async Task SchemaValidatingConnection_CanExecuteValidQuery()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO orders.orders (id, order_number, customer_name, total, status, created_at_utc)
            VALUES (@id, @order_number, @customer_name, @total, @status, @created_at_utc)";
        AddParameter(insertCmd, "@id", Guid.NewGuid());
        AddParameter(insertCmd, "@order_number", "ORD-001");
        AddParameter(insertCmd, "@customer_name", "Test Customer");
        AddParameter(insertCmd, "@total", 150.00m);
        AddParameter(insertCmd, "@status", "Pending");
        AddParameter(insertCmd, "@created_at_utc", DateTime.UtcNow);
        await insertCmd.ExecuteNonQueryAsync();

        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT COUNT(*) FROM orders.orders";
        var count = await queryCmd.ExecuteScalarAsync();

        Convert.ToInt32(count, CultureInfo.InvariantCulture).ShouldBeGreaterThanOrEqualTo(1);
    }

    [SkippableFact]
    public async Task SchemaValidatingConnection_ThrowsOnCrossModuleTableAccess()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM inventory.inventory_items";

        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
        {
            await cmd.ExecuteReaderAsync();
        });
    }

    [SkippableFact]
    public async Task SchemaValidatingConnection_AllowsSharedTableAccess()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        await using var connection = CreateSchemaValidatingConnection("Orders");
        await connection.OpenAsync();

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO shared.lookups (id, code, display_name, category, is_active, sort_order)
            VALUES (@id, @code, @display_name, @category, @is_active, @sort_order)";
        AddParameter(insertCmd, "@id", Guid.NewGuid());
        AddParameter(insertCmd, "@code", "STATUS_ACTIVE");
        AddParameter(insertCmd, "@display_name", "Active");
        AddParameter(insertCmd, "@category", "Status");
        AddParameter(insertCmd, "@is_active", true);
        AddParameter(insertCmd, "@sort_order", 1);
        await insertCmd.ExecuteNonQueryAsync();

        await using var queryCmd = connection.CreateCommand();
        queryCmd.CommandText = "SELECT * FROM shared.lookups";
        await using var reader = await queryCmd.ExecuteReaderAsync();

        reader.HasRows.ShouldBeTrue();
    }

    #endregion

    #region Module Context Tests

    [SkippableFact]
    public void ModuleContext_CanBeSwitched()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
