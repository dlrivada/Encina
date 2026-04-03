using System.Diagnostics;
using Encina.OpenTelemetry.Sharding;
using Encina.Sharding.Shadow;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="ShadowShardingActivityEnricher"/> to verify null parameter handling.
/// </summary>
public sealed class ShadowShardingActivityEnricherGuardTests : IDisposable
{
    private readonly ActivitySource _source;
    private readonly ActivityListener _listener;

    public ShadowShardingActivityEnricherGuardTests()
    {
        _source = new ActivitySource("TestGuard.Shadow");
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TestGuard.Shadow",
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
    public void EnrichWithShadowRouting_NullActivity_DoesNotThrow()
    {
        var result = new ShadowComparisonResult("key", "prod-shard", "shadow-shard", true, TimeSpan.Zero, TimeSpan.Zero, null, DateTimeOffset.UtcNow);
        Should.NotThrow(() => ShadowShardingActivityEnricher.EnrichWithShadowRouting(null, result));
    }

    [Fact]
    public void EnrichWithShadowRouting_NullResult_ThrowsArgumentNullException()
    {
        using var activity = _source.StartActivity("test");

        var act = () => ShadowShardingActivityEnricher.EnrichWithShadowRouting(activity, null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void EnrichWithShadowWrite_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => ShadowShardingActivityEnricher.EnrichWithShadowWrite(null, "shard-1", true));
    }
}
