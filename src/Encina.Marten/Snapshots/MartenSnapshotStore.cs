using Encina.DomainModeling;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Marten.Snapshots;

/// <summary>
/// Marten-based implementation of the snapshot store.
/// Uses Marten's document storage to persist snapshots as JSON documents.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public sealed class MartenSnapshotStore<TAggregate> : ISnapshotStore<TAggregate>
    where TAggregate : class, IAggregate, ISnapshotable<TAggregate>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<MartenSnapshotStore<TAggregate>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenSnapshotStore{TAggregate}"/> class.
    /// </summary>
    /// <param name="session">The Marten document session.</param>
    /// <param name="logger">The logger instance.</param>
    public MartenSnapshotStore(
        IDocumentSession session,
        ILogger<MartenSnapshotStore<TAggregate>> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<Snapshot<TAggregate>>>> GetLatestAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SnapshotLog.LoadingLatestSnapshot(_logger, typeof(TAggregate).Name, aggregateId);

            // Use ToListAsync with Take(1) instead of FirstOrDefaultAsync
            // to avoid Marten's LINQ compilation issues with generic types
            var envelopes = await _session
                .Query<SnapshotEnvelope<TAggregate>>()
                .Where(s => s.AggregateId == aggregateId)
                .OrderByDescending(s => s.Version)
                .Take(1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (envelopes.Count == 0)
            {
                SnapshotLog.NoSnapshotFound(_logger, typeof(TAggregate).Name, aggregateId);
                return Right<EncinaError, Option<Snapshot<TAggregate>>>(Option<Snapshot<TAggregate>>.None); // NOSONAR S6966
            }

            var snapshot = envelopes[0].ToSnapshot();
            SnapshotLog.LoadedSnapshot(_logger, typeof(TAggregate).Name, aggregateId, snapshot.Version);

            return Right<EncinaError, Option<Snapshot<TAggregate>>>(Option<Snapshot<TAggregate>>.Some(snapshot)); // NOSONAR S6966
        }
        catch (Exception ex)
        {
            SnapshotLog.ErrorLoadingSnapshot(_logger, ex, typeof(TAggregate).Name, aggregateId);

            return Left<EncinaError, Option<Snapshot<TAggregate>>>( // NOSONAR S6966
                EncinaErrors.FromException(
                    SnapshotErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load snapshot for aggregate {typeof(TAggregate).Name} with ID {aggregateId}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<Snapshot<TAggregate>>>> GetAtVersionAsync(
        Guid aggregateId,
        int maxVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SnapshotLog.LoadingSnapshotAtVersion(_logger, typeof(TAggregate).Name, aggregateId, maxVersion);

            // Use ToListAsync with Take(1) instead of FirstOrDefaultAsync
            // to avoid Marten's LINQ compilation issues with generic types
            var envelopes = await _session
                .Query<SnapshotEnvelope<TAggregate>>()
                .Where(s => s.AggregateId == aggregateId && s.Version <= maxVersion)
                .OrderByDescending(s => s.Version)
                .Take(1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (envelopes.Count == 0)
            {
                return Right<EncinaError, Option<Snapshot<TAggregate>>>(Option<Snapshot<TAggregate>>.None); // NOSONAR S6966
            }

            return Right<EncinaError, Option<Snapshot<TAggregate>>>(Option<Snapshot<TAggregate>>.Some(envelopes[0].ToSnapshot())); // NOSONAR S6966
        }
        catch (Exception ex)
        {
            SnapshotLog.ErrorLoadingSnapshot(_logger, ex, typeof(TAggregate).Name, aggregateId);

            return Left<EncinaError, Option<Snapshot<TAggregate>>>( // NOSONAR S6966
                EncinaErrors.FromException(
                    SnapshotErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load snapshot at version {maxVersion} for aggregate {typeof(TAggregate).Name} with ID {aggregateId}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> SaveAsync(
        Snapshot<TAggregate> snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        try
        {
            SnapshotLog.SavingSnapshot(_logger, typeof(TAggregate).Name, snapshot.AggregateId, snapshot.Version);

            var envelope = SnapshotEnvelope.Create(snapshot);
            _session.Store(envelope);

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            SnapshotLog.SavedSnapshot(_logger, typeof(TAggregate).Name, snapshot.AggregateId, snapshot.Version);

            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function, not an async operation
        }
        catch (Exception ex)
        {
            SnapshotLog.ErrorSavingSnapshot(_logger, ex, typeof(TAggregate).Name, snapshot.AggregateId);

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    SnapshotErrorCodes.SaveFailed,
                    ex,
                    $"Failed to save snapshot for aggregate {typeof(TAggregate).Name} with ID {snapshot.AggregateId}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> PruneAsync(
        Guid aggregateId,
        int keepCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keepCount);

        try
        {
            SnapshotLog.PruningSnapshots(_logger, typeof(TAggregate).Name, aggregateId, keepCount);

            // Get all snapshots ordered by version descending
            var allSnapshots = await _session
                .Query<SnapshotEnvelope<TAggregate>>()
                .Where(s => s.AggregateId == aggregateId)
                .OrderByDescending(s => s.Version)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Skip the ones to keep and delete the rest
            var toDelete = allSnapshots.Skip(keepCount).ToList();

            if (toDelete.Count == 0)
            {
                return Right<EncinaError, int>(0); // NOSONAR S6966: LanguageExt Right is a pure function, not an async operation
            }

            foreach (var snapshot in toDelete)
            {
                _session.Delete(snapshot);
            }

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            SnapshotLog.PrunedSnapshots(_logger, typeof(TAggregate).Name, aggregateId, toDelete.Count);

            return Right<EncinaError, int>(toDelete.Count); // NOSONAR S6966: LanguageExt Right is a pure function, not an async operation
        }
        catch (Exception ex)
        {
            SnapshotLog.ErrorPruningSnapshots(_logger, ex, typeof(TAggregate).Name, aggregateId);

            return Left<EncinaError, int>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    SnapshotErrorCodes.PruneFailed,
                    ex,
                    $"Failed to prune snapshots for aggregate {typeof(TAggregate).Name} with ID {aggregateId}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteAllAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SnapshotLog.DeletingAllSnapshots(_logger, typeof(TAggregate).Name, aggregateId);

            var snapshots = await _session
                .Query<SnapshotEnvelope<TAggregate>>()
                .Where(s => s.AggregateId == aggregateId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (snapshots.Count == 0)
            {
                return Right<EncinaError, int>(0); // NOSONAR S6966: LanguageExt Right is a pure function, not an async operation
            }

            foreach (var snapshot in snapshots)
            {
                _session.Delete(snapshot);
            }

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            SnapshotLog.DeletedAllSnapshots(_logger, typeof(TAggregate).Name, aggregateId, snapshots.Count);

            return Right<EncinaError, int>(snapshots.Count); // NOSONAR S6966: LanguageExt Right is a pure function, not an async operation
        }
        catch (Exception ex)
        {
            SnapshotLog.ErrorDeletingSnapshots(_logger, ex, typeof(TAggregate).Name, aggregateId);

            return Left<EncinaError, int>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    SnapshotErrorCodes.DeleteFailed,
                    ex,
                    $"Failed to delete snapshots for aggregate {typeof(TAggregate).Name} with ID {aggregateId}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> ExistsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _session
                .Query<SnapshotEnvelope<TAggregate>>()
                .AnyAsync(s => s.AggregateId == aggregateId, cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(exists); // NOSONAR S6966: LanguageExt Right is a pure function, not an async operation
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    SnapshotErrorCodes.LoadFailed,
                    ex,
                    $"Failed to check snapshot existence for aggregate {typeof(TAggregate).Name} with ID {aggregateId}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> CountAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _session
                .Query<SnapshotEnvelope<TAggregate>>()
                .CountAsync(s => s.AggregateId == aggregateId, cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>(count); // NOSONAR S6966: LanguageExt Right is a pure function, not an async operation
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    SnapshotErrorCodes.LoadFailed,
                    ex,
                    $"Failed to count snapshots for aggregate {typeof(TAggregate).Name} with ID {aggregateId}."));
        }
    }
}
