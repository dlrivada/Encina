using System.Diagnostics;
using Encina.OpenTelemetry.Enrichers;
using Tags = global::Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry.Enrichers;

public class TenancyActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _source = new("test.tenancy");
    private readonly ActivityListener _listener;

    public TenancyActivityEnricherTests()
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
    public void EnrichWithResolution_NullActivity_DoesNotThrow()
    {
        TenancyActivityEnricher.EnrichWithResolution(null, "tenant-1", "header", "success");
    }

    [Fact]
    public void EnrichWithResolution_SetsStrategyAndOutcome()
    {
        using var activity = _source.StartActivity("test")!;
        TenancyActivityEnricher.EnrichWithResolution(activity, "tenant-1", "header", "success");

        activity.GetTagItem(Tags.Tenancy.Strategy).ShouldBe("header");
        activity.GetTagItem(Tags.Tenancy.Outcome).ShouldBe("success");
        activity.GetTagItem(Tags.Tenancy.TenantId).ShouldBe("tenant-1");
    }

    [Fact]
    public void EnrichWithResolution_NullTenantId_DoesNotSetTenantIdTag()
    {
        using var activity = _source.StartActivity("test")!;
        TenancyActivityEnricher.EnrichWithResolution(activity, null, "claim", "not_found");

        activity.GetTagItem(Tags.Tenancy.TenantId).ShouldBeNull();
        activity.GetTagItem(Tags.Tenancy.Outcome).ShouldBe("not_found");
    }

    [Fact]
    public void EnrichWithTenantScope_NullActivity_DoesNotThrow()
    {
        TenancyActivityEnricher.EnrichWithTenantScope(null, "tenant-1", "Order");
    }

    [Fact]
    public void EnrichWithTenantScope_SetsTenantIdAndEntityType()
    {
        using var activity = _source.StartActivity("test")!;
        TenancyActivityEnricher.EnrichWithTenantScope(activity, "tenant-1", "Order");

        activity.GetTagItem(Tags.Tenancy.TenantId).ShouldBe("tenant-1");
        activity.GetTagItem(Tags.Tenancy.EntityType).ShouldBe("Order");
    }
}
