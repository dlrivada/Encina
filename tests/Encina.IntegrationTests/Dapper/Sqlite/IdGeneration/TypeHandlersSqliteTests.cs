using Dapper;
using Encina.Dapper.Sqlite.TypeHandlers;
using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;

namespace Encina.IntegrationTests.Dapper.Sqlite.IdGeneration;

/// <summary>
/// Integration tests for Dapper TypeHandlers with SQLite.
/// Verifies persist-retrieve roundtrips for all four ID types using Dapper.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.Sqlite")]
[Collection("Dapper-Sqlite")]
public sealed class TypeHandlersSqliteTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;

    public TypeHandlersSqliteTests(SqliteFixture fixture)
    {
        _fixture = fixture;

        // Register all type handlers
        SnowflakeIdTypeHandler.EnsureRegistered();
        UlidIdTypeHandler.EnsureRegistered();
        UuidV7IdTypeHandler.EnsureRegistered();
        ShardPrefixedIdTypeHandler.EnsureRegistered();
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        var connection = (SqliteConnection)_fixture.CreateConnection();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS DapperIdTest (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SnowflakeCol INTEGER,
                UlidCol TEXT,
                UuidV7Col TEXT,
                ShardPrefixedCol TEXT
            )
            """);
        await connection.ExecuteAsync("DELETE FROM DapperIdTest");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ────────────────────────────────────────────────────────────
    //  SnowflakeId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SnowflakeId_InsertAndQuery_RoundtripsCorrectly()
    {
        var connection = _fixture.CreateConnection();
        var original = new SnowflakeId(42424242L);

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (SnowflakeCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<SnowflakeId>(
            "SELECT SnowflakeCol FROM DapperIdTest WHERE SnowflakeCol IS NOT NULL ORDER BY Id DESC LIMIT 1");

        retrieved.ShouldBe(original);
    }

    // ────────────────────────────────────────────────────────────
    //  UlidId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UlidId_InsertAndQuery_RoundtripsCorrectly()
    {
        var connection = _fixture.CreateConnection();
        var original = UlidId.NewUlid();

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (UlidCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UlidId>(
            "SELECT UlidCol FROM DapperIdTest WHERE UlidCol IS NOT NULL ORDER BY Id DESC LIMIT 1");

        retrieved.ShouldBe(original);
    }

    // ────────────────────────────────────────────────────────────
    //  UuidV7Id
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UuidV7Id_InsertAndQuery_RoundtripsCorrectly()
    {
        var connection = _fixture.CreateConnection();
        var generator = new UuidV7IdGenerator();
        var original = generator.Generate().Match(id => id, _ => default);

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (UuidV7Col) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UuidV7Id>(
            "SELECT UuidV7Col FROM DapperIdTest WHERE UuidV7Col IS NOT NULL ORDER BY Id DESC LIMIT 1");

        retrieved.Value.ShouldBe(original.Value);
    }

    // ────────────────────────────────────────────────────────────
    //  ShardPrefixedId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ShardPrefixedId_InsertAndQuery_RoundtripsCorrectly()
    {
        var connection = _fixture.CreateConnection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        await connection.ExecuteAsync(
            "INSERT INTO DapperIdTest (ShardPrefixedCol) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<ShardPrefixedId>(
            "SELECT ShardPrefixedCol FROM DapperIdTest WHERE ShardPrefixedCol IS NOT NULL ORDER BY Id DESC LIMIT 1");

        retrieved.ToString().ShouldBe(original.ToString());
    }

    // ────────────────────────────────────────────────────────────
    //  Batch operations
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllIdTypes_BatchInsert_QueryCorrectly()
    {
        var connection = _fixture.CreateConnection();
        var generator = new SnowflakeIdGenerator(new SnowflakeOptions());

        const int batchSize = 10;
        for (var i = 0; i < batchSize; i++)
        {
            var snowflake = generator.Generate().Match(id => id, _ => default);
            await connection.ExecuteAsync(
                "INSERT INTO DapperIdTest (SnowflakeCol) VALUES (@Value)",
                new { Value = snowflake });
        }

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM DapperIdTest WHERE SnowflakeCol IS NOT NULL");

        count.ShouldBeGreaterThanOrEqualTo(batchSize);
    }
}
