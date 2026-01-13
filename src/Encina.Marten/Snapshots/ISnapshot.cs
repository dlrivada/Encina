using Encina.DomainModeling;

namespace Encina.Marten.Snapshots;

/// <summary>
/// Represents a snapshot of an aggregate's state at a specific version.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public interface ISnapshot<TAggregate> // NOSONAR S2326: TAggregate provides type-safe constraint for aggregate snapshots
    where TAggregate : class, IAggregate
{
    /// <summary>
    /// Gets the aggregate identifier.
    /// </summary>
    Guid AggregateId { get; }

    /// <summary>
    /// Gets the version of the aggregate when the snapshot was taken.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the UTC timestamp when the snapshot was created.
    /// </summary>
    DateTime CreatedAtUtc { get; }
}

/// <summary>
/// A snapshot envelope containing the aggregate state and metadata.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public sealed class Snapshot<TAggregate> : ISnapshot<TAggregate>
    where TAggregate : class, IAggregate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Snapshot{TAggregate}"/> class.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="version">The version at snapshot time.</param>
    /// <param name="state">The serialized aggregate state.</param>
    /// <param name="createdAtUtc">The creation timestamp in UTC.</param>
    public Snapshot(Guid aggregateId, int version, TAggregate state, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(state);

        AggregateId = aggregateId;
        Version = version;
        State = state;
        CreatedAtUtc = createdAtUtc;
    }

    /// <inheritdoc />
    public Guid AggregateId { get; }

    /// <inheritdoc />
    public int Version { get; }

    /// <summary>
    /// Gets the aggregate state at the time of the snapshot.
    /// </summary>
    public TAggregate State { get; }

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; }
}
