using System.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Oracle.ManagedDataAccess.Client;
using SimpleMediator.TestInfrastructure.Schemas;

namespace SimpleMediator.TestInfrastructure.Fixtures;

/// <summary>
/// Oracle database fixture using Testcontainers.
/// Provides a throwaway Oracle instance for integration tests.
/// Uses GenericContainer since there's no official Oracle module.
/// </summary>
public sealed class OracleFixture : DatabaseFixture<IContainer>
{
    private IContainer? _container;
    private string _connectionString = string.Empty;

    /// <inheritdoc />
    public override string ConnectionString => _connectionString;

    /// <inheritdoc />
    public override string ProviderName => "Oracle";

    /// <inheritdoc />
    protected override async Task<IContainer> CreateContainerAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("gvenzl/oracle-free:23-slim-faststart")
            .WithPortBinding(1521, true)
            .WithEnvironment("ORACLE_PASSWORD", "OraclePwd123")
            .WithEnvironment("APP_USER", "simplemediator")
            .WithEnvironment("APP_USER_PASSWORD", "SimplePwd123")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("DATABASE IS READY TO USE!"))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Build connection string after container starts
        var port = _container.GetMappedPublicPort(1521);
        _connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT={port}))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)));User Id=simplemediator;Password=SimplePwd123;";

        return _container;
    }

    /// <inheritdoc />
    protected override async Task CreateSchemaAsync(IDbConnection connection)
    {
        if (connection is not OracleConnection oracleConnection)
        {
            throw new InvalidOperationException("Connection must be OracleConnection");
        }

        await OracleSchema.CreateOutboxSchemaAsync(oracleConnection);
        await OracleSchema.CreateInboxSchemaAsync(oracleConnection);
        await OracleSchema.CreateSagaSchemaAsync(oracleConnection);
        await OracleSchema.CreateSchedulingSchemaAsync(oracleConnection);
    }

    /// <inheritdoc />
    protected override async Task DropSchemaAsync(IDbConnection connection)
    {
        if (connection is not OracleConnection oracleConnection)
        {
            throw new InvalidOperationException("Connection must be OracleConnection");
        }

        await OracleSchema.DropAllSchemasAsync(oracleConnection);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        var connection = new OracleConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        Container = await CreateContainerAsync();

        // Container already started in CreateContainerAsync
        // Wait a bit more for Oracle to be fully ready
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Create schema
        using var connection = CreateConnection();
        await CreateSchemaAsync(connection);
    }

    /// <summary>
    /// Clears all data from all tables (but preserves schema).
    /// Use this between tests to ensure clean state.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        using var connection = CreateConnection();
        if (connection is not OracleConnection oracleConnection)
        {
            throw new InvalidOperationException("Connection must be OracleConnection");
        }

        await OracleSchema.ClearAllDataAsync(oracleConnection);
    }

    /// <inheritdoc />
    public override async Task DisposeAsync()
    {
        // Drop schema
        try
        {
            using var connection = CreateConnection();
            await DropSchemaAsync(connection);
        }
        catch
        {
            // Best effort cleanup
        }

        // Stop and dispose container
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
