using System.Collections.Concurrent;
using Encina.Cdc.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Cdc.PostgreSql.Benchmarks.Infrastructure;

/// <summary>
/// Trivial in-memory <see cref="ICdcPositionStore"/> implementation used by the benchmarks so
/// that connector wiring does not depend on a persistent store.
/// </summary>
public sealed class InMemoryCdcPositionStore : ICdcPositionStore
{
    private readonly ConcurrentDictionary<string, CdcPosition> _positions = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        var position = _positions.TryGetValue(connectorId, out var value)
            ? Some(value)
            : Option<CdcPosition>.None;
        return Task.FromResult<Either<EncinaError, Option<CdcPosition>>>(Right(position));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SavePositionAsync(
        string connectorId,
        CdcPosition position,
        CancellationToken cancellationToken = default)
    {
        _positions[connectorId] = position;
        return Task.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> DeletePositionAsync(
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        _positions.TryRemove(connectorId, out _);
        return Task.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
    }
}
