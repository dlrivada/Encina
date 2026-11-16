using System.Text.Json;
using Encina.Sharding;
using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Sharding;

public sealed class ShardReplicaConfigSecretMaskingTests
{
    private const string PrimaryConnectionString = "Server=primary;Database=db;User Id=sa;Password=SuperSecret123!;";
    private const string Replica1 = "Server=replica1;Database=db;User Id=sa;Password=ReplicaSecret1!;";
    private const string Replica2 = "Server=replica2;Database=db;User Id=sa;Password=ReplicaSecret2!;";

    [Fact]
    public void ShardReplicaConfig_ToString_MasksConnectionStrings()
    {
        // Arrange
        var config = new ShardedReadWriteOptions.ShardReplicaConfig(
            "shard-0",
            PrimaryConnectionString,
            [Replica1, Replica2],
            ReplicaSelectionStrategy.RoundRobin);

        // Act
        var result = config.ToString();

        // Assert
        result.ShouldNotContain("SuperSecret123!");
        result.ShouldNotContain("ReplicaSecret1!");
        result.ShouldNotContain("ReplicaSecret2!");
        result.ShouldNotContain(PrimaryConnectionString);
        result.ShouldNotContain(Replica1);
        result.ShouldNotContain(Replica2);

        result.ShouldContain("PrimaryConnectionString = ***");
        result.ShouldContain("ReplicaConnectionStrings = [2 masked]");
        result.ShouldContain("ShardId = shard-0");
        result.ShouldContain("Strategy = RoundRobin");
    }

    [Fact]
    public void ShardReplicaConfig_ToString_WithNoReplicas_ShowsZeroMasked()
    {
        // Arrange
        var config = new ShardedReadWriteOptions.ShardReplicaConfig(
            "shard-1",
            PrimaryConnectionString,
            [],
            null);

        // Act
        var result = config.ToString();

        // Assert
        result.ShouldContain("PrimaryConnectionString = ***");
        result.ShouldContain("ReplicaConnectionStrings = [0 masked]");
        result.ShouldNotContain(PrimaryConnectionString);
        result.ShouldNotContain("SuperSecret123!");
    }

    [Fact]
    public void ShardReplicaConfig_StringInterpolation_DoesNotLeakSecrets()
    {
        // Arrange
        var config = new ShardedReadWriteOptions.ShardReplicaConfig(
            "shard-0",
            PrimaryConnectionString,
            [Replica1],
            ReplicaSelectionStrategy.LeastLatency);

        // Act
        var result = $"Config: {config}";

        // Assert
        result.ShouldNotContain("SuperSecret123!");
        result.ShouldNotContain("ReplicaSecret1!");
        result.ShouldNotContain(PrimaryConnectionString);
        result.ShouldNotContain(Replica1);
        result.ShouldContain("***");
        result.ShouldContain("[1 masked]");
    }

    [Fact]
    public void ShardReplicaConfig_JsonSerialization_ExcludesConnectionStrings()
    {
        // Arrange
        var config = new ShardedReadWriteOptions.ShardReplicaConfig(
            "shard-0",
            PrimaryConnectionString,
            [Replica1, Replica2],
            ReplicaSelectionStrategy.RoundRobin);

        // Act
        var json = JsonSerializer.Serialize(config);

        // Assert
        json.ShouldNotContain("SuperSecret123!");
        json.ShouldNotContain("ReplicaSecret1!");
        json.ShouldNotContain("ReplicaSecret2!");

        json.ShouldNotContain(PrimaryConnectionString);
        json.ShouldNotContain(Replica1);
        json.ShouldNotContain(Replica2);

        json.ShouldNotContain("PrimaryConnectionString");
        json.ShouldNotContain("ReplicaConnectionStrings");

        // Non-sensitive properties should still be present.
        json.ShouldContain("shard-0");
        json.ShouldContain("\"Strategy\":0");
    }

    [Fact]
    public void ShardReplicaConfig_JsonSerialization_WithNullStrategy_Works()
    {
        // Arrange
        var config = new ShardedReadWriteOptions.ShardReplicaConfig(
            "shard-2",
            PrimaryConnectionString,
            [Replica1],
            null);

        // Act
        var json = JsonSerializer.Serialize(config);

        // Assert
        json.ShouldNotContain("SuperSecret123!");
        json.ShouldNotContain("ReplicaSecret1!");
        json.ShouldNotContain(PrimaryConnectionString);
        json.ShouldNotContain(Replica1);

        json.ShouldNotContain("PrimaryConnectionString");
        json.ShouldNotContain("ReplicaConnectionStrings");
        json.ShouldContain("shard-2");
    }

    [Fact]
    public void AddShard_StillWorks_AndStoresValuesCorrectly()
    {
        // Arrange
        var options = new ShardedReadWriteOptions();

        // Act
        options.AddShard(
            "shard-0",
            PrimaryConnectionString,
            [Replica1, Replica2],
            ReplicaSelectionStrategy.RoundRobin);

        // Assert
        options.Shards.Count.ShouldBe(1);

        var shard = options.Shards[0];

        shard.ShardId.ShouldBe("shard-0");
        shard.PrimaryConnectionString.ShouldBe(PrimaryConnectionString);
        shard.ReplicaConnectionStrings.Count.ShouldBe(2);
        shard.ReplicaConnectionStrings[0].ShouldBe(Replica1);
        shard.ReplicaConnectionStrings[1].ShouldBe(Replica2);
        shard.Strategy.ShouldBe(ReplicaSelectionStrategy.RoundRobin);

        // ToString must still mask connection strings.
        var toString = shard.ToString();

        toString.ShouldNotContain("SuperSecret123!");
        toString.ShouldNotContain("ReplicaSecret1!");
        toString.ShouldNotContain("ReplicaSecret2!");
        toString.ShouldNotContain(PrimaryConnectionString);
        toString.ShouldNotContain(Replica1);
        toString.ShouldNotContain(Replica2);
        toString.ShouldContain("***");
    }
}
