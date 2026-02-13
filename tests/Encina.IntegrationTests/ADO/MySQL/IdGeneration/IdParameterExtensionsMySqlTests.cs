using System.Data;
using Encina.ADO.MySQL;
using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;

namespace Encina.IntegrationTests.ADO.MySQL.IdGeneration;

/// <summary>
/// Integration tests for ADO.NET <see cref="IdParameterExtensions"/> with MySQL.
/// Verifies persist-retrieve roundtrips for all four ID types.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.MySQL")]
[Collection("ADO-MySQL")]
public sealed class IdParameterExtensionsMySqlTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;

    public IdParameterExtensionsMySqlTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable) return;

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS IdGenerationTest (
                SnowflakeCol BIGINT NULL,
                UlidCol VARCHAR(36) NULL,
                UuidV7Col CHAR(36) NULL,
                ShardPrefixedCol VARCHAR(256) NULL
            );
            DELETE FROM IdGenerationTest;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnowflakeId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var original = new SnowflakeId(123456789L);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (SnowflakeCol) VALUES (@Id)";
        insertCmd.AddSnowflakeIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT SnowflakeCol FROM IdGenerationTest WHERE SnowflakeCol IS NOT NULL LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        new SnowflakeId((long)result!).ShouldBe(original);
    }

    [Fact]
    public async Task UlidId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var original = UlidId.NewUlid();

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (UlidCol) VALUES (@Id)";
        insertCmd.AddUlidIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT UlidCol FROM IdGenerationTest WHERE UlidCol IS NOT NULL ORDER BY UlidCol DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        // MySQL stores ULID as string
        UlidId.Parse((string)result!).ShouldBe(original);
    }

    [Fact]
    public async Task UuidV7Id_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var generator = new UuidV7IdGenerator();
        var original = generator.Generate().Match(id => id, _ => default);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (UuidV7Col) VALUES (@Id)";
        insertCmd.AddUuidV7IdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT UuidV7Col FROM IdGenerationTest WHERE UuidV7Col IS NOT NULL ORDER BY UuidV7Col DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        result.ShouldNotBeNull();

        // MySqlConnector may materialize CHAR(36) UUID values as either string or Guid
        // depending on connection settings (e.g., GuidFormat).
        var parsed = result switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            _ => throw new InvalidCastException($"Unexpected UUID materialization type: {result.GetType().FullName}")
        };

        new UuidV7Id(parsed).Value.ShouldBe(original.Value);
    }

    [Fact]
    public async Task ShardPrefixedId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container not available");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (ShardPrefixedCol) VALUES (@Id)";
        insertCmd.AddShardPrefixedIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT ShardPrefixedCol FROM IdGenerationTest WHERE ShardPrefixedCol IS NOT NULL ORDER BY ShardPrefixedCol DESC LIMIT 1";
        var result = await selectCmd.ExecuteScalarAsync();

        ShardPrefixedId.Parse((string)result!).ToString().ShouldBe(original.ToString());
    }
}
