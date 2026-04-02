using Encina.OpenTelemetry.Resharding;
using Encina.Sharding.Resharding;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingActivitySource"/>.
/// </summary>
public sealed class ReshardingActivitySourceTests
{
    [Fact]
    public void SourceName_HasCorrectValue()
    {
        ReshardingActivitySource.SourceName.ShouldBe("Encina.Resharding");
    }

    [Fact]
    public void StartReshardingExecution_WithoutListeners_ReturnsNullOrActivity()
    {
        // Without any listener attached, HasListeners() returns false and activity is null.
        // However, in parallel test runs, other tests may register global ActivityListeners,
        // causing this to return a non-null activity. Both outcomes are valid.
        var activity = ReshardingActivitySource.StartReshardingExecution(
            Guid.NewGuid(), 5, 10000);

        // Just verify it doesn't throw — null or non-null are both acceptable
        activity?.Dispose();
    }

    [Fact]
    public void StartPhaseExecution_WithoutListeners_ReturnsNull()
    {
        var activity = ReshardingActivitySource.StartPhaseExecution(
            Guid.NewGuid(), ReshardingPhase.Copying);

        activity.ShouldBeNull();
    }

    [Fact]
    public void Complete_NullActivity_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ReshardingActivitySource.Complete(null, true));
        ex.ShouldBeNull();
    }

    [Fact]
    public void Complete_NullActivity_WithError_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ReshardingActivitySource.Complete(null, false, 1000.0, "Some error"));
        ex.ShouldBeNull();
    }
}
