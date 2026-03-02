using System.Collections.Concurrent;

using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention.InMemory;

/// <summary>
/// In-memory implementation of <see cref="ILegalHoldStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by <see cref="LegalHold.Id"/>
/// with LINQ-based secondary indexes on <see cref="LegalHold.EntityId"/>
/// and <see cref="LegalHold.IsActive"/> for query operations.
/// </para>
/// <para>
/// This store is not intended for production use. All data is lost when the process exits.
/// For production, use a database-backed implementation (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
public sealed class InMemoryLegalHoldStore : ILegalHoldStore
{
    private readonly ConcurrentDictionary<string, LegalHold> _holds = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryLegalHoldStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryLegalHoldStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryLegalHoldStore(ILogger<InMemoryLegalHoldStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of holds currently stored. Useful for testing assertions.
    /// </summary>
    public int Count => _holds.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> CreateAsync(
        LegalHold hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);

        if (!_holds.TryAdd(hold.Id, hold))
        {
            _logger.LogWarning("Legal hold '{HoldId}' already exists", hold.Id);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.HoldAlreadyActive(hold.EntityId)));
        }

        _logger.LogDebug(
            "Created legal hold '{HoldId}' for entity '{EntityId}'",
            hold.Id, hold.EntityId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LegalHold>>> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        var result = _holds.TryGetValue(holdId, out var hold)
            ? Some(hold)
            : Option<LegalHold>.None;

        return ValueTask.FromResult<Either<EncinaError, Option<LegalHold>>>(
            Right<EncinaError, Option<LegalHold>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        IReadOnlyList<LegalHold> result = _holds.Values
            .Where(h => h.EntityId == entityId)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<LegalHold>>>(
            Right<EncinaError, IReadOnlyList<LegalHold>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> IsUnderHoldAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        var isHeld = _holds.Values.Any(h => h.EntityId == entityId && h.IsActive);
        return ValueTask.FromResult<Either<EncinaError, bool>>(
            Right<EncinaError, bool>(isHeld));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LegalHold> result = _holds.Values
            .Where(h => h.IsActive)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<LegalHold>>>(
            Right<EncinaError, IReadOnlyList<LegalHold>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> ReleaseAsync(
        string holdId,
        string? releasedByUserId,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        if (!_holds.TryGetValue(holdId, out var existing))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.HoldNotFound(holdId)));
        }

        if (!existing.IsActive)
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.HoldAlreadyReleased(holdId)));
        }

        var released = existing with
        {
            ReleasedAtUtc = releasedAtUtc,
            ReleasedByUserId = releasedByUserId
        };

        _holds[holdId] = released;
        _logger.LogDebug(
            "Released legal hold '{HoldId}' for entity '{EntityId}'",
            holdId, existing.EntityId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LegalHold> result = _holds.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<LegalHold>>>(
            Right<EncinaError, IReadOnlyList<LegalHold>>(result));
    }

    /// <summary>
    /// Removes all holds from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _holds.Clear();
}
