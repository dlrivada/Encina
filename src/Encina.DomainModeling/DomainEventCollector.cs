namespace Encina.DomainModeling;

/// <summary>
/// Default implementation of <see cref="IDomainEventCollector"/> for collecting
/// domain events from aggregate roots.
/// </summary>
/// <remarks>
/// <para>
/// This class maintains a set of tracked aggregates and provides methods to collect
/// and clear their domain events. It is designed to be used as a scoped service,
/// where each unit of work or request has its own instance.
/// </para>
/// <para>
/// <b>Thread Safety</b>: This implementation is NOT thread-safe. It is designed to be
/// used within a single request/scope where operations are sequential. If you need
/// thread-safe collection, consider using concurrent collections or synchronization.
/// </para>
/// <para>
/// <b>Memory Management</b>: Aggregates are held by reference in the tracking set.
/// Ensure <see cref="ClearCollectedEvents"/> is called at the end of each unit of work
/// to release references and prevent memory leaks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register as scoped service
/// services.AddScoped&lt;IDomainEventCollector, DomainEventCollector&gt;();
///
/// // Use in repository
/// public class OrderRepository
/// {
///     private readonly IDomainEventCollector _collector;
///
///     public async Task SaveAsync(Order order, CancellationToken ct)
///     {
///         // ... save to database ...
///         _collector.TrackAggregate(order);
///     }
/// }
///
/// // Use in Unit of Work
/// public class DapperUnitOfWork
/// {
///     private readonly IDomainEventCollector _collector;
///     private readonly IEncina _encina;
///
///     public async Task CommitAsync(CancellationToken ct)
///     {
///         await _transaction.CommitAsync(ct);
///
///         // Dispatch events after successful commit
///         foreach (var evt in _collector.CollectEvents().OfType&lt;INotification&gt;())
///         {
///             await _encina.Publish(evt, ct);
///         }
///         _collector.ClearCollectedEvents();
///     }
/// }
/// </code>
/// </example>
public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly HashSet<IAggregateRoot> _trackedAggregates = [];

    /// <inheritdoc/>
    public int TrackedAggregateCount => _trackedAggregates.Count;

    /// <inheritdoc/>
    public int TotalEventCount
    {
        get
        {
            var count = 0;
            foreach (var aggregate in _trackedAggregates)
            {
                count += aggregate.DomainEvents.Count;
            }
            return count;
        }
    }

    /// <inheritdoc/>
    public void TrackAggregate(IAggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _trackedAggregates.Add(aggregate);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IDomainEvent> CollectEvents()
    {
        if (_trackedAggregates.Count == 0)
        {
            return [];
        }

        var allEvents = new List<IDomainEvent>();

        foreach (var aggregate in _trackedAggregates)
        {
            var events = aggregate.DomainEvents;
            if (events.Count > 0)
            {
                allEvents.AddRange(events);
            }
        }

        return allEvents;
    }

    /// <inheritdoc/>
    public void ClearCollectedEvents()
    {
        foreach (var aggregate in _trackedAggregates)
        {
            aggregate.ClearDomainEvents();
        }

        _trackedAggregates.Clear();
    }
}
