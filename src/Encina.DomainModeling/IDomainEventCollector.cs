namespace Encina.DomainModeling;

/// <summary>
/// Interface for collecting domain events from tracked aggregates.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a provider-agnostic way to collect domain events from
/// aggregate roots. It is particularly useful for non-EF Core providers (ADO.NET,
/// Dapper, MongoDB) where automatic SaveChanges interception is not available.
/// </para>
/// <para>
/// <b>Typical Usage Pattern</b>:
/// <list type="number">
/// <item><description>Repository calls <see cref="TrackAggregate"/> when saving an aggregate</description></item>
/// <item><description>After transaction commits, Unit of Work calls <see cref="CollectEvents"/> to get all events</description></item>
/// <item><description>Events are dispatched through IEncina.Publish</description></item>
/// <item><description><see cref="ClearCollectedEvents"/> is called to clean up</description></item>
/// </list>
/// </para>
/// <para>
/// For EF Core, domain events are automatically dispatched via
/// <c>DomainEventDispatcherInterceptor</c>. This interface is for manual dispatch scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your repository
/// public async Task SaveAsync(Order order, CancellationToken ct)
/// {
///     await _connection.ExecuteAsync("INSERT INTO Orders ...", order);
///     _eventCollector.TrackAggregate(order);
/// }
///
/// // In your Unit of Work or handler
/// public async Task CommitAsync(CancellationToken ct)
/// {
///     await _transaction.CommitAsync(ct);
///
///     var events = _eventCollector.CollectEvents();
///     foreach (var evt in events.OfType&lt;INotification&gt;())
///     {
///         await _encina.Publish(evt, ct);
///     }
///     _eventCollector.ClearCollectedEvents();
/// }
/// </code>
/// </example>
public interface IDomainEventCollector
{
    /// <summary>
    /// Registers an aggregate root for domain event collection.
    /// </summary>
    /// <param name="aggregate">The aggregate root to track.</param>
    /// <remarks>
    /// <para>
    /// Call this method after persisting an aggregate to ensure its domain events
    /// are collected for dispatch. The same aggregate can be tracked multiple times
    /// without causing duplicates (uses a set internally).
    /// </para>
    /// <para>
    /// Events are not collected immediately; they remain on the aggregate until
    /// <see cref="CollectEvents"/> is called.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is null.</exception>
    void TrackAggregate(IAggregateRoot aggregate);

    /// <summary>
    /// Collects all domain events from tracked aggregates.
    /// </summary>
    /// <returns>A read-only list of all domain events from tracked aggregates.</returns>
    /// <remarks>
    /// <para>
    /// This method iterates through all tracked aggregates and collects their
    /// <see cref="IAggregateRoot.DomainEvents"/>. The events are not cleared from
    /// the aggregates until <see cref="ClearCollectedEvents"/> is called.
    /// </para>
    /// <para>
    /// Events are returned in the order they were raised across all aggregates,
    /// with events from each aggregate appearing in their original order.
    /// </para>
    /// </remarks>
    IReadOnlyList<IDomainEvent> CollectEvents();

    /// <summary>
    /// Clears all collected events from tracked aggregates and stops tracking them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs two actions:
    /// <list type="number">
    /// <item><description>Calls <see cref="IAggregateRoot.ClearDomainEvents"/> on each tracked aggregate</description></item>
    /// <item><description>Clears the internal tracking collection</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Call this method after successfully dispatching all events. If dispatch fails,
    /// you may want to keep the events for retry, so don't call this method in that case.
    /// </para>
    /// </remarks>
    void ClearCollectedEvents();

    /// <summary>
    /// Gets the number of currently tracked aggregates.
    /// </summary>
    /// <remarks>
    /// Useful for diagnostics and testing to verify aggregates are being tracked correctly.
    /// </remarks>
    int TrackedAggregateCount { get; }

    /// <summary>
    /// Gets the total number of domain events across all tracked aggregates.
    /// </summary>
    /// <remarks>
    /// This is equivalent to calling <see cref="CollectEvents"/> and getting the count,
    /// but more efficient as it doesn't create a new list.
    /// </remarks>
    int TotalEventCount { get; }
}
