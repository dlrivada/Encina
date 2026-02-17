using System.Diagnostics;
using Encina.DomainModeling;
using Encina.DomainModeling.Diagnostics;
using LanguageExt;

namespace Encina.OpenTelemetry.Repository;

/// <summary>
/// Decorator that adds OpenTelemetry tracing and metrics to any <see cref="IFunctionalRepository{TEntity, TId}"/> implementation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This decorator wraps any repository implementation and adds:
/// <list type="bullet">
///   <item><description>Distributed tracing via <see cref="RepositoryActivitySource"/></description></item>
///   <item><description>Metrics recording via <see cref="RepositoryMetrics"/></description></item>
/// </list>
/// All instrumentation is zero-cost when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal sealed class InstrumentedFunctionalRepository<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IFunctionalRepository<TEntity, TId> _inner;
    private readonly string _entityType = typeof(TEntity).Name;
    private readonly string _provider;

    public InstrumentedFunctionalRepository(
        IFunctionalRepository<TEntity, TId> inner,
        string provider)
    {
        _inner = inner;
        _provider = provider;
    }

    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("get_by_id", _entityType, _provider);
        var sw = Stopwatch.StartNew();

        var result = await _inner.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        sw.Stop();
        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity, resultCount: 1),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("list", _entityType, _provider);
        var sw = Stopwatch.StartNew();

        var result = await _inner.ListAsync(cancellationToken).ConfigureAwait(false);

        sw.Stop();
        result.Match(
            Right: entities => RepositoryActivitySource.Complete(activity, resultCount: entities.Count),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        Specification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("list", _entityType, _provider);
        var sw = Stopwatch.StartNew();

        var result = await _inner.ListAsync(specification, cancellationToken).ConfigureAwait(false);

        sw.Stop();
        result.Match(
            Right: entities => RepositoryActivitySource.Complete(activity, resultCount: entities.Count),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, TEntity>> FirstOrDefaultAsync(
        Specification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("first_or_default", _entityType, _provider);
        var sw = Stopwatch.StartNew();

        var result = await _inner.FirstOrDefaultAsync(specification, cancellationToken).ConfigureAwait(false);

        sw.Stop();
        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity, resultCount: 1),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("count", _entityType, _provider);

        var result = await _inner.CountAsync(cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: count => { RepositoryActivitySource.Complete(activity, resultCount: count); },
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, int>> CountAsync(
        Specification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("count", _entityType, _provider);

        var result = await _inner.CountAsync(specification, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: count => { RepositoryActivitySource.Complete(activity, resultCount: count); },
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, bool>> AnyAsync(
        Specification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("any", _entityType, _provider);

        var result = await _inner.AnyAsync(specification, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, bool>> AnyAsync(
        CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("any", _entityType, _provider);

        var result = await _inner.AnyAsync(cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        PaginationOptions pagination, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("get_paged", _entityType, _provider);

        var result = await _inner.GetPagedAsync(pagination, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: paged => RepositoryActivitySource.Complete(activity, resultCount: paged.Items.Count),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        Specification<TEntity> specification, PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("get_paged", _entityType, _provider);

        var result = await _inner.GetPagedAsync(specification, pagination, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: paged => RepositoryActivitySource.Complete(activity, resultCount: paged.Items.Count),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        IPagedSpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("get_paged", _entityType, _provider);

        var result = await _inner.GetPagedAsync(specification, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: paged => RepositoryActivitySource.Complete(activity, resultCount: paged.Items.Count),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("add", _entityType, _provider);

        var result = await _inner.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("update", _entityType, _provider);

        var result = await _inner.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("delete", _entityType, _provider);

        var result = await _inner.DeleteAsync(id, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TEntity entity, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("delete", _entityType, _provider);

        var result = await _inner.DeleteAsync(entity, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> AddRangeAsync(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("add_range", _entityType, _provider);

        var result = await _inner.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: added => RepositoryActivitySource.Complete(activity, resultCount: added.Count),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("update_range", _entityType, _provider);

        var result = await _inner.UpdateRangeAsync(entities, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("delete_range", _entityType, _provider);

        var result = await _inner.DeleteRangeAsync(specification, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: count => { RepositoryActivitySource.Complete(activity, resultCount: count); },
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, Unit>> UpdateImmutableAsync(
        TEntity modified, CancellationToken cancellationToken = default)
    {
        var activity = RepositoryActivitySource.StartOperation("update_immutable", _entityType, _provider);

        var result = await _inner.UpdateImmutableAsync(modified, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => RepositoryActivitySource.Complete(activity),
            Left: e => RepositoryActivitySource.Failed(activity, null, e.Message));

        return result;
    }
}
