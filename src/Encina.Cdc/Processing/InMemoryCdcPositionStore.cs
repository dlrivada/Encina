using System.Collections.Concurrent;
using Encina.Cdc.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Processing;

/// <summary>
/// In-memory implementation of <see cref="ICdcPositionStore"/> for development and testing.
/// Positions are lost when the application restarts.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is registered as the default position store when no provider-specific
/// store is configured. Production deployments should use a persistent store
/// (e.g., database-backed) provided by the CDC provider package.
/// </para>
/// <para>
/// Thread-safe for concurrent access from multiple connectors.
/// </para>
/// </remarks>
internal sealed class InMemoryCdcPositionStore : ICdcPositionStore
{
    private readonly ConcurrentDictionary<string, CdcPosition> _positions = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);

        var result = _positions.TryGetValue(connectorId, out var position)
            ? Right<EncinaError, Option<CdcPosition>>(Some(position))
            : Right<EncinaError, Option<CdcPosition>>(None);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SavePositionAsync(
        string connectorId,
        CdcPosition position,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);
        ArgumentNullException.ThrowIfNull(position);

        _positions[connectorId] = position;

        return Task.FromResult(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> DeletePositionAsync(
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);

        _positions.TryRemove(connectorId, out _);

        return Task.FromResult(Right<EncinaError, Unit>(unit));
    }
}
