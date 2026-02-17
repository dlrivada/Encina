using Encina.Database;
using Encina.Sharding;
using Encina.Sharding.Health;
using Encina.Sharding.ReplicaSelection;
using NSubstitute;

namespace Encina.UnitTests.Core.Sharding.Health;

public sealed class ShardReplicaHealthCheckTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private const string Shard0 = "shard-0";
    private const string Shard1 = "shard-1";
    private const string Replica0A = "Server=replica0a;";
    private const string Replica0B = "Server=replica0b;";
    private const string Replica1A = "Server=replica1a;";

    private static ShardTopology CreateTopology(params ShardInfo[] shards)
        => new(shards);

    private static ShardInfo CreateShardWithReplicas(string shardId, params string[] replicas)
        => new(shardId, $"Server=primary-{shardId};", ReplicaConnectionStrings: replicas);

    private static ShardInfo CreateShardWithoutReplicas(string shardId)
        => new(shardId, $"Server=primary-{shardId};");

    private static ShardedReadWriteOptions DefaultOptions() => new();

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardReplicaHealthCheck(null!, Substitute.For<IReplicaHealthTracker>(), DefaultOptions()));
    }

    [Fact]
    public void Constructor_NullHealthTracker_ThrowsArgumentNullException()
    {
        var topology = CreateTopology(CreateShardWithoutReplicas(Shard0));
        Should.Throw<ArgumentNullException>(() =>
            new ShardReplicaHealthCheck(topology, null!, DefaultOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var topology = CreateTopology(CreateShardWithoutReplicas(Shard0));
        Should.Throw<ArgumentNullException>(() =>
            new ShardReplicaHealthCheck(topology, Substitute.For<IReplicaHealthTracker>(), null!));
    }

    // ────────────────────────────────────────────────────────────
    //  MinimumHealthyReplicasPerShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MinimumHealthyReplicasPerShard_DefaultIsOne()
    {
        var check = new ShardReplicaHealthCheck(
            CreateTopology(CreateShardWithoutReplicas(Shard0)),
            Substitute.For<IReplicaHealthTracker>(),
            DefaultOptions());

        check.MinimumHealthyReplicasPerShard.ShouldBe(1);
    }

    [Fact]
    public void MinimumHealthyReplicasPerShard_CanBeSet()
    {
        var check = new ShardReplicaHealthCheck(
            CreateTopology(CreateShardWithoutReplicas(Shard0)),
            Substitute.For<IReplicaHealthTracker>(),
            DefaultOptions())
        { MinimumHealthyReplicasPerShard = 3 };

        check.MinimumHealthyReplicasPerShard.ShouldBe(3);
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — No Replicas
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_ShardWithNoReplicas_ReportsHealthyWithDescription()
    {
        var topology = CreateTopology(CreateShardWithoutReplicas(Shard0));
        var tracker = Substitute.For<IReplicaHealthTracker>();
        var check = new ShardReplicaHealthCheck(topology, tracker, DefaultOptions());

        var summary = await check.CheckReplicaHealthAsync();

        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Healthy);
        summary.ShardCount.ShouldBe(1);
        summary.ShardResults[0].ShardId.ShouldBe(Shard0);
        summary.ShardResults[0].HealthyReplicaCount.ShouldBe(0);
        summary.ShardResults[0].TotalReplicaCount.ShouldBe(0);
        summary.ShardResults[0].Description.ShouldNotBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — All Healthy
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_AllReplicasHealthy_ReportsHealthy()
    {
        var shard = CreateShardWithReplicas(Shard0, Replica0A, Replica0B);
        var topology = CreateTopology(shard);
        var tracker = Substitute.For<IReplicaHealthTracker>();
        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns([Replica0A, Replica0B]);

        var check = new ShardReplicaHealthCheck(topology, tracker, DefaultOptions());

        var summary = await check.CheckReplicaHealthAsync();

        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Healthy);
        summary.AllHealthy.ShouldBeTrue();
        summary.ShardResults[0].IsHealthy.ShouldBeTrue();
        summary.ShardResults[0].HealthyReplicaCount.ShouldBe(2);
        summary.ShardResults[0].TotalReplicaCount.ShouldBe(2);
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — Degraded
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_FewerThanMinimumHealthy_ReportsDegraded()
    {
        var shard = CreateShardWithReplicas(Shard0, Replica0A, Replica0B);
        var topology = CreateTopology(shard);
        var tracker = Substitute.For<IReplicaHealthTracker>();
        // Only 1 of 2 replicas healthy, minimum = 2
        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns([Replica0A]);

        var check = new ShardReplicaHealthCheck(topology, tracker, DefaultOptions())
        {
            MinimumHealthyReplicasPerShard = 2,
        };

        var summary = await check.CheckReplicaHealthAsync();

        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Degraded);
        summary.ShardResults[0].IsDegraded.ShouldBeTrue();
        summary.ShardResults[0].Description.ShouldNotBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — All Unhealthy + Fallback
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_AllUnhealthy_FallbackEnabled_ReportsDegraded()
    {
        var shard = CreateShardWithReplicas(Shard0, Replica0A, Replica0B);
        var topology = CreateTopology(shard);
        var tracker = Substitute.For<IReplicaHealthTracker>();
        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns(Array.Empty<string>().ToList());

        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = true };
        var check = new ShardReplicaHealthCheck(topology, tracker, options);

        var summary = await check.CheckReplicaHealthAsync();

        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Degraded);
        summary.ShardResults[0].IsDegraded.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckReplicaHealthAsync_AllUnhealthy_NoFallback_ReportsUnhealthy()
    {
        var shard = CreateShardWithReplicas(Shard0, Replica0A, Replica0B);
        var topology = CreateTopology(shard);
        var tracker = Substitute.For<IReplicaHealthTracker>();
        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns(Array.Empty<string>().ToList());

        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = false };
        var check = new ShardReplicaHealthCheck(topology, tracker, options);

        var summary = await check.CheckReplicaHealthAsync();

        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Unhealthy);
        summary.ShardResults[0].IsUnhealthy.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — Multiple Shards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_MixedShardHealth_AggregatesCorrectly()
    {
        var shard0 = CreateShardWithReplicas(Shard0, Replica0A, Replica0B);
        var shard1 = CreateShardWithReplicas(Shard1, Replica1A);
        var topology = CreateTopology(shard0, shard1);
        var tracker = Substitute.For<IReplicaHealthTracker>();

        // Shard0: all healthy
        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns([Replica0A, Replica0B]);
        // Shard1: no replicas available, fallback enabled
        tracker.GetAvailableReplicas(Shard1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns(Array.Empty<string>().ToList());

        var check = new ShardReplicaHealthCheck(topology, tracker, DefaultOptions());

        var summary = await check.CheckReplicaHealthAsync();

        // Overall should be degraded because shard1 has issues
        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Degraded);
        summary.ShardCount.ShouldBe(2);
        summary.DegradedCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CheckReplicaHealthAsync_AllShardsHealthy_AllHealthyIsTrue()
    {
        var shard0 = CreateShardWithReplicas(Shard0, Replica0A);
        var shard1 = CreateShardWithReplicas(Shard1, Replica1A);
        var topology = CreateTopology(shard0, shard1);
        var tracker = Substitute.For<IReplicaHealthTracker>();

        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns([Replica0A]);
        tracker.GetAvailableReplicas(Shard1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns([Replica1A]);

        var check = new ShardReplicaHealthCheck(topology, tracker, DefaultOptions());

        var summary = await check.CheckReplicaHealthAsync();

        summary.AllHealthy.ShouldBeTrue();
        summary.DegradedCount.ShouldBe(0);
        summary.UnhealthyCount.ShouldBe(0);
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — Worst Status Wins
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_OneUnhealthyShard_OverallIsUnhealthy()
    {
        var shard0 = CreateShardWithReplicas(Shard0, Replica0A);
        var shard1 = CreateShardWithReplicas(Shard1, Replica1A);
        var topology = CreateTopology(shard0, shard1);
        var tracker = Substitute.For<IReplicaHealthTracker>();

        // Shard0: healthy
        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns([Replica0A]);
        // Shard1: all unhealthy, no fallback
        tracker.GetAvailableReplicas(Shard1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>())
            .Returns(Array.Empty<string>().ToList());

        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = false };
        var check = new ShardReplicaHealthCheck(topology, tracker, options);

        var summary = await check.CheckReplicaHealthAsync();

        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Unhealthy);
        summary.UnhealthyCount.ShouldBe(1);
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — MaxAcceptableReplicationLag
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_PassesMaxLagToTracker()
    {
        var shard = CreateShardWithReplicas(Shard0, Replica0A);
        var topology = CreateTopology(shard);
        var tracker = Substitute.For<IReplicaHealthTracker>();
        var maxLag = TimeSpan.FromSeconds(5);

        tracker.GetAvailableReplicas(Shard0, Arg.Any<IReadOnlyList<string>>(), maxLag)
            .Returns([Replica0A]);

        var options = new ShardedReadWriteOptions { MaxAcceptableReplicationLag = maxLag };
        var check = new ShardReplicaHealthCheck(topology, tracker, options);

        await check.CheckReplicaHealthAsync();

        tracker.Received(1).GetAvailableReplicas(
            Shard0,
            Arg.Any<IReadOnlyList<string>>(),
            maxLag);
    }

    // ────────────────────────────────────────────────────────────
    //  CheckReplicaHealthAsync — Cancellation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckReplicaHealthAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var topology = CreateTopology(
            CreateShardWithReplicas(Shard0, Replica0A),
            CreateShardWithReplicas(Shard1, Replica1A));
        var tracker = Substitute.For<IReplicaHealthTracker>();
        var check = new ShardReplicaHealthCheck(topology, tracker, DefaultOptions());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            check.CheckReplicaHealthAsync(cts.Token));
    }
}
