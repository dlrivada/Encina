using System.Collections.ObjectModel;
// Implicit usings enabled globally; explicit System.Collections.Generic is not required.
using Shouldly;

namespace Encina.Testing.Modules;

/// <summary>
/// Collects and provides assertions for integration events published during module tests.
/// </summary>
/// <remarks>
/// <para>
/// This collector captures all notifications published via <c>PublishAsync</c> and provides
/// fluent assertion methods for verifying expected integration events.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Assert specific event was published
/// fixture.IntegrationEvents.ShouldContain&lt;OrderPlacedEvent&gt;();
///
/// // Assert event with specific properties
/// fixture.IntegrationEvents.ShouldContain&lt;OrderPlacedEvent&gt;(e =&gt;
///     e.OrderId == expectedOrderId);
///
/// // Get all events of a type
/// var events = fixture.IntegrationEvents.GetEvents&lt;OrderPlacedEvent&gt;();
/// </code>
/// </example>
public sealed class IntegrationEventCollector
{
    private readonly List<INotification> _events = [];
    private readonly object _sync = new();

    /// <summary>
    /// Gets all captured events as an immutable snapshot for thread-safe enumeration.
    /// This method takes a snapshot copy of the internal list under a lock and returns
    /// a read-only wrapper over that snapshot so callers can enumerate safely without
    /// races even if the collector is modified concurrently.
    /// </summary>
    public IReadOnlyList<INotification> Events
    {
        get
        {
            lock (_sync)
            {
                // Create a snapshot copy under lock and return a read-only wrapper.
                return _events.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets the count of captured events.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _events.Count;
            }
        }
    }

