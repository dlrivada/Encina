using System.Collections.Concurrent;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// In-memory implementation of <see cref="IReferenceTableStateStore"/> backed by a
/// concurrent dictionary.
/// </summary>
/// <remarks>
/// <para>
/// Suitable for single-instance deployments. Hashes are lost on application restart,
/// which causes a full replication on the first polling cycle after startup. For
/// distributed deployments, use a durable implementation (e.g., database-backed).
/// </para>
/// </remarks>
internal sealed class InMemoryReferenceTableStateStore : IReferenceTableStateStore
{
    private readonly ConcurrentDictionary<Type, string> _hashes = new();
    private readonly ConcurrentDictionary<Type, DateTime> _replicationTimes = new();

    /// <inheritdoc />
    public Task<string?> GetLastHashAsync(Type entityType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        var hash = _hashes.TryGetValue(entityType, out var stored) ? stored : null;
        return Task.FromResult(hash);
    }

    /// <inheritdoc />
    public Task SaveHashAsync(Type entityType, string hash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(hash);
        _hashes[entityType] = hash;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DateTime?> GetLastReplicationTimeAsync(Type entityType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        DateTime? time = _replicationTimes.TryGetValue(entityType, out var stored) ? stored : null;
        return Task.FromResult(time);
    }

    /// <inheritdoc />
    public Task SaveReplicationTimeAsync(Type entityType, DateTime timeUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        _replicationTimes[entityType] = timeUtc;
        return Task.CompletedTask;
    }
}
