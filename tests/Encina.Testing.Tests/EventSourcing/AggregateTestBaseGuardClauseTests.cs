using Encina.Marten;
using Encina.Testing.EventSourcing;
using FluentAssertions;

namespace Encina.Testing.Tests.EventSourcing;

/// <summary>
/// Guard clause tests for <see cref="AggregateTestBase{TAggregate,TId}"/>.
/// Verifies that all public methods properly validate their arguments.
/// </summary>
public sealed class AggregateTestBaseGuardClauseTests
{
    #region Given Guard Clauses

    [Fact]
    public void Given_WithNullEvents_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();

        // Act
        var act = () => test.TestGiven(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("events");
    }

    [Fact]
    public void Given_WithEmptyEvents_ShouldNotThrow()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();

        // Act
        var act = () => test.TestGiven([]);

        // Assert - Empty array is valid (creates aggregate with no history)
        act.Should().NotThrow();
    }

    #endregion

    #region When Guard Clauses

    [Fact]
    public void When_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestWhen(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("action");
    }

    [Fact]
    public void When_WithoutPriorGiven_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();

        // Act
        var act = () => test.TestWhen(order => { });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Given()*GivenEmpty()*");
    }

    #endregion

    #region WhenAsync Guard Clauses

    [Fact]
    public async Task WhenAsync_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = async () => await test.TestWhenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public async Task WhenAsync_WithoutPriorGiven_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();

        // Act
        var act = async () => await test.TestWhenAsync(async order => await Task.CompletedTask);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Given()*");
    }

    #endregion

    #region Then Guard Clauses

    [Fact]
    public void Then_WithoutPriorWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestThen<OrderCreated>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
    }

    [Fact]
    public void Then_WithValidator_NullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();
        test.TestWhen(order => order.Create(Guid.NewGuid(), "Customer"));

        // Act
        var act = () => test.TestThen<OrderCreated>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("validator");
    }

    #endregion

    #region ThenEvents Guard Clauses

    [Fact]
    public void ThenEvents_WithNullEventTypes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();
        test.TestWhen(order => order.Create(Guid.NewGuid(), "Customer"));

        // Act
        var act = () => test.TestThenEvents(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("expectedEventTypes");
    }

    [Fact]
    public void ThenEvents_WithoutPriorWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestThenEvents(typeof(OrderCreated));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
    }

    #endregion

    #region ThenNoEvents Guard Clauses

    [Fact]
    public void ThenNoEvents_WithoutPriorWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestThenNoEvents();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
    }

    #endregion

    #region ThenState Guard Clauses

    [Fact]
    public void ThenState_WithNullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();
        test.TestWhen(order => { });

        // Act
        var act = () => test.TestThenState(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("stateValidator");
    }

    [Fact]
    public void ThenState_WithoutPriorWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestThenState(order => { });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
    }

    #endregion

    #region ThenThrows Guard Clauses

    [Fact]
    public void ThenThrows_WithoutPriorWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => test.TestThenThrows<Exception>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
    }

    [Fact]
    public void ThenThrows_WhenNoExceptionWasThrown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();
        test.TestWhen(order => { }); // No exception

        // Act
        var act = () => test.TestThenThrows<Exception>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no exception was thrown*");
    }

    [Fact]
    public void ThenThrows_WithValidator_NullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();
        test.TestWhen(order => throw new InvalidOperationException());

        // Act
        var act = () => test.TestThenThrows<InvalidOperationException>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("validator");
    }

    #endregion

    #region GetUncommittedEvents Guard Clauses

    [Fact]
    public void GetUncommittedEvents_WithoutPriorWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => test.GetTestUncommittedEvents();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
    }

    #endregion

    #region Aggregate Property Guard Clauses

    [Fact]
    public void Aggregate_WithoutPriorWhen_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var test = new GuardTestOrderAggregate();
        test.TestGivenEmpty();

        // Act
        var act = () => _ = test.GetTestAggregate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*When()*");
    }

    #endregion

    #region Test Helper Classes

    private sealed class GuardTestOrderAggregate : AggregateTestBase<TestAggregate, Guid>
    {
        public void TestGiven(params object[] events) => Given(events);
        public void TestGivenEmpty() => GivenEmpty();
        public void TestWhen(Action<TestAggregate> action) => When(action);
        public Task TestWhenAsync(Func<TestAggregate, Task> action) => WhenAsync(action);
        public TEvent TestThen<TEvent>() where TEvent : class => Then<TEvent>();
        public TEvent TestThen<TEvent>(Action<TEvent> validator) where TEvent : class => Then(validator);
        public void TestThenEvents(params Type[] eventTypes) => ThenEvents(eventTypes);
        public void TestThenNoEvents() => ThenNoEvents();
        public void TestThenState(Action<TestAggregate> validator) => ThenState(validator);
        public TException TestThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
        public TException TestThenThrows<TException>(Action<TException> validator) where TException : Exception => ThenThrows(validator);
        public IReadOnlyList<object> GetTestUncommittedEvents() => GetUncommittedEvents();
        public TestAggregate GetTestAggregate() => Aggregate;
    }

    private sealed class TestAggregate : AggregateBase<Guid>
    {
        public string? CustomerId { get; private set; }

        public void Create(Guid orderId, string customerId)
        {
            RaiseEvent(new OrderCreated(orderId, customerId));
        }

        protected override void Apply(object domainEvent)
        {
            if (domainEvent is OrderCreated created)
            {
                Id = created.OrderId;
                CustomerId = created.CustomerId;
            }
        }
    }

    private sealed record OrderCreated(Guid OrderId, string CustomerId);

    #endregion
}
