using System.Data;
using System.Data.Common;
using Encina.ADO.PostgreSQL;
using Encina.ADO.PostgreSQL.Outbox;
using Encina.IntegrationTests.Messaging;
using Encina.IntegrationTests.Messaging.Outbox;
using Encina.Modules.Isolation;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace Encina.IntegrationTests.ADO.PostgreSQL.Outbox;

/// <summary>
/// Transactional outbox on ADO.NET over a real PostgreSQL (#718): the business change and the outbox rows of a
/// request commit or roll back together.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class OutboxAtomicityTests : OutboxAtomicityTestsBase
{
    private const string BusinessTableSql = """
        CREATE TABLE IF NOT EXISTS outboxatomicitybusiness (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL);
        DELETE FROM outboxatomicitybusiness;
        """;

    private readonly PostgreSqlFixture _fixture;

    public OutboxAtomicityTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        Harness = new OutboxAtomicityScenario.Harness
        {
            Register = (services, configure) =>
            {
                services.AddScoped<IDbConnection>(_ => fixture.CreateConnection());
                services.AddEncinaADO(configure);
            },
            // AddEncinaADO of this provider has no module isolation support (see the follow-up issue), so the
            // scoped connection is decorated the way the inbox module isolation tests do.
            RegisterWithModuleIsolation = (services, configure) =>
            {
                services.AddScoped<IDbConnection>(_ =>
                {
                    var options = new ModuleIsolationOptions { Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly };
                    return new global::Encina.ADO.PostgreSQL.Modules.SchemaValidatingConnection(
                        (DbConnection)fixture.CreateConnection(), new ModuleExecutionContext(), new ModuleSchemaRegistry(options), options);
                });
                services.AddEncinaADO(configure);
            },
            RegisterWithTenancy = (services, configure, defaultConnectionString) =>
            {
                services.AddSingleton(Options.Create(new TenancyOptions { DefaultConnectionString = defaultConnectionString }));
                global::Encina.ADO.PostgreSQL.Tenancy.TenancyServiceCollectionExtensions.AddEncinaADOPostgreSQLWithTenancy(services, configure);
            },
            CreateUnitOfWork = (accessor, services) => new global::Encina.ADO.PostgreSQL.UnitOfWork.UnitOfWorkADO(accessor, services),
            CreateInnerOutboxFactory = () => new OutboxMessageFactory(),
            CreateConnection = fixture.CreateConnection,
            CreateTenantDatabaseAsync = CreateTenantDatabaseAsync,
            OpenConnection = connectionString => new NpgsqlConnection(connectionString),
            ConnectionString = fixture.ConnectionString,
            PrepareAsync = PrepareAsync,
            ReadInboxRow = InboxPipelineHarnesses.AdoPostgreSql(fixture).ReadRow,
            InsertBusinessSql = "INSERT INTO outboxatomicitybusiness (id, name) VALUES (@id, @name)",
            CountBusinessSql = "SELECT COUNT(*) FROM outboxatomicitybusiness",
            CountOutboxSql = "SELECT COUNT(*) FROM outboxmessages"
        };
    }

    protected override OutboxAtomicityScenario.Harness Harness { get; }

    private async Task PrepareAsync()
    {
        await _fixture.ClearAllDataAsync();
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, BusinessTableSql);
    }

    private async Task<string> CreateTenantDatabaseAsync(string name)
    {
        await using (var admin = new NpgsqlConnection(_fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await ExecuteAsync(admin, $"CREATE DATABASE {name}");
        }

        var tenantConnectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var tenant = new NpgsqlConnection(tenantConnectionString);
        await tenant.OpenAsync();
        await PostgreSqlSchema.CreateOutboxSchemaAsync(tenant);
        await PostgreSqlSchema.CreateInboxSchemaAsync(tenant);
        await ExecuteAsync(tenant, BusinessTableSql);
        return tenantConnectionString;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
