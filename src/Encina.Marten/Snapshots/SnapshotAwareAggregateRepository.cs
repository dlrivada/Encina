using Encina.DomainModeling;
using LanguageExt;
using Marten;
using Marten.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Marten.Snapshots;

/// <summary>
/// Aggregate repository that uses snapshots to optimize loading of aggregates
/// with long event streams. Falls back to full event replay when no snapshot exists.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type (must implement <see cref="ISnapshotable{TAggregate}"/>).</typeparam>
public sealed class SnapshotAwareAggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
    where TAggregate : class, IAggregate, ISnapshotable<TAggregate>, new()
{
    private readonly IDocumentSession _session;
    private readonly ISnapshotStore<TAggregate> _snapshotStore;
    private readonly ILogger<SnapshotAwareAggregateRepository<TAggregate>> _logger;
    private readonly EncinaMartenOptions _options;
    private readonly AggregateSnapshotConfig _snapshotConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotAwareAggregateRepository{TAggregate}"/> class.
    /// </summary>
    /// <param name="session">The Marten document session.</param>
    /// <param name="snapshotStore">The snapshot store.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public SnapshotAwareAggregateRepository(
        IDocumentSession session,
        ISnapshotStore<TAggregate> snapshotStore,
        ILogger<SnapshotAwareAggregateRepository<TAggregate>> logger,
        IOptions<EncinaMartenOptions> options)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _session = session;
        _snapshotStore = snapshotStore;
        _logger = logger;
        _options = options.Value;
        _snapshotConfig = _options.Snapshots.GetConfigFor<TAggregate>();
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TAggregate>> LoadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to load from snapshot first
            var snapshotResult = await _snapshotStore.GetLatestAsync(id, cancellationToken)
                .ConfigureAwait(false);

            return await snapshotResult.MatchAsync(
                async snapshot => await LoadWithSnapshotAsync(id, snapshot, null, cancellationToken)
                    .ConfigureAwait(false),
                error => Task.FromResult(Left<EncinaError, TAggregate>(error)))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.ErrorLoadingAggregate(_logger, ex, typeof(TAggregate).Name, id);

            return Left<EncinaError, TAggregate>(
                EncinaErrors.FromException(
                    MartenErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load aggregate {typeof(TAggregate).Name} with ID {id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TAggregate>> LoadAsync(
        Guid id,
        int version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to load snapshot at or before the requested version
            var snapshotResult = await _snapshotStore.GetAtVersionAsync(id, version, cancellationToken)
                .ConfigureAwait(false);

            return await snapshotResult.MatchAsync(
                async snapshot => await LoadWithSnapshotAsync(id, snapshot, version, cancellationToken)
                    .ConfigureAwait(false),
                error => Task.FromResult(Left<EncinaError, TAggregate>(error)))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TAggregate>(
                EncinaErrors.FromException(
                    MartenErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load aggregate {typeof(TAggregate).Name} with ID {id} at version {version}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> SaveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        try
        {
            var uncommittedEvents = aggregate.UncommittedEvents;
            if (uncommittedEvents.Count == 0)
            {
                Log.NoUncommittedEvents(_logger, typeof(TAggregate).Name, aggregate.Id);
                return Right<EncinaError, Unit>(Unit.Default);
            }

            Log.SavingEvents(_logger, uncommittedEvents.Count, typeof(TAggregate).Name, aggregate.Id);

            // Append events to the stream
            var expectedVersion = aggregate.Version - uncommittedEvents.Count;
            _session.Events.Append(aggregate.Id, expectedVersion, uncommittedEvents.ToArray());

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.SavedEvents(_logger, uncommittedEvents.Count, typeof(TAggregate).Name, aggregate.Id);

            // Check if we should create a snapshot
            await TryCreateSnapshotAsync(aggregate, cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            Log.ConcurrencyConflict(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            if (_options.ThrowOnConcurrencyConflict)
            {
                throw;
            }

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    MartenErrorCodes.ConcurrencyConflict,
                    ex,
                    $"Concurrency conflict saving aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
        catch (Exception ex) when (!IsConcurrencyException(ex))
        {
            Log.ErrorSavingAggregate(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    MartenErrorCodes.SaveFailed,
                    ex,
                    $"Failed to save aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> CreateAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        try
        {
            var uncommittedEvents = aggregate.UncommittedEvents;
            if (uncommittedEvents.Count == 0)
            {
                return Left<EncinaError, Unit>(
                    EncinaErrors.Create(
                        MartenErrorCodes.NoEventsToCreate,
                        "Cannot create aggregate without any events."));
            }

            Log.CreatingAggregate(_logger, typeof(TAggregate).Name, aggregate.Id, uncommittedEvents.Count);

            // Start a new stream
            _session.Events.StartStream<TAggregate>(aggregate.Id, uncommittedEvents.ToArray());

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.CreatedAggregate(_logger, typeof(TAggregate).Name, aggregate.Id);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex) when (IsStreamCollisionException(ex))
        {
            Log.StreamAlreadyExists(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    MartenErrorCodes.StreamAlreadyExists,
                    ex,
                    $"Stream already exists for aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
        catch (Exception ex) when (!IsStreamCollisionException(ex))
        {
            Log.ErrorCreatingAggregate(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    MartenErrorCodes.CreateFailed,
                    ex,
                    $"Failed to create aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
    }

    /// <summary>
    /// Loads an aggregate using a snapshot (if available) plus remaining events.
    /// </summary>
    private async Task<Either<EncinaError, TAggregate>> LoadWithSnapshotAsync(
        Guid id,
        Snapshot<TAggregate>? snapshot,
        int? targetVersion,
        CancellationToken cancellationToken)
    {
        if (snapshot is null)
        {
            // No snapshot, load all events from the beginning
            Log.LoadingAggregate(_logger, typeof(TAggregate).Name, id);

            TAggregate? aggregate;
            if (targetVersion.HasValue)
            {
                aggregate = await _session.Events.AggregateStreamAsync<TAggregate>(
                    id,
                    version: targetVersion.Value,
                    token: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                aggregate = await _session.Events.AggregateStreamAsync<TAggregate>(
                    id,
                    token: cancellationToken).ConfigureAwait(false);
            }

            if (aggregate is null)
            {
                Log.AggregateNotFound(_logger, typeof(TAggregate).Name, id);

                return Left<EncinaError, TAggregate>(
                    EncinaErrors.Create(
                        MartenErrorCodes.AggregateNotFound,
                        $"Aggregate {typeof(TAggregate).Name} with ID {id} was not found."));
            }

            Log.LoadedAggregate(_logger, typeof(TAggregate).Name, id, aggregate.Version);

            return Right<EncinaError, TAggregate>(aggregate);
        }

        // We have a snapshot, start from there
        SnapshotLog.LoadingFromSnapshot(_logger, typeof(TAggregate).Name, id, snapshot.Version);

        // Fetch events after the snapshot version
        var fromVersion = snapshot.Version + 1;
        var toVersion = targetVersion ?? long.MaxValue;

        var events = await _session.Events.FetchStreamAsync(
            id,
            version: fromVersion,
            token: cancellationToken).ConfigureAwait(false);

        // Filter to target version if specified
        var eventsToApply = targetVersion.HasValue
            ? events.Where(e => e.Version <= targetVersion.Value).ToList()
            : events.ToList();

        if (eventsToApply.Count > 0)
        {
            SnapshotLog.ReplayingEventsAfterSnapshot(_logger, eventsToApply.Count, typeof(TAggregate).Name, id);
        }

        // Start with the snapshot state and apply remaining events
        var restoredAggregate = snapshot.State;

        foreach (var @event in eventsToApply)
        {
            ApplyEvent(restoredAggregate, @event.Data);
        }

        SnapshotLog.LoadedFromSnapshotWithEvents(
            _logger,
            typeof(TAggregate).Name,
            id,
            snapshot.Version,
            eventsToApply.Count,
            restoredAggregate.Version);

        return Right<EncinaError, TAggregate>(restoredAggregate);
    }

    /// <summary>
    /// Applies an event to the aggregate using reflection to invoke the protected Apply method.
    /// </summary>
    private static void ApplyEvent(TAggregate aggregate, object @event)
    {
        // Access the protected Apply method via the RaiseEvent mechanism
        // Since we're replaying, we need to use reflection to call Apply
        var applyMethod = typeof(TAggregate).GetMethod(
            "Apply",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(object)],
            null);

        applyMethod?.Invoke(aggregate, [@event]);

        // Increment version manually since we're not going through RaiseEvent
        var versionProperty = typeof(AggregateBase).GetProperty(
            "Version",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (versionProperty?.SetMethod is not null)
        {
            var currentVersion = (int)versionProperty.GetValue(aggregate)!;
            versionProperty.SetValue(aggregate, currentVersion + 1);
        }
    }

    /// <summary>
    /// Creates a snapshot if the aggregate has crossed the snapshot threshold.
    /// </summary>
    private async Task TryCreateSnapshotAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        var snapshotEvery = _snapshotConfig.SnapshotEvery;

        SnapshotLog.CheckingSnapshotCreation(_logger, typeof(TAggregate).Name, aggregate.Id, aggregate.Version);

        // Check if we should create a snapshot
        // We create a snapshot when version is a multiple of snapshotEvery
        if (aggregate.Version % snapshotEvery != 0)
        {
            SnapshotLog.SnapshotNotNeeded(
                _logger,
                typeof(TAggregate).Name,
                aggregate.Id,
                aggregate.Version,
                snapshotEvery);
            return;
        }

        SnapshotLog.SnapshotThresholdReached(
            _logger,
            typeof(TAggregate).Name,
            aggregate.Id,
            aggregate.Version,
            snapshotEvery);

        try
        {
            var snapshot = new Snapshot<TAggregate>(
                aggregate.Id,
                aggregate.Version,
                aggregate,
                DateTime.UtcNow);

            if (_options.Snapshots.AsyncSnapshotCreation)
            {
                // Fire and forget - snapshot creation happens in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _snapshotStore.SaveAsync(snapshot, CancellationToken.None)
                            .ConfigureAwait(false);

                        // Prune old snapshots
                        await _snapshotStore.PruneAsync(
                            aggregate.Id,
                            _snapshotConfig.KeepSnapshots,
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        SnapshotLog.ErrorCreatingAutomaticSnapshot(
                            _logger,
                            ex,
                            typeof(TAggregate).Name,
                            aggregate.Id);
                    }
                }, CancellationToken.None);
            }
            else
            {
                // Synchronous snapshot creation
                await _snapshotStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);

                // Prune old snapshots
                await _snapshotStore.PruneAsync(
                    aggregate.Id,
                    _snapshotConfig.KeepSnapshots,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Snapshot creation failure should not fail the save operation
            SnapshotLog.ErrorCreatingAutomaticSnapshot(
                _logger,
                ex,
                typeof(TAggregate).Name,
                aggregate.Id);
        }
    }

    /// <summary>
    /// Determines if the exception is a concurrency-related exception.
    /// </summary>
    private static bool IsConcurrencyException(Exception ex)
    {
        var typeName = ex.GetType().Name;
        return typeName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("UnexpectedMaxEventId", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("EventStreamVersion", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the exception is a stream collision exception.
    /// </summary>
    private static bool IsStreamCollisionException(Exception ex)
    {
        var typeName = ex.GetType().Name;
        return typeName.Contains("StreamIdCollision", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("ExistingStream", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("DuplicateStream", StringComparison.OrdinalIgnoreCase);
    }
}
