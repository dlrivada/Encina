using System.Data;
using Encina.TestInfrastructure.Schemas;
using MySqlConnector;
using Testcontainers.MySql;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// MySQL database fixture using Testcontainers.
/// Provides a throwaway MySQL instance for integration tests.
/// </summary>
public sealed class MySqlFixture : DatabaseFixture<MySqlContainer>
{
    private MySqlContainer? _container;

    /// <inheritdoc />
    public override string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <inheritdoc />
    public override string ProviderName => "MySQL";

    /// <inheritdoc />
    protected override async Task<MySqlContainer> CreateContainerAsync()
    {
        _container = new MySqlBuilder()
            .WithImage("mysql:9.1")
            .WithDatabase("Encina_test")
            .WithUsername("root")
            .WithPassword("mysql")
            // Enable local_infile for MySqlBulkLoader support in tests
            .WithCommand("--local-infile=1")
            .WithCleanUp(true)
            .Build();

        return await Task.FromResult(_container);
    }

    /// <inheritdoc />
    protected override async Task CreateSchemaAsync(IDbConnection connection)
    {
        if (connection is not MySqlConnection mysqlConnection)
        {
            throw new InvalidOperationException("Connection must be MySqlConnection");
        }

        await MySqlSchema.CreateOutboxSchemaAsync(mysqlConnection);
        await MySqlSchema.CreateInboxSchemaAsync(mysqlConnection);
        await MySqlSchema.CreateSagaSchemaAsync(mysqlConnection);
        await MySqlSchema.CreateSchedulingSchemaAsync(mysqlConnection);
        await MySqlSchema.CreateTestRepositorySchemaAsync(mysqlConnection);
        await MySqlSchema.CreateOrdersSchemaAsync(mysqlConnection);
    }

    /// <inheritdoc />
    protected override async Task DropSchemaAsync(IDbConnection connection)
    {
        if (connection is not MySqlConnection mysqlConnection)
        {
            throw new InvalidOperationException("Connection must be MySqlConnection");
        }

        await MySqlSchema.DropAllSchemasAsync(mysqlConnection);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Clears all data from all tables (but preserves schema).
    /// Use this between tests to ensure clean state.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        using var connection = CreateConnection();
        if (connection is not MySqlConnection mysqlConnection)
        {
            throw new InvalidOperationException("Connection must be MySqlConnection");
        }

        await MySqlSchema.ClearAllDataAsync(mysqlConnection);
    }
}
