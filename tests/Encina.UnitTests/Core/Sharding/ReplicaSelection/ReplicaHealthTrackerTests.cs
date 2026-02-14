using Encina.Sharding.ReplicaSelection;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class ReplicaHealthTrackerTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private const string ShardId = "shard-0";
    private const string Replica1 = "Server=replica1;";
    private const string Replica2 = "Server=replica2;";
    private const string Replica3 = "Server=replica3;";
    private static readonly IReadOnlyList<string> AllReplicas = [Replica1, Replica2, Replica3];

    private static FakeTimeProvider CreateTimeProvider()
        => new(new DateTimeOffset(2026, 2, 14, 12, 0, 0, TimeSpan.Zero));

    // ────────────────────────────────────────────────────────────
    //  MarkHealthy / MarkUnhealthy
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MarkHealthy_UpdatesHealthState()
    {
        var time = CreateTimeProvider();
        var tracker = new ReplicaHealthTracker(timeProvider: time);

        tracker.MarkHealthy(ShardId, Replica1);

        var state = tracker.GetHealthState(ShardId, Replica1);
        state.IsHealthy.ShouldBeTrue();
        state.LastSuccess.ShouldNotBeNull();
    }

    [Fact]
    public void MarkUnhealthy_UpdatesHealthState()
    {
        var time = CreateTimeProvider();
        var tracker = new ReplicaHealthTracker(timeProvider: time);

        tracker.MarkUnhealthy(ShardId, Replica1);

        var state = tracker.GetHealthState(ShardId, Replica1);
        state.IsHealthy.ShouldBeFalse();
        state.LastFailure.ShouldNotBeNull();
        state.FailureCount.ShouldBe(1);
    }

    [Fact]
    public void MarkUnhealthy_MultipleTimes_IncrementsFailureCount()
    {
        var tracker = new ReplicaHealthTracker();

        tracker.MarkUnhealthy(ShardId, Replica1);
        tracker.MarkUnhealthy(ShardId, Replica1);
        tracker.MarkUnhealthy(ShardId, Replica1);

        var state = tracker.GetHealthState(ShardId, Replica1);
        state.FailureCount.ShouldBe(3);
    }

    [Fact]
    public void MarkHealthy_AfterUnhealthy_RestoresHealthy()
    {
        var tracker = new ReplicaHealthTracker();

        tracker.MarkUnhealthy(ShardId, Replica1);
        tracker.MarkHealthy(ShardId, Replica1);

        var state = tracker.GetHealthState(ShardId, Replica1);
        state.IsHealthy.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  GetAvailableReplicas — Health Filtering
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAvailableReplicas_NoStateRecorded_ReturnsAllReplicas()
    {
        var tracker = new ReplicaHealthTracker();

        var available = tracker.GetAvailableReplicas(ShardId, AllReplicas);

        available.Count.ShouldBe(3);
    }

    [Fact]
    public void GetAvailableReplicas_OneUnhealthy_ExcludesUnhealthyReplica()
    {
        var tracker = new ReplicaHealthTracker();
        tracker.MarkUnhealthy(ShardId, Replica2);

        var available = tracker.GetAvailableReplicas(ShardId, AllReplicas);

        available.Count.ShouldBe(2);
        available.ShouldContain(Replica1);
        available.ShouldContain(Replica3);
        available.ShouldNotContain(Replica2);
    }

    [Fact]
    public void GetAvailableReplicas_AllUnhealthy_ReturnsEmpty()
    {
        var tracker = new ReplicaHealthTracker();
        tracker.MarkUnhealthy(ShardId, Replica1);
        tracker.MarkUnhealthy(ShardId, Replica2);
        tracker.MarkUnhealthy(ShardId, Replica3);

        var available = tracker.GetAvailableReplicas(ShardId, AllReplicas);

        available.Count.ShouldBe(0);
    }

    // ────────────────────────────────────────────────────────────
    //  Recovery Window
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAvailableReplicas_AfterRecoveryWindow_ReincludesUnhealthyReplica()
    {
        var time = CreateTimeProvider();
        var recoveryDelay = TimeSpan.FromSeconds(30);
        var tracker = new ReplicaHealthTracker(recoveryDelay, time);

        // Mark unhealthy
        tracker.MarkUnhealthy(ShardId, Replica1);
        tracker.GetAvailableReplicas(ShardId, AllReplicas).ShouldNotContain(Replica1);

        // Advance past recovery window
        time.Advance(TimeSpan.FromSeconds(31));
        var available = tracker.GetAvailableReplicas(ShardId, AllReplicas);

        available.ShouldContain(Replica1);
    }

    [Fact]
    public void GetAvailableReplicas_BeforeRecoveryWindow_StillExcludesUnhealthy()
    {
        var time = CreateTimeProvider();
        var recoveryDelay = TimeSpan.FromSeconds(30);
        var tracker = new ReplicaHealthTracker(recoveryDelay, time);

        tracker.MarkUnhealthy(ShardId, Replica1);

        // Advance, but not past recovery window
        time.Advance(TimeSpan.FromSeconds(20));
        var available = tracker.GetAvailableReplicas(ShardId, AllReplicas);

        available.ShouldNotContain(Replica1);
    }

    // ────────────────────────────────────────────────────────────
    //  GetHealthState / GetAllHealthStates
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetHealthState_NoStateRecorded_ReturnsHealthyDefault()
    {
        var tracker = new ReplicaHealthTracker();

        var state = tracker.GetHealthState(ShardId, Replica1);

        state.IsHealthy.ShouldBeTrue();
        state.FailureCount.ShouldBe(0);
    }

    [Fact]
    public void GetAllHealthStates_NoStateRecorded_ReturnsEmptyDictionary()
    {
        var tracker = new ReplicaHealthTracker();

        var states = tracker.GetAllHealthStates(ShardId);

        states.Count.ShouldBe(0);
    }

    [Fact]
    public void GetAllHealthStates_MultipleReplicas_ReturnsAllTrackedStates()
    {
        var tracker = new ReplicaHealthTracker();
        tracker.MarkHealthy(ShardId, Replica1);
        tracker.MarkUnhealthy(ShardId, Replica2);

        var states = tracker.GetAllHealthStates(ShardId);

        states.Count.ShouldBe(2);
        states[Replica1].IsHealthy.ShouldBeTrue();
        states[Replica2].IsHealthy.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  Shard ID Case-Insensitivity
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MarkUnhealthy_ShardIdCaseInsensitive_RecognizesSameShard()
    {
        var tracker = new ReplicaHealthTracker();

        tracker.MarkUnhealthy("SHARD-0", Replica1);
        var state = tracker.GetHealthState("shard-0", Replica1);

        state.IsHealthy.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  ReportReplicationLag
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ReportReplicationLag_TracksLagPerReplica()
    {
        var time = CreateTimeProvider();
        var tracker = new ReplicaHealthTracker(timeProvider: time);

        tracker.ReportReplicationLag(ShardId, Replica1, TimeSpan.FromMilliseconds(100));

        var state = tracker.GetHealthState(ShardId, Replica1);
        state.ObservedReplicationLag.ShouldBe(TimeSpan.FromMilliseconds(100));
        state.LagObservedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void ReportReplicationLag_UpdatesExistingLag()
    {
        var tracker = new ReplicaHealthTracker();

        tracker.ReportReplicationLag(ShardId, Replica1, TimeSpan.FromMilliseconds(100));
        tracker.ReportReplicationLag(ShardId, Replica1, TimeSpan.FromMilliseconds(50));

        var state = tracker.GetHealthState(ShardId, Replica1);
        state.ObservedReplicationLag.ShouldBe(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void ReportReplicationLag_NegativeLag_ThrowsArgumentOutOfRangeException()
    {
        var tracker = new ReplicaHealthTracker();
        Should.Throw<ArgumentOutOfRangeException>(() =>
            tracker.ReportReplicationLag(ShardId, Replica1, TimeSpan.FromMilliseconds(-1)));
    }

    // ────────────────────────────────────────────────────────────
    //  GetAvailableReplicas — With Lag Filtering
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAvailableReplicas_WithMaxLag_ExcludesStaleReplicas()
    {
        var tracker = new ReplicaHealthTracker();
        tracker.ReportReplicationLag(ShardId, Replica1, TimeSpan.FromSeconds(2));
        tracker.ReportReplicationLag(ShardId, Replica2, TimeSpan.FromSeconds(10));
        tracker.ReportReplicationLag(ShardId, Replica3, TimeSpan.FromSeconds(1));

        var available = tracker.GetAvailableReplicas(
            ShardId, AllReplicas, TimeSpan.FromSeconds(5));

        available.Count.ShouldBe(2);
        available.ShouldContain(Replica1);
        available.ShouldContain(Replica3);
        available.ShouldNotContain(Replica2);
    }

    [Fact]
    public void GetAvailableReplicas_NullMaxLag_SkipsLagFiltering()
    {
        var tracker = new ReplicaHealthTracker();
        tracker.ReportReplicationLag(ShardId, Replica1, TimeSpan.FromSeconds(999));

        var available = tracker.GetAvailableReplicas(ShardId, AllReplicas, null);

        available.Count.ShouldBe(3);
    }

    [Fact]
    public void GetAvailableReplicas_NoLagData_AssumesWithinThreshold()
    {
        var tracker = new ReplicaHealthTracker();

        var available = tracker.GetAvailableReplicas(
            ShardId, AllReplicas, TimeSpan.FromSeconds(5));

        available.Count.ShouldBe(3);
    }

    [Fact]
    public void GetAvailableReplicas_CombinesHealthAndLagFiltering()
    {
        var tracker = new ReplicaHealthTracker();

        // Replica1: healthy, low lag
        tracker.MarkHealthy(ShardId, Replica1);
        tracker.ReportReplicationLag(ShardId, Replica1, TimeSpan.FromSeconds(1));

        // Replica2: unhealthy
        tracker.MarkUnhealthy(ShardId, Replica2);

        // Replica3: healthy, high lag
        tracker.MarkHealthy(ShardId, Replica3);
        tracker.ReportReplicationLag(ShardId, Replica3, TimeSpan.FromSeconds(20));

        var available = tracker.GetAvailableReplicas(
            ShardId, AllReplicas, TimeSpan.FromSeconds(5));

        // Only replica1 passes both health and lag filters
        available.Count.ShouldBe(1);
        available.ShouldContain(Replica1);
    }

    // ────────────────────────────────────────────────────────────
    //  Validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MarkHealthy_NullShardId_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        Should.Throw<ArgumentNullException>(() => tracker.MarkHealthy(null!, Replica1));
    }

    [Fact]
    public void MarkHealthy_NullReplica_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        Should.Throw<ArgumentNullException>(() => tracker.MarkHealthy(ShardId, null!));
    }

    [Fact]
    public void MarkUnhealthy_NullShardId_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        Should.Throw<ArgumentNullException>(() => tracker.MarkUnhealthy(null!, Replica1));
    }

    [Fact]
    public void GetAvailableReplicas_NullShardId_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        Should.Throw<ArgumentNullException>(() => tracker.GetAvailableReplicas(null!, AllReplicas));
    }

    [Fact]
    public void GetAvailableReplicas_NullAllReplicas_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        Should.Throw<ArgumentNullException>(() => tracker.GetAvailableReplicas(ShardId, null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Thread Safety
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConcurrentMarkAndGet_NoExceptions()
    {
        var tracker = new ReplicaHealthTracker();
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var replica = AllReplicas[i % 3];
            tasks.Add(Task.Run(() => tracker.MarkUnhealthy(ShardId, replica)));
            tasks.Add(Task.Run(() => tracker.MarkHealthy(ShardId, replica)));
            tasks.Add(Task.Run(() => tracker.GetAvailableReplicas(ShardId, AllReplicas)));
            tasks.Add(Task.Run(() => tracker.GetHealthState(ShardId, replica)));
        }

        await Task.WhenAll(tasks);
    }
}
