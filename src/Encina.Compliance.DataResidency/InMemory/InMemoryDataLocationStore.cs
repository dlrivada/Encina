using System.Collections.Concurrent;

using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IDataLocationStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by entity ID,
/// with each value being a thread-safe list of <see cref="DataLocation"/> records
/// for that entity. An entity may have multiple locations (primary, replica, cache, etc.).
/// </para>
/// <para>
/// Per GDPR Article 30(1)(e), the controller must maintain records of processing
/// activities including transfers to third countries. This store enables location
/// tracking for compliance audits and data residency verification.
/// </para>
/// <para>
/// This store is not intended for production use. All data is lost when the process exits.
/// For production, use a database-backed implementation (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
public sealed class InMemoryDataLocationStore : IDataLocationStore
{
    private readonly ConcurrentDictionary<string, List<DataLocation>> _locations = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryDataLocationStore> _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDataLocationStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryDataLocationStore(ILogger<InMemoryDataLocationStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the total number of data location records currently stored. Useful for testing assertions.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _locations.Values.Sum(list => list.Count);
            }
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DataLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        lock (_lock)
        {
            var list = _locations.GetOrAdd(location.EntityId, _ => []);
            list.Add(location);
        }

        _logger.LogDebug(
            "Recorded data location '{LocationId}' for entity '{EntityId}' in region '{RegionCode}'",
            location.Id, location.EntityId, location.Region.Code);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        IReadOnlyList<DataLocation> result;
        lock (_lock)
        {
            result = _locations.TryGetValue(entityId, out var list)
                ? list.ToList().AsReadOnly()
                : [];
        }

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataLocation>>>(
            Right<EncinaError, IReadOnlyList<DataLocation>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByRegionAsync(
        Region region,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(region);

        IReadOnlyList<DataLocation> result;
        lock (_lock)
        {
            result = _locations.Values
                .SelectMany(list => list)
                .Where(loc => loc.Region.Equals(region))
                .ToList()
                .AsReadOnly();
        }

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataLocation>>>(
            Right<EncinaError, IReadOnlyList<DataLocation>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        IReadOnlyList<DataLocation> result;
        lock (_lock)
        {
            result = _locations.Values
                .SelectMany(list => list)
                .Where(loc => loc.DataCategory == dataCategory)
                .ToList()
                .AsReadOnly();
        }

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataLocation>>>(
            Right<EncinaError, IReadOnlyList<DataLocation>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> DeleteByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        _locations.TryRemove(entityId, out _);

        _logger.LogDebug("Deleted all data locations for entity '{EntityId}'", entityId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <summary>
    /// Returns all stored data locations. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all data locations in the store.</returns>
    public IReadOnlyList<DataLocation> GetAllRecords()
    {
        lock (_lock)
        {
            return _locations.Values
                .SelectMany(list => list)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Removes all data locations from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _locations.Clear();
}
