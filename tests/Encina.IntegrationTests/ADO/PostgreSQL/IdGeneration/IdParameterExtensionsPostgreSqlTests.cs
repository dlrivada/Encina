using System.Data;
using Encina.ADO.PostgreSQL;
using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;

namespace Encina.IntegrationTests.ADO.PostgreSQL.IdGeneration;

/// <summary>
/// Integration tests for ADO.NET <see cref="IdParameterExtensions"/> with PostgreSQL.
/// Verifies persist-retrieve roundtrips for all four ID types.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class IdParameterExtensionsPostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public IdParameterExtensionsPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable) return;

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS id_generation_test (
                snowflake_col BIGINT,
                ulid_col UUID,
                uuid_v7_col UUID,
                shard_prefixed_col TEXT
            );
            DELETE FROM id_generation_test;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnowflakeId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var original = new SnowflakeId(123456789L);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO id_generation_test (snowflake_col) VALUES (@Id)";
        insertCmd.AddSnowflakeIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT snowflake_col FROM id_generation_test WHERE snowflake_col IS NOT NULL LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        new SnowflakeId((long)result!).ShouldBe(original);
    }

    [Fact]
    public async Task UlidId_PersistAndRetrieve_RoundtripsViaGuid()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var original = UlidId.NewUlid();

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO id_generation_test (ulid_col) VALUES (@Id)";
        insertCmd.AddUlidIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT ulid_col FROM id_generation_test WHERE ulid_col IS NOT NULL ORDER BY ulid_col DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        // PostgreSQL stores ULID as UUID
        var retrieved = new UlidId((Guid)result!);
        retrieved.ToGuid().ShouldBe(original.ToGuid());
    }

    [Fact]
    public async Task UuidV7Id_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var generator = new UuidV7IdGenerator();
        var original = generator.Generate().Match(id => id, _ => default);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO id_generation_test (uuid_v7_col) VALUES (@Id)";
        insertCmd.AddUuidV7IdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT uuid_v7_col FROM id_generation_test WHERE uuid_v7_col IS NOT NULL ORDER BY uuid_v7_col DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        new UuidV7Id((Guid)result!).Value.ShouldBe(original.Value);
    }

    [Fact]
    public async Task ShardPrefixedId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "PostgreSQL container not available");

        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO id_generation_test (shard_prefixed_col) VALUES (@Id)";
        insertCmd.AddShardPrefixedIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT shard_prefixed_col FROM id_generation_test WHERE shard_prefixed_col IS NOT NULL ORDER BY shard_prefixed_col DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        ShardPrefixedId.Parse((string)result!).ToString().ShouldBe(original.ToString());
    }
}
