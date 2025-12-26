namespace Encina.Marten.Snapshots;

/// <summary>
/// Factory for creating snapshot envelopes.
/// </summary>
public static class SnapshotEnvelope
{
    /// <summary>
    /// Creates a snapshot envelope from a snapshot.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="snapshot">The snapshot to wrap.</param>
    /// <returns>The envelope.</returns>
    public static SnapshotEnvelope<TAggregate> Create<TAggregate>(Snapshot<TAggregate> snapshot)
        where TAggregate : class, IAggregate
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new SnapshotEnvelope<TAggregate>
        {
            Id = CreateId<TAggregate>(snapshot.AggregateId, snapshot.Version),
            AggregateId = snapshot.AggregateId,
            Version = snapshot.Version,
            State = snapshot.State,
            CreatedAtUtc = snapshot.CreatedAtUtc,
            AggregateType = typeof(TAggregate).FullName ?? typeof(TAggregate).Name
        };
    }

    /// <summary>
    /// Creates a composite document ID.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="version">The version.</param>
    /// <returns>The composite ID.</returns>
    internal static string CreateId<TAggregate>(Guid aggregateId, int version) =>
        $"{typeof(TAggregate).Name}:{aggregateId}:{version}";
}

/// <summary>
/// Document envelope for storing snapshots in Marten.
/// This is the persisted document that wraps the aggregate state.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public sealed class SnapshotEnvelope<TAggregate>
    where TAggregate : class, IAggregate
{
    /// <summary>
    /// Gets or sets the unique document identifier.
    /// Composite key of AggregateId and Version.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the aggregate identifier.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the version of the aggregate when the snapshot was taken.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the aggregate state.
    /// </summary>
    public TAggregate? State { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the snapshot was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the aggregate.
    /// Used for diagnostics and debugging.
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Converts the envelope back to a snapshot.
    /// </summary>
    /// <returns>The snapshot.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the state is null.</exception>
    public Snapshot<TAggregate> ToSnapshot()
    {
        if (State is null)
        {
            throw new InvalidOperationException(
                $"Cannot convert envelope to snapshot: State is null for aggregate {AggregateId} at version {Version}.");
        }

        return new Snapshot<TAggregate>(AggregateId, Version, State, CreatedAtUtc);
    }
}
