using System.Collections.Frozen;
using System.Globalization;
using Encina.Sharding.Colocation;
using Encina.Sharding.Routing;
using LanguageExt;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Routes queries and writes to time-partitioned shards using binary search on period boundaries,
/// with tier-awareness for hot/warm/cold/archived data lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// Internally maintains a sorted array of <see cref="TimeBasedShardEntry"/> instances, each combining
/// a <see cref="ShardRange"/> (with ISO 8601 date string keys) with <see cref="ShardTierInfo"/>.
/// Routing resolves a timestamp to a date string and performs binary search over the sorted ranges.
/// </para>
/// <para>
/// Write operations are only permitted on <see cref="ShardTier.Hot"/> shards. A write attempt
/// targeting a non-Hot shard returns error code <c>encina.sharding.shard_read_only</c>.
/// </para>
/// <para>
/// The <see cref="GetShardIds(CompoundShardKey)"/> method supports scatter-gather by prefix matching
/// on period strings (e.g., a partial key of <c>"2026"</c> matches all periods starting with that year).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var tierInfos = new[]
/// {
///     new ShardTierInfo("orders-2026-01", ShardTier.Hot, new DateOnly(2026, 1, 1),
///         new DateOnly(2026, 2, 1), false, "Server=hot;Database=orders_2026_01", DateTime.UtcNow),
///     new ShardTierInfo("orders-2025-12", ShardTier.Warm, new DateOnly(2025, 12, 1),
///         new DateOnly(2026, 1, 1), true, "Server=warm;Database=orders_2025_12", DateTime.UtcNow),
/// };
///
/// var topology = new ShardTopology(tierInfos.Select(t =>
///     new ShardInfo(t.ShardId, t.ConnectionString)));
///
/// var router = new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly);
///
/// // Route a write (succeeds only for Hot shards)
/// var result = await router.RouteByTimestampAsync(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
/// </code>
/// </example>
public sealed class TimeBasedShardRouter : ITimeBasedShardRouter
{
    private readonly ShardTopology _topology;
    private readonly ColocationGroupRegistry? _colocationRegistry;
    private readonly IShardFallbackCreator? _fallbackCreator;
    private readonly TimeBasedShardEntry[] _sortedEntries;
    private readonly FrozenDictionary<string, TimeBasedShardEntry> _entriesByShardId;

