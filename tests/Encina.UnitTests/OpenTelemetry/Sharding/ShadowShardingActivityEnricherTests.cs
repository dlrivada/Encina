using System.Diagnostics;
using Encina.OpenTelemetry.Sharding;
using Encina.Sharding.Shadow;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.Sharding;

/// <summary>
/// Unit tests for <see cref="ShadowShardingActivityEnricher"/>.
/// </summary>
public sealed class ShadowShardingActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _source;
    private readonly ActivityListener _listener;

    public ShadowShardingActivityEnricherTests()
    {
        _source = new ActivitySource("Test.ShadowEnricher");
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Test.ShadowEnricher",
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
    public void EnrichWithShadowRouting_SetsRoutingTags()
    {
        using var activity = _source.StartActivity("test");
        var result = new ShadowComparisonResult("key", "prod-1", "shadow-1", true, TimeSpan.Zero, TimeSpan.Zero, true, DateTimeOffset.UtcNow);

        ShadowShardingActivityEnricher.EnrichWithShadowRouting(activity, result);

        activity!.Tags.ShouldContain(t => t.Key == "encina.sharding.shadow.production_shard" && t.Value == "prod-1");
        activity.Tags.ShouldContain(t => t.Key == "encina.sharding.shadow.shadow_shard" && t.Value == "shadow-1");
    }

    [Fact]
    public void EnrichWithShadowRouting_WithResultsMatch_SetsReadResultsTag()
    {
        using var activity = _source.StartActivity("test");
        var result = new ShadowComparisonResult("key", "prod-1", "shadow-1", true, TimeSpan.Zero, TimeSpan.Zero, true, DateTimeOffset.UtcNow);

        ShadowShardingActivityEnricher.EnrichWithShadowRouting(activity, result);

        // Tags collection only includes string values; bool tags are stored separately
        // Verify via GetTagItem instead
        activity!.GetTagItem("encina.sharding.shadow.read_results_match").ShouldNotBeNull();
    }

    [Fact]
    public void EnrichWithShadowRouting_WithoutResultsMatch_OmitsReadResultsTag()
    {
        using var activity = _source.StartActivity("test");
        var result = new ShadowComparisonResult("key", "prod-1", "shadow-1", true, TimeSpan.Zero, TimeSpan.Zero, null, DateTimeOffset.UtcNow);

        ShadowShardingActivityEnricher.EnrichWithShadowRouting(activity, result);

        activity!.GetTagItem("encina.sharding.shadow.read_results_match").ShouldBeNull();
    }

    [Fact]
    public void EnrichWithShadowWrite_SetsWriteTags()
    {
        using var activity = _source.StartActivity("test");

        ShadowShardingActivityEnricher.EnrichWithShadowWrite(activity, "shard-2", true);

        activity!.Tags.ShouldContain(t => t.Key == "encina.sharding.shadow.shadow_shard" && t.Value == "shard-2");
        activity.Tags.ShouldContain(t => t.Key == "encina.sharding.shadow.write_outcome" && t.Value == "success");
    }

    [Fact]
    public void EnrichWithShadowWrite_Failure_SetsFailureOutcome()
    {
        using var activity = _source.StartActivity("test");

        ShadowShardingActivityEnricher.EnrichWithShadowWrite(activity, "shard-2", false);

        activity!.Tags.ShouldContain(t => t.Key == "encina.sharding.shadow.write_outcome" && t.Value == "failure");
    }
}
