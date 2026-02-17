using System.Diagnostics;
using Encina.OpenTelemetry;
using Encina.OpenTelemetry.Resharding;
using Encina.Sharding.Resharding;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for <see cref="ReshardingActivitySource"/>.
/// Validates the source name constant, activity creation with and without listeners,
/// tag population, and completion status handling.
/// </summary>
public sealed class ReshardingActivitySourceTests : IDisposable
{
    private ActivityListener? _listener;

    #region SourceName

    [Fact]
    public void SourceName_IsEncinaResharding()
    {
        // Assert
        ReshardingActivitySource.SourceName.ShouldBe("Encina.Resharding");
    }

    #endregion

    #region StartReshardingExecution - No Listeners

    [Fact]
    public void StartReshardingExecution_NoListeners_ReturnsNull()
    {
        // Act
        var activity = ReshardingActivitySource.StartReshardingExecution(
            Guid.NewGuid(), stepCount: 3, estimatedRows: 10000);

        // Assert
        activity.ShouldBeNull();
    }

    #endregion

    #region StartPhaseExecution - No Listeners

    [Fact]
    public void StartPhaseExecution_NoListeners_ReturnsNull()
    {
        // Act
        var activity = ReshardingActivitySource.StartPhaseExecution(
            Guid.NewGuid(), ReshardingPhase.Copying);

        // Assert
        activity.ShouldBeNull();
    }

    #endregion

    #region Complete - Null Activity

    [Fact]
    public void Complete_NullActivity_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            ReshardingActivitySource.Complete(null, succeeded: true, durationMs: 100.0));
    }

    #endregion

    #region StartReshardingExecution - With Listener

    [Fact]
    public void StartReshardingExecution_WithListener_ReturnsActivityWithCorrectTags()
    {
        // Arrange
        SetupListener();
        var reshardingId = Guid.NewGuid();

        // Act
        using var activity = ReshardingActivitySource.StartReshardingExecution(
            reshardingId, stepCount: 5, estimatedRows: 25000);

        // Assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Encina.Resharding.Execute");
        activity.Kind.ShouldBe(ActivityKind.Internal);

        var tags = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value);
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.Id].ShouldBe(reshardingId.ToString());
        tags["resharding.step_count"].ShouldBe(5);
        tags["resharding.estimated_rows"].ShouldBe(25000L);
    }

    #endregion

    #region StartPhaseExecution - With Listener

    [Fact]
    public void StartPhaseExecution_WithListener_ReturnsActivityWithCorrectTags()
    {
        // Arrange
        SetupListener();
        var reshardingId = Guid.NewGuid();

        // Act
        using var activity = ReshardingActivitySource.StartPhaseExecution(
            reshardingId, ReshardingPhase.Verifying);

        // Assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Encina.Resharding.Phase");
        activity.Kind.ShouldBe(ActivityKind.Internal);

        var tags = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value);
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.Id].ShouldBe(reshardingId.ToString());
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.Phase].ShouldBe(ReshardingPhase.Verifying.ToString());
    }

    #endregion

    #region Complete - With Listener

    [Fact]
    public void Complete_WithActivitySucceeded_SetsOkStatus()
    {
        // Arrange
        SetupListener();
        var activity = ReshardingActivitySource.StartReshardingExecution(
            Guid.NewGuid(), stepCount: 1, estimatedRows: 500);
        activity.ShouldNotBeNull();

        // Act
        ReshardingActivitySource.Complete(activity, succeeded: true, durationMs: 1234.5);

        // Assert
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        var tags = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value);
        tags["resharding.duration_ms"].ShouldBe(1234.5);
    }

    [Fact]
    public void Complete_WithActivityFailed_SetsErrorStatus()
    {
        // Arrange
        SetupListener();
        var activity = ReshardingActivitySource.StartReshardingExecution(
            Guid.NewGuid(), stepCount: 1, estimatedRows: 500);
        activity.ShouldNotBeNull();

        // Act
        ReshardingActivitySource.Complete(
            activity, succeeded: false, errorMessage: "Verification failed");

        // Assert
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Verification failed");
        var tags = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value);
        tags["resharding.error"].ShouldBe("Verification failed");
    }

    #endregion

    #region Test Helpers

    private void SetupListener()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ReshardingActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }

    #endregion
}
