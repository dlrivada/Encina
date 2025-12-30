using Encina.DomainModeling;
using Encina.Testing.EventSourcing;
using FluentAssertions;

namespace Encina.Testing.Tests.EventSourcing;

public class AggregateTestBaseTests
{
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
        test.ExecuteWhenAndGetAggregate(order => { })
            .Should().Match<TestOrderAggregate>(o =>
                o.CustomerId == "Customer1" &&
                o.Items.Count == 1 &&
                o.Items["Product1"] == 2);
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
        aggregate.UncommittedEvents.Should().BeEmpty();
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
        aggregate.Version.Should().Be(3);
    }

    [Fact]
    public void Given_WithNullEvents_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        var act = () => test.TestGiven(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("events");
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
        aggregate.Version.Should().Be(0);
        aggregate.UncommittedEvents.Should().BeEmpty();
        aggregate.CustomerId.Should().BeNull();
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
        test.GetTestUncommittedEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreated>();
    }

    [Fact]
    public void When_WithoutGiven_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new TestOrderAggregateTest();

        // Act
        var act = () => test.TestWhen(order => order.Submit());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Given()*");
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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
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
        test.TestThenThrows<InvalidOperationException>();
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
            await Task.Delay(1); // Simulate async work
            order.Submit();
        });

        // Assert
        test.GetTestUncommittedEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderSubmitted>();
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
            await Task.Delay(1);
            throw new InvalidOperationException("Async error");
        });

        // Assert
        test.TestThenThrows<InvalidOperationException>(ex =>
            ex.Message.Should().Contain("Async error"));
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
        @event.OrderId.Should().Be(orderId);
        @event.CustomerId.Should().Be("Customer1");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OrderSubmitted*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
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
            @event.CustomerId.Should().Be("ValidatedCustomer");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exception was thrown*");
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
        test.TestThenEvents(typeof(OrderCreated), typeof(ItemAdded));
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*position 0*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Expected 2 events but got 1*");
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
        test.TestThenNoEvents();
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Expected no events but found*");
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
            order.CustomerId.Should().Be("Customer1");
            order.Items.Should().ContainKey("Product1");
            order.Items["Product1"].Should().Be(5);
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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stateValidator");
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
        exception.Message.Should().Contain("Cannot submit");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no exception was thrown*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ArgumentException*InvalidOperationException*");
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
            ex.Message.Should().Contain("no items");
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
        events.Should().HaveCount(3);
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
        var itemEvents = test.GetTestUncommittedEvents<ItemAdded>();
        itemEvents.Should().HaveCount(2);
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
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