    /// <summary>
    /// Initializes a new <see cref="TimeBasedShardRouter"/> from pre-configured tier info entries.
    /// </summary>
    /// <param name="topology">The shard topology containing connection strings.</param>
    /// <param name="tierInfos">
    /// The shard tier metadata entries. Each entry defines a shard's time period, tier, and connection details.
    /// Periods must not overlap.
    /// </param>
    /// <param name="period">The period granularity used for generating range keys.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <param name="colocationRegistry">
    /// Optional co-location group registry for co-location metadata lookups.
    /// </param>
    /// <param name="fallbackCreator">
    /// Optional fallback shard creator for resilience when a timestamp targets a period
    /// not yet covered by any configured shard. When provided, async routing methods will
    /// attempt on-demand shard creation instead of returning
    /// <see cref="ShardingErrorCodes.TimestampOutsideRange"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="topology"/> or <paramref name="tierInfos"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when overlapping periods are detected.</exception>
    public TimeBasedShardRouter(
        ShardTopology topology,
        IEnumerable<ShardTierInfo> tierInfos,
        ShardPeriod period,
        DayOfWeek weekStart = DayOfWeek.Monday,
        ColocationGroupRegistry? colocationRegistry = null,
        IShardFallbackCreator? fallbackCreator = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(tierInfos);

        _topology = topology;
        _colocationRegistry = colocationRegistry;
        _fallbackCreator = fallbackCreator;
        Period = period;
        WeekStart = weekStart;

        var entries = BuildEntries(tierInfos.ToList());
        ValidateNoOverlaps(entries);

        _sortedEntries = [.. entries.OrderBy(e => e.Range.StartKey, StringComparer.Ordinal)];
        _entriesByShardId = _sortedEntries.ToFrozenDictionary(
            e => e.TierInfo.ShardId,
            e => e,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the period granularity used by this router.
    /// </summary>
    public ShardPeriod Period { get; }

    /// <summary>
    /// Gets the first day of the week used for weekly period calculations.
    /// </summary>
    public DayOfWeek WeekStart { get; }

    /// <inheritdoc />
    public async Task<Either<EncinaError, string>> RouteByTimestampAsync(
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        var result = RouteByTimestamp(timestamp);

        if (result.IsLeft && _fallbackCreator is not null)
        {
            return await AttemptFallbackCreationAsync(timestamp, result, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, string>> RouteWriteByTimestampAsync(
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        var result = RouteWriteByTimestamp(timestamp);

        if (result.IsLeft && _fallbackCreator is not null)
        {
            // Attempt fallback creation; the created shard will be Hot (writable)
            return await AttemptFallbackCreationAsync(timestamp, result, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, IReadOnlyList<string>>> GetShardsInRangeAsync(
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default)
    {
        var result = GetShardsInRange(from, toExclusive);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Either<EncinaError, ShardTier> GetShardTier(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        return _entriesByShardId.TryGetValue(shardId, out var entry)
            ? Either<EncinaError, ShardTier>.Right(entry.TierInfo.CurrentTier)
            : Either<EncinaError, ShardTier>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardNotFound,
                    $"Shard '{shardId}' was not found in the time-based router."));
    }

    /// <inheritdoc />
    public Either<EncinaError, ShardTierInfo> GetShardTierInfo(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        return _entriesByShardId.TryGetValue(shardId, out var entry)
            ? Either<EncinaError, ShardTierInfo>.Right(entry.TierInfo)
            : Either<EncinaError, ShardTierInfo>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardNotFound,
                    $"Shard '{shardId}' was not found in the time-based router."));
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        if (_sortedEntries.Length == 0)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.NoTimeBasedShards,
                    "No time-based shard entries are configured in the router."));
        }

        return BinarySearchByKey(shardKey);
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(CompoundShardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return GetShardId(key.ToString());
    }

    /// <summary>
    /// Gets all shard IDs whose period labels match the partial key prefix via prefix matching.
    /// </summary>
    /// <remarks>
    /// For time-based routing, a partial key (e.g., <c>"2026"</c>) matches all shard ranges
    /// whose start key begins with that prefix, enabling scatter-gather across time periods.
    /// </remarks>
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey)
    {
        ArgumentNullException.ThrowIfNull(partialKey);

        var prefix = partialKey.PrimaryComponent;
        var matchingShards = new List<string>();

        foreach (var entry in _sortedEntries)
        {
            var startsWithPrefix = entry.Range.StartKey.StartsWith(prefix, StringComparison.Ordinal);
            var prefixInRange = StringComparer.Ordinal.Compare(prefix, entry.Range.StartKey) >= 0
                && (entry.Range.EndKey is null || StringComparer.Ordinal.Compare(prefix, entry.Range.EndKey) < 0);
            var endOverlaps = entry.Range.EndKey is not null
                && entry.Range.EndKey.StartsWith(prefix, StringComparison.Ordinal);

            if (startsWithPrefix || prefixInRange || endOverlaps)
            {
                matchingShards.Add(entry.Range.ShardId);
            }
        }

        if (matchingShards.Count == 0)
        {
            return Either<EncinaError, IReadOnlyList<string>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.PartialKeyRoutingFailed,
                    $"No time-based shard ranges match the partial key prefix '{prefix}'."));
        }

        return Either<EncinaError, IReadOnlyList<string>>.Right(matchingShards);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() => _topology.AllShardIds;

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        if (_entriesByShardId.TryGetValue(shardId, out var entry))
        {
            return Either<EncinaError, string>.Right(entry.TierInfo.ConnectionString);
        }

        return _topology.GetConnectionString(shardId);
    }

    /// <inheritdoc />
    public IColocationGroup? GetColocationGroup(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        if (_colocationRegistry is not null && _colocationRegistry.TryGetGroup(entityType, out var group))
        {
            return group;
        }

        return null;
    }

    private async Task<Either<EncinaError, string>> AttemptFallbackCreationAsync(
        DateTime timestamp,
        Either<EncinaError, string> originalError,
        CancellationToken cancellationToken)
    {
        // Only attempt fallback for TimestampOutsideRange errors
        var isOutsideRange = originalError.Match(
            Right: _ => false,
            Left: error => error.GetCode().Match(
                Some: code => code == ShardingErrorCodes.TimestampOutsideRange,
                None: () => false));

        if (!isOutsideRange)
        {
            return originalError;
        }

        var fallbackResult = await _fallbackCreator!.CreateShardForTimestampAsync(timestamp, cancellationToken)
            .ConfigureAwait(false);

        return fallbackResult.Match<Either<EncinaError, string>>(
            Right: tierInfo => Either<EncinaError, string>.Right(tierInfo.ShardId),
            Left: error => error);
    }

