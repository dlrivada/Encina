using System.Collections.Concurrent;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// In-memory implementation of <see cref="ITierStore"/> backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe access.
/// </summary>
/// <remarks>
/// <para>
/// This is the default tier store suitable for single-process deployments and testing.
/// State is lost on application restart. For persistent tier metadata, use a
/// database-backed <see cref="ITierStore"/> implementation.
/// </para>
/// <para>
/// The <see cref="UpdateTierAsync"/> method atomically updates the shard's tier and
/// records the transition timestamp. The <see cref="ShardTierInfo.IsReadOnly"/> flag is automatically
/// set to <see langword="true"/> for non-<see cref="ShardTier.Hot"/> tiers.
/// </para>
/// </remarks>
public sealed class InMemoryTierStore : ITierStore
{
    private readonly ConcurrentDictionary<string, ShardTierInfo> _shards = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryTierStore"/>.
    /// </summary>
    /// <param name="timeProvider">
    /// Optional time provider for timestamp generation. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public InMemoryTierStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ShardTierInfo>> GetAllTierInfoAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ShardTierInfo> result = _shards.Values.ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ShardTierInfo?> GetTierInfoAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        var result = _shards.TryGetValue(shardId, out var tierInfo) ? tierInfo : null;
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> UpdateTierAsync(
        string shardId,
        ShardTier newTier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        if (!_shards.TryGetValue(shardId, out var existing))
        {
            return Task.FromResult(false);
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var updated = existing with
        {
            CurrentTier = newTier,
            IsReadOnly = newTier != ShardTier.Hot,
            LastTransitionAtUtc = nowUtc
        };

        _shards[shardId] = updated;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task AddShardAsync(
        ShardTierInfo tierInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tierInfo);

        if (!_shards.TryAdd(tierInfo.ShardId, tierInfo))
        {
            throw new ArgumentException(
                $"A shard with ID '{tierInfo.ShardId}' already exists in the tier store.",
                nameof(tierInfo));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ShardTierInfo>> GetShardsDueForTransitionAsync(
        ShardTier fromTier,
        TimeSpan ageThreshold,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var cutoffDate = DateOnly.FromDateTime(nowUtc - ageThreshold);

        IReadOnlyList<ShardTierInfo> result = _shards.Values
            .Where(s => s.CurrentTier == fromTier && s.PeriodEnd <= cutoffDate)
            .OrderBy(s => s.PeriodStart)
            .ToList();

        return Task.FromResult(result);
    }
}
