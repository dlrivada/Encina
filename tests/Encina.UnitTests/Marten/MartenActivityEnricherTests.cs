using System.Diagnostics;
using Encina.Marten;
using Encina.Marten.Instrumentation;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="MartenActivityEnricher"/>.
/// </summary>
public sealed class MartenActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;

    public MartenActivityEnricherTests()
    {
        _activitySource = new ActivitySource("MartenActivityEnricherTests");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
    }

    private static EventWithMetadata CreateTestEvent(
        string? correlationId = null,
        string? causationId = null) => new()
    {
        Id = Guid.NewGuid(),
        StreamId = Guid.NewGuid(),
        Version = 5,
        Sequence = 1000,
        EventTypeName = "OrderCreated",
        Data = new { OrderId = 123 },
        Timestamp = DateTimeOffset.UtcNow,
        CorrelationId = correlationId,
        CausationId = causationId,
    };

    [Fact]
    public void EnrichWithEvent_NullActivity_DoesNotThrow()
    {
        // Arrange
        var eventWithMetadata = CreateTestEvent();

        // Act & Assert
        var exception = Record.Exception(() =>
            MartenActivityEnricher.EnrichWithEvent(null, eventWithMetadata));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithEvent_NullEvent_DoesNotThrow()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act & Assert
        var exception = Record.Exception(() =>
            MartenActivityEnricher.EnrichWithEvent(activity, null));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithEvent_ValidInputs_SetsAllTags()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var eventWithMetadata = CreateTestEvent("correlation-123", "causation-456");

        // Act
        MartenActivityEnricher.EnrichWithEvent(activity, eventWithMetadata);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.message_id").ShouldBe(eventWithMetadata.Id.ToString());
        activity.GetTagItem("event.stream_id").ShouldBe(eventWithMetadata.StreamId.ToString());
        activity.GetTagItem("event.type_name").ShouldBe("OrderCreated");
        activity.GetTagItem("event.version").ShouldBe(5L);
        activity.GetTagItem("event.sequence").ShouldBe(1000L);
        activity.GetTagItem("event.timestamp").ShouldBe(eventWithMetadata.Timestamp.ToString("O"));
        activity.GetTagItem("event.correlation_id").ShouldBe("correlation-123");
        activity.GetTagItem("event.causation_id").ShouldBe("causation-456");
    }

    [Fact]
    public void EnrichWithEvent_NoCorrelationId_DoesNotSetCorrelationIdTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var eventWithMetadata = CreateTestEvent();

        // Act
        MartenActivityEnricher.EnrichWithEvent(activity, eventWithMetadata);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.correlation_id").ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCorrelationIds_NullActivity_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() =>
            MartenActivityEnricher.EnrichWithCorrelationIds(null, "correlation-123"));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCorrelationIds_ValidActivity_SetsCorrelationId()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        MartenActivityEnricher.EnrichWithCorrelationIds(activity, "correlation-xyz");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.correlation_id").ShouldBe("correlation-xyz");
    }

    [Fact]
    public void EnrichWithCorrelationIds_WithCausationId_SetsBothTags()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        MartenActivityEnricher.EnrichWithCorrelationIds(activity, "correlation-xyz", "causation-abc");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.correlation_id").ShouldBe("correlation-xyz");
        activity.GetTagItem("event.causation_id").ShouldBe("causation-abc");
    }

    [Fact]
    public void EnrichWithCorrelationIds_NullCorrelationId_DoesNotSetTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        MartenActivityEnricher.EnrichWithCorrelationIds(activity, null);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.correlation_id").ShouldBeNull();
    }

    [Fact]
    public void EnrichWithQueryResult_NullActivity_DoesNotThrow()
    {
        // Arrange
        var result = new EventQueryResult
        {
            Events = [],
            TotalCount = 0,
            HasMore = false,
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            MartenActivityEnricher.EnrichWithQueryResult(null, result));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithQueryResult_NullResult_DoesNotThrow()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act & Assert
        var exception = Record.Exception(() =>
            MartenActivityEnricher.EnrichWithQueryResult(activity, null));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithQueryResult_ValidInputs_SetsSummaryTags()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var events = new List<EventWithMetadata>
        {
            CreateTestEvent("shared-correlation"),
            CreateTestEvent("shared-correlation"),
        };
        var result = new EventQueryResult
        {
            Events = events,
            TotalCount = 100,
            HasMore = true,
        };

        // Act
        MartenActivityEnricher.EnrichWithQueryResult(activity, result);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.query.total_count").ShouldBe(100);
        activity.GetTagItem("event.query.returned_count").ShouldBe(2);
        activity.GetTagItem("event.query.has_more").ShouldBe(true);
        activity.GetTagItem("event.correlation_id").ShouldBe("shared-correlation");
    }

    [Fact]
    public void EnrichWithQueryResult_DifferentCorrelationIds_DoesNotSetCorrelationIdTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var events = new List<EventWithMetadata>
        {
            CreateTestEvent("correlation-1"),
            CreateTestEvent("correlation-2"),
        };
        var result = new EventQueryResult
        {
            Events = events,
            TotalCount = 2,
            HasMore = false,
        };

        // Act
        MartenActivityEnricher.EnrichWithQueryResult(activity, result);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.correlation_id").ShouldBeNull();
    }

    [Fact]
    public void EnrichWithQueryResult_EmptyEvents_DoesNotSetCorrelationIdTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var result = new EventQueryResult
        {
            Events = [],
            TotalCount = 0,
            HasMore = false,
        };

        // Act
        MartenActivityEnricher.EnrichWithQueryResult(activity, result);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.correlation_id").ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCausalChain_NullActivity_DoesNotThrow()
    {
        // Arrange
        var events = new List<EventWithMetadata> { CreateTestEvent() };

        // Act & Assert
        var exception = Record.Exception(() =>
            MartenActivityEnricher.EnrichWithCausalChain(null, events, CausalChainDirection.Ancestors));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCausalChain_NullEvents_DoesNotThrow()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act & Assert
        var exception = Record.Exception(() =>
            MartenActivityEnricher.EnrichWithCausalChain(activity, null, CausalChainDirection.Ancestors));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCausalChain_ValidInputs_SetsChainTags()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var events = new List<EventWithMetadata>
        {
            CreateTestEvent("shared-correlation"),
            CreateTestEvent("shared-correlation"),
            CreateTestEvent("shared-correlation"),
        };

        // Act
        MartenActivityEnricher.EnrichWithCausalChain(activity, events, CausalChainDirection.Ancestors);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.causal_chain.depth").ShouldBe(3);
        activity.GetTagItem("event.causal_chain.direction").ShouldBe("Ancestors");
        activity.GetTagItem("event.correlation_id").ShouldBe("shared-correlation");
    }

    [Fact]
    public void EnrichWithCausalChain_DescendantsDirection_SetsDirectionTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var events = new List<EventWithMetadata> { CreateTestEvent() };

        // Act
        MartenActivityEnricher.EnrichWithCausalChain(activity, events, CausalChainDirection.Descendants);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.causal_chain.direction").ShouldBe("Descendants");
    }

    [Fact]
    public void EnrichWithCausalChain_DifferentCorrelationIds_DoesNotSetCorrelationIdTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var events = new List<EventWithMetadata>
        {
            CreateTestEvent("correlation-1"),
            CreateTestEvent("correlation-2"),
        };

        // Act
        MartenActivityEnricher.EnrichWithCausalChain(activity, events, CausalChainDirection.Ancestors);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.correlation_id").ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCausalChain_EmptyEvents_SetsZeroDepth()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var events = new List<EventWithMetadata>();

        // Act
        MartenActivityEnricher.EnrichWithCausalChain(activity, events, CausalChainDirection.Ancestors);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.causal_chain.depth").ShouldBe(0);
    }
}
