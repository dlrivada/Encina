using System.Data;
using Encina.ADO.Sqlite;
using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;

namespace Encina.IntegrationTests.ADO.Sqlite.IdGeneration;

/// <summary>
/// Integration tests for ADO.NET <see cref="IdParameterExtensions"/> with SQLite.
/// Verifies persist-retrieve roundtrips for all four ID types.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.Sqlite")]
[Collection("ADO-Sqlite")]
public sealed class IdParameterExtensionsSqliteTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;

    public IdParameterExtensionsSqliteTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        // Create IdGeneration test table
        var connection = (SqliteConnection)_fixture.CreateConnection();
        using var cmd = new SqliteCommand("""
            CREATE TABLE IF NOT EXISTS IdGenerationTest (
                SnowflakeCol INTEGER,
                UlidCol TEXT,
                UuidV7Col TEXT,
                ShardPrefixedCol TEXT
            )
            """, connection);
        await cmd.ExecuteNonQueryAsync();

        // Clear any existing data
        using var clearCmd = new SqliteCommand("DELETE FROM IdGenerationTest", connection);
        await clearCmd.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ────────────────────────────────────────────────────────────
    //  SnowflakeId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SnowflakeId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var connection = (SqliteConnection)_fixture.CreateConnection();
        var original = new SnowflakeId(123456789L);

        // Insert
        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (SnowflakeCol) VALUES (@Id)";
        insertCmd.AddSnowflakeIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        // Retrieve
        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT SnowflakeCol FROM IdGenerationTest WHERE SnowflakeCol IS NOT NULL";
        var result = await selectCmd.ExecuteScalarAsync();

        result.ShouldNotBeNull();
        var retrieved = new SnowflakeId((long)result!);
        retrieved.ShouldBe(original);
    }

    [Fact]
    public async Task SnowflakeId_GeneratedValue_PersistsCorrectly()
    {
        var connection = (SqliteConnection)_fixture.CreateConnection();
        var generator = new SnowflakeIdGenerator(new SnowflakeOptions());
        var genResult = generator.Generate();
        genResult.IsRight.ShouldBeTrue();

        var original = genResult.Match(id => id, _ => default);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (SnowflakeCol) VALUES (@Id)";
        insertCmd.AddSnowflakeIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT SnowflakeCol FROM IdGenerationTest ORDER BY rowid DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        var retrieved = new SnowflakeId((long)result!);
        retrieved.Value.ShouldBe(original.Value);
    }

    // ────────────────────────────────────────────────────────────
    //  UlidId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UlidId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var connection = (SqliteConnection)_fixture.CreateConnection();
        var original = UlidId.NewUlid();

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (UlidCol) VALUES (@Id)";
        insertCmd.AddUlidIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT UlidCol FROM IdGenerationTest WHERE UlidCol IS NOT NULL ORDER BY rowid DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        result.ShouldNotBeNull();
        var retrieved = UlidId.Parse((string)result!);
        retrieved.ShouldBe(original);
    }

    // ────────────────────────────────────────────────────────────
    //  UuidV7Id
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UuidV7Id_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var connection = (SqliteConnection)_fixture.CreateConnection();
        var generator = new UuidV7IdGenerator();
        var genResult = generator.Generate();
        genResult.IsRight.ShouldBeTrue();
        var original = genResult.Match(id => id, _ => default);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (UuidV7Col) VALUES (@Id)";
        insertCmd.AddUuidV7IdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT UuidV7Col FROM IdGenerationTest WHERE UuidV7Col IS NOT NULL ORDER BY rowid DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        result.ShouldNotBeNull();
        var parsed = Guid.Parse((string)result!);
        var retrieved = new UuidV7Id(parsed);
        retrieved.Value.ShouldBe(original.Value);
    }

    // ────────────────────────────────────────────────────────────
    //  ShardPrefixedId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ShardPrefixedId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var connection = (SqliteConnection)_fixture.CreateConnection();
        var inner = UlidId.NewUlid();
        var original = ShardPrefixedId.Parse($"shard-01:{inner}");

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (ShardPrefixedCol) VALUES (@Id)";
        insertCmd.AddShardPrefixedIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT ShardPrefixedCol FROM IdGenerationTest WHERE ShardPrefixedCol IS NOT NULL ORDER BY rowid DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        result.ShouldNotBeNull();
        var retrieved = ShardPrefixedId.Parse((string)result!);
        retrieved.ToString().ShouldBe(original.ToString());
    }

    // ────────────────────────────────────────────────────────────
    //  Multiple IDs in same row
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllIdTypes_InsertedInSameRow_RetrieveCorrectly()
    {
        var connection = (SqliteConnection)_fixture.CreateConnection();

        var snowflake = new SnowflakeId(999L);
        var ulid = UlidId.NewUlid();
        var uuid = new UuidV7Id(Guid.NewGuid());
        var shardPrefixed = ShardPrefixedId.Parse($"tenant-a:{ulid}");

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO IdGenerationTest (SnowflakeCol, UlidCol, UuidV7Col, ShardPrefixedCol)
            VALUES (@Snowflake, @Ulid, @Uuid, @ShardPrefixed)
            """;
        insertCmd.AddSnowflakeIdParameter("@Snowflake", snowflake);
        insertCmd.AddUlidIdParameter("@Ulid", ulid);
        insertCmd.AddUuidV7IdParameter("@Uuid", uuid);
        insertCmd.AddShardPrefixedIdParameter("@ShardPrefixed", shardPrefixed);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT SnowflakeCol, UlidCol, UuidV7Col, ShardPrefixedCol FROM IdGenerationTest ORDER BY rowid DESC LIMIT 1";
        using var reader = await selectCmd.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();

        new SnowflakeId(reader.GetInt64(0)).ShouldBe(snowflake);
        UlidId.Parse(reader.GetString(1)).ShouldBe(ulid);
        new UuidV7Id(Guid.Parse(reader.GetString(2))).Value.ShouldBe(uuid.Value);
        ShardPrefixedId.Parse(reader.GetString(3)).ToString().ShouldBe(shardPrefixed.ToString());
    }
}
