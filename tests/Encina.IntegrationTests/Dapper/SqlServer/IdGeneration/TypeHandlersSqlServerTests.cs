using Dapper;
using Encina.Dapper.SqlServer.TypeHandlers;
using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;

namespace Encina.IntegrationTests.Dapper.SqlServer.IdGeneration;

/// <summary>
/// Integration tests for Dapper TypeHandlers with SQL Server.
/// Verifies persist-retrieve roundtrips for all four ID types using Dapper.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class TypeHandlersSqlServerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;

    public TypeHandlersSqlServerTests(SqlServerFixture fixture)
    {
        _fixture = fixture;

        SnowflakeIdTypeHandler.EnsureRegistered();
        UlidIdTypeHandler.EnsureRegistered();
        UuidV7IdTypeHandler.EnsureRegistered();
        ShardPrefixedIdTypeHandler.EnsureRegistered();
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable) return;

        using var connection = (SqlConnection)_fixture.CreateConnection();
        await connection.ExecuteAsync("""
            IF OBJECT_ID('DapperIdTest', 'U') IS NULL
            CREATE TABLE DapperIdTest (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                SnowflakeCol BIGINT NULL,
                UlidCol UNIQUEIDENTIFIER NULL,
                UuidV7Col UNIQUEIDENTIFIER NULL,
                ShardPrefixedCol NVARCHAR(256) NULL
            );
            DELETE FROM DapperIdTest;
            """);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnowflakeId_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var original = new SnowflakeId(42424242L);

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (SnowflakeCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<SnowflakeId>(
            "SELECT TOP 1 SnowflakeCol FROM DapperIdTest WHERE SnowflakeCol IS NOT NULL ORDER BY Id DESC");

        retrieved.ShouldBe(original);
    }

    [Fact]
    public async Task UlidId_InsertAndQuery_RoundtripsViaGuid()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var original = UlidId.NewUlid();

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (UlidCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UlidId>(
            "SELECT TOP 1 UlidCol FROM DapperIdTest WHERE UlidCol IS NOT NULL ORDER BY Id DESC");

        retrieved.ToGuid().ShouldBe(original.ToGuid());
    }

    [Fact]
    public async Task UuidV7Id_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var original = new UuidV7IdGenerator().Generate().Match(id => id, _ => default);

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (UuidV7Col) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UuidV7Id>(
            "SELECT TOP 1 UuidV7Col FROM DapperIdTest WHERE UuidV7Col IS NOT NULL ORDER BY Id DESC");

        retrieved.Value.ShouldBe(original.Value);
    }

    [Fact]
    public async Task ShardPrefixedId_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (ShardPrefixedCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<ShardPrefixedId>(
            "SELECT TOP 1 ShardPrefixedCol FROM DapperIdTest WHERE ShardPrefixedCol IS NOT NULL ORDER BY Id DESC");

        retrieved.ToString().ShouldBe(original.ToString());
    }
}
