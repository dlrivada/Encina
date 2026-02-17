using Encina.DomainModeling;
using Encina.DomainModeling.Diagnostics;
using LanguageExt;

namespace Encina.OpenTelemetry.BulkOperations;

/// <summary>
/// Decorator that adds OpenTelemetry tracing to any <see cref="IBulkOperations{TEntity}"/> implementation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This decorator wraps any bulk operations implementation and adds distributed tracing
/// via <see cref="BulkOperationsActivitySource"/>. All instrumentation is zero-cost when
/// no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal sealed class InstrumentedBulkOperations<TEntity> : IBulkOperations<TEntity>
    where TEntity : class
{
    private readonly IBulkOperations<TEntity> _inner;
    private readonly string _entityType = typeof(TEntity).Name;
    private readonly string _provider;

    public InstrumentedBulkOperations(IBulkOperations<TEntity> inner, string provider)
    {
        _inner = inner;
        _provider = provider;
    }

    public async Task<Either<EncinaError, int>> BulkInsertAsync(
        IEnumerable<TEntity> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        var activity = BulkOperationsActivitySource.StartBulkOperation("insert", _entityType, _provider);

        var result = await _inner.BulkInsertAsync(entities, config, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: rows => BulkOperationsActivitySource.Complete(activity, rows),
            Left: e => BulkOperationsActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, int>> BulkUpdateAsync(
        IEnumerable<TEntity> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        var activity = BulkOperationsActivitySource.StartBulkOperation("update", _entityType, _provider);

        var result = await _inner.BulkUpdateAsync(entities, config, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: rows => BulkOperationsActivitySource.Complete(activity, rows),
            Left: e => BulkOperationsActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, int>> BulkDeleteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var activity = BulkOperationsActivitySource.StartBulkOperation("delete", _entityType, _provider);

        var result = await _inner.BulkDeleteAsync(entities, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: rows => BulkOperationsActivitySource.Complete(activity, rows),
            Left: e => BulkOperationsActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, int>> BulkMergeAsync(
        IEnumerable<TEntity> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        var activity = BulkOperationsActivitySource.StartBulkOperation("merge", _entityType, _provider);

        var result = await _inner.BulkMergeAsync(entities, config, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: rows => BulkOperationsActivitySource.Complete(activity, rows),
            Left: e => BulkOperationsActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> BulkReadAsync(
        IEnumerable<object> ids,
        CancellationToken cancellationToken = default)
    {
        var activity = BulkOperationsActivitySource.StartBulkOperation("read", _entityType, _provider);

        var result = await _inner.BulkReadAsync(ids, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: entities => BulkOperationsActivitySource.Complete(activity, entities.Count),
            Left: e => BulkOperationsActivitySource.Failed(activity, null, e.Message));

        return result;
    }
}
