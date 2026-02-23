using System.Data;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// SQL Server database fixture using Testcontainers.
/// Provides a throwaway SQL Server instance for integration tests.
/// </summary>
public sealed class SqlServerFixture : DatabaseFixture<MsSqlContainer>
{
    private MsSqlContainer? _container;

    /// <inheritdoc />
    public override string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <inheritdoc />
    public override string ProviderName => "SqlServer";

    /// <inheritdoc />
    protected override async Task<MsSqlContainer> CreateContainerAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("StrongP@ssw0rd!")
            .WithCleanUp(true)
            .Build();

        return await Task.FromResult(_container);
    }

    /// <inheritdoc />
    protected override async Task CreateSchemaAsync(IDbConnection connection)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("Connection must be SqlConnection");
        }

        await SqlServerSchema.CreateOutboxSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateInboxSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateSagaSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateSchedulingSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateRepositoryTestSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateOrdersSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateTenantTestSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateReadWriteTestSchemaAsync(sqlConnection);
        await SqlServerSchema.CreateConsentSchemaAsync(sqlConnection);
    }

    /// <inheritdoc />
    protected override async Task DropSchemaAsync(IDbConnection connection)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("Connection must be SqlConnection");
        }

        await SqlServerSchema.DropAllSchemasAsync(sqlConnection);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(ConnectionString);
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
        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("Connection must be SqlConnection");
        }

        await SqlServerSchema.ClearAllDataAsync(sqlConnection);
    }
}
