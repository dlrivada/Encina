using System.Collections.Concurrent;
using Encina.Cdc;
using Encina.Cdc.DeadLetter;
using Encina.Cdc.Errors;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Testing.Fakes.Stores;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ICdcDeadLetterStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the CDC dead letter store interface using an in-memory
/// concurrent dictionary. Tracks all operations for test verification via
/// <see cref="GetEntries"/>, <see cref="WasEventDeadLettered"/> and similar helpers.
/// </para>
/// </remarks>
public sealed class FakeCdcDeadLetterStore : ICdcDeadLetterStore
{
    private readonly ConcurrentDictionary<Guid, CdcDeadLetterEntry> _entries = new();
    private readonly ConcurrentBag<CdcDeadLetterEntry> _addedEntries = new();
    private readonly ConcurrentBag<(Guid Id, CdcDeadLetterResolution Resolution)> _resolvedEntries = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> AddAsync(
        CdcDeadLetterEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_lock)
        {
            _entries[entry.Id] = entry;
            _addedEntries.Add(entry);
        }

        return Task.FromResult<Either<EncinaError, Unit>>(Right(unit));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CdcDeadLetterEntry> pending;

        lock (_lock)
        {
            pending = _entries.Values
                .Where(e => e.Status == CdcDeadLetterStatus.Pending)
                .Take(maxCount)
                .ToList();
        }

        return Task.FromResult<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>>(Right(pending));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> ResolveAsync(
        Guid id,
        CdcDeadLetterResolution resolution,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
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
            _resolvedEntries.Add((id, resolution));
        }

        return Task.FromResult<Either<EncinaError, Unit>>(Right(unit));
    }

    /// <summary>
    /// Checks whether an entry with the specified ID was added to the dead letter store.
    /// </summary>
    /// <param name="id">The entry ID to check.</param>
    /// <returns><c>true</c> if the entry was added; otherwise, <c>false</c>.</returns>
    public bool WasEventDeadLettered(Guid id)
    {
        lock (_lock)
        {
            return _addedEntries.Any(e => e.Id == id);
        }
    }

    /// <summary>
    /// Gets a snapshot of all entries that have been added to the store (for verification).
    /// </summary>
    /// <returns>A point-in-time copy of all added entries.</returns>
    public IReadOnlyList<CdcDeadLetterEntry> GetEntries()
    {
        lock (_lock)
        {
            return _addedEntries.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets entries filtered by connector ID.
    /// </summary>
    /// <param name="connectorId">The connector ID to filter by.</param>
    /// <returns>Collection of entries from the specified connector.</returns>
    public IReadOnlyList<CdcDeadLetterEntry> GetEntriesByConnector(string connectorId)
    {
        lock (_lock)
        {
            return _entries.Values
                .Where(e => e.ConnectorId == connectorId)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets a snapshot of all resolved entry operations (for verification).
    /// </summary>
    /// <returns>A point-in-time copy of resolved entry IDs and their resolutions.</returns>
    public IReadOnlyList<(Guid Id, CdcDeadLetterResolution Resolution)> GetResolvedEntries()
    {
        lock (_lock)
        {
            return _resolvedEntries.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Clears all entries and resets verification state.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            _addedEntries.Clear();
            _resolvedEntries.Clear();
        }
    }
}
