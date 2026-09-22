using System.Reflection;
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
    // The read model type is only known at runtime, so the generic session calls go through
    // these private helpers. Looking the methods up on our own type is deliberate: the Marten
    // members (LoadAsync, Store, Delete) live on base interfaces of IDocumentSession, which
    // Type.GetMethod does not search on an interface type.
    private const BindingFlags HelperBinding = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly MethodInfo LoadHelper = typeof(MartenInlineProjectionDispatcher).GetMethod(nameof(LoadReadModelCoreAsync), HelperBinding)!;
    private static readonly MethodInfo StoreHelper = typeof(MartenInlineProjectionDispatcher).GetMethod(nameof(StoreReadModelCoreAsync), HelperBinding)!;
    private static readonly MethodInfo DeleteHelper = typeof(MartenInlineProjectionDispatcher).GetMethod(nameof(DeleteReadModelCoreAsync), HelperBinding)!;

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

        return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
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

        return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
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
                return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
            }

            object? resultReadModel = null;
            var shouldDelete = false;

            switch (handlerInfo.Value.HandlerType)
            {
                case ProjectionHandlerType.Creator:
                    if (existingReadModel != null)
                    {
                        // Already exists, skip creation
                        return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
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
                        return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
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

            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex)
        {
            ProjectionLog.ErrorApplyingEvent(_logger, ex, eventType.Name, registration.ProjectionName);

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    ProjectionErrorCodes.ApplyFailed,
                    ex,
                    $"Failed to apply event {eventType.Name} to projection {registration.ProjectionName}."));
        }
    }

    private Task<object?> LoadReadModelAsync(
        Type readModelType,
        Guid id,
        CancellationToken cancellationToken)
    {
        return (Task<object?>)LoadHelper.MakeGenericMethod(readModelType).Invoke(this, [id, cancellationToken])!;
    }

    private Task StoreReadModelAsync(object readModel, CancellationToken cancellationToken)
    {
        return (Task)StoreHelper.MakeGenericMethod(readModel.GetType()).Invoke(this, [readModel, cancellationToken])!;
    }

    private Task DeleteReadModelAsync(Type readModelType, Guid id, CancellationToken cancellationToken)
    {
        return (Task)DeleteHelper.MakeGenericMethod(readModelType).Invoke(this, [id, cancellationToken])!;
    }

    private async Task<object?> LoadReadModelCoreAsync<TReadModel>(Guid id, CancellationToken cancellationToken)
        where TReadModel : class
    {
        return await _session.LoadAsync<TReadModel>(id, cancellationToken).ConfigureAwait(false);
    }

    private async Task StoreReadModelCoreAsync<TReadModel>(TReadModel readModel, CancellationToken cancellationToken)
        where TReadModel : class
    {
        _session.Store(readModel);
        await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteReadModelCoreAsync<TReadModel>(Guid id, CancellationToken cancellationToken)
        where TReadModel : class
    {
        _session.Delete<TReadModel>(id);
        await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
