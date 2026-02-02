using System.Diagnostics;
using Encina.OpenTelemetry;
using Encina.OpenTelemetry.Enrichers;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Unit tests for <see cref="EventMetadataActivityEnricher"/>.
/// </summary>
public sealed class EventMetadataActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;

    public EventMetadataActivityEnricherTests()
    {
        _activitySource = new ActivitySource("EventMetadataActivityEnricherTests");
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

    [Fact]
    public void EnrichWithEvent_NullActivity_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
            EventMetadataActivityEnricher.EnrichWithEvent(
                null,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "TestEvent",
                1,
                100,
                DateTimeOffset.UtcNow));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithEvent_ValidActivity_SetsAllRequiredTags()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");
        var eventId = Guid.NewGuid();
        var streamId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        EventMetadataActivityEnricher.EnrichWithEvent(
            activity,
            eventId,
            streamId,
            "OrderCreated",
            5,
            1000,
            timestamp);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.MessageId).ShouldBe(eventId.ToString());
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.StreamId).ShouldBe(streamId.ToString());
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.TypeName).ShouldBe("OrderCreated");
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Version).ShouldBe(5L);
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Sequence).ShouldBe(1000L);
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Timestamp).ShouldBe(timestamp.ToString("O"));
    }

    [Fact]
    public void EnrichWithEvent_WithCorrelationId_SetsCorrelationIdTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithEvent(
            activity,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TestEvent",
            1,
            100,
            DateTimeOffset.UtcNow,
            correlationId: "correlation-123");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBe("correlation-123");
    }

    [Fact]
    public void EnrichWithEvent_WithCausationId_SetsCausationIdTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithEvent(
            activity,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TestEvent",
            1,
            100,
            DateTimeOffset.UtcNow,
            causationId: "causation-456");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CausationId).ShouldBe("causation-456");
    }

    [Fact]
    public void EnrichWithEvent_NullCorrelationId_DoesNotSetTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithEvent(
            activity,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TestEvent",
            1,
            100,
            DateTimeOffset.UtcNow,
            correlationId: null);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBeNull();
    }

    [Fact]
    public void EnrichWithEvent_EmptyCorrelationId_DoesNotSetTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithEvent(
            activity,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TestEvent",
            1,
            100,
            DateTimeOffset.UtcNow,
            correlationId: "");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBeNull();
    }

    [Fact]
    public void EnrichWithEvent_WhitespaceCorrelationId_DoesNotSetTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithEvent(
            activity,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TestEvent",
            1,
            100,
            DateTimeOffset.UtcNow,
            correlationId: "   ");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCorrelationIds_NullActivity_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() =>
            EventMetadataActivityEnricher.EnrichWithCorrelationIds(null, "correlation-123"));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithCorrelationIds_ValidActivity_SetsCorrelationId()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithCorrelationIds(activity, "correlation-xyz");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBe("correlation-xyz");
    }

    [Fact]
    public void EnrichWithCorrelationIds_WithCausationId_SetsBothTags()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithCorrelationIds(activity, "correlation-xyz", "causation-abc");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBe("correlation-xyz");
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CausationId).ShouldBe("causation-abc");
    }

    [Fact]
    public void EnrichWithCorrelationIds_NullCorrelationId_DoesNotSetTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithCorrelationIds(activity, null);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBeNull();
    }

    [Fact]
    public void EnrichWithQueryResult_NullActivity_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() =>
            EventMetadataActivityEnricher.EnrichWithQueryResult(null, 10, 5, true));

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnrichWithQueryResult_ValidActivity_SetsSummaryTags()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithQueryResult(activity, 100, 50, true);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.query.total_count").ShouldBe(100);
        activity.GetTagItem("event.query.returned_count").ShouldBe(50);
        activity.GetTagItem("event.query.has_more").ShouldBe(true);
    }

    [Fact]
    public void EnrichWithQueryResult_WithCorrelationId_SetsCorrelationIdTag()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithQueryResult(activity, 10, 5, false, "shared-correlation");

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId).ShouldBe("shared-correlation");
    }

    [Fact]
    public void EnrichWithQueryResult_NoMoreResults_HasMoreIsFalse()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestOperation");

        // Act
        EventMetadataActivityEnricher.EnrichWithQueryResult(activity, 5, 5, false);

        // Assert
        activity.ShouldNotBeNull();
        activity.GetTagItem("event.query.has_more").ShouldBe(false);
    }
}
