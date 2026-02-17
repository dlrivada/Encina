using Encina.DomainModeling;
using Encina.DomainModeling.Diagnostics;
using LanguageExt;

namespace Encina.OpenTelemetry.UnitOfWork;

/// <summary>
/// Decorator that adds OpenTelemetry tracing to any <see cref="IUnitOfWork"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// This decorator wraps any Unit of Work implementation and adds distributed tracing
/// via <see cref="UnitOfWorkActivitySource"/>. All instrumentation is zero-cost when
/// no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal sealed class InstrumentedUnitOfWork : IUnitOfWork
{
    private readonly IUnitOfWork _inner;

    public InstrumentedUnitOfWork(IUnitOfWork inner)
    {
        _inner = inner;
    }

    public bool HasActiveTransaction => _inner.HasActiveTransaction;

    public IFunctionalRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class
        where TId : notnull
    {
        return _inner.Repository<TEntity, TId>();
    }

    public async Task<Either<EncinaError, int>> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var activity = UnitOfWorkActivitySource.StartSaveChanges();

        var result = await _inner.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: rows => UnitOfWorkActivitySource.CompleteSaveChanges(activity, rows),
            Left: e => UnitOfWorkActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, Unit>> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var activity = UnitOfWorkActivitySource.StartTransaction();

        var result = await _inner.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => UnitOfWorkActivitySource.CompleteTransaction(activity, "started"),
            Left: e => UnitOfWorkActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task<Either<EncinaError, Unit>> CommitAsync(
        CancellationToken cancellationToken = default)
    {
        var activity = UnitOfWorkActivitySource.StartTransaction();

        var result = await _inner.CommitAsync(cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => UnitOfWorkActivitySource.CompleteTransaction(activity, "committed"),
            Left: e => UnitOfWorkActivitySource.Failed(activity, null, e.Message));

        return result;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        var activity = UnitOfWorkActivitySource.StartTransaction();

        await _inner.RollbackAsync(cancellationToken).ConfigureAwait(false);

        UnitOfWorkActivitySource.CompleteTransaction(activity, "rolledback");
    }

    public Either<EncinaError, Unit> UpdateImmutable<TEntity>(TEntity modified)
        where TEntity : class
    {
        return _inner.UpdateImmutable(modified);
    }

    public Task<Either<EncinaError, Unit>> UpdateImmutableAsync<TEntity>(
        TEntity modified, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return _inner.UpdateImmutableAsync(modified, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _inner.DisposeAsync();
    }
}
