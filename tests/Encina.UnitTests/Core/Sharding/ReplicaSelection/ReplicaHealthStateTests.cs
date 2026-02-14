using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class ReplicaHealthStateTests
{
    // ────────────────────────────────────────────────────────────
    //  Static Healthy
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Healthy_IsHealthyIsTrue()
    {
        ReplicaHealthState.Healthy.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    public void Healthy_NoFailures()
    {
        ReplicaHealthState.Healthy.LastFailure.ShouldBeNull();
        ReplicaHealthState.Healthy.FailureCount.ShouldBe(0);
    }

    [Fact]
    public void Healthy_NoSuccess()
    {
        ReplicaHealthState.Healthy.LastSuccess.ShouldBeNull();
    }

    [Fact]
    public void Healthy_NoLag()
    {
        ReplicaHealthState.Healthy.ObservedReplicationLag.ShouldBeNull();
        ReplicaHealthState.Healthy.LagObservedAtUtc.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor — All Parameters
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AllParams_SetsProperties()
    {
        var now = DateTime.UtcNow;
        var lag = TimeSpan.FromSeconds(5);

        var state = new ReplicaHealthState(
            IsHealthy: false,
            LastFailure: now,
            FailureCount: 3,
            LastSuccess: now.AddMinutes(-5),
            ObservedReplicationLag: lag,
            LagObservedAtUtc: now);

        state.IsHealthy.ShouldBeFalse();
        state.LastFailure.ShouldBe(now);
        state.FailureCount.ShouldBe(3);
        state.LastSuccess.ShouldBe(now.AddMinutes(-5));
        state.ObservedReplicationLag.ShouldBe(lag);
        state.LagObservedAtUtc.ShouldBe(now);
    }

    [Fact]
    public void Constructor_OptionalLagParams_DefaultToNull()
    {
        var state = new ReplicaHealthState(true, null, 0, null);

        state.ObservedReplicationLag.ShouldBeNull();
        state.LagObservedAtUtc.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  Record Equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new ReplicaHealthState(true, null, 0, null);
        var b = new ReplicaHealthState(true, null, 0, null);

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new ReplicaHealthState(true, null, 0, null);
        var b = new ReplicaHealthState(false, DateTime.UtcNow, 1, null);

        a.ShouldNotBe(b);
    }
}
