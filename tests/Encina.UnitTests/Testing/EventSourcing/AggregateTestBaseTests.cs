using Encina.Testing;
using Encina.DomainModeling;
using Encina.Testing.EventSourcing;
using Shouldly;

namespace Encina.UnitTests.Testing.EventSourcing;

public class AggregateTestBaseTests
{
    /// <summary>
    /// Shared constants for error message fragments to avoid brittle string literals in assertions.
    /// </summary>
    private static class ErrorMessages
    {
        public const string Given = "Given()";
        public const string When = "When()";
        public const string ExceptionWasThrown = "exception was thrown";
        public const string NoExceptionWasThrown = "no exception was thrown";
        public const string Position0 = "position 0";
        public const string ExpectedNoEventsButFound = "Expected no events but found";
    }

    #region Given Tests

    [Fact]
    public void Given_WithEvents_ShouldApplyEventsToAggregate()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        test.TestGiven(
            new OrderCreated(Guid.NewGuid(), "Customer1"),
            new ItemAdded("Product1", 2)
        );

        // Assert
        var aggregate = test.ExecuteWhenAndGetAggregate(order => { });
        aggregate.CustomerId.ShouldBe("Customer1");
        aggregate.Items.Count.ShouldBe(1);
        aggregate.Items["Product1"].ShouldBe(2);
    }

    [Fact]
    public void Given_WithEvents_ShouldClearUncommittedEvents()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));
        var aggregate = test.ExecuteWhenAndGetAggregate(order => { });

        // Assert - uncommitted should be empty since Given events are historical
        aggregate.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Given_WithEvents_ShouldIncrementVersion()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        test.TestGiven(
            new OrderCreated(Guid.NewGuid(), "Customer1"),
            new ItemAdded("Product1", 1),
            new ItemAdded("Product2", 3)
        );
        var aggregate = test.ExecuteWhenAndGetAggregate(order => { });

        // Assert - version should reflect applied events
        aggregate.Version.ShouldBe(3);
    }

    [Fact]
    public void Given_WithNullEvents_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        var act = () => test.TestGiven(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("events");
    }

    #endregion

    #region GivenEmpty Tests

    [Fact]
    public void GivenEmpty_ShouldCreateFreshAggregate()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        test.TestGivenEmpty();
        var aggregate = test.ExecuteWhenAndGetAggregate(order => { });

        // Assert
        aggregate.Version.ShouldBe(0);
        aggregate.UncommittedEvents.ShouldBeEmpty();
        aggregate.CustomerId.ShouldBeNull();
    }

    #endregion

    #region When Tests

    [Fact]
    public void When_WithAction_ShouldExecuteAction()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order => order.Create(Guid.NewGuid(), "TestCustomer"));

        // Assert
        _ = test.GetSingleUncommittedEvent<OrderCreated>();
    }

    [Fact]
    public void When_WithoutGiven_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        var act = () => test.TestWhen(order => order.Submit());

        // Assert
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(ErrorMessages.Given);
    }

    [Fact]
    public void When_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestWhen(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("action");
    }

    [Fact]
    public void When_ActionThrowsException_ShouldCaptureException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));

        // Act - Submit without items should throw
        test.TestWhen(order => order.SubmitWithValidation());

        // Assert - should be able to use ThenThrows
        var thrownException = test.TestThenThrows<InvalidOperationException>();
        Assert.NotNull(thrownException);
    }

    #endregion

    #region WhenAsync Tests

    [Fact]
    public async Task WhenAsync_WithAsyncAction_ShouldExecuteAction()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(
            new OrderCreated(Guid.NewGuid(), "Customer1"),
            new ItemAdded("Product1", 1)
        );

        // Act
        await test.TestWhenAsync(async order =>
        {
            await Task.CompletedTask;
            order.Submit();
        });

        // Assert
        _ = test.GetSingleUncommittedEvent<OrderSubmitted>();
    }

    [Fact]
    public async Task WhenAsync_ActionThrowsException_ShouldCaptureException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        await test.TestWhenAsync(async order =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Async error");
        });

        // Assert
        test.TestThenThrows<InvalidOperationException>(ex =>
            ex.Message.ShouldContain("Async error"));
    }

    #endregion

    #region Then<TEvent> Tests

    [Fact]
    public void Then_WhenEventRaised_ShouldReturnEvent()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();
        var orderId = Guid.NewGuid();

        // Act
        test.TestWhen(order => order.Create(orderId, "Customer1"));

        // Assert
        var @event = test.TestThen<OrderCreated>();
        @event.OrderId.ShouldBe(orderId);
        @event.CustomerId.ShouldBe("Customer1");
    }

    [Fact]
    public void Then_WhenNoMatchingEvent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order => order.Create(Guid.NewGuid(), "Customer1"));

        // Assert
        var act = () => test.TestThen<OrderSubmitted>();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(nameof(OrderSubmitted));
    }

    [Fact]
    public void Then_WithoutWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestThen<OrderCreated>();

        // Assert
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(ErrorMessages.When);
    }

    [Fact]
    public void Then_WithValidator_ShouldValidateEvent()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order => order.Create(Guid.NewGuid(), "ValidatedCustomer"));

        // Assert
        test.TestThen<OrderCreated>(@event =>
        {
            @event.CustomerId.ShouldBe("ValidatedCustomer");
        });
    }

    [Fact]
    public void Then_WhenExceptionWasThrown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));

        // Act
        test.TestWhen(order => order.SubmitWithValidation());

        // Assert
        var act = () => test.TestThen<OrderSubmitted>();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(ErrorMessages.ExceptionWasThrown);
    }

    #endregion

    #region ThenEvents Tests

    [Fact]
    public void ThenEvents_WhenEventsMatchInOrder_ShouldPass()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order =>
        {
            order.Create(Guid.NewGuid(), "Customer1");
            order.AddItem("Product1", 2);
        });

        // Assert
        var exception = Record.Exception(() => test.TestThenEvents(typeof(OrderCreated), typeof(ItemAdded)));
        Assert.Null(exception);
    }

    [Fact]
    public void ThenEvents_WhenWrongOrder_ShouldThrow()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order =>
        {
            order.Create(Guid.NewGuid(), "Customer1");
            order.AddItem("Product1", 2);
        });

        // Assert
        var act = () => test.TestThenEvents(typeof(ItemAdded), typeof(OrderCreated));
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(ErrorMessages.Position0);
    }

    [Fact]
    public void ThenEvents_WhenWrongCount_ShouldThrow()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order => order.Create(Guid.NewGuid(), "Customer1"));

        // Assert
        var act = () => test.TestThenEvents(typeof(OrderCreated), typeof(ItemAdded));
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("Expected");
        ex.Message.ShouldContain("events");
    }

    #endregion

    #region ThenNoEvents Tests

    [Fact]
    public void ThenNoEvents_WhenNoEventsRaised_ShouldPass()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(
            new OrderCreated(Guid.NewGuid(), "Customer1"),
            new OrderSubmitted(DateTime.UtcNow)
        );

        // Act - Submit again (idempotent)
        test.TestWhen(order => order.SubmitIdempotent());

        // Assert
        var exception = Record.Exception(() => test.TestThenNoEvents());
        Assert.Null(exception);
    }

    [Fact]
    public void ThenNoEvents_WhenEventsRaised_ShouldThrow()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order => order.Create(Guid.NewGuid(), "Customer1"));

        // Assert
        var act = () => test.TestThenNoEvents();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(ErrorMessages.ExpectedNoEventsButFound);
    }

    #endregion

    #region ThenState Tests

    [Fact]
    public void ThenState_ShouldValidateAggregateState()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));

        // Act
        test.TestWhen(order => order.AddItem("Product1", 5));

        // Assert
        test.TestThenState(order =>
        {
            order.CustomerId.ShouldBe("Customer1");
            order.Items.ShouldContainKey("Product1");
            order.Items["Product1"].ShouldBe(5);
        });
    }

    [Fact]
    public void ThenState_WithNullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();
        test.TestWhen(order => order.Create(Guid.NewGuid(), "Customer1"));

        // Act
        var act = () => test.TestThenState(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("stateValidator");
    }

    #endregion

    #region ThenThrows Tests

    [Fact]
    public void ThenThrows_WhenExceptionThrown_ShouldReturnException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));

        // Act
        test.TestWhen(order => order.SubmitWithValidation());

        // Assert
        var exception = test.TestThenThrows<InvalidOperationException>();
        exception.Message.ShouldContain("Cannot submit");
    }

    [Fact]
    public void ThenThrows_WhenNoExceptionThrown_ShouldThrow()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(
            new OrderCreated(Guid.NewGuid(), "Customer1"),
            new ItemAdded("Product1", 1)
        );

        // Act
        test.TestWhen(order => order.Submit());

        // Assert
        var act = () => test.TestThenThrows<InvalidOperationException>();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(ErrorMessages.NoExceptionWasThrown);
    }

    [Fact]
    public void ThenThrows_WhenWrongExceptionType_ShouldThrow()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));

        // Act
        test.TestWhen(order => order.SubmitWithValidation());

        // Assert
        var act = () => test.TestThenThrows<ArgumentException>();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(nameof(ArgumentException));
    }

    [Fact]
    public void ThenThrows_WithValidator_ShouldValidateException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGiven(new OrderCreated(Guid.NewGuid(), "Customer1"));

        // Act
        test.TestWhen(order => order.SubmitWithValidation());

        // Assert
        test.TestThenThrows<InvalidOperationException>(ex =>
        {
            ex.Message.ShouldContain("no items");
        });
    }

    #endregion

    #region GetUncommittedEvents Tests

    [Fact]
    public void GetUncommittedEvents_ShouldReturnAllEvents()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order =>
        {
            order.Create(Guid.NewGuid(), "Customer1");
            order.AddItem("Product1", 1);
            order.AddItem("Product2", 2);
        });

        // Assert
        var events = test.GetTestUncommittedEvents();
        events.Count.ShouldBe(3);
    }

    [Fact]
    public void GetUncommittedEvents_Generic_ShouldFilterByType()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        test.TestWhen(order =>
        {
            order.Create(Guid.NewGuid(), "Customer1");
            order.AddItem("Product1", 1);
            order.AddItem("Product2", 2);
        });

        // Assert
        var itemEvents = test.GetTestUncommittedEvents<ItemAdded>().ToList();
        itemEvents.Count.ShouldBe(2);
    }

    #endregion

    #region Aggregate Property Tests

    [Fact]
    public void Aggregate_BeforeWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();
        test.TestGivenEmpty();

        // Act
        var act = () => _ = test.GetTestAggregate();

        // Assert
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain(ErrorMessages.When);
    }

    #endregion

    #region Test Helper Classes

    private sealed class TestOrderAggregateTest : AggregateTestBase<TestOrderAggregate, Guid>
    {
        public void TestGiven(params object[] events) => Given(events);
        public void TestGivenEmpty() => GivenEmpty();
        public void TestWhen(Action<TestOrderAggregate> action) => When(action);
        public Task TestWhenAsync(Func<TestOrderAggregate, Task> action) => WhenAsync(action);
        public TEvent TestThen<TEvent>() where TEvent : class => Then<TEvent>();
        public TEvent TestThen<TEvent>(Action<TEvent> validator) where TEvent : class => Then(validator);
        public void TestThenEvents(params Type[] eventTypes) => ThenEvents(eventTypes);
        public void TestThenNoEvents() => ThenNoEvents();
        public void TestThenState(Action<TestOrderAggregate> validator) => ThenState(validator);
        public TException TestThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
        public TException TestThenThrows<TException>(Action<TException> validator) where TException : Exception => ThenThrows(validator);
        public IReadOnlyList<object> GetTestUncommittedEvents() => GetUncommittedEvents();
        public IEnumerable<TEvent> GetTestUncommittedEvents<TEvent>() where TEvent : class => GetUncommittedEvents<TEvent>();
        public TestOrderAggregate GetTestAggregate() => Aggregate;

        /// <summary>
        /// Materializes uncommitted events once, asserts exactly one exists, and returns it cast to T.
        /// </summary>
        public TEvent GetSingleUncommittedEvent<TEvent>() where TEvent : class
        {
            var events = GetUncommittedEvents();
            events.Count.ShouldBe(1, "Expected exactly one uncommitted event");
            return events[0].ShouldBeOfType<TEvent>();
        }

        public TestOrderAggregate ExecuteWhenAndGetAggregate(Action<TestOrderAggregate> action)
        {
            When(action);
            return Aggregate;
        }
    }

    private sealed class TestOrderAggregate : AggregateBase<Guid>
    {
        public string? CustomerId { get; private set; }
        public Dictionary<string, int> Items { get; } = [];
        public bool IsSubmitted { get; private set; }
        public DateTime? SubmittedAt { get; private set; }

        public void Create(Guid orderId, string customerId)
        {
            RaiseEvent(new OrderCreated(orderId, customerId));
        }

        public void AddItem(string productId, int quantity)
        {
            RaiseEvent(new ItemAdded(productId, quantity));
        }

        public void Submit()
        {
            RaiseEvent(new OrderSubmitted(DateTime.UtcNow));
        }

        public void SubmitWithValidation()
        {
            if (Items.Count == 0)
            {
                throw new InvalidOperationException("Cannot submit order with no items");
            }

            RaiseEvent(new OrderSubmitted(DateTime.UtcNow));
        }

        public void SubmitIdempotent()
        {
            if (IsSubmitted)
            {
                return; // Idempotent - no event raised
            }

            RaiseEvent(new OrderSubmitted(DateTime.UtcNow));
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
                    if (Items.TryGetValue(itemAdded.ProductId, out var existing))
                    {
                        Items[itemAdded.ProductId] = existing + itemAdded.Quantity;
                    }
                    else
                    {
                        Items[itemAdded.ProductId] = itemAdded.Quantity;
                    }
                    break;

                case OrderSubmitted submitted:
                    IsSubmitted = true;
                    SubmittedAt = submitted.SubmittedAt;
                    break;
            }
        }
    }

    private sealed record OrderCreated(Guid OrderId, string CustomerId);
    private sealed record ItemAdded(string ProductId, int Quantity);
    private sealed record OrderSubmitted(DateTime SubmittedAt);

    #endregion
}
