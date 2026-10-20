using System.Data;
using Encina.TestInfrastructure.Schemas;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// PostgreSQL database fixture using Testcontainers.
/// Provides a throwaway PostgreSQL instance for integration tests.
/// </summary>
public sealed class PostgreSqlFixture : DatabaseFixture<PostgreSqlContainer>
{
    private readonly bool _includeDeadLetterSchema;
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Initializes the fixture with the complete base schema.
    /// </summary>
    public PostgreSqlFixture()
        : this(includeDeadLetterSchema: true)
    {
    }

    /// <summary>
    /// Initializes the fixture.
    /// </summary>
    /// <param name="includeDeadLetterSchema">
    /// <c>false</c> leaves the <c>DeadLetterMessages</c> table out of the base schema. The EF Core fixture uses
    /// it: the EF model creates that table itself (with its own indexes), and a base-schema table of the same name
    /// would make <c>EFCoreSchema.CreateTablesAsync</c> roll back every other table of the model.
    /// </param>
    internal PostgreSqlFixture(bool includeDeadLetterSchema)
    {
        _includeDeadLetterSchema = includeDeadLetterSchema;
    }

    /// <inheritdoc />
    public override string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <inheritdoc />
    public override string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    protected override async Task<PostgreSqlContainer> CreateContainerAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("Encina_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        return await Task.FromResult(_container);
    }

    /// <inheritdoc />
    protected override async Task CreateSchemaAsync(IDbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Connection must be NpgsqlConnection");
        }

        await PostgreSqlSchema.CreateOutboxSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateInboxSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateSagaSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateSchedulingSchemaAsync(npgsqlConnection);
        if (_includeDeadLetterSchema)
        {
            await PostgreSqlSchema.CreateDeadLetterSchemaAsync(npgsqlConnection);
        }

        await PostgreSqlSchema.CreateTestRepositorySchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateOrdersSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateTenantTestSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateReadWriteTestSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateConsentSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateLawfulBasisSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateProcessingActivitySchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateAbacSchemaAsync(npgsqlConnection);
        await PostgreSqlSchema.CreateDpiaSchemaAsync(npgsqlConnection);
    }

    /// <inheritdoc />
    protected override async Task DropSchemaAsync(IDbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Connection must be NpgsqlConnection");
        }

        await PostgreSqlSchema.DropAllSchemasAsync(npgsqlConnection);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
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
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Connection must be NpgsqlConnection");
        }

        await PostgreSqlSchema.ClearAllDataAsync(npgsqlConnection);
    }
}
