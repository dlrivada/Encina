using System.Diagnostics;
using Encina.OpenTelemetry.Enrichers;
using Tags = global::Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry.Enrichers;

public class ReferenceTableActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _source = new("test.reftable");
    private readonly ActivityListener _listener;

    public ReferenceTableActivityEnricherTests()
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
    public void EnrichWithReplication_NullActivity_DoesNotThrow()
    {
        ReferenceTableActivityEnricher.EnrichWithReplication(null, "Country", 195, 4, 250.0);
    }

    [Fact]
    public void EnrichWithReplication_SetsAllTags()
    {
        using var activity = _source.StartActivity("test")!;
        ReferenceTableActivityEnricher.EnrichWithReplication(activity, "Country", 195, 4, 250.0);

        activity.GetTagItem(Tags.ReferenceTable.EntityType).ShouldBe("Country");
        activity.GetTagItem(Tags.ReferenceTable.RowsSynced).ShouldBe(195);
        activity.GetTagItem(Tags.ReferenceTable.ShardCount).ShouldBe(4);
        activity.GetTagItem(Tags.ReferenceTable.DurationMs).ShouldBe(250.0);
    }

    [Fact]
    public void EnrichWithChangeDetection_NullActivity_DoesNotThrow()
    {
        ReferenceTableActivityEnricher.EnrichWithChangeDetection(null, "Country", true, "abc123");
    }

    [Fact]
    public void EnrichWithChangeDetection_WithHash_SetsAllTags()
    {
        using var activity = _source.StartActivity("test")!;
        ReferenceTableActivityEnricher.EnrichWithChangeDetection(activity, "Country", true, "hash123");

        activity.GetTagItem(Tags.ReferenceTable.EntityType).ShouldBe("Country");
        activity.GetTagItem(Tags.ReferenceTable.ChangeDetected).ShouldBe(true);
        activity.GetTagItem(Tags.ReferenceTable.HashValue).ShouldBe("hash123");
    }

    [Fact]
    public void EnrichWithChangeDetection_NullHash_DoesNotSetHashTag()
    {
        using var activity = _source.StartActivity("test")!;
        ReferenceTableActivityEnricher.EnrichWithChangeDetection(activity, "Currency", false, null);

        activity.GetTagItem(Tags.ReferenceTable.ChangeDetected).ShouldBe(false);
        activity.GetTagItem(Tags.ReferenceTable.HashValue).ShouldBeNull();
    }
}
