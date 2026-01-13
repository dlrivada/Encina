using System.Collections.Concurrent;
using JasperFx.Events;
using LanguageExt;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Marten.Projections;

/// <summary>
/// Marten-based implementation of the projection manager.
/// </summary>
public sealed class MartenProjectionManager : IProjectionManager
{
    private readonly IDocumentStore _store;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MartenProjectionManager> _logger;
    private readonly ProjectionRegistry _registry;
    private readonly ConcurrentDictionary<string, ProjectionStatus> _statuses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenProjectionManager"/> class.
    /// </summary>
    /// <param name="store">The Marten document store.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="registry">The projection registry.</param>
    public MartenProjectionManager(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        ILogger<MartenProjectionManager> logger,
        ProjectionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(registry);

        _store = store;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _registry = registry;

        InitializeStatuses();
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, long>> RebuildAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel
    {
        return await RebuildAsync<TReadModel>(new RebuildOptions(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, long>> RebuildAsync<TReadModel>(
        RebuildOptions options,
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel
    {
        ArgumentNullException.ThrowIfNull(options);

        var registration = _registry.GetProjectionForReadModel<TReadModel>();
        if (registration == null)
        {
            // NOSONAR S6966: LanguageExt Left is a pure function, not an async operation
            return Left<EncinaError, long>(
                EncinaErrors.Create(
                    ProjectionErrorCodes.NotRegistered,
                    $"No projection registered for read model {typeof(TReadModel).Name}."));
        }

        var projectionName = registration.ProjectionName;

        try
        {
            return await ExecuteRebuildAsync<TReadModel>(registration, options, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return HandleRebuildCancellation(projectionName);
        }
        catch (Exception ex)
        {
            return HandleRebuildException(projectionName, ex);
        }
    }

    private async Task<Either<EncinaError, long>> ExecuteRebuildAsync<TReadModel>(
        ProjectionRegistration registration,
        RebuildOptions options,
        CancellationToken cancellationToken)
        where TReadModel : class, IReadModel
    {
        var projectionName = registration.ProjectionName;

        ProjectionLog.StartingRebuild(_logger, projectionName);
        UpdateStatusForRebuildStart(projectionName);

        if (options.DeleteExisting)
        {
            await DeleteExistingReadModelsAsync<TReadModel>(cancellationToken).ConfigureAwait(false);
        }

        var totalEvents = await GetTotalEventCountAsync(cancellationToken).ConfigureAwait(false);

        if (totalEvents == 0)
        {
            return HandleNoEventsToRebuild(projectionName);
        }

        return await ProcessEventBatchesAsync(
            registration, options, totalEvents, cancellationToken).ConfigureAwait(false);
    }

    private void UpdateStatusForRebuildStart(string projectionName)
    {
        UpdateStatus(projectionName, status =>
        {
            status.State = ProjectionState.Rebuilding;
            status.IsRebuilding = true;
            status.RebuildProgressPercent = 0;
            status.StartedAtUtc = DateTime.UtcNow;
            status.ErrorMessage = null;
        });
    }

    private async Task DeleteExistingReadModelsAsync<TReadModel>(CancellationToken cancellationToken)
        where TReadModel : class, IReadModel
    {
        await using var deleteSession = _store.LightweightSession();
        deleteSession.DeleteWhere<TReadModel>(_ => true);
        await deleteSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> GetTotalEventCountAsync(CancellationToken cancellationToken)
    {
        await using var countSession = _store.QuerySession();
        return await countSession.Events
            .QueryAllRawEvents()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private Either<EncinaError, long> HandleNoEventsToRebuild(string projectionName)
    {
        UpdateStatus(projectionName, status =>
        {
            status.State = ProjectionState.Stopped;
            status.IsRebuilding = false;
            status.RebuildProgressPercent = 100;
            status.LastProcessedAtUtc = DateTime.UtcNow;
        });

        ProjectionLog.CompletedRebuild(_logger, projectionName, 0);
        return Right<EncinaError, long>(0); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    private async Task<Either<EncinaError, long>> ProcessEventBatchesAsync(
        ProjectionRegistration registration,
        RebuildOptions options,
        int totalEvents,
        CancellationToken cancellationToken)
    {
        var projectionName = registration.ProjectionName;
        long eventsProcessed = 0;
        long position = options.StartPosition;
        var endPosition = options.EndPosition ?? long.MaxValue;

        while (position < endPosition && !cancellationToken.IsCancellationRequested)
        {
            var batchResult = await ProcessSingleBatchAsync(
                registration, options.BatchSize, position, cancellationToken).ConfigureAwait(false);

            if (batchResult.EventCount == 0)
            {
                break;
            }

            if (batchResult.WasCancelled)
            {
                // NOSONAR S6966: LanguageExt Left is a pure function, not an async operation
                return Left<EncinaError, long>(
                    EncinaErrors.Create(ProjectionErrorCodes.Cancelled, "Rebuild was cancelled."));
            }

            position = batchResult.LastPosition;
            eventsProcessed += batchResult.EventCount;

            UpdateRebuildProgress(projectionName, position, eventsProcessed, totalEvents, options);
        }

        return CompleteRebuild(projectionName, position, eventsProcessed);
    }

    private async Task<BatchResult> ProcessSingleBatchAsync(
        ProjectionRegistration registration,
        int batchSize,
        long position,
        CancellationToken cancellationToken)
    {
        await using var session = _store.LightweightSession();
        var projection = _serviceProvider.GetRequiredService(registration.ProjectionType);

        var events = await session.Events
            .QueryAllRawEvents()
            .Where(e => e.Sequence > position)
            .OrderBy(e => e.Sequence)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (events.Count == 0)
        {
            return BatchResult.Empty;
        }

        foreach (var eventData in events)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return BatchResult.Cancelled;
            }

            await ProcessSingleEventAsync(session, projection, eventData, registration, cancellationToken)
                .ConfigureAwait(false);

            position = eventData.Sequence;
        }

        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new BatchResult(events.Count, position, false);
    }

    private static async Task ProcessSingleEventAsync(
        IDocumentSession session,
        object projection,
        IEvent eventData,
        ProjectionRegistration registration,
        CancellationToken cancellationToken)
    {
        var eventType = eventData.Data.GetType();
        var handlerInfo = registration.GetHandlerInfo(eventType);

        if (handlerInfo != null)
        {
            var context = CreateContext(eventData);
            await ApplyEventAsync(session, projection, eventData.Data, context, registration, handlerInfo.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void UpdateRebuildProgress(
        string projectionName,
        long position,
        long eventsProcessed,
        int totalEvents,
        RebuildOptions options)
    {
        var progressPercent = (int)((eventsProcessed * 100) / totalEvents);
        UpdateStatus(projectionName, status =>
        {
            status.RebuildProgressPercent = progressPercent;
            status.LastProcessedPosition = position;
            status.EventsProcessed = eventsProcessed;
            status.LastProcessedAtUtc = DateTime.UtcNow;
        });

        ProjectionLog.RebuildProgress(_logger, projectionName, progressPercent, eventsProcessed);
        options.OnProgress?.Invoke(progressPercent, eventsProcessed);
    }

    private Either<EncinaError, long> CompleteRebuild(string projectionName, long position, long eventsProcessed)
    {
        UpdateStatus(projectionName, status =>
        {
            status.State = ProjectionState.Stopped;
            status.IsRebuilding = false;
            status.RebuildProgressPercent = 100;
            status.LastProcessedPosition = position;
            status.EventsProcessed = eventsProcessed;
            status.LastProcessedAtUtc = DateTime.UtcNow;
        });

        ProjectionLog.CompletedRebuild(_logger, projectionName, eventsProcessed);
        return Right<EncinaError, long>(eventsProcessed); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    private Either<EncinaError, long> HandleRebuildCancellation(string projectionName)
    {
        UpdateStatus(projectionName, status =>
        {
            status.State = ProjectionState.Stopped;
            status.IsRebuilding = false;
            status.ErrorMessage = "Rebuild was cancelled.";
        });

        // NOSONAR S6966: LanguageExt Left is a pure function
        return Left<EncinaError, long>(
            EncinaErrors.Create(ProjectionErrorCodes.Cancelled, "Rebuild was cancelled."));
    }

    private Either<EncinaError, long> HandleRebuildException(string projectionName, Exception ex)
    {
        ProjectionLog.ErrorRebuild(_logger, ex, projectionName);

        UpdateStatus(projectionName, status =>
        {
            status.State = ProjectionState.Faulted;
            status.IsRebuilding = false;
            status.ErrorMessage = ex.Message;
        });

        // NOSONAR S6966: LanguageExt Left is a pure function
        return Left<EncinaError, long>(
            EncinaErrors.FromException(ProjectionErrorCodes.RebuildFailed, ex, $"Failed to rebuild projection {projectionName}."));
    }

    private readonly record struct BatchResult(int EventCount, long LastPosition, bool WasCancelled)
    {
        public static BatchResult Empty => new(0, 0, false);
        public static BatchResult Cancelled => new(0, 0, true);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, ProjectionStatus>> GetStatusAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel
    {
        var registration = _registry.GetProjectionForReadModel<TReadModel>();
        if (registration == null)
        {
            // NOSONAR S6966: LanguageExt Left is a pure function
            return Task.FromResult(Left<EncinaError, ProjectionStatus>(
                EncinaErrors.Create(
                    ProjectionErrorCodes.NotRegistered,
                    $"No projection registered for read model {typeof(TReadModel).Name}.")));
        }

        if (_statuses.TryGetValue(registration.ProjectionName, out var status))
        {
            return Task.FromResult(Right<EncinaError, ProjectionStatus>(status)); // NOSONAR S6966: LanguageExt Right is a pure function
        }

        // NOSONAR S6966: LanguageExt Left is a pure function
        return Task.FromResult(Left<EncinaError, ProjectionStatus>(
            EncinaErrors.Create(
                ProjectionErrorCodes.StatusFailed,
                $"Status not available for projection {registration.ProjectionName}.")));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, IReadOnlyDictionary<string, ProjectionStatus>>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ProjectionStatus>(_statuses);
        return Task.FromResult(Right<EncinaError, IReadOnlyDictionary<string, ProjectionStatus>>(result)); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> StartAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel
    {
        var registration = _registry.GetProjectionForReadModel<TReadModel>();
        if (registration == null)
        {
            // NOSONAR S6966: LanguageExt Left is a pure function
            return Task.FromResult(Left<EncinaError, Unit>(
                EncinaErrors.Create(
                    ProjectionErrorCodes.NotRegistered,
                    $"No projection registered for read model {typeof(TReadModel).Name}.")));
        }

        ProjectionLog.StartingProjection(_logger, registration.ProjectionName);

        UpdateStatus(registration.ProjectionName, status =>
        {
            status.State = ProjectionState.Running;
            status.StartedAtUtc = DateTime.UtcNow;
            status.ErrorMessage = null;
        });

        return Task.FromResult(Right<EncinaError, Unit>(Unit.Default)); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> StopAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel
    {
        var registration = _registry.GetProjectionForReadModel<TReadModel>();
        if (registration == null)
        {
            // NOSONAR S6966: LanguageExt Left is a pure function
            return Task.FromResult(Left<EncinaError, Unit>(
                EncinaErrors.Create(
                    ProjectionErrorCodes.NotRegistered,
                    $"No projection registered for read model {typeof(TReadModel).Name}.")));
        }

        ProjectionLog.StoppedProjection(_logger, registration.ProjectionName);

        UpdateStatus(registration.ProjectionName, status =>
        {
            status.State = ProjectionState.Stopped;
        });

        return Task.FromResult(Right<EncinaError, Unit>(Unit.Default)); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> PauseAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel
    {
        var registration = _registry.GetProjectionForReadModel<TReadModel>();
        if (registration == null)
        {
            // NOSONAR S6966: LanguageExt Left is a pure function
            return Task.FromResult(Left<EncinaError, Unit>(
                EncinaErrors.Create(
                    ProjectionErrorCodes.NotRegistered,
                    $"No projection registered for read model {typeof(TReadModel).Name}.")));
        }

        ProjectionLog.PausedProjection(_logger, registration.ProjectionName);

        UpdateStatus(registration.ProjectionName, status =>
        {
            status.State = ProjectionState.Paused;
        });

        return Task.FromResult(Right<EncinaError, Unit>(Unit.Default)); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> ResumeAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel
    {
        var registration = _registry.GetProjectionForReadModel<TReadModel>();
        if (registration == null)
        {
            // NOSONAR S6966: LanguageExt Left is a pure function
            return Task.FromResult(Left<EncinaError, Unit>(
                EncinaErrors.Create(
                    ProjectionErrorCodes.NotRegistered,
                    $"No projection registered for read model {typeof(TReadModel).Name}.")));
        }

        ProjectionLog.ResumedProjection(_logger, registration.ProjectionName);

        UpdateStatus(registration.ProjectionName, status =>
        {
            status.State = ProjectionState.Running;
        });

        return Task.FromResult(Right<EncinaError, Unit>(Unit.Default)); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    private void InitializeStatuses()
    {
        foreach (var registration in _registry.GetAllProjections())
        {
            _statuses[registration.ProjectionName] = new ProjectionStatus
            {
                ProjectionName = registration.ProjectionName,
                State = ProjectionState.Stopped,
            };
        }
    }

    private void UpdateStatus(string projectionName, Action<ProjectionStatus> update)
    {
        _statuses.AddOrUpdate(
            projectionName,
            _ =>
            {
                var status = new ProjectionStatus { ProjectionName = projectionName };
                update(status);
                return status;
            },
            (_, existing) =>
            {
                update(existing);
                return existing;
            });
    }

    private static ProjectionContext CreateContext(IEvent eventData)
    {
        return new ProjectionContext
        {
            StreamId = eventData.StreamId,
            SequenceNumber = eventData.Version,
            GlobalPosition = eventData.Sequence,
            Timestamp = eventData.Timestamp.UtcDateTime,
            EventType = eventData.EventTypeName,
        };
    }

    private static async Task ApplyEventAsync(
        IDocumentSession session,
        object projection,
        object domainEvent,
        ProjectionContext context,
        ProjectionRegistration registration,
        ProjectionHandlerInfo handlerInfo,
        CancellationToken cancellationToken)
    {
        // Load existing read model
        var existingReadModel = await LoadReadModelAsync(
            session,
            registration.ReadModelType,
            context.StreamId,
            cancellationToken).ConfigureAwait(false);

        switch (handlerInfo.HandlerType)
        {
            case ProjectionHandlerType.Creator:
                if (existingReadModel == null)
                {
                    var created = handlerInfo.InvokeCreate(projection, domainEvent, context);
                    StoreReadModel(session, created);
                }

                break;

            case ProjectionHandlerType.Handler:
                if (existingReadModel != null)
                {
                    var updated = handlerInfo.InvokeApply(projection, domainEvent, existingReadModel, context);
                    StoreReadModel(session, updated);
                }

                break;

            case ProjectionHandlerType.Deleter:
                if (existingReadModel != null)
                {
                    var shouldDelete = handlerInfo.InvokeShouldDelete(projection, domainEvent, existingReadModel, context);
                    if (shouldDelete)
                    {
                        DeleteReadModel(session, registration.ReadModelType, context.StreamId);
                    }
                }

                break;
        }
    }

    private static async Task<object?> LoadReadModelAsync(
        IDocumentSession session,
        Type readModelType,
        Guid id,
        CancellationToken cancellationToken)
    {
        var loadMethod = typeof(IDocumentSession).GetMethod(
            nameof(IDocumentSession.LoadAsync),
            [typeof(Guid), typeof(CancellationToken)]);

        var genericLoadMethod = loadMethod!.MakeGenericMethod(readModelType);

        var task = (Task)genericLoadMethod.Invoke(session, [id, cancellationToken])!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty!.GetValue(task);
    }

    private static void StoreReadModel(IDocumentSession session, object readModel)
    {
        var storeMethod = typeof(IDocumentSession).GetMethod(nameof(IDocumentSession.Store))!;
        var genericStoreMethod = storeMethod.MakeGenericMethod(readModel.GetType());
        genericStoreMethod.Invoke(session, [new[] { readModel }]);
    }

    private static void DeleteReadModel(IDocumentSession session, Type readModelType, Guid id)
    {
        var deleteMethod = typeof(IDocumentSession).GetMethods()
            .First(m => m.Name == nameof(IDocumentSession.Delete) && m.IsGenericMethod);

        var genericDeleteMethod = deleteMethod.MakeGenericMethod(readModelType);
        genericDeleteMethod.Invoke(session, [id]);
    }
}
