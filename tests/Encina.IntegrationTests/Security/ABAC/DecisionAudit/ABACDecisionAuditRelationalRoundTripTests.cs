using System.Data;
using System.Data.Common;

using Encina.IntegrationTests.Security.Audit;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;

using AdoMySqlStore = Encina.ADO.MySQL.Auditing.OperationAuditStoreADO;
using AdoPostgreSqlStore = Encina.ADO.PostgreSQL.Auditing.OperationAuditStoreADO;
using AdoSqlServerStore = Encina.ADO.SqlServer.Auditing.OperationAuditStoreADO;
using DapperMySqlStore = Encina.Dapper.MySQL.Auditing.OperationAuditStoreDapper;
using DapperPostgreSqlStore = Encina.Dapper.PostgreSQL.Auditing.OperationAuditStoreDapper;
using DapperSqlServerStore = Encina.Dapper.SqlServer.Auditing.OperationAuditStoreDapper;

namespace Encina.IntegrationTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Prepares the <c>OperationAuditEntries</c> table of a relational provider from the script the
/// package ships, and empties it before each test.
/// </summary>
internal static class OperationAuditTable
{
    public static async Task PrepareAsync(IDbConnection connection, string package, bool splitBatches, string deleteSql)
    {
        var dbConnection = (DbConnection)connection;
        var script = ShippedSqlScript.Read(package, ShippedSqlScript.OperationAuditEntries);
        IReadOnlyList<string> batches = splitBatches ? ShippedSqlScript.SplitBatches(script) : [script];

        foreach (var sql in batches.Append(deleteSql))
        {
            await using var command = dbConnection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }
}

/// <summary>Shared lifecycle of the ADO.NET and Dapper round trips: one fixture connection per test.</summary>
public abstract class ABACDecisionAuditConnectionRoundTripTestsBase : ABACDecisionAuditRoundTripTestsBase, IAsyncLifetime
{
    protected IDbConnection Connection { get; private set; } = null!;

    protected abstract IDbConnection OpenConnection();

    protected abstract Task PrepareTableAsync();

    public async ValueTask InitializeAsync()
    {
        Connection = OpenConnection();
        await PrepareTableAsync();
    }

    public ValueTask DisposeAsync()
    {
        Connection.Dispose();
        return ValueTask.CompletedTask;
    }
}

[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class ABACDecisionAuditADOSqlServerRoundTripTests(SqlServerFixture fixture) : ABACDecisionAuditConnectionRoundTripTestsBase
{
    protected override IDbConnection OpenConnection() => fixture.CreateConnection();

    protected override Task PrepareTableAsync() =>
        OperationAuditTable.PrepareAsync(Connection, "Encina.ADO.SqlServer", splitBatches: true, "DELETE FROM OperationAuditEntries;");

    protected override IOperationAuditStore CreateStore() => new AdoSqlServerStore(Connection);
}

[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class ABACDecisionAuditADOPostgreSqlRoundTripTests(PostgreSqlFixture fixture) : ABACDecisionAuditConnectionRoundTripTestsBase
{
    protected override IDbConnection OpenConnection() => fixture.CreateConnection();

    protected override Task PrepareTableAsync() =>
        OperationAuditTable.PrepareAsync(Connection, "Encina.ADO.PostgreSQL", splitBatches: false, """DELETE FROM "OperationAuditEntries";""");

    protected override IOperationAuditStore CreateStore() => new AdoPostgreSqlStore(Connection);
}

[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("ADO-MySQL")]
public sealed class ABACDecisionAuditADOMySqlRoundTripTests(MySqlFixture fixture) : ABACDecisionAuditConnectionRoundTripTestsBase
{
    protected override IDbConnection OpenConnection() => fixture.CreateConnection();

    protected override Task PrepareTableAsync() =>
        OperationAuditTable.PrepareAsync(Connection, "Encina.ADO.MySQL", splitBatches: false, "DELETE FROM `OperationAuditEntries`;");

    protected override IOperationAuditStore CreateStore() => new AdoMySqlStore(Connection);
}

[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class ABACDecisionAuditDapperSqlServerRoundTripTests(SqlServerFixture fixture) : ABACDecisionAuditConnectionRoundTripTestsBase
{
    protected override IDbConnection OpenConnection() => fixture.CreateConnection();

    protected override Task PrepareTableAsync() =>
        OperationAuditTable.PrepareAsync(Connection, "Encina.Dapper.SqlServer", splitBatches: true, "DELETE FROM OperationAuditEntries;");

    protected override IOperationAuditStore CreateStore() => new DapperSqlServerStore(Connection);
}

[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("Dapper-PostgreSQL")]
public sealed class ABACDecisionAuditDapperPostgreSqlRoundTripTests(PostgreSqlFixture fixture) : ABACDecisionAuditConnectionRoundTripTestsBase
{
    protected override IDbConnection OpenConnection() => fixture.CreateConnection();

    protected override Task PrepareTableAsync() =>
        OperationAuditTable.PrepareAsync(Connection, "Encina.Dapper.PostgreSQL", splitBatches: false, """DELETE FROM "OperationAuditEntries";""");

    protected override IOperationAuditStore CreateStore() => new DapperPostgreSqlStore(Connection);
}

[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("Dapper-MySQL")]
public sealed class ABACDecisionAuditDapperMySqlRoundTripTests(MySqlFixture fixture) : ABACDecisionAuditConnectionRoundTripTestsBase
{
    protected override IDbConnection OpenConnection() => fixture.CreateConnection();

    protected override Task PrepareTableAsync() =>
        OperationAuditTable.PrepareAsync(Connection, "Encina.Dapper.MySQL", splitBatches: false, "DELETE FROM `OperationAuditEntries`;");

    protected override IOperationAuditStore CreateStore() => new DapperMySqlStore(Connection);
}