    private Either<EncinaError, string> RouteByTimestamp(DateTime timestamp)
    {
        if (_sortedEntries.Length == 0)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.NoTimeBasedShards,
                    "No time-based shard entries are configured in the router."));
        }

        var dateKey = DateOnly.FromDateTime(timestamp)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return BinarySearchByKey(dateKey);
    }

    private Either<EncinaError, string> RouteWriteByTimestamp(DateTime timestamp)
    {
        if (_sortedEntries.Length == 0)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.NoTimeBasedShards,
                    "No time-based shard entries are configured in the router."));
        }

        var dateKey = DateOnly.FromDateTime(timestamp)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return BinarySearchEntry(dateKey)
            .Bind(entry =>
            {
                if (entry.TierInfo.IsReadOnly || entry.TierInfo.CurrentTier != ShardTier.Hot)
                {
                    return Either<EncinaError, string>.Left(
                        EncinaErrors.Create(
                            ShardingErrorCodes.ShardReadOnly,
                            $"Shard '{entry.TierInfo.ShardId}' is in tier '{entry.TierInfo.CurrentTier}' " +
                            $"and does not accept writes. Only Hot-tier shards accept write operations."));
                }

                return Either<EncinaError, string>.Right(entry.Range.ShardId);
            });
    }

    private Either<EncinaError, IReadOnlyList<string>> GetShardsInRange(DateTime from, DateTime to)
    {
        if (_sortedEntries.Length == 0)
        {
            return Either<EncinaError, IReadOnlyList<string>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.NoTimeBasedShards,
                    "No time-based shard entries are configured in the router."));
        }

        var fromKey = DateOnly.FromDateTime(from)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toKey = DateOnly.FromDateTime(to)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var matchingShards = new List<string>();

        foreach (var entry in _sortedEntries)
        {
            // A range [entryStart, entryEnd) overlaps [fromKey, toKey) if:
            // entryStart < toKey AND entryEnd > fromKey
            var entryStartBeforeTo = StringComparer.Ordinal.Compare(entry.Range.StartKey, toKey) < 0;
            var entryEndAfterFrom = entry.Range.EndKey is null
                || StringComparer.Ordinal.Compare(entry.Range.EndKey, fromKey) > 0;

            if (entryStartBeforeTo && entryEndAfterFrom)
            {
                matchingShards.Add(entry.Range.ShardId);
            }
        }

        if (matchingShards.Count == 0)
        {
            return Either<EncinaError, IReadOnlyList<string>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.TimestampOutsideRange,
                    $"No time-based shards cover the range [{from:O}, {to:O})."));
        }

        return Either<EncinaError, IReadOnlyList<string>>.Right(matchingShards);
    }

    private Either<EncinaError, string> BinarySearchByKey(string key)
    {
        return BinarySearchEntry(key).Map(e => e.Range.ShardId);
    }

    private Either<EncinaError, TimeBasedShardEntry> BinarySearchEntry(string key)
    {
        var lo = 0;
        var hi = _sortedEntries.Length - 1;

        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            var entry = _sortedEntries[mid];

            var startCmp = StringComparer.Ordinal.Compare(key, entry.Range.StartKey);

            if (startCmp < 0)
            {
                hi = mid - 1;
            }
            else
            {
                // Key >= StartKey, check EndKey
                if (entry.Range.EndKey is null || StringComparer.Ordinal.Compare(key, entry.Range.EndKey) < 0)
                {
                    return Either<EncinaError, TimeBasedShardEntry>.Right(entry);
                }

                lo = mid + 1;
            }
        }

        return Either<EncinaError, TimeBasedShardEntry>.Left(
            EncinaErrors.Create(
                ShardingErrorCodes.TimestampOutsideRange,
                $"Key '{key}' does not fall within any configured time-based shard period."));
    }

    private static List<TimeBasedShardEntry> BuildEntries(List<ShardTierInfo> tierInfos)
    {
        var entries = new List<TimeBasedShardEntry>(tierInfos.Count);

        foreach (var tierInfo in tierInfos)
        {
            var startKey = tierInfo.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endKey = tierInfo.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var range = new ShardRange(startKey, endKey, tierInfo.ShardId);
            entries.Add(new TimeBasedShardEntry(range, tierInfo));
        }

        return entries;
    }

    private static void ValidateNoOverlaps(List<TimeBasedShardEntry> entries)
    {
        var sorted = entries.OrderBy(e => e.Range.StartKey, StringComparer.Ordinal).ToList();

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            if (current.Range.EndKey is null)
            {
                throw new ArgumentException(
                    $"Time-based shard '{current.TierInfo.ShardId}' " +
                    $"(start={current.Range.StartKey}, end=unbounded) overlaps with shard " +
                    $"'{next.TierInfo.ShardId}' (start={next.Range.StartKey}).");
            }

            if (StringComparer.Ordinal.Compare(current.Range.EndKey, next.Range.StartKey) > 0)
            {
                throw new ArgumentException(
                    $"Time-based shard '{current.TierInfo.ShardId}' " +
                    $"(start={current.Range.StartKey}, end={current.Range.EndKey}) overlaps with shard " +
                    $"'{next.TierInfo.ShardId}' (start={next.Range.StartKey}).");
            }
        }
    }
}
