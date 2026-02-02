using Encina.Marten;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="EventWithMetadata"/>.
/// </summary>
public sealed class EventWithMetadataTests
{
    [Fact]
    public void RequiredProperties_MustBeInitialized()
    {
        var id = Guid.NewGuid();
        var streamId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var data = new TestEvent("Test Data");

        var eventWithMetadata = new EventWithMetadata
        {
            Id = id,
            StreamId = streamId,
            Version = 5,
            Sequence = 123,
            EventTypeName = "TestEvent",
            Data = data,
            Timestamp = timestamp,
        };

        eventWithMetadata.Id.ShouldBe(id);
        eventWithMetadata.StreamId.ShouldBe(streamId);
        eventWithMetadata.Version.ShouldBe(5);
        eventWithMetadata.Sequence.ShouldBe(123);
        eventWithMetadata.EventTypeName.ShouldBe("TestEvent");
        eventWithMetadata.Data.ShouldBe(data);
        eventWithMetadata.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void OptionalProperties_DefaultToNull()
    {
        var eventWithMetadata = new EventWithMetadata
        {
            Id = Guid.NewGuid(),
            StreamId = Guid.NewGuid(),
            Version = 1,
            Sequence = 1,
            EventTypeName = "TestEvent",
            Data = new object(),
            Timestamp = DateTimeOffset.UtcNow,
        };

        eventWithMetadata.CorrelationId.ShouldBeNull();
        eventWithMetadata.CausationId.ShouldBeNull();
    }

    [Fact]
    public void Headers_DefaultToEmptyDictionary()
    {
        var eventWithMetadata = new EventWithMetadata
        {
            Id = Guid.NewGuid(),
            StreamId = Guid.NewGuid(),
            Version = 1,
            Sequence = 1,
            EventTypeName = "TestEvent",
            Data = new object(),
            Timestamp = DateTimeOffset.UtcNow,
        };

        eventWithMetadata.Headers.ShouldNotBeNull();
        eventWithMetadata.Headers.ShouldBeEmpty();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var headers = new Dictionary<string, object>
        {
            ["UserId"] = "user-123",
            ["TenantId"] = "tenant-abc",
        };

        var eventWithMetadata = new EventWithMetadata
        {
            Id = Guid.NewGuid(),
            StreamId = Guid.NewGuid(),
            Version = 3,
            Sequence = 456,
            EventTypeName = "OrderCreated",
            Data = new TestEvent("Order Data"),
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = "correlation-xyz",
            CausationId = "causation-123",
            Headers = headers,
        };

        eventWithMetadata.CorrelationId.ShouldBe("correlation-xyz");
        eventWithMetadata.CausationId.ShouldBe("causation-123");
        eventWithMetadata.Headers.ShouldBe(headers);
        eventWithMetadata.Headers["UserId"].ShouldBe("user-123");
        eventWithMetadata.Headers["TenantId"].ShouldBe("tenant-abc");
    }

    private sealed record TestEvent(string Value);
}
