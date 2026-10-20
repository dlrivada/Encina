using System.Data;
using Encina.ADO.SqlServer;
using Encina.ADO.SqlServer.Outbox;
using Encina.IntegrationTests.Messaging;
using Encina.IntegrationTests.Messaging.Outbox;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Encina.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.IntegrationTests.ADO.SqlServer.Outbox;

/// <summary>
/// Transactional outbox on ADO.NET over a real SQL Server (#718): the business change and the outbox rows of a
/// request commit or roll back together.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class OutboxAtomicityTests : OutboxAtomicityTestsBase
{
    private const string BusinessTableSql = """
        IF OBJECT_ID('OutboxAtomicityBusiness', 'U') IS NULL
            CREATE TABLE OutboxAtomicityBusiness (Id INT PRIMARY KEY, Name NVARCHAR(100) NOT NULL);
        DELETE FROM OutboxAtomicityBusiness;
        """;

    private readonly SqlServerFixture _fixture;

    public OutboxAtomicityTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
        Harness = new OutboxAtomicityScenario.Harness
        {
            Register = (services, configure) =>
            {
                services.AddScoped<IDbConnection>(_ => fixture.CreateConnection());
                services.AddEncinaADO(configure);
            },
            RegisterWithModuleIsolation = (services, configure) =>
                services.AddEncinaADO(_ => fixture.CreateConnection(), config =>
                {
                    configure(config);
                    config.UseModuleIsolation = true;
                }),
            RegisterWithTenancy = (services, configure, defaultConnectionString) =>
            {
                services.AddSingleton(Options.Create(new TenancyOptions { DefaultConnectionString = defaultConnectionString }));
                global::Encina.ADO.SqlServer.Tenancy.TenancyServiceCollectionExtensions.AddEncinaADOWithTenancy(services, configure);
            },
            CreateUnitOfWork = (accessor, services) => new global::Encina.ADO.SqlServer.UnitOfWork.UnitOfWorkADO(accessor, services),
            CreateInnerOutboxFactory = () => new OutboxMessageFactory(),
            CreateConnection = fixture.CreateConnection,
            CreateTenantDatabaseAsync = CreateTenantDatabaseAsync,
            OpenConnection = connectionString => new SqlConnection(connectionString),
            ConnectionString = fixture.ConnectionString,
            PrepareAsync = PrepareAsync,
            ReadInboxRow = InboxPipelineHarnesses.AdoSqlServer(fixture).ReadRow,
            InsertBusinessSql = "INSERT INTO OutboxAtomicityBusiness (Id, Name) VALUES (@id, @name)",
            CountBusinessSql = "SELECT COUNT(*) FROM OutboxAtomicityBusiness",
            CountOutboxSql = "SELECT COUNT(*) FROM OutboxMessages"
        };
    }

    protected override OutboxAtomicityScenario.Harness Harness { get; }

    private async Task PrepareAsync()
    {
        await _fixture.ClearAllDataAsync();
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, BusinessTableSql);
    }

    private async Task<string> CreateTenantDatabaseAsync(string name)
    {
        await using (var admin = new SqlConnection(_fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await ExecuteAsync(admin, $"CREATE DATABASE [{name}]");
        }

        var tenantConnectionString = new SqlConnectionStringBuilder(_fixture.ConnectionString) { InitialCatalog = name }.ConnectionString;
        await using var tenant = new SqlConnection(tenantConnectionString);
        await tenant.OpenAsync();
        await SqlServerSchema.CreateOutboxSchemaAsync(tenant);
        await SqlServerSchema.CreateInboxSchemaAsync(tenant);
        await ExecuteAsync(tenant, BusinessTableSql);
        return tenantConnectionString;
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
