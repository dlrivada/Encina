using Encina.Sharding;
using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding;

public sealed class ShardedReadWriteOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void DefaultReplicaStrategy_DefaultIsRoundRobin()
    {
        var options = new ShardedReadWriteOptions();
        options.DefaultReplicaStrategy.ShouldBe(ReplicaSelectionStrategy.RoundRobin);
    }

    [Fact]
    public void ReplicaHealthCheckInterval_DefaultIs30Seconds()
    {
        var options = new ShardedReadWriteOptions();
        options.ReplicaHealthCheckInterval.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void UnhealthyReplicaRecoveryDelay_DefaultIs30Seconds()
    {
        var options = new ShardedReadWriteOptions();
        options.UnhealthyReplicaRecoveryDelay.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void MaxAcceptableReplicationLag_DefaultIsNull()
    {
        var options = new ShardedReadWriteOptions();
        options.MaxAcceptableReplicationLag.ShouldBeNull();
    }

    [Fact]
    public void FallbackToPrimaryWhenNoReplicas_DefaultIsTrue()
    {
        var options = new ShardedReadWriteOptions();
        options.FallbackToPrimaryWhenNoReplicas.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  AddShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddShard_ValidParams_ReturnsSameInstance()
    {
        var options = new ShardedReadWriteOptions();
        var returned = options.AddShard("shard-0", "Server=primary;");
        returned.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddShard_WithReplicas_AddsToShardsCollection()
    {
        var options = new ShardedReadWriteOptions();
        options.AddShard("shard-0", "Server=primary;",
            ["Server=replica1;", "Server=replica2;"]);

        options.Shards.Count.ShouldBe(1);
    }

    [Fact]
    public void AddShard_MultipleShards_AddsAll()
    {
        var options = new ShardedReadWriteOptions();
        options.AddShard("shard-0", "conn0")
               .AddShard("shard-1", "conn1")
               .AddShard("shard-2", "conn2");

        options.Shards.Count.ShouldBe(3);
    }

    [Fact]
    public void AddShard_WithStrategy_StoresPerShardStrategy()
    {
        var options = new ShardedReadWriteOptions();
        options.AddShard("shard-0", "conn0",
            strategy: ReplicaSelectionStrategy.LeastLatency);

        options.Shards[0].Strategy.ShouldBe(ReplicaSelectionStrategy.LeastLatency);
    }

    [Fact]
    public void AddShard_NullReplicas_DefaultsToEmptyList()
    {
        var options = new ShardedReadWriteOptions();
        options.AddShard("shard-0", "conn0", replicaConnectionStrings: null);

        options.Shards[0].ReplicaConnectionStrings.Count.ShouldBe(0);
    }

    // ────────────────────────────────────────────────────────────
    //  AddShard — Validation
    // ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddShard_NullOrWhitespaceShardId_Throws(string? shardId)
    {
        var options = new ShardedReadWriteOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddShard(shardId!, "conn"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddShard_NullOrWhitespacePrimaryConnectionString_Throws(string? connString)
    {
        var options = new ShardedReadWriteOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddShard("shard-0", connString!));
    }

    // ────────────────────────────────────────────────────────────
    //  Properties — Can Be Set
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AllProperties_CanBeModified()
    {
        var options = new ShardedReadWriteOptions
        {
            DefaultReplicaStrategy = ReplicaSelectionStrategy.LeastConnections,
            ReplicaHealthCheckInterval = TimeSpan.FromMinutes(1),
            UnhealthyReplicaRecoveryDelay = TimeSpan.FromMinutes(2),
            MaxAcceptableReplicationLag = TimeSpan.FromSeconds(10),
            FallbackToPrimaryWhenNoReplicas = false,
        };

        options.DefaultReplicaStrategy.ShouldBe(ReplicaSelectionStrategy.LeastConnections);
        options.ReplicaHealthCheckInterval.ShouldBe(TimeSpan.FromMinutes(1));
        options.UnhealthyReplicaRecoveryDelay.ShouldBe(TimeSpan.FromMinutes(2));
        options.MaxAcceptableReplicationLag.ShouldBe(TimeSpan.FromSeconds(10));
        options.FallbackToPrimaryWhenNoReplicas.ShouldBeFalse();
    }
}
