using Encina.DomainModeling;
using Shouldly;

namespace Encina.Testing.Shouldly;

/// <summary>
/// Shouldly assertion extensions for aggregate testing in event-sourced systems.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient assertions for testing aggregates that implement
/// <see cref="IAggregate"/>. They help verify that the correct domain events have been raised
/// by aggregate operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Fact]
/// public void CreateOrder_ShouldRaiseOrderCreatedEvent()
/// {
///     var order = new Order();
///     order.Create("CUST-001", DateTime.UtcNow);
///
///     var evt = order.ShouldHaveRaisedEvent&lt;OrderCreatedEvent&gt;();
///     evt.CustomerId.ShouldBe("CUST-001");
/// }
/// </code>
/// </example>
public static class AggregateShouldlyExtensions
{
    /// <summary>
    /// Asserts that the aggregate has raised an event of the specified type and returns it.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The first event of the specified type for further assertions.</returns>
    /// <exception cref="ShouldAssertException">Thrown when no event of the specified type was raised.</exception>
    public static TEvent ShouldHaveRaisedEvent<TEvent>(
        this IAggregate aggregate,
        string? customMessage = null)
        where TEvent : class
    {
        var events = aggregate.UncommittedEvents.OfType<TEvent>().ToList();

        if (events.Count == 0)
        {
            var raisedTypes = aggregate.UncommittedEvents.Select(e => e.GetType().Name).Distinct().ToList();
            var raisedTypesMessage = raisedTypes.Count > 0
                ? $"Raised events: {string.Join(", ", raisedTypes)}"
                : "No events were raised";

            throw new ShouldAssertException(
                customMessage ?? $"Expected event of type {typeof(TEvent).Name} to be raised but it was not. {raisedTypesMessage}");
        }

        return events[0];
    }

    /// <summary>
    /// Asserts that the aggregate has raised exactly the specified number of events of the given type.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedCount">The expected number of events.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The events of the specified type for further assertions.</returns>
    public static IReadOnlyList<TEvent> ShouldHaveRaisedEvents<TEvent>(
        this IAggregate aggregate,
        int expectedCount,
        string? customMessage = null)
        where TEvent : class
    {
        var events = aggregate.UncommittedEvents.OfType<TEvent>().ToList();

        events.Count.ShouldBe(expectedCount,
            customMessage ?? $"Expected {expectedCount} event(s) of type {typeof(TEvent).Name} but found {events.Count}");

        return events;
    }

    /// <summary>
    /// Asserts that the aggregate has raised an event of the specified type that matches the predicate.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="predicate">A predicate to match the event.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The matching event for further assertions.</returns>
    public static TEvent ShouldHaveRaisedEvent<TEvent>(
        this IAggregate aggregate,
        Func<TEvent, bool> predicate,
        string? customMessage = null)
        where TEvent : class
    {
        var events = aggregate.UncommittedEvents.OfType<TEvent>().Where(predicate).ToList();

        if (events.Count == 0)
        {
            var allEventsOfType = aggregate.UncommittedEvents.OfType<TEvent>().ToList();

            var defaultMessage = allEventsOfType.Count == 0
                ? $"Expected event of type {typeof(TEvent).Name} matching predicate but no events of that type were found"
                : $"Expected event of type {typeof(TEvent).Name} matching predicate but found {allEventsOfType.Count} event(s) that did not match";

            throw new ShouldAssertException(customMessage ?? defaultMessage);
        }

        return events[0];
    }

    /// <summary>
    /// Asserts that the aggregate has not raised any event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type that should not have been raised.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    public static void ShouldNotHaveRaisedEvent<TEvent>(
        this IAggregate aggregate,
        string? customMessage = null)
        where TEvent : class
    {
        var events = aggregate.UncommittedEvents.OfType<TEvent>().ToList();

        if (events.Count > 0)
        {
            throw new ShouldAssertException(
                customMessage ?? $"Expected no event of type {typeof(TEvent).Name} but found {events.Count}");
        }
    }

    /// <summary>
    /// Asserts that the aggregate has no uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    public static void ShouldHaveNoUncommittedEvents(
        this IAggregate aggregate,
        string? customMessage = null)
    {
        var count = aggregate.UncommittedEvents.Count;

        if (count > 0)
        {
            var eventTypes = aggregate.UncommittedEvents.Select(e => e.GetType().Name).Distinct().ToList();

            throw new ShouldAssertException(
                customMessage ?? $"Expected no uncommitted events but found {count}. Types: {string.Join(", ", eventTypes)}");
        }
    }

    /// <summary>
    /// Asserts that the aggregate has exactly the specified number of uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedCount">The expected number of uncommitted events.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The uncommitted events for further assertions.</returns>
    public static IReadOnlyList<object> ShouldHaveUncommittedEventCount(
        this IAggregate aggregate,
        int expectedCount,
        string? customMessage = null)
    {
        var events = aggregate.UncommittedEvents;

        events.Count.ShouldBe(expectedCount,
            customMessage ?? $"Expected {expectedCount} uncommitted event(s) but found {events.Count}");

        return events;
    }

    /// <summary>
    /// Asserts that the aggregate has the specified version.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedVersion">The expected version number.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    public static void ShouldHaveVersion(
        this IAggregate aggregate,
        int expectedVersion,
        string? customMessage = null)
    {
        aggregate.Version.ShouldBe(expectedVersion,
            customMessage ?? $"Expected version {expectedVersion} but found {aggregate.Version}");
    }

    /// <summary>
    /// Asserts that the aggregate's ID matches the expected value.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedId">The expected ID value.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    public static void ShouldHaveId(
        this IAggregate aggregate,
        Guid expectedId,
        string? customMessage = null)
    {
        aggregate.Id.ShouldBe(expectedId,
            customMessage ?? $"Expected ID {expectedId} but found {aggregate.Id}");
    }

    /// <summary>
    /// Asserts that the strongly-typed aggregate's ID matches the expected value.
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedId">The expected ID value.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    public static void ShouldHaveId<TId>(
        this IAggregate<TId> aggregate,
        TId expectedId,
        string? customMessage = null)
        where TId : notnull
    {
        aggregate.Id.ShouldBe(expectedId,
            customMessage ?? $"Expected ID {expectedId} but found {aggregate.Id}");
    }

    /// <summary>
    /// Gets all raised events of the specified type for further assertions.
    /// </summary>
    /// <typeparam name="TEvent">The event type to filter.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <returns>A collection of events of the specified type.</returns>
    public static IReadOnlyList<TEvent> GetRaisedEvents<TEvent>(this IAggregate aggregate)
        where TEvent : class
    {
        return aggregate.UncommittedEvents.OfType<TEvent>().ToList();
    }

    /// <summary>
    /// Gets the last raised event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to find.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <returns>The last event of the specified type, or null if none found.</returns>
    public static TEvent? GetLastRaisedEvent<TEvent>(this IAggregate aggregate)
        where TEvent : class
    {
        return aggregate.UncommittedEvents.OfType<TEvent>().LastOrDefault();
    }
}
