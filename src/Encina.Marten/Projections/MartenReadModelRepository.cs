using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Marten.Projections;

/// <summary>
/// Marten-based implementation of the read model repository.
/// </summary>
/// <typeparam name="TReadModel">The type of read model.</typeparam>
public sealed class MartenReadModelRepository<TReadModel> : IReadModelRepository<TReadModel>
    where TReadModel : class, IReadModel
{
    private readonly IDocumentSession _session;
    private readonly ILogger<MartenReadModelRepository<TReadModel>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenReadModelRepository{TReadModel}"/> class.
    /// </summary>
    /// <param name="session">The Marten document session.</param>
    /// <param name="logger">The logger instance.</param>
    public MartenReadModelRepository(
        IDocumentSession session,
        ILogger<MartenReadModelRepository<TReadModel>> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TReadModel>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProjectionLog.LoadingReadModel(_logger, typeof(TReadModel).Name, id);

            var readModel = await _session.LoadAsync<TReadModel>(id, cancellationToken)
                .ConfigureAwait(false);

            if (readModel is null)
            {
                ProjectionLog.ReadModelNotFound(_logger, typeof(TReadModel).Name, id);

                return Left<EncinaError, TReadModel>(
                    EncinaErrors.Create(
                        ProjectionErrorCodes.ReadModelNotFound,
                        $"Read model {typeof(TReadModel).Name} with ID {id} was not found."));
            }

            ProjectionLog.LoadedReadModel(_logger, typeof(TReadModel).Name, id);

            return Right<EncinaError, TReadModel>(readModel);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorLoadingReadModel(_logger, ex, typeof(TReadModel).Name, id);

            return Left<EncinaError, TReadModel>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.QueryFailed,
                    ex,
                    $"Failed to load read model {typeof(TReadModel).Name} with ID {id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<TReadModel>>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        try
        {
            var idList = ids.ToList();
            ProjectionLog.LoadingReadModels(_logger, typeof(TReadModel).Name, idList.Count);

            var readModels = await _session.LoadManyAsync<TReadModel>(cancellationToken, [.. idList])
                .ConfigureAwait(false);

            var result = readModels.Where(r => r is not null).ToList()!;

            ProjectionLog.LoadedReadModels(_logger, typeof(TReadModel).Name, result.Count, idList.Count);

            return Right<EncinaError, IReadOnlyList<TReadModel>>(result);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorLoadingReadModels(_logger, ex, typeof(TReadModel).Name);

            return Left<EncinaError, IReadOnlyList<TReadModel>>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.QueryFailed,
                    ex,
                    $"Failed to load read models of type {typeof(TReadModel).Name}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<TReadModel>>> QueryAsync(
        Func<IQueryable<TReadModel>, IQueryable<TReadModel>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        try
        {
            ProjectionLog.QueryingReadModels(_logger, typeof(TReadModel).Name);

            var query = _session.Query<TReadModel>();
            var filteredQuery = predicate(query);
            var results = await filteredQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            ProjectionLog.QueriedReadModels(_logger, typeof(TReadModel).Name, results.Count);

            return Right<EncinaError, IReadOnlyList<TReadModel>>(results);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorQueryingReadModels(_logger, ex, typeof(TReadModel).Name);

            return Left<EncinaError, IReadOnlyList<TReadModel>>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.QueryFailed,
                    ex,
                    $"Failed to query read models of type {typeof(TReadModel).Name}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> StoreAsync(
        TReadModel readModel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(readModel);

        try
        {
            ProjectionLog.StoringReadModel(_logger, typeof(TReadModel).Name, readModel.Id);

            _session.Store(readModel);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            ProjectionLog.StoredReadModel(_logger, typeof(TReadModel).Name, readModel.Id);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorStoringReadModel(_logger, ex, typeof(TReadModel).Name, readModel.Id);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.StoreFailed,
                    ex,
                    $"Failed to store read model {typeof(TReadModel).Name} with ID {readModel.Id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> StoreManyAsync(
        IEnumerable<TReadModel> readModels,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(readModels);

        try
        {
            var modelList = readModels.ToList();
            ProjectionLog.StoringReadModels(_logger, typeof(TReadModel).Name, modelList.Count);

            _session.Store<TReadModel>([.. modelList]);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            ProjectionLog.StoredReadModels(_logger, typeof(TReadModel).Name, modelList.Count);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorStoringReadModels(_logger, ex, typeof(TReadModel).Name);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.StoreFailed,
                    ex,
                    $"Failed to store read models of type {typeof(TReadModel).Name}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProjectionLog.DeletingReadModel(_logger, typeof(TReadModel).Name, id);

            _session.Delete<TReadModel>(id);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            ProjectionLog.DeletedReadModel(_logger, typeof(TReadModel).Name, id);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorDeletingReadModel(_logger, ex, typeof(TReadModel).Name, id);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.DeleteFailed,
                    ex,
                    $"Failed to delete read model {typeof(TReadModel).Name} with ID {id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, long>> DeleteAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProjectionLog.DeletingAllReadModels(_logger, typeof(TReadModel).Name);

            // Count before deleting
            var count = await _session.Query<TReadModel>()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            _session.DeleteWhere<TReadModel>(_ => true);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            ProjectionLog.DeletedAllReadModels(_logger, typeof(TReadModel).Name, count);

            return Right<EncinaError, long>(count);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorDeletingAllReadModels(_logger, ex, typeof(TReadModel).Name);

            return Left<EncinaError, long>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.DeleteFailed,
                    ex,
                    $"Failed to delete all read models of type {typeof(TReadModel).Name}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _session.Query<TReadModel>()
                .AnyAsync(r => r.Id == id, cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(exists);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.QueryFailed,
                    ex,
                    $"Failed to check existence of read model {typeof(TReadModel).Name} with ID {id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, long>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _session.Query<TReadModel>()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, long>(count);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, long>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.QueryFailed,
                    ex,
                    $"Failed to count read models of type {typeof(TReadModel).Name}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, long>> CountAsync(
        Func<IQueryable<TReadModel>, IQueryable<TReadModel>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        try
        {
            var query = _session.Query<TReadModel>();
            var filteredQuery = predicate(query);
            var count = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, long>(count);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, long>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.QueryFailed,
                    ex,
                    $"Failed to count read models of type {typeof(TReadModel).Name} with predicate."));
        }
    }
}
