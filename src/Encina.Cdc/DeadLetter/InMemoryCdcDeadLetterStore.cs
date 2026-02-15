using System.Collections.Concurrent;
using Encina.Cdc.Errors;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Cdc.DeadLetter;

/// <summary>
/// In-memory implementation of <see cref="ICdcDeadLetterStore"/> using a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe storage.
/// Suitable for development, testing, and single-instance deployments.
/// </summary>
/// <remarks>
/// <para>
/// This store does not persist data across process restarts. For production
/// use in multi-instance deployments, register a database-backed implementation
/// via <c>TryAddSingleton</c> override.
/// </para>
/// </remarks>
internal sealed class InMemoryCdcDeadLetterStore : ICdcDeadLetterStore
{
    private readonly ConcurrentDictionary<Guid, CdcDeadLetterEntry> _entries = new();

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> AddAsync(
        CdcDeadLetterEntry entry,
        CancellationToken cancellationToken = default)
    {
        _entries[entry.Id] = entry;
        return Task.FromResult<Either<EncinaError, Unit>>(Right(unit));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CdcDeadLetterEntry> pending = _entries.Values
            .Where(e => e.Status == CdcDeadLetterStatus.Pending)
            .Take(maxCount)
            .ToList();

        return Task.FromResult<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>>(Right(pending));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> ResolveAsync(
        Guid id,
        CdcDeadLetterResolution resolution,
        CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(id, out var entry))
        {
            return Task.FromResult<Either<EncinaError, Unit>>(
                Left(CdcErrors.DeadLetterNotFound(id)));
        }

        if (entry.Status != CdcDeadLetterStatus.Pending)
        {
            return Task.FromResult<Either<EncinaError, Unit>>(
                Left(CdcErrors.DeadLetterAlreadyResolved(id)));
        }

        var newStatus = resolution switch
        {
            CdcDeadLetterResolution.Replay => CdcDeadLetterStatus.Replayed,
            CdcDeadLetterResolution.Discard => CdcDeadLetterStatus.Discarded,
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null)
        };

        _entries[id] = entry with { Status = newStatus };
        return Task.FromResult<Either<EncinaError, Unit>>(Right(unit));
    }
}
