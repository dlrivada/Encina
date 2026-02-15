using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Default implementation of <see cref="IShardArchiver"/> that coordinates tier transitions,
/// read-only enforcement, archival, and retention through the <see cref="ITierStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tier transitions update the <see cref="ITierStore"/> metadata and optionally delegate
/// database-level read-only enforcement to an <see cref="IReadOnlyEnforcer"/>. When no
/// enforcer is registered, read-only state is enforced only at the application level via
/// the <see cref="ITimeBasedShardRouter"/>.
/// </para>
/// <para>
/// The <see cref="ArchiveShardAsync"/> method is a no-op in this default implementation
/// because actual data export is provider-specific. Override or replace this implementation
/// to integrate with cloud storage (S3, Azure Blob) or backup tools.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming pattern, returning
/// <c>Either&lt;EncinaError, Unit&gt;</c>. Exceptions from the tier store or enforcer are
/// caught and wrapped in typed errors with appropriate error codes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var archiver = new ShardArchiver(tierStore, topologyProvider);
///
/// // Transition a shard from Hot to Warm
/// var result = await archiver.TransitionTierAsync("orders-2025-10", ShardTier.Warm);
///
/// // With database-level read-only enforcement
/// var archiverWithEnforcer = new ShardArchiver(tierStore, topologyProvider, enforcer);
/// await archiverWithEnforcer.EnforceReadOnlyAsync("orders-2025-10");
/// </code>
/// </example>
public sealed class ShardArchiver : IShardArchiver
{
    private readonly ITierStore _tierStore;
    private readonly IShardTopologyProvider _topologyProvider;
    private readonly IReadOnlyEnforcer? _readOnlyEnforcer;

    /// <summary>
    /// Initializes a new instance of <see cref="ShardArchiver"/>.
    /// </summary>
    /// <param name="tierStore">The tier metadata store.</param>
    /// <param name="topologyProvider">The shard topology provider for connection string resolution.</param>
    /// <param name="readOnlyEnforcer">
    /// Optional provider-specific read-only enforcer. When <see langword="null"/>,
    /// read-only state is enforced only at the application level through the tier store.
    /// </param>
    public ShardArchiver(
        ITierStore tierStore,
        IShardTopologyProvider topologyProvider,
        IReadOnlyEnforcer? readOnlyEnforcer = null)
    {
        ArgumentNullException.ThrowIfNull(tierStore);
        ArgumentNullException.ThrowIfNull(topologyProvider);

        _tierStore = tierStore;
        _topologyProvider = topologyProvider;
        _readOnlyEnforcer = readOnlyEnforcer;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> TransitionTierAsync(
        string shardId,
        ShardTier newTier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        try
        {
            var updated = await _tierStore.UpdateTierAsync(shardId, newTier, cancellationToken);

            if (!updated)
            {
                return EncinaErrors.Create(
                    ShardingErrorCodes.TierTransitionFailed,
                    $"Tier transition failed: shard '{shardId}' was not found in the tier store.");
            }

            // Enforce read-only at the database level for non-Hot tiers
            if (newTier != ShardTier.Hot && _readOnlyEnforcer is not null)
            {
                var enforceResult = await EnforceReadOnlyForShardAsync(shardId, cancellationToken);
                if (enforceResult.IsLeft)
                {
                    return enforceResult;
                }
            }

            return unit;
        }
        catch (Exception ex)
        {
            return EncinaErrors.FromException(
                ShardingErrorCodes.TierTransitionFailed,
                ex,
                $"Tier transition to '{newTier}' failed for shard '{shardId}'.");
        }
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> ArchiveShardAsync(
        string shardId,
        ArchiveOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(options);

        // Default implementation is a no-op. Actual data export to external storage
        // (S3, Azure Blob, etc.) requires a provider-specific implementation.
        // The tier store metadata is updated by TransitionTierAsync when moving
        // a shard to the Archived tier.
        return Task.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> EnforceReadOnlyAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        if (_readOnlyEnforcer is null)
        {
            // No enforcer registered; read-only is enforced at the application level only
            // via the tier store's IsReadOnly flag.
            return unit;
        }

        return await EnforceReadOnlyForShardAsync(shardId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DeleteShardDataAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        try
        {
            // Default implementation only updates tier store metadata.
            // Database-level deletion requires a provider-specific IShardArchiver.
            var updated = await _tierStore.UpdateTierAsync(shardId, ShardTier.Archived, cancellationToken);

            if (!updated)
            {
                return EncinaErrors.Create(
                    ShardingErrorCodes.RetentionPolicyFailed,
                    $"Retention policy failed: shard '{shardId}' was not found in the tier store.");
            }

            return unit;
        }
        catch (Exception ex)
        {
            return EncinaErrors.FromException(
                ShardingErrorCodes.RetentionPolicyFailed,
                ex,
                $"Retention policy execution failed for shard '{shardId}'.");
        }
    }

    private async Task<Either<EncinaError, Unit>> EnforceReadOnlyForShardAsync(
        string shardId,
        CancellationToken cancellationToken)
    {
        try
        {
            var tierInfo = await _tierStore.GetTierInfoAsync(shardId, cancellationToken);

            if (tierInfo is null)
            {
                return EncinaErrors.Create(
                    ShardingErrorCodes.ShardNotFound,
                    $"Shard '{shardId}' was not found in the tier store.");
            }

            return await _readOnlyEnforcer!.EnforceReadOnlyAsync(
                shardId,
                tierInfo.ConnectionString,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return EncinaErrors.FromException(
                ShardingErrorCodes.TierTransitionFailed,
                ex,
                $"Read-only enforcement failed for shard '{shardId}'.");
        }
    }
}
