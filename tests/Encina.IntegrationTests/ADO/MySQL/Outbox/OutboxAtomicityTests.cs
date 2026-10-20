using System.Data;
using System.Data.Common;
using Encina.ADO.MySQL;
using Encina.ADO.MySQL.Outbox;
using Encina.IntegrationTests.Messaging;
using Encina.IntegrationTests.Messaging.Outbox;
using Encina.Modules.Isolation;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Xunit;

namespace Encina.IntegrationTests.ADO.MySQL.Outbox;

/// <summary>
/// Transactional outbox on ADO.NET over a real MySQL (#718): the business change and the outbox rows of a
/// request commit or roll back together.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("ADO-MySQL")]
public sealed class OutboxAtomicityTests : OutboxAtomicityTestsBase
{
    private const string BusinessTableSql = """
        CREATE TABLE IF NOT EXISTS OutboxAtomicityBusiness (Id INT PRIMARY KEY, Name VARCHAR(100) NOT NULL);
        DELETE FROM OutboxAtomicityBusiness;
        """;

    private readonly MySqlFixture _fixture;

    public OutboxAtomicityTests(MySqlFixture fixture)
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
                    return new global::Encina.ADO.MySQL.Modules.SchemaValidatingConnection(
                        (DbConnection)fixture.CreateConnection(), new ModuleExecutionContext(), new ModuleSchemaRegistry(options), options);
                });
                services.AddEncinaADO(configure);
            },
            RegisterWithTenancy = (services, configure, defaultConnectionString) =>
            {
                services.AddSingleton(Options.Create(new TenancyOptions { DefaultConnectionString = defaultConnectionString }));
                global::Encina.ADO.MySQL.Tenancy.TenancyServiceCollectionExtensions.AddEncinaADOMySQLWithTenancy(services, configure);
            },
            CreateUnitOfWork = (accessor, services) => new global::Encina.ADO.MySQL.UnitOfWork.UnitOfWorkADO(accessor, services),
            CreateInnerOutboxFactory = () => new OutboxMessageFactory(),
            CreateConnection = fixture.CreateConnection,
            CreateTenantDatabaseAsync = CreateTenantDatabaseAsync,
            OpenConnection = connectionString => new MySqlConnection(connectionString),
            ConnectionString = fixture.ConnectionString,
            PrepareAsync = PrepareAsync,
            ReadInboxRow = InboxPipelineHarnesses.AdoMySql(fixture).ReadRow,
            InsertBusinessSql = "INSERT INTO OutboxAtomicityBusiness (Id, Name) VALUES (@id, @name)",
            CountBusinessSql = "SELECT COUNT(*) FROM OutboxAtomicityBusiness",
            CountOutboxSql = "SELECT COUNT(*) FROM OutboxMessages"
        };
    }

    protected override OutboxAtomicityScenario.Harness Harness { get; }

    private async Task PrepareAsync()
    {
        await _fixture.ClearAllDataAsync();
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, BusinessTableSql);
    }

    private async Task<string> CreateTenantDatabaseAsync(string name)
    {
        await using (var admin = new MySqlConnection(_fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await ExecuteAsync(admin, $"CREATE DATABASE `{name}`");
        }

        var tenantConnectionString = new MySqlConnectionStringBuilder(_fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var tenant = new MySqlConnection(tenantConnectionString);
        await tenant.OpenAsync();
        await MySqlSchema.CreateOutboxSchemaAsync(tenant);
        await MySqlSchema.CreateInboxSchemaAsync(tenant);
        await ExecuteAsync(tenant, BusinessTableSql);
        return tenantConnectionString;
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
