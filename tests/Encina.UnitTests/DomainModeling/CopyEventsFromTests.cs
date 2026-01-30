using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Tests for the CopyEventsFrom method on Entity/AggregateRoot.
/// </summary>
public class CopyEventsFromTests
{
    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAggregateRoot(Guid id) : base(id) { }

        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(Guid EntityId) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    private sealed record AnotherDomainEvent(string Message) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    #region CopyEventsFrom Tests

    [Fact]
    public void CopyEventsFrom_SourceWithSingleEvent_ShouldCopyToTarget()
    {
        // Arrange
        var source = new TestAggregateRoot(Guid.NewGuid());
        var target = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(source.Id);
        source.RaiseEvent(domainEvent);

        // Act
        target.CopyEventsFrom(source);

        // Assert
        target.DomainEvents.Count.ShouldBe(1);
        target.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void CopyEventsFrom_SourceWithMultipleEvents_ShouldCopyAllToTarget()
    {
        // Arrange
        var source = new TestAggregateRoot(Guid.NewGuid());
        var target = new TestAggregateRoot(Guid.NewGuid());
        var event1 = new TestDomainEvent(source.Id);
        var event2 = new AnotherDomainEvent("Test message");
        var event3 = new TestDomainEvent(Guid.NewGuid());
        source.RaiseEvent(event1);
        source.RaiseEvent(event2);
        source.RaiseEvent(event3);

        // Act
        target.CopyEventsFrom(source);

        // Assert
        target.DomainEvents.Count.ShouldBe(3);
        target.DomainEvents.ShouldContain(event1);
        target.DomainEvents.ShouldContain(event2);
        target.DomainEvents.ShouldContain(event3);
    }

    [Fact]
    public void CopyEventsFrom_SourceWithNoEvents_ShouldNotAddAnyEvents()
    {
        // Arrange
        var source = new TestAggregateRoot(Guid.NewGuid());
        var target = new TestAggregateRoot(Guid.NewGuid());

        // Act
        target.CopyEventsFrom(source);

        // Assert
        target.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void CopyEventsFrom_TargetAlreadyHasEvents_ShouldAppendSourceEvents()
    {
        // Arrange
        var source = new TestAggregateRoot(Guid.NewGuid());
        var target = new TestAggregateRoot(Guid.NewGuid());
        var sourceEvent = new TestDomainEvent(source.Id);
        var targetEvent = new AnotherDomainEvent("Target event");

        target.RaiseEvent(targetEvent);
        source.RaiseEvent(sourceEvent);

        // Act
        target.CopyEventsFrom(source);

        // Assert
        target.DomainEvents.Count.ShouldBe(2);
        target.DomainEvents.ShouldContain(targetEvent);
        target.DomainEvents.ShouldContain(sourceEvent);
    }

    [Fact]
    public void CopyEventsFrom_NullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var target = new TestAggregateRoot(Guid.NewGuid());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => target.CopyEventsFrom(null!));
    }

    [Fact]
    public void CopyEventsFrom_SameSourceMultipleTimes_ShouldCopyEventsEachTime()
    {
        // Arrange
        var source = new TestAggregateRoot(Guid.NewGuid());
        var target = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(source.Id);
        source.RaiseEvent(domainEvent);

        // Act
        target.CopyEventsFrom(source);
        target.CopyEventsFrom(source);

        // Assert
        target.DomainEvents.Count.ShouldBe(2);
    }

    [Fact]
    public void CopyEventsFrom_PreservesEventOrder()
    {
        // Arrange
        var source = new TestAggregateRoot(Guid.NewGuid());
        var target = new TestAggregateRoot(Guid.NewGuid());
        var event1 = new TestDomainEvent(Guid.NewGuid());
        var event2 = new AnotherDomainEvent("Second");
        var event3 = new TestDomainEvent(Guid.NewGuid());

        source.RaiseEvent(event1);
        source.RaiseEvent(event2);
        source.RaiseEvent(event3);

        // Act
        target.CopyEventsFrom(source);

        // Assert
        var events = target.DomainEvents.ToList();
        events[0].ShouldBe(event1);
        events[1].ShouldBe(event2);
        events[2].ShouldBe(event3);
    }

    [Fact]
    public void CopyEventsFrom_SourceEventsRemainIntact()
    {
        // Arrange
        var source = new TestAggregateRoot(Guid.NewGuid());
        var target = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(source.Id);
        source.RaiseEvent(domainEvent);

        // Act
        target.CopyEventsFrom(source);

        // Assert - Source should still have its events
        source.DomainEvents.Count.ShouldBe(1);
        source.DomainEvents.ShouldContain(domainEvent);
    }

    #endregion
}
