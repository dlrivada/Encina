using System.Diagnostics;
using Encina.IdGeneration.Diagnostics;

namespace Encina.UnitTests.IdGeneration.Diagnostics;

/// <summary>
/// Unit tests for <see cref="IdGenerationActivitySource"/>.
/// </summary>
public sealed class IdGenerationActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public IdGenerationActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.IdGeneration",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    // ────────────────────────────────────────────────────────────
    //  SourceName
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SourceName_IsEncinaIdGeneration()
    {
        IdGenerationActivitySource.SourceName.ShouldBe("Encina.IdGeneration");
    }

    // ────────────────────────────────────────────────────────────
    //  StartIdGeneration
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StartIdGeneration_WithListener_ReturnsActivity()
    {
        var activity = IdGenerationActivitySource.StartIdGeneration("Snowflake");
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("encina.id_generation.generate");
        activity.Dispose();
    }

    [Fact]
    public void StartIdGeneration_SetsStrategyTag()
    {
        var activity = IdGenerationActivitySource.StartIdGeneration("ULID");
        activity.ShouldNotBeNull();
        activity!.GetTagItem("id.strategy").ShouldBe("ULID");
        activity.Dispose();
    }

    [Fact]
    public void StartIdGeneration_WithShardId_SetsShardIdTag()
    {
        var activity = IdGenerationActivitySource.StartIdGeneration("Snowflake", "42");
        activity.ShouldNotBeNull();
        activity!.GetTagItem("id.shard_id").ShouldBe("42");
        activity.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Complete
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_SetsOkStatus()
    {
        var activity = IdGenerationActivitySource.StartIdGeneration("Snowflake");
        IdGenerationActivitySource.Complete(activity, "12345");

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void Complete_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => IdGenerationActivitySource.Complete(null, "12345"));
    }

    // ────────────────────────────────────────────────────────────
    //  StartShardExtraction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StartShardExtraction_WithListener_ReturnsActivity()
    {
        var activity = IdGenerationActivitySource.StartShardExtraction("some-id");
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("encina.id_generation.extract_shard");
        activity.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Failed
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Failed_SetsErrorStatus()
    {
        var activity = IdGenerationActivitySource.StartIdGeneration("Snowflake");
        IdGenerationActivitySource.Failed(activity, "encina.idgen.clock_drift_detected", "Clock drift");

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void Failed_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => IdGenerationActivitySource.Failed(null, "code", "msg"));
    }
}
