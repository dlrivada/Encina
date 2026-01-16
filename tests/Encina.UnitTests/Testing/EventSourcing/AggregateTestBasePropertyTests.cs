using Encina.Testing;
using Encina.DomainModeling;
using Encina.Testing.EventSourcing;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.UnitTests.Testing.EventSourcing;

/// <summary>
/// Property-based tests for <see cref="AggregateTestBase{TAggregate,TId}"/> invariants.
/// </summary>
public sealed class AggregateTestBasePropertyTests
{
    #region Given Invariants

    [Property(MaxTest = 100)]
    public bool Given_WithAnyNumberOfEvents_ShouldClearUncommittedEvents(PositiveInt eventCount)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        var count = Math.Min(eventCount.Get, 20); // Limit to reasonable count
        var events = Enumerable.Range(0, count)
            .Select(i => (object)new ItemAddedEvent($"Product{i}", i + 1))
            .ToArray();

        // Act
        test.TestGiven(events);
        var aggregate = test.ExecuteWhenAndGetAggregate(o => { });

        // Assert - uncommitted should always be empty after Given
        return aggregate.UncommittedEvents.Count == 0;
    }

    [Property(MaxTest = 100)]
    public bool Given_WithAnyNumberOfEvents_ShouldSetCorrectVersion(PositiveInt eventCount)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        var count = Math.Min(eventCount.Get, 20);
        var events = Enumerable.Range(0, count)
            .Select(i => (object)new ItemAddedEvent($"Product{i}", i + 1))
            .ToArray();

        // Act
        test.TestGiven(events);
        var aggregate = test.ExecuteWhenAndGetAggregate(o => { });

        // Assert - version should equal number of events applied
        return aggregate.Version == count;
    }

    [Property(MaxTest = 50)]
    public bool GivenEmpty_VersionIsAlwaysZero()
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();

        // Act
        test.TestGivenEmpty();
        var aggregate = test.ExecuteWhenAndGetAggregate(o => { });

        // Assert
        return aggregate.Version == 0;
    }

    [Property(MaxTest = 50)]
    public bool GivenEmpty_UncommittedEventsIsAlwaysEmpty()
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();

        // Act
        test.TestGivenEmpty();
        var aggregate = test.ExecuteWhenAndGetAggregate(o => { });

        // Assert
        return aggregate.UncommittedEvents.Count == 0;
    }

    #endregion

    #region When/Then Invariants

    [Property(MaxTest = 100)]
    public bool When_RaisesEvent_ThenAlwaysFindsEvent(PositiveInt quantity)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        test.TestGivenEmpty();
        var qty = Math.Max(1, quantity.Get % 1000);

        // Act
        test.TestWhen(order => order.AddItem("TestProduct", qty));

        // Assert - Then should find the event
        var @event = test.TestThen<ItemAddedEvent>();
        return @event.Quantity == qty;
    }

    [Property(MaxTest = 50)]
    public bool When_ThrowsException_ThenThrowsAlwaysFindsException(NonEmptyString errorMessage)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        test.TestGivenEmpty();

        // Act - force an exception
        test.TestWhen(order => order.ThrowError(errorMessage.Get));

        // Assert - ThenThrows should find it
        var ex = test.TestThenThrows<InvalidOperationException>();
        return ex.Message.Contains(errorMessage.Get);
    }

    [Property(MaxTest = 100)]
    public bool When_NoEventsRaised_ThenNoEventsAlwaysPasses()
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        test.TestGivenEmpty();

        // Act - do nothing
        test.TestWhen(order => { });

        // Assert
        test.TestThenNoEvents();
        return true;
    }

    #endregion

    #region State Invariants

    [Property(MaxTest = 100)]
    public bool ThenState_AlwaysReflectsAppliedEvents(PositiveInt itemCount)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        test.TestGivenEmpty();
        var count = Math.Min(itemCount.Get, 10);

        // Act - add multiple items
        test.TestWhen(order =>
        {
            for (var i = 0; i < count; i++)
            {
                order.AddItem($"Product{i}", 1);
            }
        });

        // Assert - state should reflect all items
        var stateIsCorrect = true;
        test.TestThenState(order =>
        {
            stateIsCorrect = order.Items.Count == count;
        });

        return stateIsCorrect;
    }

    [Property(MaxTest = 50)]
    public bool Aggregate_AfterWhen_IsAlwaysAccessible()
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order => { });

        // Assert
        var aggregate = test.GetTestAggregate();
        return aggregate is not null;
    }

    #endregion

    #region Event Order Invariants

    [Property(MaxTest = 50)]
    public bool ThenEvents_PreservesEventOrder(PositiveInt count)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        test.TestGivenEmpty();
        var eventCount = Math.Min(count.Get, 5);

        // Act
        test.TestWhen(order =>
        {
            order.Create(Guid.NewGuid(), "Customer");
            for (var i = 0; i < eventCount; i++)
            {
                order.AddItem($"Product{i}", 1);
            }
        });

        // Build expected types
        var expectedTypes = new List<Type> { typeof(OrderCreatedEvent) };
        expectedTypes.AddRange(Enumerable.Repeat(typeof(ItemAddedEvent), eventCount));

        // Assert
        test.TestThenEvents([.. expectedTypes]);
        return true;
    }

    #endregion

    #region GetUncommittedEvents Invariants

    [Property(MaxTest = 100)]
    public bool GetUncommittedEvents_CountMatchesRaisedEvents(PositiveInt count)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        test.TestGivenEmpty();
        var eventCount = Math.Min(count.Get, 20);

        // Act
        test.TestWhen(order =>
        {
            for (var i = 0; i < eventCount; i++)
            {
                order.AddItem($"Product{i}", 1);
            }
        });

        // Assert
        var events = test.GetTestUncommittedEvents();
        return events.Count == eventCount;
    }

    [Property(MaxTest = 100)]
    public bool GetUncommittedEventsGeneric_FiltersCorrectly(PositiveInt itemCount, PositiveInt submitCount)
    {
        // Arrange
        var test = new PropertyTestOrderAggregate();
        var items = Math.Min(itemCount.Get, 5);
        var submits = Math.Min(submitCount.Get, 3);

        test.TestGiven(new OrderCreatedEvent(Guid.NewGuid(), "Customer"));

        // Act - mix different event types
        test.TestWhen(order =>
        {
            for (var i = 0; i < items; i++)
            {
                order.AddItem($"Product{i}", 1);
            }
        });

        // Assert
        var itemEvents = test.GetTestUncommittedEvents<ItemAddedEvent>();
        return itemEvents.Count() == items;
    }

    #endregion

    #region Test Helper Classes

    private sealed class PropertyTestOrderAggregate : AggregateTestBase<TestAggregate, Guid>
    {
        public void TestGiven(params object[] events) => Given(events);
        public void TestGivenEmpty() => GivenEmpty();
        public void TestWhen(Action<TestAggregate> action) => When(action);
        public TEvent TestThen<TEvent>() where TEvent : class => Then<TEvent>();
        public void TestThenEvents(params Type[] eventTypes) => ThenEvents(eventTypes);
        public void TestThenNoEvents() => ThenNoEvents();
        public void TestThenState(Action<TestAggregate> validator) => ThenState(validator);
        public TException TestThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
        public IReadOnlyList<object> GetTestUncommittedEvents() => GetUncommittedEvents();
        public IEnumerable<TEvent> GetTestUncommittedEvents<TEvent>() where TEvent : class => GetUncommittedEvents<TEvent>();
        public TestAggregate GetTestAggregate() => Aggregate;

        public TestAggregate ExecuteWhenAndGetAggregate(Action<TestAggregate> action)
        {
            When(action);
            return Aggregate;
        }
    }

    private sealed class TestAggregate : AggregateBase<Guid>
    {
        public string? CustomerId { get; private set; }
        public Dictionary<string, int> Items { get; } = [];

        public void Create(Guid orderId, string customerId)
        {
            RaiseEvent(new OrderCreatedEvent(orderId, customerId));
        }

        public void AddItem(string productId, int quantity)
        {
            RaiseEvent(new ItemAddedEvent(productId, quantity));
        }

        public void ThrowError(string message)
        {
            // Access instance data to avoid CA1822
            _ = CustomerId;
            throw new InvalidOperationException(message);
        }

        protected override void Apply(object domainEvent)
        {
            switch (domainEvent)
            {
                case OrderCreatedEvent created:
                    Id = created.OrderId;
                    CustomerId = created.CustomerId;
                    break;

                case ItemAddedEvent itemAdded:
                    if (Items.TryGetValue(itemAdded.ProductId, out var existing))
                    {
                        Items[itemAdded.ProductId] = existing + itemAdded.Quantity;
                    }
                    else
                    {
                        Items[itemAdded.ProductId] = itemAdded.Quantity;
                    }
                    break;
            }
        }
    }

    private sealed record OrderCreatedEvent(Guid OrderId, string CustomerId);
    private sealed record ItemAddedEvent(string ProductId, int Quantity);

    #endregion
}
