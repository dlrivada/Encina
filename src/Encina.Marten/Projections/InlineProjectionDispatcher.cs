using LanguageExt;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Marten.Projections;

/// <summary>
/// Dispatches events to inline projections for immediate read model updates.
/// </summary>
/// <remarks>
/// <para>
/// Inline projections are processed synchronously during command execution.
/// This ensures read models are immediately consistent with the event stream.
/// </para>
/// <para>
/// <b>Trade-offs</b>:
/// <list type="bullet">
/// <item><description><b>Pros</b>: Immediate consistency, simpler mental model</description></item>
/// <item><description><b>Cons</b>: Increased command latency, coupled failure modes</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IInlineProjectionDispatcher
{
    /// <summary>
    /// Applies an event to all registered projections that handle it.
    /// </summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <param name="context">The projection context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success; otherwise, an error.</returns>
    Task<Either<EncinaError, Unit>> DispatchAsync(
        object domainEvent,
        ProjectionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies multiple events to all registered projections.
    /// </summary>
    /// <param name="events">The events with their contexts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success; otherwise, an error.</returns>
    Task<Either<EncinaError, Unit>> DispatchManyAsync(
        IEnumerable<(object Event, ProjectionContext Context)> events,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Marten-based implementation of the inline projection dispatcher.
/// </summary>
public sealed class MartenInlineProjectionDispatcher : IInlineProjectionDispatcher
{
    private readonly IDocumentSession _session;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MartenInlineProjectionDispatcher> _logger;
    private readonly ProjectionRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenInlineProjectionDispatcher"/> class.
    /// </summary>
    /// <param name="session">The Marten document session.</param>
    /// <param name="serviceProvider">The service provider for resolving projections.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="registry">The projection registry.</param>
    public MartenInlineProjectionDispatcher(
        IDocumentSession session,
        IServiceProvider serviceProvider,
        ILogger<MartenInlineProjectionDispatcher> logger,
        ProjectionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(registry);

        _session = session;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _registry = registry;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DispatchAsync(
        object domainEvent,
        ProjectionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(context);

        var eventType = domainEvent.GetType();
        var registrations = _registry.GetProjectionsForEvent(eventType);

        foreach (var registration in registrations)
        {
            var result = await ApplyEventToProjectionAsync(
                domainEvent,
                eventType,
                context,
                registration,
                cancellationToken).ConfigureAwait(false);

            if (result.IsLeft)
            {
                return result;
            }
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DispatchManyAsync(
        IEnumerable<(object Event, ProjectionContext Context)> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var (eventData, context) in events)
        {
            var result = await DispatchAsync(eventData, context, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsLeft)
            {
                return result;
            }
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }

    private async Task<Either<EncinaError, Unit>> ApplyEventToProjectionAsync(
        object @event,
        Type eventType,
        ProjectionContext context,
        ProjectionRegistration registration,
        CancellationToken cancellationToken)
    {
        try
        {
            var projectionName = registration.ProjectionName;
            ProjectionLog.DispatchingEvent(_logger, eventType.Name, projectionName);

            // Get the projection instance
            var projection = _serviceProvider.GetRequiredService(registration.ProjectionType);

            // Load existing read model if it exists
            var existingReadModel = await LoadReadModelAsync(
                registration.ReadModelType,
                context.StreamId,
                cancellationToken).ConfigureAwait(false);

            // Determine if this is a create, update, or delete operation
            var handlerInfo = registration.GetHandlerInfo(eventType);

            if (handlerInfo == null)
            {
                ProjectionLog.NoHandlerForEvent(_logger, eventType.Name, projectionName);
                return Right<EncinaError, Unit>(Unit.Default);
            }

            object? resultReadModel = null;
            var shouldDelete = false;

            switch (handlerInfo.Value.HandlerType)
            {
                case ProjectionHandlerType.Creator:
                    if (existingReadModel != null)
                    {
                        // Already exists, skip creation
                        return Right<EncinaError, Unit>(Unit.Default);
                    }

                    resultReadModel = handlerInfo.Value.InvokeCreate(projection, @event, context);
                    ProjectionLog.CreatedReadModel(
                        _logger,
                        registration.ReadModelType.Name,
                        context.StreamId,
                        eventType.Name);
                    break;

                case ProjectionHandlerType.Handler:
                    if (existingReadModel == null)
                    {
                        // No existing read model, nothing to update
                        return Right<EncinaError, Unit>(Unit.Default);
                    }

                    resultReadModel = handlerInfo.Value.InvokeApply(projection, @event, existingReadModel, context);
                    ProjectionLog.AppliedEvent(
                        _logger,
                        eventType.Name,
                        registration.ReadModelType.Name,
                        context.StreamId);
                    break;

                case ProjectionHandlerType.Deleter:
                    if (existingReadModel != null)
                    {
                        shouldDelete = handlerInfo.Value.InvokeShouldDelete(projection, @event, existingReadModel, context);
                        if (shouldDelete)
                        {
                            ProjectionLog.DeletedReadModelFromEvent(
                                _logger,
                                registration.ReadModelType.Name,
                                context.StreamId,
                                eventType.Name);
                        }
                    }

                    break;
            }

            // Persist the changes
            if (shouldDelete)
            {
                await DeleteReadModelAsync(registration.ReadModelType, context.StreamId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (resultReadModel != null)
            {
                await StoreReadModelAsync(resultReadModel, cancellationToken)
                    .ConfigureAwait(false);
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorApplyingEvent(_logger, ex, eventType.Name, registration.ProjectionName);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    ProjectionErrorCodes.ApplyFailed,
                    ex,
                    $"Failed to apply event {eventType.Name} to projection {registration.ProjectionName}."));
        }
    }

    private async Task<object?> LoadReadModelAsync(
        Type readModelType,
        Guid id,
        CancellationToken cancellationToken)
    {
        // Use reflection to call LoadAsync<T> on the session
        var loadMethod = typeof(IDocumentSession).GetMethod(
            nameof(IDocumentSession.LoadAsync),
            [typeof(Guid), typeof(CancellationToken)]);

        var genericLoadMethod = loadMethod!.MakeGenericMethod(readModelType);

        var task = (Task)genericLoadMethod.Invoke(_session, [id, cancellationToken])!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty!.GetValue(task);
    }

    private async Task StoreReadModelAsync(object readModel, CancellationToken cancellationToken)
    {
        var storeMethod = typeof(IDocumentSession).GetMethod(nameof(IDocumentSession.Store))!;
        var genericStoreMethod = storeMethod.MakeGenericMethod(readModel.GetType());

        genericStoreMethod.Invoke(_session, [new[] { readModel }]);
        await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteReadModelAsync(Type readModelType, Guid id, CancellationToken cancellationToken)
    {
        var deleteMethod = typeof(IDocumentSession).GetMethods()
            .First(m => m.Name == nameof(IDocumentSession.Delete) && m.IsGenericMethod);

        var genericDeleteMethod = deleteMethod.MakeGenericMethod(readModelType);

        genericDeleteMethod.Invoke(_session, [id]);
        await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
