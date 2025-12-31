using Encina.DomainModeling;
using Encina.Testing.EventSourcing;
using Shouldly;

namespace Encina.Testing.Tests.EventSourcing;

/// <summary>
/// Contract tests for <see cref="AggregateTestBase{TAggregate,TId}"/> ensuring the public API contract.
/// These tests verify the expected behavior that consumers rely on.
/// </summary>
public sealed class AggregateTestBaseContractTests
{
    #region Given Contract

    [Fact]
    public void Given_ShouldResetAggregateState_BetweenCalls()
    {
        // Contract: Each Given() call should start with a fresh aggregate
        var test = new ContractTestOrderAggregate();

        // First Given
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));
        test.TestWhen(order => { });
        var firstAggregate = test.GetTestAggregate();

        // Second Given should create a new aggregate
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer2"));
        test.TestWhen(order => { });
        var secondAggregate = test.GetTestAggregate();

        // Assert - they should have different customer IDs
        firstAggregate.CustomerId.ShouldBe("Customer1");
        secondAggregate.CustomerId.ShouldBe("Customer2");
    }

    [Fact]
    public void Given_ShouldApplyEventsInOrder()
    {
        // Contract: Events should be applied in the order provided
        var test = new ContractTestOrderAggregate();
        var orderId = Guid.NewGuid();

        test.TestGiven(
            new OrderCreated(orderId, "Customer"),
            new ItemAdded("Product1", 1),
            new ItemAdded("Product2", 2)
        );

        test.TestWhen(order => { });

        test.TestThenState(order =>
        {
            order.Id.ShouldBe(orderId);
            order.Items.Count.ShouldBe(2);
            order.Items["Product1"].ShouldBe(1);
            order.Items["Product2"].ShouldBe(2);
        });
    }

    #endregion

    #region When Contract

    [Fact]
    public void When_ShouldCaptureException_WithoutRethrow()
    {
        // Contract: Exceptions should be captured, not thrown
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        // This should not throw
        var act = () => test.TestWhen(order =>
        {
            throw new InvalidOperationException("Test exception");
        });

        Should.NotThrow(act);
    }

    [Fact]
    public void When_ShouldBeCallableOnce_PerGiven()
    {
        // Contract: When can be called after Given
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        // First When
        test.TestWhen(order => order.AddItem("Product1", 1));

        // Can check results
        test.GetTestUncommittedEvents().Count.ShouldBe(1);
    }

    #endregion

    #region Then Contract

    [Fact]
    public void Then_ShouldReturnFirstMatchingEvent()
    {
        // Contract: Then<T>() returns the first matching event
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        test.TestWhen(order =>
        {
            order.AddItem("First", 1);
            order.AddItem("Second", 2);
        });

        var firstEvent = test.TestThen<ItemAdded>();
        firstEvent.ProductId.ShouldBe("First");
    }

    [Fact]
    public void ThenEvents_ShouldVerifyExactOrder()
    {
        // Contract: ThenEvents verifies exact order and types
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        test.TestWhen(order =>
        {
            order.Create(Guid.NewGuid(), "Customer");
            order.AddItem("Product", 1);
        });

        // Should pass - exact order
        test.TestThenEvents(typeof(OrderCreated), typeof(ItemAdded));
    }

    [Fact]
    public void ThenNoEvents_ShouldPassWhenNoEventsRaised()
    {
        // Contract: ThenNoEvents passes when aggregate doesn't raise events
        var test = new ContractTestOrderAggregate();
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer"));

        test.TestWhen(order => { }); // No action that raises events

        var act = () => test.TestThenNoEvents();
        Should.NotThrow(act);
    }

    #endregion

    #region ThenState Contract

    [Fact]
    public void ThenState_ShouldProvideAccessToFinalState()
    {
        // Contract: ThenState provides the aggregate after all events are applied
        var test = new ContractTestOrderAggregate();
        var orderId = Guid.NewGuid();

        test.TestGiven(new OrderCreated(orderId, "Customer"));
        test.TestWhen(order => order.AddItem("Product", 5));

        test.TestThenState(order =>
        {
            order.Id.ShouldBe(orderId);
            order.Items["Product"].ShouldBe(5);
        });
    }

    #endregion

    #region ThenThrows Contract

    [Fact]
    public void ThenThrows_ShouldReturnExactExceptionType()
    {
        // Contract: ThenThrows returns the exact exception that was thrown
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        test.TestWhen(order =>
        {
            throw new ArgumentException("Test message", "testParam");
        });

        var exception = test.TestThenThrows<ArgumentException>();
        exception.Message.ShouldContain("Test message");
        exception.ParamName.ShouldBe("testParam");
    }

    #endregion

    #region Error Handling Contract

    [Fact]
    public void Then_AfterException_ShouldThrowHelpfulMessage()
    {
        // Contract: When an exception was thrown, Then should guide user to ThenThrows
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        test.TestWhen(order => throw new InvalidOperationException());

        var act = () => test.TestThen<OrderCreated>();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldMatch("*exception*ThenThrows*");
    }

    [Fact]
    public void When_WithoutGiven_ShouldThrowHelpfulMessage()
    {
        // Contract: When without Given should explain the error
        var test = new ContractTestOrderAggregate();

        var act = () => test.TestWhen(order => { });
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldMatch("*Given*");
    }

    [Fact]
    public void Then_WithoutWhen_ShouldThrowHelpfulMessage()
    {
        // Contract: Then without When should explain the error
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        var act = () => test.TestThen<OrderCreated>();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldMatch("*When*");
    }

    #endregion

    #region Aggregate Property Contract

    [Fact]
    public void Aggregate_AfterWhen_ShouldBeAccessible()
    {
        // Contract: Aggregate property is accessible after When
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();
        test.TestWhen(order => order.Create(Guid.NewGuid(), "Customer"));

        var aggregate = test.GetTestAggregate();
        aggregate.ShouldNotBeNull();
        aggregate.CustomerId.ShouldBe("Customer");
    }

    #endregion

    #region GetUncommittedEvents Contract

    [Fact]
    public void GetUncommittedEvents_ShouldReturnReadOnlyList()
    {
        // Contract: GetUncommittedEvents returns IReadOnlyList
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();
        test.TestWhen(order => order.AddItem("Product", 1));

        var events = test.GetTestUncommittedEvents();

        events.ShouldBeAssignableTo<IReadOnlyList<object>>();
    }

    [Fact]
    public void GetUncommittedEvents_Generic_ShouldFilterCorrectly()
    {
        // Contract: Generic version filters to matching type
        var test = new ContractTestOrderAggregate();
        test.TestGivenEmpty();

        test.TestWhen(order =>
        {
            order.Create(Guid.NewGuid(), "Customer");
            order.AddItem("Product1", 1);
            order.AddItem("Product2", 2);
        });

        var itemEvents = test.GetTestUncommittedEvents<ItemAdded>();
        itemEvents.Count.ShouldBe(2);
        itemEvents.ShouldAllBe(x => x is ItemAdded);
    }

    #endregion

    #region Test Helper Classes

    private sealed class ContractTestOrderAggregate : AggregateTestBase<TestAggregate, Guid>
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
    }

    private sealed class TestAggregate : AggregateBase<Guid>
    {
        public string? CustomerId { get; private set; }
        public Dictionary<string, int> Items { get; } = [];

        public void Create(Guid orderId, string customerId)
        {
            RaiseEvent(new OrderCreated(orderId, customerId));
        }

        public void AddItem(string productId, int quantity)
        {
            RaiseEvent(new ItemAdded(productId, quantity));
        }

        protected override void Apply(object domainEvent)
        {
            switch (domainEvent)
            {
                case OrderCreated created:
                    Id = created.OrderId;
                    CustomerId = created.CustomerId;
                    break;

                case ItemAdded itemAdded:
                    Items.TryAdd(itemAdded.ProductId, 0);
                    Items[itemAdded.ProductId] += itemAdded.Quantity;
                    break;
            }
        }
    }

    private sealed record OrderCreated(Guid OrderId, string CustomerId);
    private sealed record ItemAdded(string ProductId, int Quantity);

    #endregion
}
