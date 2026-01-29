using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Tests for domain event functionality in Entity base class.
/// </summary>
public class EntityDomainEventsTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity(Guid id) : base(id) { }

        // Expose AddDomainEvent for testing
        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
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

    #region AddDomainEvent Tests

    [Fact]
    public void AddDomainEvent_SingleEvent_ShouldBeInCollection()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(entity.Id);

        // Act
        entity.RaiseEvent(domainEvent);

        // Assert
        entity.DomainEvents.Count.ShouldBe(1);
        entity.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_MultipleEvents_ShouldAllBeInCollection()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var event1 = new TestDomainEvent(entity.Id);
        var event2 = new AnotherDomainEvent("Test message");
        var event3 = new TestDomainEvent(Guid.NewGuid());

        // Act
        entity.RaiseEvent(event1);
        entity.RaiseEvent(event2);
        entity.RaiseEvent(event3);

        // Assert
        entity.DomainEvents.Count.ShouldBe(3);
        entity.DomainEvents.ShouldContain(event1);
        entity.DomainEvents.ShouldContain(event2);
        entity.DomainEvents.ShouldContain(event3);
    }

    [Fact]
    public void AddDomainEvent_SameEventTwice_ShouldAddBothTimes()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(entity.Id);

        // Act
        entity.RaiseEvent(domainEvent);
        entity.RaiseEvent(domainEvent);

        // Assert
        // List allows duplicates, so both should be added
        entity.DomainEvents.Count.ShouldBe(2);
    }

    [Fact]
    public void AddDomainEvent_NullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => entity.RaiseEvent(null!));
    }

    #endregion

    #region RemoveDomainEvent Tests

    [Fact]
    public void RemoveDomainEvent_ExistingEvent_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(entity.Id);
        entity.RaiseEvent(domainEvent);

        // Act
        var result = entity.RemoveDomainEvent(domainEvent);

        // Assert
        result.ShouldBeTrue();
        entity.DomainEvents.Count.ShouldBe(0);
        entity.DomainEvents.ShouldNotContain(domainEvent);
    }

    [Fact]
    public void RemoveDomainEvent_NonExistingEvent_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var existingEvent = new TestDomainEvent(entity.Id);
        var nonExistingEvent = new TestDomainEvent(Guid.NewGuid());
        entity.RaiseEvent(existingEvent);

        // Act
        var result = entity.RemoveDomainEvent(nonExistingEvent);

        // Assert
        result.ShouldBeFalse();
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveDomainEvent_FromEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(entity.Id);

        // Act
        var result = entity.RemoveDomainEvent(domainEvent);

        // Assert
        result.ShouldBeFalse();
        entity.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveDomainEvent_OneOfMultiple_ShouldOnlyRemoveSpecified()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var event1 = new TestDomainEvent(entity.Id);
        var event2 = new AnotherDomainEvent("Keep this");
        var event3 = new TestDomainEvent(Guid.NewGuid());
        entity.RaiseEvent(event1);
        entity.RaiseEvent(event2);
        entity.RaiseEvent(event3);

        // Act
        var result = entity.RemoveDomainEvent(event2);

        // Assert
        result.ShouldBeTrue();
        entity.DomainEvents.Count.ShouldBe(2);
        entity.DomainEvents.ShouldContain(event1);
        entity.DomainEvents.ShouldNotContain(event2);
        entity.DomainEvents.ShouldContain(event3);
    }

    #endregion

    #region ClearDomainEvents Tests

    [Fact]
    public void ClearDomainEvents_WithMultipleEvents_ShouldClearAll()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        entity.RaiseEvent(new TestDomainEvent(entity.Id));
        entity.RaiseEvent(new AnotherDomainEvent("Message 1"));
        entity.RaiseEvent(new TestDomainEvent(Guid.NewGuid()));
        entity.RaiseEvent(new AnotherDomainEvent("Message 2"));

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.Count.ShouldBe(0);
        entity.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_OnEmptyCollection_ShouldNotThrow()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert - Should not throw
        Should.NotThrow(() => entity.ClearDomainEvents());
        entity.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_ThenAddNew_ShouldOnlyContainNewEvents()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        entity.RaiseEvent(new TestDomainEvent(entity.Id));
        entity.RaiseEvent(new AnotherDomainEvent("Old event"));
        entity.ClearDomainEvents();

        var newEvent = new TestDomainEvent(Guid.NewGuid());

        // Act
        entity.RaiseEvent(newEvent);

        // Assert
        entity.DomainEvents.Count.ShouldBe(1);
        entity.DomainEvents.ShouldContain(newEvent);
    }

    #endregion

    #region DomainEvents Collection Behavior Tests

    [Fact]
    public void DomainEvents_ShouldReturnIReadOnlyCollection()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act
        var events = entity.DomainEvents;

        // Assert
        events.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void DomainEvents_NewEntity_ShouldBeEmpty()
    {
        // Arrange & Act
        var entity = new TestEntity(Guid.NewGuid());

        // Assert
        entity.DomainEvents.ShouldNotBeNull();
        entity.DomainEvents.ShouldBeEmpty();
        entity.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void DomainEvents_ShouldPreserveOrder()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var event1 = new TestDomainEvent(Guid.NewGuid());
        var event2 = new AnotherDomainEvent("Second");
        var event3 = new TestDomainEvent(Guid.NewGuid());

        // Act
        entity.RaiseEvent(event1);
        entity.RaiseEvent(event2);
        entity.RaiseEvent(event3);

        // Assert
        var eventsList = entity.DomainEvents.ToList();
        eventsList[0].ShouldBe(event1);
        eventsList[1].ShouldBe(event2);
        eventsList[2].ShouldBe(event3);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void AggregateRoot_ShouldHaveDomainEventCapabilities()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(aggregate.Id);

        // Act
        aggregate.RaiseEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Count.ShouldBe(1);
        aggregate.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_ShouldWork()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.RaiseEvent(new TestDomainEvent(aggregate.Id));
        aggregate.RaiseEvent(new AnotherDomainEvent("Test"));

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Entity_WithDomainEvents_EqualityShouldStillWorkByIdOnly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        entity1.RaiseEvent(new TestDomainEvent(Guid.NewGuid()));
        // entity2 has no events

        // Act & Assert - Entities should still be equal because they have the same Id
        entity1.ShouldBe(entity2);
        (entity1 == entity2).ShouldBeTrue();
    }

    #endregion
}
