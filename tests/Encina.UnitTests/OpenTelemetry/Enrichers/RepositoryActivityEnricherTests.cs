using System.Diagnostics;
using Encina.OpenTelemetry.Enrichers;
using Tags = global::Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry.Enrichers;

public class RepositoryActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _source = new("test.repo");
    private readonly ActivityListener _listener;

    public RepositoryActivityEnricherTests()
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
    public void EnrichWithOperation_NullActivity_DoesNotThrow()
    {
        RepositoryActivityEnricher.EnrichWithOperation(null, "get_by_id", "Order", "ef_core");
    }

    [Fact]
    public void EnrichWithOperation_SetsAllTags()
    {
        using var activity = _source.StartActivity("test")!;
        RepositoryActivityEnricher.EnrichWithOperation(activity, "find", "Order", "dapper", 42);

        activity.GetTagItem(Tags.Repository.Operation).ShouldBe("find");
        activity.GetTagItem(Tags.Repository.EntityType).ShouldBe("Order");
        activity.GetTagItem(Tags.Repository.Provider).ShouldBe("dapper");
        activity.GetTagItem(Tags.Repository.ResultCount).ShouldBe(42);
    }

    [Fact]
    public void EnrichWithOperation_WithoutResultCount_DoesNotSetResultCount()
    {
        using var activity = _source.StartActivity("test")!;
        RepositoryActivityEnricher.EnrichWithOperation(activity, "add", "Order", "ef_core");

        activity.GetTagItem(Tags.Repository.ResultCount).ShouldBeNull();
    }

    [Fact]
    public void EnrichWithError_NullActivity_DoesNotThrow()
    {
        RepositoryActivityEnricher.EnrichWithError(null, "get_by_id", "Order", "NOT_FOUND");
    }

    [Fact]
    public void EnrichWithError_SetsAllTags()
    {
        using var activity = _source.StartActivity("test")!;
        RepositoryActivityEnricher.EnrichWithError(activity, "update", "Order", "CONCURRENCY_CONFLICT");

        activity.GetTagItem(Tags.Repository.Operation).ShouldBe("update");
        activity.GetTagItem(Tags.Repository.EntityType).ShouldBe("Order");
        activity.GetTagItem(Tags.Repository.ErrorCode).ShouldBe("CONCURRENCY_CONFLICT");
    }
}
