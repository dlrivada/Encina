
using System.Text.Json;
using Encina.Sharding;
using Encina.Sharding.ReplicaSelection;
using Xunit;

namespace Encina.UnitTests.Sharding;

public class ShardReplicaConfigSecretMaskingTests
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
        Assert.DoesNotContain("SuperSecret123!", result);
        Assert.DoesNotContain("ReplicaSecret1!", result);
        Assert.DoesNotContain("ReplicaSecret2!", result);
        Assert.DoesNotContain(PrimaryConnectionString, result);
        Assert.DoesNotContain(Replica1, result);
        Assert.DoesNotContain(Replica2, result);

        Assert.Contains("PrimaryConnectionString = ***", result);
        Assert.Contains("ReplicaConnectionStrings = [2 masked]", result);
        Assert.Contains("ShardId = shard-0", result);
        Assert.Contains("Strategy = RoundRobin", result);
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
        Assert.Contains("PrimaryConnectionString = ***", result);
        Assert.Contains("ReplicaConnectionStrings = [0 masked]", result);
        Assert.DoesNotContain(PrimaryConnectionString, result);
        Assert.DoesNotContain("SuperSecret123!", result);
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
        Assert.DoesNotContain("SuperSecret123!", result);
        Assert.DoesNotContain("ReplicaSecret1!", result);
        Assert.DoesNotContain(PrimaryConnectionString, result);
        Assert.DoesNotContain(Replica1, result);
        Assert.Contains("***", result);
        Assert.Contains("[1 masked]", result);
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
        Assert.DoesNotContain("SuperSecret123!", json);
        Assert.DoesNotContain("ReplicaSecret1!", json);
        Assert.DoesNotContain("ReplicaSecret2!", json);

        Assert.DoesNotContain(PrimaryConnectionString, json);
        Assert.DoesNotContain(Replica1, json);
        Assert.DoesNotContain(Replica2, json);

        Assert.DoesNotContain("PrimaryConnectionString", json);
        Assert.DoesNotContain("ReplicaConnectionStrings", json);

        // Non-sensitive properties should still be present.
        Assert.Contains("shard-0", json);
        Assert.Contains("\"Strategy\":0", json);
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
        Assert.DoesNotContain("SuperSecret123!", json);
        Assert.DoesNotContain("ReplicaSecret1!", json);
        Assert.DoesNotContain(PrimaryConnectionString, json);
        Assert.DoesNotContain(Replica1, json);

        Assert.DoesNotContain("PrimaryConnectionString", json);
        Assert.DoesNotContain("ReplicaConnectionStrings", json);
        Assert.Contains("shard-2", json);
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
        Assert.Single(options.Shards);

        var shard = options.Shards[0];

        Assert.Equal("shard-0", shard.ShardId);
        Assert.Equal(PrimaryConnectionString, shard.PrimaryConnectionString);
        Assert.Equal(2, shard.ReplicaConnectionStrings.Count);
        Assert.Equal(Replica1, shard.ReplicaConnectionStrings[0]);
        Assert.Equal(Replica2, shard.ReplicaConnectionStrings[1]);
        Assert.Equal(ReplicaSelectionStrategy.RoundRobin, shard.Strategy);

        // ToString must still mask connection strings.
        var toString = shard.ToString();

        Assert.DoesNotContain("SuperSecret123!", toString);
        Assert.DoesNotContain("ReplicaSecret1!", toString);
        Assert.DoesNotContain("ReplicaSecret2!", toString);
        Assert.DoesNotContain(PrimaryConnectionString, toString);
        Assert.DoesNotContain(Replica1, toString);
        Assert.DoesNotContain(Replica2, toString);
        Assert.Contains("***", toString);
    }
}
