using System.Diagnostics;
using Encina.OpenTelemetry;
using Encina.OpenTelemetry.Resharding;
using Encina.Sharding.Resharding;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for <see cref="ReshardingActivityEnricher"/>.
/// Validates null-safety for all enrichment methods and verifies that tags
/// are correctly applied to activities when a trace collector is listening.
/// </summary>
public sealed class ReshardingActivityEnricherTests : IDisposable
{
    private ActivityListener? _listener;

    #region EnrichWithReshardingResult - Null Safety

    [Fact]
    public void EnrichWithReshardingResult_NullActivity_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            ReshardingActivityEnricher.EnrichWithReshardingResult(
                null, Guid.NewGuid(), ReshardingPhase.Completed, phaseCount: 6, totalDurationMs: 5000.0));
    }

    #endregion

    #region EnrichWithShardCopy - Null Safety

    [Fact]
    public void EnrichWithShardCopy_NullActivity_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            ReshardingActivityEnricher.EnrichWithShardCopy(
                null, "shard-0", "shard-1", rowsAffected: 1500));
    }

    #endregion

    #region EnrichWithRollback - Null Safety

    [Fact]
    public void EnrichWithRollback_NullActivity_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            ReshardingActivityEnricher.EnrichWithRollback(
                null, Guid.NewGuid(), ReshardingPhase.Copying, rollbackDurationMs: 300.0));
    }

    #endregion

    #region EnrichWithReshardingResult - With Activity

    [Fact]
    public void EnrichWithReshardingResult_WithActivity_SetsExpectedTags()
    {
        // Arrange
        SetupListener();
        using var activity = CreateTestActivity();
        var reshardingId = Guid.NewGuid();

        // Act
        ReshardingActivityEnricher.EnrichWithReshardingResult(
            activity, reshardingId, ReshardingPhase.Completed, phaseCount: 6, totalDurationMs: 12345.0);

        // Assert
        var tags = activity!.TagObjects.ToDictionary(t => t.Key, t => t.Value);
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.Id].ShouldBe(reshardingId.ToString());
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.Phase].ShouldBe(ReshardingPhase.Completed.ToString());
        tags["resharding.phase_count"].ShouldBe(6);
        tags["resharding.total_duration_ms"].ShouldBe(12345.0);
    }

    #endregion

    #region EnrichWithShardCopy - With Activity

    [Fact]
    public void EnrichWithShardCopy_WithActivity_SetsExpectedTags()
    {
        // Arrange
        SetupListener();
        using var activity = CreateTestActivity();

        // Act
        ReshardingActivityEnricher.EnrichWithShardCopy(
            activity, "shard-0", "shard-2", rowsAffected: 7500);

        // Assert
        var tags = activity!.TagObjects.ToDictionary(t => t.Key, t => t.Value);
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.SourceShard].ShouldBe("shard-0");
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.TargetShard].ShouldBe("shard-2");
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.RowsAffected].ShouldBe(7500L);
    }

    #endregion

    #region EnrichWithRollback - With Activity

    [Fact]
    public void EnrichWithRollback_WithActivity_SetsExpectedTags()
    {
        // Arrange
        SetupListener();
        using var activity = CreateTestActivity();
        var reshardingId = Guid.NewGuid();

        // Act
        ReshardingActivityEnricher.EnrichWithRollback(
            activity, reshardingId, ReshardingPhase.Verifying, rollbackDurationMs: 456.7);

        // Assert
        var tags = activity!.TagObjects.ToDictionary(t => t.Key, t => t.Value);
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.Id].ShouldBe(reshardingId.ToString());
        tags[global::Encina.OpenTelemetry.ActivityTagNames.Resharding.Phase].ShouldBe(ReshardingPhase.Verifying.ToString());
        tags["resharding.rollback_duration_ms"].ShouldBe(456.7);
    }

    #endregion

    #region Test Helpers

    private static readonly ActivitySource TestSource = new("Encina.Test.Enricher");

    private static Activity? CreateTestActivity()
    {
        return TestSource.StartActivity("test-operation", ActivityKind.Internal);
    }

    private void SetupListener()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Test.Enricher",
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
