using System.Data;
using Encina.ADO.SqlServer;
using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;

namespace Encina.IntegrationTests.ADO.SqlServer.IdGeneration;

/// <summary>
/// Integration tests for ADO.NET <see cref="IdParameterExtensions"/> with SQL Server.
/// Verifies persist-retrieve roundtrips for all four ID types.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class IdParameterExtensionsSqlServerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;

    public IdParameterExtensionsSqlServerTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable) return;

        using var connection = (SqlConnection)_fixture.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            IF OBJECT_ID('IdGenerationTest', 'U') IS NULL
            CREATE TABLE IdGenerationTest (
                SnowflakeCol BIGINT NULL,
                UlidCol UNIQUEIDENTIFIER NULL,
                UuidV7Col UNIQUEIDENTIFIER NULL,
                ShardPrefixedCol NVARCHAR(256) NULL
            );
            DELETE FROM IdGenerationTest;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnowflakeId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var original = new SnowflakeId(123456789L);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (SnowflakeCol) VALUES (@Id)";
        insertCmd.AddSnowflakeIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT TOP 1 SnowflakeCol FROM IdGenerationTest WHERE SnowflakeCol IS NOT NULL";
        var result = await selectCmd.ExecuteScalarAsync();

        new SnowflakeId((long)result!).ShouldBe(original);
    }

    [Fact]
    public async Task UlidId_PersistAndRetrieve_RoundtripsViaGuid()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var original = UlidId.NewUlid();

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (UlidCol) VALUES (@Id)";
        insertCmd.AddUlidIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT TOP 1 UlidCol FROM IdGenerationTest WHERE UlidCol IS NOT NULL ORDER BY UlidCol DESC";
        var result = await selectCmd.ExecuteScalarAsync();

        // SQL Server stores ULID as GUID
        var retrieved = new UlidId((Guid)result!);
        retrieved.ToGuid().ShouldBe(original.ToGuid());
    }

    [Fact]
    public async Task UuidV7Id_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var generator = new UuidV7IdGenerator();
        var original = generator.Generate().Match(id => id, _ => default);

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (UuidV7Col) VALUES (@Id)";
        insertCmd.AddUuidV7IdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT TOP 1 UuidV7Col FROM IdGenerationTest WHERE UuidV7Col IS NOT NULL ORDER BY UuidV7Col DESC";
        var result = await selectCmd.ExecuteScalarAsync();

        new UuidV7Id((Guid)result!).Value.ShouldBe(original.Value);
    }

    [Fact]
    public async Task ShardPrefixedId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        using var connection = (SqlConnection)_fixture.CreateConnection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO IdGenerationTest (ShardPrefixedCol) VALUES (@Id)";
        insertCmd.AddShardPrefixedIdParameter("@Id", original);
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT TOP 1 ShardPrefixedCol FROM IdGenerationTest WHERE ShardPrefixedCol IS NOT NULL ORDER BY ShardPrefixedCol DESC";
        var result = await selectCmd.ExecuteScalarAsync();

        ShardPrefixedId.Parse((string)result!).ToString().ShouldBe(original.ToString());
    }
}
