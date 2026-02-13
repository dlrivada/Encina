using Dapper;
using Encina.Dapper.PostgreSQL.TypeHandlers;
using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;

namespace Encina.IntegrationTests.Dapper.PostgreSQL.IdGeneration;

/// <summary>
/// Integration tests for Dapper TypeHandlers with PostgreSQL.
/// Verifies persist-retrieve roundtrips for all four ID types using Dapper.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.PostgreSQL")]
[Collection("Dapper-PostgreSQL")]
public sealed class TypeHandlersPostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public TypeHandlersPostgreSqlTests(PostgreSqlFixture fixture)
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

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS dapper_id_test (
                id SERIAL PRIMARY KEY,
                snowflake_col BIGINT,
                ulid_col UUID,
                uuid_v7_col UUID,
                shard_prefixed_col TEXT
            );
            DELETE FROM dapper_id_test;
            """);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnowflakeId_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var original = new SnowflakeId(42424242L);

        await connection.ExecuteAsync(
            "INSERT INTO dapper_id_test (snowflake_col) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<SnowflakeId>(
            "SELECT snowflake_col FROM dapper_id_test WHERE snowflake_col IS NOT NULL ORDER BY id DESC LIMIT 1");

        retrieved.ShouldBe(original);
    }

    [Fact]
    public async Task UlidId_InsertAndQuery_RoundtripsViaGuid()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var original = UlidId.NewUlid();

        await connection.ExecuteAsync(
            "INSERT INTO dapper_id_test (ulid_col) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UlidId>(
            "SELECT ulid_col FROM dapper_id_test WHERE ulid_col IS NOT NULL ORDER BY id DESC LIMIT 1");

        retrieved.ToGuid().ShouldBe(original.ToGuid());
    }

    [Fact]
    public async Task UuidV7Id_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var original = new UuidV7IdGenerator().Generate().Match(id => id, _ => default);

        await connection.ExecuteAsync(
            "INSERT INTO dapper_id_test (uuid_v7_col) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<UuidV7Id>(
            "SELECT uuid_v7_col FROM dapper_id_test WHERE uuid_v7_col IS NOT NULL ORDER BY id DESC LIMIT 1");

        retrieved.Value.ShouldBe(original.Value);
    }

    [Fact]
    public async Task ShardPrefixedId_InsertAndQuery_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        await connection.ExecuteAsync(
            "INSERT INTO dapper_id_test (shard_prefixed_col) VALUES (@Value)",
            new { Value = original });

        var retrieved = await connection.QuerySingleAsync<ShardPrefixedId>(
            "SELECT shard_prefixed_col FROM dapper_id_test WHERE shard_prefixed_col IS NOT NULL ORDER BY id DESC LIMIT 1");

        retrieved.ToString().ShouldBe(original.ToString());
    }
}
