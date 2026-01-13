using System.Diagnostics.CodeAnalysis;
using Encina.DomainModeling;

namespace Encina.Testing.EventSourcing;

/// <summary>
/// Base class for testing event-sourced aggregates using the Given/When/Then pattern.
/// Provides a fluent API for setting up event history, executing commands, and verifying outcomes.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate being tested.</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
/// <remarks>
/// <para>
/// This class provides a structured approach to testing event-sourced aggregates:
/// </para>
/// <list type="bullet">
/// <item><description><b>Given</b>: Set up the aggregate's event history</description></item>
/// <item><description><b>When</b>: Execute a command or action on the aggregate</description></item>
/// <item><description><b>Then</b>: Verify the resulting events, state, or errors</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class OrderAggregateTests : AggregateTestBase&lt;Order, OrderId&gt;
/// {
///     [Fact]
///     public void Submit_WhenOrderCreated_ShouldProduceOrderSubmittedEvent()
///     {
///         Given(
///             new OrderCreated(OrderId, CustomerId, Items),
///             new PaymentReceived(OrderId, Amount)
///         );
///
///         When(order => order.Submit());
///
///         Then&lt;OrderSubmitted&gt;(e =>
///         {
///             Assert.Equal(OrderId, e.OrderId);
///         });
///     }
/// }
/// </code>
/// </example>
public abstract class AggregateTestBase<TAggregate, TId>
    where TAggregate : AggregateBase<TId>, new()
    where TId : notnull
{
    private TAggregate? _aggregate;
    private Exception? _caughtException;
    private bool _whenExecuted;

    /// <summary>
    /// Gets the aggregate under test after <see cref="When"/> has been called.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before calling <see cref="When"/>.</exception>
    protected TAggregate Aggregate
    {
        get
        {
            EnsureWhenExecuted();
            return _aggregate!;
        }
    }

    /// <summary>
    /// Sets up the aggregate's event history by applying the given events.
    /// This simulates loading an aggregate from an event store.
    /// </summary>
    /// <param name="events">The events that represent the aggregate's history.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events"/> is null.</exception>
    /// <example>
    /// <code>
    /// Given(
    ///     new OrderCreated(orderId, customerId),
    ///     new ItemAdded(orderId, productId, quantity)
    /// );
    /// </code>
    /// </example>
    protected void Given(params object[] events)
    {
        ArgumentNullException.ThrowIfNull(events);

        _aggregate = CreateAggregate();
        _caughtException = null;
        _whenExecuted = false;

        foreach (var @event in events)
        {
            ApplyEventToAggregate(_aggregate, @event);
        }

        // Clear uncommitted events since these are "historical" events
        _aggregate.ClearUncommittedEvents();
    }

    /// <summary>
    /// Sets up an empty aggregate with no event history.
    /// Use this for testing aggregate creation scenarios.
    /// </summary>
    /// <example>
    /// <code>
    /// GivenEmpty();
    /// When(order => order.Create(orderId, customerId));
    /// Then&lt;OrderCreated&gt;();
    /// </code>
    /// </example>
    protected void GivenEmpty()
    {
        _aggregate = CreateAggregate();
        _caughtException = null;
        _whenExecuted = false;
    }

    /// <summary>
    /// Executes an action on the aggregate. The action typically represents a domain command.
    /// </summary>
    /// <param name="action">The action to execute on the aggregate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Given"/> or <see cref="GivenEmpty"/> was not called first.</exception>
    /// <example>
    /// <code>
    /// When(order => order.Submit());
    /// </code>
    /// </example>
    protected void When(Action<TAggregate> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureGivenCalled();

        try
        {
            action(_aggregate!);
            _caughtException = null;
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }

        _whenExecuted = true;
    }

    /// <summary>
    /// Executes an async action on the aggregate. The action typically represents a domain command.
    /// </summary>
    /// <param name="action">The async action to execute on the aggregate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Given"/> or <see cref="GivenEmpty"/> was not called first.</exception>
    /// <example>
    /// <code>
    /// await WhenAsync(async order => await order.ProcessAsync());
    /// </code>
    /// </example>
    protected async Task WhenAsync(Func<TAggregate, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureGivenCalled();

        try
        {
            await action(_aggregate!);
            _caughtException = null;
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }

        _whenExecuted = true;
    }

    /// <summary>
    /// Asserts that a specific event type was raised by the aggregate.
    /// </summary>
    /// <typeparam name="TEvent">The type of event expected.</typeparam>
    /// <returns>The event that was raised, for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="When"/> was not called first.</exception>
    /// <example>
    /// <code>
    /// var @event = Then&lt;OrderSubmitted&gt;();
    /// Assert.Equal(expectedOrderId, @event.OrderId);
    /// </code>
    /// </example>
    protected TEvent Then<TEvent>() where TEvent : class
    {
        EnsureWhenExecuted();
        EnsureNoException();

        var events = _aggregate!.UncommittedEvents;
        var matchingEvent = events.OfType<TEvent>().FirstOrDefault();

        if (matchingEvent is null)
        {
            var eventTypes = events.Select(e => e.GetType().Name);
            throw new InvalidOperationException(
                $"Expected event of type {typeof(TEvent).Name} but found: [{string.Join(", ", eventTypes)}]");
        }

        return matchingEvent;
    }

    /// <summary>
    /// Asserts that a specific event type was raised and validates it.
    /// </summary>
    /// <typeparam name="TEvent">The type of event expected.</typeparam>
    /// <param name="validator">Action to validate the event.</param>
    /// <returns>The event that was raised.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validator"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="When"/> was not called first.</exception>
    /// <example>
    /// <code>
    /// Then&lt;OrderSubmitted&gt;(e =>
    /// {
    ///     Assert.Equal(OrderId, e.OrderId);
    ///     Assert.True(e.SubmittedAt &lt;= DateTime.UtcNow);
    /// });
    /// </code>
    /// </example>
    protected TEvent Then<TEvent>(Action<TEvent> validator) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(validator);

        var @event = Then<TEvent>();
        validator(@event);
        return @event;
    }

    /// <summary>
    /// Asserts that the specified events were raised in order.
    /// </summary>
    /// <param name="expectedEventTypes">The expected event types in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedEventTypes"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="When"/> was not called first.</exception>
    /// <example>
    /// <code>
    /// ThenEvents(typeof(OrderSubmitted), typeof(InventoryReserved));
    /// </code>
    /// </example>
    protected void ThenEvents(params Type[] expectedEventTypes)
    {
        ArgumentNullException.ThrowIfNull(expectedEventTypes);
        EnsureWhenExecuted();
        EnsureNoException();

        var actualEvents = _aggregate!.UncommittedEvents.ToList();
        var actualTypes = actualEvents.Select(e => e.GetType()).ToList();

        if (actualTypes.Count != expectedEventTypes.Length)
        {
            throw new InvalidOperationException(
                $"Expected {expectedEventTypes.Length} events but got {actualTypes.Count}. " +
                $"Expected: [{string.Join(", ", expectedEventTypes.Select(t => t.Name))}]. " +
                $"Actual: [{string.Join(", ", actualTypes.Select(t => t.Name))}]");
        }

        for (var i = 0; i < expectedEventTypes.Length; i++)
        {
            if (actualTypes[i] != expectedEventTypes[i])
            {
                throw new InvalidOperationException(
                    $"Event at position {i} was {actualTypes[i].Name} but expected {expectedEventTypes[i].Name}");
            }
        }
    }

    /// <summary>
    /// Asserts that no events were raised by the aggregate.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="When"/> was not called first or events were raised.</exception>
    /// <example>
    /// <code>
    /// // Testing idempotency
    /// Given(new OrderSubmitted(orderId));
    /// When(order => order.Submit()); // Submitting again
    /// ThenNoEvents(); // Should not raise duplicate event
    /// </code>
    /// </example>
    protected void ThenNoEvents()
    {
        EnsureWhenExecuted();
        EnsureNoException();

        var events = _aggregate!.UncommittedEvents;
        if (events.Count > 0)
        {
            var eventTypes = events.Select(e => e.GetType().Name);
            throw new InvalidOperationException(
                $"Expected no events but found: [{string.Join(", ", eventTypes)}]");
        }
    }

    /// <summary>
    /// Asserts the aggregate's state after the command execution.
    /// </summary>
    /// <param name="stateValidator">Action to validate the aggregate state.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stateValidator"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="When"/> was not called first.</exception>
    /// <example>
    /// <code>
    /// ThenState(order =>
    /// {
    ///     Assert.Equal(OrderStatus.Submitted, order.Status);
    ///     Assert.NotNull(order.SubmittedAt);
    /// });
    /// </code>
    /// </example>
    protected void ThenState(Action<TAggregate> stateValidator)
    {
        ArgumentNullException.ThrowIfNull(stateValidator);
        EnsureWhenExecuted();
        EnsureNoException();

        stateValidator(_aggregate!);
    }

    /// <summary>
    /// Asserts that an exception of the specified type was thrown during <see cref="When"/>.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>The exception that was thrown.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="When"/> was not called first or no exception was thrown.</exception>
    /// <example>
    /// <code>
    /// Given(new OrderShipped(orderId));
    /// When(order => order.Cancel());
    /// ThenThrows&lt;InvalidOperationException&gt;();
    /// </code>
    /// </example>
    protected TException ThenThrows<TException>() where TException : Exception
    {
        EnsureWhenExecuted();

        if (_caughtException is null)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but no exception was thrown");
        }

        if (_caughtException is not TException typedException)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but got {_caughtException.GetType().Name}: {_caughtException.Message}");
        }

        return typedException;
    }

    /// <summary>
    /// Asserts that an exception of the specified type was thrown and validates it.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="validator">Action to validate the exception.</param>
    /// <returns>The exception that was thrown.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validator"/> is null.</exception>
    /// <example>
    /// <code>
    /// Given(new OrderShipped(orderId));
    /// When(order => order.Cancel());
    /// ThenThrows&lt;InvalidOperationException&gt;(ex =>
    /// {
    ///     Assert.Contains("Cannot cancel shipped order", ex.Message);
    /// });
    /// </code>
    /// </example>
    protected TException ThenThrows<TException>(Action<TException> validator) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(validator);

        var exception = ThenThrows<TException>();
        validator(exception);
        return exception;
    }

    /// <summary>
    /// Gets the uncommitted events from the aggregate after <see cref="When"/> execution.
    /// </summary>
    /// <returns>A read-only list of uncommitted events.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="When"/> was not called first.</exception>
    protected IReadOnlyList<object> GetUncommittedEvents()
    {
        EnsureWhenExecuted();
        EnsureNoException();

        return _aggregate!.UncommittedEvents;
    }

    /// <summary>
    /// Gets all uncommitted events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of events to retrieve.</typeparam>
    /// <returns>A collection of events of the specified type.</returns>
    protected IEnumerable<TEvent> GetUncommittedEvents<TEvent>() where TEvent : class
    {
        return GetUncommittedEvents().OfType<TEvent>();
    }

    /// <summary>
    /// Creates a new instance of the aggregate.
    /// Override this method to provide custom aggregate creation logic.
    /// </summary>
    /// <returns>A new aggregate instance.</returns>
    protected virtual TAggregate CreateAggregate()
    {
        return new TAggregate();
    }

    /// <summary>
    /// Applies an event to the aggregate to build up its state.
    /// This uses reflection to call the protected Apply method.
    /// </summary>
    /// <param name="aggregate">The aggregate to apply the event to.</param>
    /// <param name="event">The event to apply.</param>
    [SuppressMessage("SonarAnalyzer.CSharp", "S3011:Reflection should not be used to increase accessibility",
        Justification = "Test helper must invoke protected Apply method to simulate event replay in Given/When/Then pattern")]
    private static void ApplyEventToAggregate(TAggregate aggregate, object @event)
    {
        // Use reflection to call the protected Apply method
        var applyMethod = typeof(AggregateBase).GetMethod(
            "Apply",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (applyMethod is null)
        {
            throw new InvalidOperationException(
                $"Could not find Apply method on {typeof(TAggregate).Name}. " +
                "Ensure the aggregate inherits from AggregateBase.");
        }

        applyMethod.Invoke(aggregate, [@event]);

        // Increment version to simulate event stream replay
        var versionProperty = typeof(AggregateBase).GetProperty(
            "Version",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (versionProperty?.CanWrite is true)
        {
            var currentVersion = (int)versionProperty.GetValue(aggregate)!;
            versionProperty.SetValue(aggregate, currentVersion + 1);
        }
    }

    private void EnsureGivenCalled()
    {
        if (_aggregate is null)
        {
            throw new InvalidOperationException(
                "Given() or GivenEmpty() must be called before When()");
        }
    }

    private void EnsureWhenExecuted()
    {
        if (!_whenExecuted)
        {
            throw new InvalidOperationException(
                "When() must be called before Then assertions");
        }
    }

    private void EnsureNoException()
    {
        if (_caughtException is not null)
        {
            throw new InvalidOperationException(
                $"An exception was thrown during When(): {_caughtException.GetType().Name}: {_caughtException.Message}. " +
                "Use ThenThrows<T>() to assert on exceptions.",
                _caughtException);
        }
    }
}

/// <summary>
/// Base class for testing event-sourced aggregates with Guid identifiers.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate being tested.</typeparam>
/// <remarks>
/// This is a convenience class for aggregates that use <see cref="Guid"/> as their identifier type.
/// </remarks>
public abstract class AggregateTestBase<TAggregate> : AggregateTestBase<TAggregate, Guid>
    where TAggregate : AggregateBase<Guid>, new()
{
}
