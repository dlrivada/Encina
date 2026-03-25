using System.Diagnostics;
using Encina.OpenTelemetry.Enrichers;
using Tags = global::Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry.Enrichers;

public class AuditActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _source = new("test.audit");
    private readonly ActivityListener _listener;

    public AuditActivityEnricherTests()
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
    public void EnrichWithAuditRecord_NullActivity_DoesNotThrow()
    {
        AuditActivityEnricher.EnrichWithAuditRecord(null, "Order", "created");
    }

    [Fact]
    public void EnrichWithAuditRecord_SetsEntityTypeAndAction()
    {
        using var activity = _source.StartActivity("test")!;
        AuditActivityEnricher.EnrichWithAuditRecord(activity, "Order", "updated");

        activity.GetTagItem(Tags.Audit.EntityType).ShouldBe("Order");
        activity.GetTagItem(Tags.Audit.Action).ShouldBe("updated");
    }

    [Fact]
    public void EnrichWithAuditQuery_NullActivity_DoesNotThrow()
    {
        AuditActivityEnricher.EnrichWithAuditQuery(null, "by_entity");
    }

    [Fact]
    public void EnrichWithAuditQuery_SetsQueryType()
    {
        using var activity = _source.StartActivity("test")!;
        AuditActivityEnricher.EnrichWithAuditQuery(activity, "by_entity");

        activity.GetTagItem(Tags.Audit.QueryType).ShouldBe("by_entity");
    }

    [Fact]
    public void EnrichWithAuditQuery_WithEntityType_SetsBothTags()
    {
        using var activity = _source.StartActivity("test")!;
        AuditActivityEnricher.EnrichWithAuditQuery(activity, "by_entity", "Order");

        activity.GetTagItem(Tags.Audit.QueryType).ShouldBe("by_entity");
        activity.GetTagItem(Tags.Audit.EntityType).ShouldBe("Order");
    }

    [Fact]
    public void EnrichWithAuditQuery_WithoutEntityType_DoesNotSetEntityType()
    {
        using var activity = _source.StartActivity("test")!;
        AuditActivityEnricher.EnrichWithAuditQuery(activity, "by_date_range");

        activity.GetTagItem(Tags.Audit.QueryType).ShouldBe("by_date_range");
        activity.GetTagItem(Tags.Audit.EntityType).ShouldBeNull();
    }
}
