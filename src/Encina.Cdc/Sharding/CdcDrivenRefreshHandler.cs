using Encina.Cdc.Abstractions;
using Encina.Sharding.ReferenceTables;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Sharding;

/// <summary>
/// Handles CDC change events for reference tables by triggering a full replication
/// of the affected table when any insert, update, or delete is detected.
/// </summary>
/// <typeparam name="TEntity">The reference table entity type.</typeparam>
/// <remarks>
/// <para>
/// This handler is registered for reference tables configured with
/// <see cref="RefreshStrategy.CdcDriven"/>. When the CDC infrastructure detects
/// a change on the primary shard, this handler invokes
/// <see cref="IReferenceTableReplicator.ReplicateAsync{TEntity}"/> to synchronize
/// all target shards.
/// </para>
/// <para>
/// Reference tables are typically small (hundreds to thousands of rows), so a full
/// replication on every change is efficient and avoids the complexity of incremental
/// delta tracking.
/// </para>
/// </remarks>
internal sealed class CdcDrivenRefreshHandler<TEntity>(
    IReferenceTableReplicator replicator,
    ILogger<CdcDrivenRefreshHandler<TEntity>> logger) : IChangeEventHandler<TEntity>
    where TEntity : class
{
    private readonly IReferenceTableReplicator _replicator = replicator ?? throw new ArgumentNullException(nameof(replicator));
    private readonly ILogger<CdcDrivenRefreshHandler<TEntity>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
        TEntity entity,
        ChangeContext context)
    {
        return await ReplicateOnChangeAsync("Insert", context.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
        TEntity before,
        TEntity after,
        ChangeContext context)
    {
        return await ReplicateOnChangeAsync("Update", context.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
        TEntity entity,
        ChangeContext context)
    {
        return await ReplicateOnChangeAsync("Delete", context.CancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, Unit>> ReplicateOnChangeAsync(
        string operation,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "CDC {Operation} detected for reference table '{EntityType}' — triggering replication",
            operation,
            typeof(TEntity).Name);

        var result = await _replicator.ReplicateAsync<TEntity>(cancellationToken).ConfigureAwait(false);

        return result.Match<Either<EncinaError, Unit>>(
            Right: rep =>
            {
                if (rep.IsPartial)
                {
                    _logger.LogWarning(
                        "Partial replication for '{EntityType}' after CDC {Operation}: " +
                        "{SuccessCount}/{TotalCount} shards succeeded",
                        typeof(TEntity).Name,
                        operation,
                        rep.ShardResults.Count,
                        rep.TotalShardsTargeted);
                }

                return unit;
            },
            Left: error => error);
    }
}
