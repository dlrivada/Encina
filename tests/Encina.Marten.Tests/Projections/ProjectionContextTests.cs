using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

public sealed class ProjectionContextTests
{
    [Fact]
    public void DefaultConstructor_InitializesWithDefaults()
    {
        // Act
        var context = new ProjectionContext();

        // Assert
        context.StreamId.ShouldBe(Guid.Empty);
        context.SequenceNumber.ShouldBe(0);
        context.GlobalPosition.ShouldBe(0);
        context.Timestamp.ShouldBe(default);
        context.EventType.ShouldBe(string.Empty);
        context.CorrelationId.ShouldBeNull();
        context.CausationId.ShouldBeNull();
        context.Metadata.ShouldNotBeNull();
        context.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void ParameterizedConstructor_SetsValues()
    {
        // Arrange
        var streamId = Guid.NewGuid();
        var sequenceNumber = 5L;
        var globalPosition = 100L;
        var timestamp = DateTime.UtcNow;

        // Act
        var context = new ProjectionContext(streamId, sequenceNumber, globalPosition, timestamp);

        // Assert
        context.StreamId.ShouldBe(streamId);
        context.SequenceNumber.ShouldBe(sequenceNumber);
        context.GlobalPosition.ShouldBe(globalPosition);
        context.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void InitSyntax_SetsAllProperties()
    {
        // Arrange
        var streamId = Guid.NewGuid();
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var context = new ProjectionContext
        {
            StreamId = streamId,
            SequenceNumber = 10,
            GlobalPosition = 200,
            Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EventType = "OrderCreated",
            CorrelationId = "corr-123",
            CausationId = "cause-456",
            Metadata = metadata,
        };

        // Assert
        context.StreamId.ShouldBe(streamId);
        context.SequenceNumber.ShouldBe(10);
        context.GlobalPosition.ShouldBe(200);
        context.Timestamp.ShouldBe(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        context.EventType.ShouldBe("OrderCreated");
        context.CorrelationId.ShouldBe("corr-123");
        context.CausationId.ShouldBe("cause-456");
        context.Metadata.ShouldBe(metadata);
    }
}