    /// <summary>
    /// Adds an event to the collector.
    /// </summary>
    /// <param name="notification">The notification to capture.</param>
    internal void Add(INotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        lock (_sync)
        {
            _events.Add(notification);
        }
    }

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _events.Clear();
        }
    }

    #region Query Methods

    /// <summary>
    /// Gets all events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to retrieve.</typeparam>
    /// <returns>A list of events of the specified type.</returns>
    public IReadOnlyList<TEvent> GetEvents<TEvent>() where TEvent : INotification
    {
        lock (_sync)
        {
            return _events.OfType<TEvent>().ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the first event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to retrieve.</typeparam>
    /// <returns>The first event of the specified type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no event of the specified type was captured.
    /// </exception>
    public TEvent GetFirst<TEvent>() where TEvent : class, INotification
    {
        TEvent? result;
        lock (_sync)
        {
            result = _events.OfType<TEvent>().FirstOrDefault();
        }

        if (result is null)
        {
            throw new InvalidOperationException(
                $"No event of type {typeof(TEvent).Name} was captured.");
        }

        return result;
    }

    /// <summary>
    /// Gets the first event of the specified type, or null if not found.
    /// </summary>
    /// <typeparam name="TEvent">The event type to retrieve.</typeparam>
    /// <returns>The first event of the specified type, or null.</returns>
    public TEvent? GetFirstOrDefault<TEvent>() where TEvent : class, INotification
    {
        lock (_sync)
        {
            return _events.OfType<TEvent>().FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets the single event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to retrieve.</typeparam>
    /// <returns>The single event of the specified type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when zero or more than one event of the specified type was captured.
    /// </exception>
    public TEvent GetSingle<TEvent>() where TEvent : INotification
    {
        List<TEvent> events;
        lock (_sync)
        {
            events = _events.OfType<TEvent>().ToList();
        }
        return events.Count switch
        {
            0 => throw new InvalidOperationException(
                $"No event of type {typeof(TEvent).Name} was captured."),
            1 => events[0],
            _ => throw new InvalidOperationException(
                $"Expected exactly one event of type {typeof(TEvent).Name} but found {events.Count}.")
        };
    }

    /// <summary>
    /// Checks if an event of the specified type was captured.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check.</typeparam>
    /// <returns>True if at least one event of the type was captured.</returns>
    public bool Contains<TEvent>() where TEvent : INotification
    {
        lock (_sync)
        {
            return _events.OfType<TEvent>().Any();
        }
    }

    /// <summary>
    /// Checks if an event matching the predicate was captured.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check.</typeparam>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>True if a matching event was captured.</returns>
    public bool Contains<TEvent>(Func<TEvent, bool> predicate) where TEvent : INotification
    {
        ArgumentNullException.ThrowIfNull(predicate);
        lock (_sync)
        {
            return _events.OfType<TEvent>().Any(predicate);
        }
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Asserts that an event of the specified type was captured.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <returns>The collector for chaining.</returns>
    public IntegrationEventCollector ShouldContain<TEvent>() where TEvent : INotification
    {
        Contains<TEvent>().ShouldBeTrue(
            $"Expected integration event of type {typeof(TEvent).Name} but none was captured. " +
            $"Captured events: [{string.Join(", ", GetTypeNamesSnapshot())}]");
        return this;
    }

    /// <summary>
    /// Asserts that an event matching the predicate was captured.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The collector for chaining.</returns>
    public IntegrationEventCollector ShouldContain<TEvent>(Func<TEvent, bool> predicate)
        where TEvent : INotification
    {
        ArgumentNullException.ThrowIfNull(predicate);
        Contains(predicate).ShouldBeTrue(
            $"No integration event of type {typeof(TEvent).Name} matched the predicate. " +
            $"Found {GetEvents<TEvent>().Count} event(s) of this type.");
        return this;
    }

    /// <summary>
    /// Asserts that an event of the specified type was captured and returns it for further assertions.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <returns>An AndConstraint wrapping the event.</returns>
    public AndConstraint<TEvent> ShouldContainAnd<TEvent>() where TEvent : class, INotification
    {
        ShouldContain<TEvent>();
        return new AndConstraint<TEvent>(GetFirst<TEvent>());
    }

    /// <summary>
    /// Asserts that exactly one event of the specified type was captured.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <returns>The collector for chaining.</returns>
    public IntegrationEventCollector ShouldContainSingle<TEvent>() where TEvent : INotification
    {
        var count = GetEvents<TEvent>().Count;
        count.ShouldBe(1,
            $"Expected exactly one event of type {typeof(TEvent).Name} but found {count}.");
        return this;
    }

    /// <summary>
    /// Asserts that exactly one event of the specified type was captured and returns it.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <returns>An AndConstraint wrapping the single event.</returns>
    public AndConstraint<TEvent> ShouldContainSingleAnd<TEvent>() where TEvent : INotification
    {
        ShouldContainSingle<TEvent>();
        return new AndConstraint<TEvent>(GetSingle<TEvent>());
    }

    /// <summary>
    /// Asserts that no event of the specified type was captured.
    /// </summary>
    /// <typeparam name="TEvent">The event type that should not be present.</typeparam>
    /// <returns>The collector for chaining.</returns>
    public IntegrationEventCollector ShouldNotContain<TEvent>() where TEvent : INotification
    {
        Contains<TEvent>().ShouldBeFalse(
            $"Expected no event of type {typeof(TEvent).Name} but found {GetEvents<TEvent>().Count}.");
        return this;
    }

    /// <summary>
    /// Asserts that no events were captured.
    /// </summary>
    /// <returns>The collector for chaining.</returns>
    public IntegrationEventCollector ShouldBeEmpty()
    {
        var snapshot = GetTypeNamesSnapshot();
        lock (_sync)
        {
            var count = _events.Count;
            count.ShouldBe(0,
                $"Expected no events but found {count}: " +
                $"[{string.Join(", ", snapshot)}]");
        }
        return this;
    }

    /// <summary>
    /// Asserts that exactly the specified number of events were captured.
    /// </summary>
    /// <param name="expectedCount">The expected event count.</param>
    /// <returns>The collector for chaining.</returns>
    public IntegrationEventCollector ShouldHaveCount(int expectedCount)
    {
        lock (_sync)
        {
            _events.Count.ShouldBe(expectedCount,
                $"Expected {expectedCount} event(s) but found {_events.Count}.");
        }
        return this;
    }

    /// <summary>
    /// Asserts that exactly the specified number of events of a type were captured.
    /// </summary>
    /// <typeparam name="TEvent">The event type to count.</typeparam>
    /// <param name="expectedCount">The expected count.</param>
    /// <returns>The collector for chaining.</returns>
    public IntegrationEventCollector ShouldHaveCount<TEvent>(int expectedCount) where TEvent : INotification
    {
        var count = GetEvents<TEvent>().Count;
        count.ShouldBe(expectedCount,
            $"Expected {expectedCount} event(s) of type {typeof(TEvent).Name} but found {count}.");
        return this;
    }

    #endregion

    private ReadOnlyCollection<string> GetTypeNamesSnapshot()
    {
        lock (_sync)
        {
            return _events.Select(e => e.GetType().Name).ToList().AsReadOnly();
        }
    }
}
