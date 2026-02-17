using Dapper;
using Encina.Dapper.MySQL.TypeHandlers;
using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;

namespace Encina.IntegrationTests.Dapper.MySQL.IdGeneration;

/// <summary>
/// Integration tests for Dapper TypeHandlers with MySQL.
/// Verifies persist-retrieve roundtrips for all four ID types using Dapper.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.MySQL")]
[Collection("Dapper-MySQL")]
public sealed class TypeHandlersMySqlTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;

    public TypeHandlersMySqlTests(MySqlFixture fixture)
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

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS DapperIdTest (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                SnowflakeCol BIGINT NULL,
                UlidCol VARCHAR(36) NULL,
                UuidV7Col CHAR(36) NULL,
                ShardPrefixedCol VARCHAR(256) NULL
            );
            DELETE FROM DapperIdTest;
            """);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnowflakeId_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var original = new SnowflakeId(42424242L);

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (SnowflakeCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<SnowflakeId>(
            "SELECT SnowflakeCol FROM DapperIdTest WHERE SnowflakeCol IS NOT NULL ORDER BY Id DESC LIMIT 1");

        retrieved.ShouldBe(original);
    }

    [Fact]
    public async Task UlidId_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var original = UlidId.NewUlid();

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (UlidCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UlidId>(
            "SELECT UlidCol FROM DapperIdTest WHERE UlidCol IS NOT NULL ORDER BY Id DESC LIMIT 1");

        // MySQL stores ULID as string
        retrieved.ShouldBe(original);
    }

    [Fact]
    public async Task UuidV7Id_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var original = new UuidV7IdGenerator().Generate().Match(id => id, _ => default);

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (UuidV7Col) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UuidV7Id>(
            "SELECT UuidV7Col FROM DapperIdTest WHERE UuidV7Col IS NOT NULL ORDER BY Id DESC LIMIT 1");

        retrieved.Value.ShouldBe(original.Value);
    }

    [Fact]
    public async Task ShardPrefixedId_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (ShardPrefixedCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<ShardPrefixedId>(
            "SELECT ShardPrefixedCol FROM DapperIdTest WHERE ShardPrefixedCol IS NOT NULL ORDER BY Id DESC LIMIT 1");

        retrieved.ToString().ShouldBe(original.ToString());
    }
}
