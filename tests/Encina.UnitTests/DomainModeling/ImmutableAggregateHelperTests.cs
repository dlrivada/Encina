using Encina.DomainModeling;
using NSubstitute;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Tests for <see cref="ImmutableAggregateHelper"/> utility class.
/// </summary>
public class ImmutableAggregateHelperTests
{
    #region Test Types

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

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public int Value { get; init; }

        public TestAggregateRoot() : base(Guid.NewGuid()) { }
        public TestAggregateRoot(Guid id) : base(id) { }

        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);

        /// <summary>
        /// Simulates an immutable update that raises an event and returns a new instance.
        /// </summary>
        public TestAggregateRoot WithNewValue(int newValue)
        {
            RaiseDomainEvent(new TestDomainEvent(Id));
            return new TestAggregateRoot(Id) { Name = this.Name, Value = newValue };
        }
    }

    #endregion

    #region PrepareForUpdate - Null Parameter Tests

    [Fact]
    public void PrepareForUpdate_WithNullModified_ThrowsArgumentNullException()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var collector = Substitute.For<IDomainEventCollector>();
        TestAggregateRoot modified = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector));
        ex.ParamName.ShouldBe("modified");
    }

    [Fact]
    public void PrepareForUpdate_WithNullOriginal_ThrowsArgumentNullException()
    {
        // Arrange
        var modified = new TestAggregateRoot();
        var collector = Substitute.For<IDomainEventCollector>();
        TestAggregateRoot original = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector));
        ex.ParamName.ShouldBe("original");
    }

    [Fact]
    public void PrepareForUpdate_WithNullCollector_ThrowsArgumentNullException()
    {
        // Arrange
        var modified = new TestAggregateRoot();
        var original = new TestAggregateRoot();
        IDomainEventCollector collector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector));
        ex.ParamName.ShouldBe("collector");
    }

    #endregion

    #region PrepareForUpdate - Event Copying Tests

    [Fact]
    public void PrepareForUpdate_CopiesEventsFromOriginalToModified()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent(original.Id);
        original.RaiseEvent(domainEvent);

        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert
        modified.DomainEvents.Count.ShouldBe(1);
        modified.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void PrepareForUpdate_WithZeroEvents_CompletesSuccessfully()
    {
        // Arrange
        var original = new TestAggregateRoot(); // No events
        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert
        result.ShouldBeSameAs(modified);
        modified.DomainEvents.ShouldBeEmpty();
        collector.Received(1).TrackAggregate(modified);
    }

    [Fact]
    public void PrepareForUpdate_WithMultipleEvents_PreservesAllEvents()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var event1 = new TestDomainEvent(original.Id);
        var event2 = new AnotherDomainEvent("Message 1");
        var event3 = new TestDomainEvent(Guid.NewGuid());
        original.RaiseEvent(event1);
        original.RaiseEvent(event2);
        original.RaiseEvent(event3);

        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert
        modified.DomainEvents.Count.ShouldBe(3);
        modified.DomainEvents.ShouldContain(event1);
        modified.DomainEvents.ShouldContain(event2);
        modified.DomainEvents.ShouldContain(event3);
    }

    [Fact]
    public void PrepareForUpdate_PreservesEventOrder()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var event1 = new TestDomainEvent(Guid.NewGuid());
        var event2 = new AnotherDomainEvent("Second");
        var event3 = new TestDomainEvent(Guid.NewGuid());
        original.RaiseEvent(event1);
        original.RaiseEvent(event2);
        original.RaiseEvent(event3);

        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert
        var events = modified.DomainEvents.ToList();
        events[0].ShouldBe(event1);
        events[1].ShouldBe(event2);
        events[2].ShouldBe(event3);
    }

    #endregion

    #region PrepareForUpdate - Aggregate Tracking Tests

    [Fact]
    public void PrepareForUpdate_TracksModifiedAggregate()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert - Verify TrackAggregate was called with the modified entity
        collector.Received(1).TrackAggregate(modified);
    }

    [Fact]
    public void PrepareForUpdate_TracksModifiedNotOriginal()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var trackedAggregates = new List<IAggregateRoot>();
        var collector = Substitute.For<IDomainEventCollector>();
        collector.When(x => x.TrackAggregate(Arg.Any<IAggregateRoot>()))
            .Do(callInfo => trackedAggregates.Add(callInfo.Arg<IAggregateRoot>()));

        // Act
        ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert - Should track only the modified instance (by reference)
        trackedAggregates.Count.ShouldBe(1);
        trackedAggregates[0].ShouldBeSameAs(modified);
        // Verify original is NOT tracked by reference identity
        ReferenceEquals(trackedAggregates[0], original).ShouldBeFalse();
    }

    #endregion

    #region PrepareForUpdate - Return Value Tests

    [Fact]
    public void PrepareForUpdate_ReturnsModifiedEntity()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert
        result.ShouldBeSameAs(modified);
    }

    [Fact]
    public void PrepareForUpdate_AllowsFluentChaining()
    {
        // Arrange
        var original = new TestAggregateRoot { Name = "Original", Value = 10 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act - Simulate fluent pattern: operation -> prepare -> update
        var prepared = ImmutableAggregateHelper.PrepareForUpdate(
            original.WithNewValue(20),
            original,
            collector);

        // Assert
        prepared.Value.ShouldBe(20);
        prepared.Name.ShouldBe("Original");
        // Event from WithNewValue should be on the prepared instance
        prepared.DomainEvents.Count.ShouldBe(1);
    }

    #endregion

    #region PrepareForUpdate - Edge Cases

    [Fact]
    public void PrepareForUpdate_ModifiedAlreadyHasEvents_AppendsOriginalEvents()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var originalEvent = new TestDomainEvent(original.Id);
        original.RaiseEvent(originalEvent);

        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var modifiedEvent = new AnotherDomainEvent("New event on modified");
        modified.RaiseEvent(modifiedEvent);

        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert - Both events should be present
        modified.DomainEvents.Count.ShouldBe(2);
        modified.DomainEvents.ShouldContain(modifiedEvent);
        modified.DomainEvents.ShouldContain(originalEvent);
    }

    [Fact]
    public void PrepareForUpdate_OriginalEventsRemainIntact()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent(original.Id);
        original.RaiseEvent(domainEvent);

        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act
        ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert - Original should still have its events
        original.DomainEvents.Count.ShouldBe(1);
        original.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void PrepareForUpdate_SameAggregateAsOriginalAndModified_ThrowsInvalidOperation()
    {
        // Arrange - Edge case: same instance passed as both
        // This is an invalid use case and will throw because CopyEventsFrom
        // modifies the collection while iterating over it
        var aggregate = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent(aggregate.Id);
        aggregate.RaiseEvent(domainEvent);

        var collector = Substitute.For<IDomainEventCollector>();

        // Act & Assert - This should throw InvalidOperationException
        // because CopyEventsFrom attempts to add events to the same collection
        // that it's iterating over
        Should.Throw<InvalidOperationException>(() =>
            ImmutableAggregateHelper.PrepareForUpdate(aggregate, aggregate, collector));
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public void PrepareForUpdate_TypicalWorkflow_WorksCorrectly()
    {
        // Arrange - Simulate typical Dapper/ADO.NET workflow
        var original = new TestAggregateRoot { Name = "Order", Value = 100 };

        // Simulate loading from database (no events initially)
        // Then performing a domain operation that raises an event
        original.RaiseEvent(new TestDomainEvent(original.Id));

        // Create modified version (simulating with-expression)
        var modified = new TestAggregateRoot(original.Id)
        {
            Name = original.Name,
            Value = 200 // Changed value
        };

        var trackedAggregates = new List<IAggregateRoot>();
        var collector = Substitute.For<IDomainEventCollector>();
        collector.When(x => x.TrackAggregate(Arg.Any<IAggregateRoot>()))
            .Do(callInfo => trackedAggregates.Add(callInfo.Arg<IAggregateRoot>()));

        // Act
        var prepared = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Assert
        // 1. Returns the modified aggregate
        prepared.ShouldBeSameAs(modified);
        prepared.Value.ShouldBe(200);

        // 2. Events are copied
        prepared.DomainEvents.Count.ShouldBe(1);

        // 3. Aggregate is tracked for event dispatch
        trackedAggregates.Count.ShouldBe(1);
        trackedAggregates[0].ShouldBeSameAs(modified);
    }

    #endregion
}
