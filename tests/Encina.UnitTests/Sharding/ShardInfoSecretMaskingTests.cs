using System.Text.Json;
using Encina.Sharding;
using Encina.Sharding.ReplicaSelection;
using Microsoft.Extensions.Configuration;

namespace Encina.UnitTests.Sharding;

/// <summary>
/// Unit tests proving that <see cref="ShardInfo"/> never exposes its primary or replica
/// connection strings through <see cref="ShardInfo.ToString"/>, string interpolation or
/// System.Text.Json serialization (#3685).
/// </summary>
public sealed class ShardInfoSecretMaskingTests
{
    private const string PrimaryConnectionString = "Server=db;User Id=sa;Password=secret;";
    private const string ReplicaConnectionString = "Server=r1;User Id=sa;Password=rsecret;";

    private static ShardInfo CreateShardWithReplica() =>
        new("s1", PrimaryConnectionString, ReplicaConnectionStrings: [ReplicaConnectionString]);

    [Fact]
    public void ToString_WithPrimaryAndReplica_MasksEveryConnectionString()
    {
        // Arrange
        var shard = CreateShardWithReplica();

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("rsecret");
        text.ShouldNotContain("Server=db");
        text.ShouldNotContain("Server=r1");
        text.ShouldNotContain("Password");
        text.ShouldBe(
            "ShardInfo { ShardId = s1, ConnectionString = ***, Weight = 1, IsActive = True, ReplicaConnectionStrings = [1 masked], ReplicaStrategy =  }");
    }

    [Fact]
    public void StringInterpolation_WithPrimaryAndReplica_MasksEveryConnectionString()
    {
        // Arrange
        var shard = CreateShardWithReplica();

        // Act
        var text = $"Routing to {shard}";

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("Server=db");
        text.ShouldNotContain("Server=r1");
        text.ShouldNotContain("Password");
        text.ShouldContain("ConnectionString = ***");
        text.ShouldContain("ReplicaConnectionStrings = [1 masked]");
    }

    [Fact]
    public void ToString_WithoutReplicas_ShowsZeroMaskedReplicas()
    {
        // Arrange
        var shard = new ShardInfo("s2", PrimaryConnectionString, Weight: 3, IsActive: false);

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldBe(
            "ShardInfo { ShardId = s2, ConnectionString = ***, Weight = 3, IsActive = False, ReplicaConnectionStrings = [0 masked], ReplicaStrategy =  }");
    }

    [Fact]
    public void ToString_WithEmptyReplicaListAndStrategy_ShowsStrategyAndZeroReplicas()
    {
        // Arrange
        var shard = new ShardInfo(
            "s3",
            PrimaryConnectionString,
            ReplicaConnectionStrings: [],
            ReplicaStrategy: ReplicaSelectionStrategy.RoundRobin);

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldContain("ReplicaConnectionStrings = [0 masked]");
        text.ShouldContain("ReplicaStrategy = RoundRobin");
        text.ShouldNotContain("Password");
    }

    [Fact]
    public void ToString_WithSeveralReplicas_ShowsOnlyTheReplicaCount()
    {
        // Arrange
        var shard = new ShardInfo(
            "s4",
            PrimaryConnectionString,
            ReplicaConnectionStrings: [ReplicaConnectionString, "Server=r2;Password=r2secret;"]);

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldContain("ReplicaConnectionStrings = [2 masked]");
        text.ShouldNotContain("Server=r2");
        text.ShouldNotContain("r2secret");
    }

    [Fact]
    public void JsonSerialize_WithPrimaryAndReplica_ExcludesEveryConnectionString()
    {
        // Arrange
        var shard = CreateShardWithReplica();

        // Act
        var json = JsonSerializer.Serialize(shard);

        // Assert
        json.ShouldNotContain("ConnectionString", Case.Insensitive);
        json.ShouldNotContain("Password", Case.Insensitive);
        json.ShouldNotContain("secret");
        json.ShouldNotContain("Server=");
        json.ShouldContain("\"ShardId\":\"s1\"");
        json.ShouldContain("\"ReplicaCount\":1");
    }

    [Fact]
    public void JsonDeserialize_OfSerializedShard_FailsBecauseTheConnectionStringIsNotSerialized()
    {
        // Arrange
        var json = JsonSerializer.Serialize(CreateShardWithReplica());

        // Act & Assert
        Should.Throw<ArgumentException>(() => JsonSerializer.Deserialize<ShardInfo>(json));
    }

    [Fact]
    public void ConfigurationBinding_ShardList_StillBindsPrimaryAndReplicaConnectionStrings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Shards:0:ShardId"] = "s1",
                ["Shards:0:ConnectionString"] = PrimaryConnectionString,
                ["Shards:0:Weight"] = "2",
                ["Shards:0:IsActive"] = "true",
                ["Shards:0:ReplicaConnectionStrings:0"] = ReplicaConnectionString,
            })
            .Build();

        // Act
        var shards = configuration.GetSection("Shards").Get<List<ShardInfo>>();

        // Assert
        shards.ShouldNotBeNull();
        shards.Count.ShouldBe(1);
        shards[0].ShardId.ShouldBe("s1");
        shards[0].Weight.ShouldBe(2);
        shards[0].ConnectionString.ShouldBe(PrimaryConnectionString);
        shards[0].ReplicaConnectionStrings.ShouldBe([ReplicaConnectionString]);
    }
}
