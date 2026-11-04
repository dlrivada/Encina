using System.Data;
using Encina.ADO.SqlServer.BreakTheGlass;
using Encina.ContractTests.Security.BreakTheGlass;
using Encina.Dapper.SqlServer.BreakTheGlass;
using Encina.IntegrationTests.Security.Audit;
using Encina.Messaging.Transactions;
using Encina.Security.BreakTheGlass;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;

namespace Encina.IntegrationTests.Security.BreakTheGlass;

/// <summary>
/// Creates the break-the-glass table on the shared SQL Server fixture from the script a package ships, and
/// hands out stores over independent connections.
/// </summary>
internal static class BreakTheGlassSqlServerSchema
{
    public const string Script = "030_CreateBreakTheGlassElevationsTable.sql";

    /// <summary>
    /// An accessor that reports a business transaction on <paramref name="connection"/>, so that every store call
    /// leases a connection of its own (ADR-048): the facts then run concurrent callers without sharing one
    /// connection, and prove that the writes do not depend on the business transaction.
    /// </summary>
    public static IDbTransactionAccessor BusinessTransactionOn(IDbConnection connection)
    {
        var transaction = Substitute.For<IDbTransaction>();
        transaction.Connection.Returns(connection);
        var accessor = Substitute.For<IDbTransactionAccessor>();
        accessor.Current.Returns(transaction);
        return accessor;
    }

    public static async Task PrepareAsync(SqlServerFixture fixture, string package)
    {
        await fixture.ClearAllDataAsync();
        using var connection = fixture.CreateConnection();
        foreach (var batch in ShippedSqlScript.SplitBatches(ShippedSqlScript.Read(package, Script)))
        {
            await using var create = new SqlCommand(batch, (SqlConnection)connection);
            await create.ExecuteNonQueryAsync();
        }

        await using var clear = new SqlCommand("DELETE FROM BreakTheGlassElevations;", (SqlConnection)connection);
        await clear.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// The shared <see cref="BreakTheGlassStoreContract"/> against <see cref="BreakTheGlassStoreADO"/> on SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class BreakTheGlassStoreADOSqlServerIntegrationTests : BreakTheGlassStoreContract
{
    private readonly SqlServerFixture _fixture;

    public BreakTheGlassStoreADOSqlServerIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IBreakTheGlassStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await BreakTheGlassSqlServerSchema.PrepareAsync(_fixture, "Encina.ADO.SqlServer");
        return CreateSecondStore();
    }

    protected override IBreakTheGlassStore CreateSecondStore()
    {
        var connection = Track(_fixture.CreateConnection());
        return new BreakTheGlassStoreADO(connection, BreakTheGlassSqlServerSchema.BusinessTransactionOn(connection));
    }
}

/// <summary>
/// The shared <see cref="BreakTheGlassStoreContract"/> against <see cref="BreakTheGlassStoreDapper"/> on SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class BreakTheGlassStoreDapperSqlServerIntegrationTests : BreakTheGlassStoreContract
{
    private readonly SqlServerFixture _fixture;

    public BreakTheGlassStoreDapperSqlServerIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IBreakTheGlassStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await BreakTheGlassSqlServerSchema.PrepareAsync(_fixture, "Encina.Dapper.SqlServer");
        return CreateSecondStore();
    }

    protected override IBreakTheGlassStore CreateSecondStore()
    {
        var connection = Track(_fixture.CreateConnection());
        return new BreakTheGlassStoreDapper(connection, BreakTheGlassSqlServerSchema.BusinessTransactionOn(connection));
    }
}
