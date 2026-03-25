using System.Diagnostics;
using Encina.OpenTelemetry.Enrichers;
using Encina.Sharding.Migrations;
using Tags = global::Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry.Enrichers;

public class MigrationActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _source = new("test.migration");
    private readonly ActivityListener _listener;

    public MigrationActivityEnricherTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    [Fact]
    public void EnrichWithMigrationResult_NullActivity_DoesNotThrow()
    {
        MigrationActivityEnricher.EnrichWithMigrationResult(null, Guid.NewGuid(), MigrationStrategy.Sequential, 5, 4, 1, 1500.0);
    }

    [Fact]
    public void EnrichWithMigrationResult_SetsAllTags()
    {
        using var activity = _source.StartActivity("test")!;
        var id = Guid.NewGuid();
        MigrationActivityEnricher.EnrichWithMigrationResult(activity, id, MigrationStrategy.Parallel, 10, 8, 2, 3000.0);

        activity.GetTagItem(Tags.Migration.Id).ShouldBe(id.ToString());
        activity.GetTagItem(Tags.Migration.Strategy).ShouldBe("Parallel");
        activity.GetTagItem(Tags.Migration.ShardCount).ShouldBe(10);
        activity.GetTagItem(Tags.Migration.ShardsSucceeded).ShouldBe(8);
        activity.GetTagItem(Tags.Migration.ShardsFailed).ShouldBe(2);
        activity.GetTagItem(Tags.Migration.DurationMs).ShouldBe(3000.0);
    }

    [Fact]
    public void EnrichWithDriftDetection_NullActivity_DoesNotThrow()
    {
        MigrationActivityEnricher.EnrichWithDriftDetection(null, true, 3, "shard-1");
    }

    [Fact]
    public void EnrichWithDriftDetection_WithBaseline_SetsAllTags()
    {
        using var activity = _source.StartActivity("test")!;
        MigrationActivityEnricher.EnrichWithDriftDetection(activity, true, 2, "shard-baseline");

        activity.GetTagItem(Tags.Migration.DriftDetected).ShouldBe(true);
        activity.GetTagItem(Tags.Migration.DriftedShardCount).ShouldBe(2);
        activity.GetTagItem(Tags.Migration.BaselineShardId).ShouldBe("shard-baseline");
    }

    [Fact]
    public void EnrichWithDriftDetection_NullBaseline_DoesNotSetBaselineTag()
    {
        using var activity = _source.StartActivity("test")!;
        MigrationActivityEnricher.EnrichWithDriftDetection(activity, false, 0, null);

        activity.GetTagItem(Tags.Migration.DriftDetected).ShouldBe(false);
        activity.GetTagItem(Tags.Migration.BaselineShardId).ShouldBeNull();
    }

    [Fact]
    public void EnrichWithRollback_NullActivity_DoesNotThrow()
    {
        MigrationActivityEnricher.EnrichWithRollback(null, Guid.NewGuid(), 3, 500.0);
    }

    [Fact]
    public void EnrichWithRollback_SetsAllTags()
    {
        using var activity = _source.StartActivity("test")!;
        var id = Guid.NewGuid();
        MigrationActivityEnricher.EnrichWithRollback(activity, id, 5, 750.0);

        activity.GetTagItem(Tags.Migration.Id).ShouldBe(id.ToString());
        activity.GetTagItem(Tags.Migration.ShardsRolledBack).ShouldBe(5);
        activity.GetTagItem(Tags.Migration.RollbackDurationMs).ShouldBe(750.0);
    }
}
