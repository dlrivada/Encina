using System.Collections.Concurrent;
using Encina.Cdc.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Processing;

/// <summary>
/// In-memory implementation of <see cref="IShardedCdcPositionStore"/> for development and testing.
/// Positions are lost when the application restarts.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is registered as the default sharded position store when no provider-specific
/// store is configured. Production deployments should use a persistent store
/// (e.g., database-backed) provided by the CDC provider package.
/// </para>
/// <para>
/// Thread-safe for concurrent access from multiple shard processors.
/// Uses a composite key of <c>(shardId, connectorId)</c> with case-insensitive comparison.
/// </para>
/// </remarks>
internal sealed class InMemoryShardedCdcPositionStore : IShardedCdcPositionStore
{
    private readonly ConcurrentDictionary<(string ShardId, string ConnectorId), CdcPosition> _positions = new();

    /// <inheritdoc />
    public Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(
        string shardId,
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);

        var result = _positions.TryGetValue(NormalizeKey(shardId, connectorId), out var position)
            ? Right<EncinaError, Option<CdcPosition>>(Some(position))
            : Right<EncinaError, Option<CdcPosition>>(None);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SavePositionAsync(
        string shardId,
        string connectorId,
        CdcPosition position,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);
        ArgumentNullException.ThrowIfNull(position);

        _positions[NormalizeKey(shardId, connectorId)] = position;

        return Task.FromResult(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>> GetAllPositionsAsync(
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);

        var normalizedConnectorId = connectorId.ToUpperInvariant();
        var result = new Dictionary<string, CdcPosition>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in _positions)
        {
            if (string.Equals(kvp.Key.ConnectorId, normalizedConnectorId, StringComparison.OrdinalIgnoreCase))
            {
                result[kvp.Key.ShardId] = kvp.Value;
            }
        }

        return Task.FromResult(
            Right<EncinaError, IReadOnlyDictionary<string, CdcPosition>>(result));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> DeletePositionAsync(
        string shardId,
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);

        _positions.TryRemove(NormalizeKey(shardId, connectorId), out _);

        return Task.FromResult(Right<EncinaError, Unit>(unit));
    }

    private static (string ShardId, string ConnectorId) NormalizeKey(string shardId, string connectorId)
        => (shardId.ToUpperInvariant(), connectorId.ToUpperInvariant());
}
