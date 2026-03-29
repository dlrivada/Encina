using Encina.Marten.Projections;
using Shouldly;

namespace Encina.UnitTests.Marten.Projections;

public class ProjectionContextTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var context = new ProjectionContext();

        // Assert
        context.StreamId.ShouldBe(Guid.Empty);
        context.SequenceNumber.ShouldBe(0);
        context.GlobalPosition.ShouldBe(0);
        context.Timestamp.ShouldBe(default(DateTime));
        context.CorrelationId.ShouldBeNull();
        context.CausationId.ShouldBeNull();
        context.EventType.ShouldBe(string.Empty);
        context.Metadata.ShouldNotBeNull();
        context.Metadata.Count.ShouldBe(0);
    }

    [Fact]
    public void ParameterizedConstructor_SetsValues()
    {
        // Arrange
        var streamId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var context = new ProjectionContext(streamId, 5, 100, timestamp);

        // Assert
        context.StreamId.ShouldBe(streamId);
        context.SequenceNumber.ShouldBe(5);
        context.GlobalPosition.ShouldBe(100);
        context.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void InitSyntax_SetsAllProperties()
    {
        // Arrange
        var streamId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var context = new ProjectionContext
        {
            StreamId = streamId,
            SequenceNumber = 42,
            GlobalPosition = 1000,
            Timestamp = timestamp,
            CorrelationId = "corr-123",
            CausationId = "cause-456",
            EventType = "OrderCreated",
            Metadata = metadata,
        };

        // Assert
        context.StreamId.ShouldBe(streamId);
        context.SequenceNumber.ShouldBe(42);
        context.GlobalPosition.ShouldBe(1000);
        context.Timestamp.ShouldBe(timestamp);
        context.CorrelationId.ShouldBe("corr-123");
        context.CausationId.ShouldBe("cause-456");
        context.EventType.ShouldBe("OrderCreated");
        context.Metadata["key"].ShouldBe("value");
    }
}
