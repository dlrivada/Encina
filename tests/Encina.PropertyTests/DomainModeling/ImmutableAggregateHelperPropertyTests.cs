using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Shouldly;

namespace Encina.PropertyTests.DomainModeling;

/// <summary>
/// Property-based tests for <see cref="ImmutableAggregateHelper"/>.
/// Verifies invariants that MUST hold for immutable record updates.
/// </summary>
[Trait("Category", "Property")]
public sealed class ImmutableAggregateHelperPropertyTests
{
    #region Event Copying Invariants

    [Fact]
    public void Property_PrepareForUpdate_CopiesAllEventsFromOriginal()
    {
        // Property: All events from original MUST be copied to modified
        var eventCount = 5;
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original" };
        var modified = new TestPropertyAggregateRoot(id) { Name = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        // Raise events on original
        for (int i = 0; i < eventCount; i++)
        {
            original.RaiseEvent(new TestPropertyEvent($"Event {i}"));
        }

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        result.DomainEvents.Count.ShouldBe(eventCount,
            "Modified must have all events from original");

        for (int i = 0; i < eventCount; i++)
        {
            var evt = result.DomainEvents.ElementAt(i) as TestPropertyEvent;
            evt.ShouldNotBeNull();
            evt.Message.ShouldBe($"Event {i}");
        }
    }

    [Fact]
    public void Property_PrepareForUpdate_PreservesEventOrder()
    {
        // Property: Events MUST maintain their original order
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original" };
        var modified = new TestPropertyAggregateRoot(id) { Name = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        original.RaiseEvent(new TestPropertyEvent("First"));
        original.RaiseEvent(new TestPropertyEvent("Second"));
        original.RaiseEvent(new TestPropertyEvent("Third"));

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        var events = result.DomainEvents.Cast<TestPropertyEvent>().ToList();
        events[0].Message.ShouldBe("First");
        events[1].Message.ShouldBe("Second");
        events[2].Message.ShouldBe("Third");
    }

    [Property(MaxTest = 50)]
    public bool Property_PrepareForUpdate_EventCountInvariant(PositiveInt eventCount)
    {
        // Property: Number of events in modified MUST equal number in original
        var count = Math.Min(eventCount.Get, 20); // Limit for test speed
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original" };
        var modified = new TestPropertyAggregateRoot(id) { Name = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        for (int i = 0; i < count; i++)
        {
            original.RaiseEvent(new TestPropertyEvent($"Event {i}"));
        }

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        return result.DomainEvents.Count == count;
    }

    #endregion

    #region Collector Tracking Invariants

    [Fact]
    public void Property_PrepareForUpdate_RegistersWithCollector()
    {
        // Property: Modified aggregate MUST be registered with collector
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original" };
        var modified = new TestPropertyAggregateRoot(id) { Name = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        collector.Received(1).TrackAggregate(Arg.Is<TestPropertyAggregateRoot>(a => a.Id == id));
    }

    [Property(MaxTest = 20)]
    public bool Property_PrepareForUpdate_TracksOnlyOnce(PositiveInt iterations)
    {
        // Property: Multiple calls with same aggregate MUST each register once
        var count = Math.Min(iterations.Get, 5);
        var collector = Substitute.For<IDomainEventCollector>();

        for (int i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            var original = new TestPropertyAggregateRoot(id) { Name = $"Original {i}" };
            var modified = new TestPropertyAggregateRoot(id) { Name = $"Modified {i}" };

            ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);
        }

        // Should have exactly 'count' TrackAggregate calls
        collector.Received(count).TrackAggregate(Arg.Any<IAggregateRoot>());
        return true;
    }

    #endregion

    #region Return Value Invariants

    [Fact]
    public void Property_PrepareForUpdate_ReturnsModifiedAggregate()
    {
        // Property: Return value MUST be the modified aggregate (same reference)
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original" };
        var modified = new TestPropertyAggregateRoot(id) { Name = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        result.ShouldBeSameAs(modified,
            "PrepareForUpdate must return the modified aggregate");
    }

    [Fact]
    public void Property_PrepareForUpdate_PreservesModifiedProperties()
    {
        // Property: Modified aggregate properties MUST be preserved
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original", Amount = 100m };
        var modified = new TestPropertyAggregateRoot(id) { Name = "NewName", Amount = 200m };
        var collector = Substitute.For<IDomainEventCollector>();

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        result.Name.ShouldBe("NewName");
        result.Amount.ShouldBe(200m);
        result.Id.ShouldBe(id);
    }

    #endregion

    #region Empty Events Invariants

    [Fact]
    public void Property_PrepareForUpdate_EmptyOriginalEvents_ModifiedHasNoEvents()
    {
        // Property: If original has no events, modified MUST have no events
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original" };
        var modified = new TestPropertyAggregateRoot(id) { Name = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        result.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Property_PrepareForUpdate_AppendsOriginalEventsToModified()
    {
        // Property: CopyEventsFrom APPENDS events from original to modified's existing events
        var id = Guid.NewGuid();
        var original = new TestPropertyAggregateRoot(id) { Name = "Original" };
        var modified = new TestPropertyAggregateRoot(id) { Name = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        // Raise events on both
        original.RaiseEvent(new TestPropertyEvent("OriginalEvent"));
        modified.RaiseEvent(new TestPropertyEvent("ModifiedEvent"));

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        // Should have both events (existing on modified + copied from original)
        result.DomainEvents.Count.ShouldBe(2);
        result.DomainEvents.Cast<TestPropertyEvent>()
            .ShouldContain(e => e.Message == "ModifiedEvent");
        result.DomainEvents.Cast<TestPropertyEvent>()
            .ShouldContain(e => e.Message == "OriginalEvent");
    }

    #endregion

    #region Type Safety Invariants

    [Fact]
    public void Property_PrepareForUpdate_WorksWithAnyAggregateRoot()
    {
        // Property: PrepareForUpdate MUST work with any IAggregateRoot implementation
        var id = Guid.NewGuid();
        var original = new AnotherTestAggregate(id) { Description = "Original" };
        var modified = new AnotherTestAggregate(id) { Description = "Modified" };
        var collector = Substitute.For<IDomainEventCollector>();

        original.RaiseEvent(new TestPropertyEvent("Test"));

        var result = ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector);

        result.ShouldBeSameAs(modified);
        result.Description.ShouldBe("Modified");
        result.DomainEvents.Count.ShouldBe(1);
    }

    #endregion
}

#region Test Infrastructure

/// <summary>
/// Test domain event for property tests.
/// </summary>
public sealed record TestPropertyEvent(string Message) : DomainEvent;

/// <summary>
/// Test aggregate root for property tests.
/// </summary>
public sealed class TestPropertyAggregateRoot : AggregateRoot<Guid>
{
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }

    public TestPropertyAggregateRoot(Guid id) : base(id) { }

    public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}

/// <summary>
/// Another test aggregate to verify type independence.
/// </summary>
public sealed class AnotherTestAggregate : AggregateRoot<Guid>
{
    public string Description { get; init; } = string.Empty;

    public AnotherTestAggregate(Guid id) : base(id) { }

    public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}

#endregion
