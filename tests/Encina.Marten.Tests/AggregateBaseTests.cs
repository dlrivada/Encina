namespace Encina.Marten.Tests;

public class AggregateBaseTests
{
    [Fact]
    public void NewAggregate_HasVersionZero()
    {
        // Arrange & Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.Version.Should().Be(0);
    }

    [Fact]
    public void NewAggregate_HasNoUncommittedEvents()
    {
        // Arrange & Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void RaiseEvent_IncrementsVersion()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoSomething("test");

        // Assert
        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void RaiseEvent_AddsToUncommittedEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoSomething("test");

        // Assert
        aggregate.UncommittedEvents.Should().HaveCount(1);
        aggregate.UncommittedEvents[0].Should().BeOfType<TestEvent>();
    }

    [Fact]
    public void RaiseEvent_AppliesEventToState()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoSomething("test-value");

        // Assert
        aggregate.CurrentValue.Should().Be("test-value");
    }

    [Fact]
    public void MultipleEvents_VersionIncrementsCorrectly()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoSomething("first");
        aggregate.DoSomething("second");
        aggregate.DoSomething("third");

        // Assert
        aggregate.Version.Should().Be(3);
        aggregate.UncommittedEvents.Should().HaveCount(3);
    }

    [Fact]
    public void ClearUncommittedEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething("test");
        aggregate.DoSomething("test2");

        // Act
        aggregate.ClearUncommittedEvents();

        // Assert
        aggregate.UncommittedEvents.Should().BeEmpty();
        aggregate.Version.Should().Be(2); // Version should remain unchanged
    }

    [Fact]
    public void Aggregate_WithGuidId_SetsBaseId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = new TestAggregateWithId(id);

        // Assert
        aggregate.Id.Should().Be(id);
        ((IAggregate)aggregate).Id.Should().Be(id);
    }

    // Test aggregate implementation
    private sealed class TestAggregate : AggregateBase
    {
        public string CurrentValue { get; private set; } = string.Empty;

        public void DoSomething(string value)
        {
            RaiseEvent(new TestEvent(value));
        }

        protected override void Apply(object domainEvent)
        {
            if (domainEvent is TestEvent testEvent)
            {
                CurrentValue = testEvent.Value;
            }
        }
    }

    private sealed class TestAggregateWithId : AggregateBase<Guid>
    {
        public TestAggregateWithId(Guid id)
        {
            Id = id;
        }

        protected override void Apply(object domainEvent)
        {
            // No-op for this test
        }
    }

    private sealed record TestEvent(string Value);
}
