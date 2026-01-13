using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Marten.Projections;

/// <summary>
/// Registry for projection types and their event handlers.
/// </summary>
public sealed class ProjectionRegistry
{
    private readonly ConcurrentDictionary<Type, List<ProjectionRegistration>> _eventToProjections = new();
    private readonly ConcurrentDictionary<Type, ProjectionRegistration> _readModelToProjection = new();
    private readonly List<ProjectionRegistration> _allRegistrations = [];

    /// <summary>
    /// Registers a projection with the registry.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    public void Register<TProjection, TReadModel>()
        where TProjection : class, IProjection<TReadModel>
        where TReadModel : class, IReadModel
    {
        Register(typeof(TProjection), typeof(TReadModel));
    }

    /// <summary>
    /// Registers a projection with the registry.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="readModelType">The read model type.</param>
    public void Register(Type projectionType, Type readModelType)
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(readModelType);

        var registration = new ProjectionRegistration(projectionType, readModelType);
        _readModelToProjection[readModelType] = registration;
        _allRegistrations.Add(registration);

        // Register event handlers
        foreach (var eventType in registration.HandledEventTypes)
        {
            _eventToProjections.AddOrUpdate(
                eventType,
                _ => [registration],
                (_, list) =>
                {
                    list.Add(registration);
                    return list;
                });
        }
    }

    /// <summary>
    /// Gets the projections that handle a specific event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>The projection registrations.</returns>
    public IReadOnlyList<ProjectionRegistration> GetProjectionsForEvent(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return _eventToProjections.TryGetValue(eventType, out var registrations)
            ? registrations
            : [];
    }

    /// <summary>
    /// Gets the projection for a specific read model type.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <returns>The projection registration, or null if not found.</returns>
    public ProjectionRegistration? GetProjectionForReadModel<TReadModel>()
        where TReadModel : class, IReadModel
    {
        return GetProjectionForReadModel(typeof(TReadModel));
    }

    /// <summary>
    /// Gets the projection for a specific read model type.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The projection registration, or null if not found.</returns>
    public ProjectionRegistration? GetProjectionForReadModel(Type readModelType)
    {
        ArgumentNullException.ThrowIfNull(readModelType);

        return _readModelToProjection.TryGetValue(readModelType, out var registration)
            ? registration
            : null;
    }

    /// <summary>
    /// Gets all registered projections.
    /// </summary>
    /// <returns>All projection registrations.</returns>
    public IReadOnlyList<ProjectionRegistration> GetAllProjections()
    {
        return _allRegistrations;
    }
}

/// <summary>
/// Registration information for a projection.
/// </summary>
public sealed class ProjectionRegistration
{
    private readonly Dictionary<Type, ProjectionHandlerInfo> _handlers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionRegistration"/> class.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="readModelType">The read model type.</param>
    public ProjectionRegistration(Type projectionType, Type readModelType)
    {
        ProjectionType = projectionType;
        ReadModelType = readModelType;

        // Get projection name from IProjection<TReadModel>.ProjectionName property
        var projectionInterface = projectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IProjection<>));

        if (projectionInterface != null)
        {
            var nameProperty = projectionInterface.GetProperty(nameof(IProjection<IReadModel>.ProjectionName));
            if (nameProperty != null)
            {
                // We'll get the actual name when we have an instance
                // For now, use the type name as default
                ProjectionName = projectionType.Name;
            }
        }

        ProjectionName = projectionType.Name;

        // Discover handlers
        DiscoverHandlers();
    }

    /// <summary>
    /// Gets the projection type.
    /// </summary>
    public Type ProjectionType { get; }

    /// <summary>
    /// Gets the read model type.
    /// </summary>
    public Type ReadModelType { get; }

    /// <summary>
    /// Gets the projection name.
    /// </summary>
    public string ProjectionName { get; }

    /// <summary>
    /// Gets the event types handled by this projection.
    /// </summary>
    public IEnumerable<Type> HandledEventTypes => _handlers.Keys;

    /// <summary>
    /// Gets handler information for an event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>The handler info, or null if not found.</returns>
    public ProjectionHandlerInfo? GetHandlerInfo(Type eventType)
    {
        return _handlers.TryGetValue(eventType, out var info) ? info : null;
    }

    private void DiscoverHandlers()
    {
        var interfaces = ProjectionType.GetInterfaces();

        foreach (var iface in interfaces.Where(i => i.IsGenericType))
        {
            var genericDef = iface.GetGenericTypeDefinition();
            var genericArgs = iface.GetGenericArguments();

            if (genericDef == typeof(IProjectionCreator<,>) && genericArgs.Length == 2)
            {
                var eventType = genericArgs[0];
                var method = iface.GetMethod(nameof(IProjectionCreator<object, IReadModel>.Create))!;

                _handlers[eventType] = new ProjectionHandlerInfo(
                    ProjectionHandlerType.Creator,
                    eventType,
                    method);
            }
            else if (genericDef == typeof(IProjectionHandler<,>) && genericArgs.Length == 2)
            {
                var eventType = genericArgs[0];
                var method = iface.GetMethod(nameof(IProjectionHandler<object, IReadModel>.Apply))!;

                _handlers[eventType] = new ProjectionHandlerInfo(
                    ProjectionHandlerType.Handler,
                    eventType,
                    method);
            }
            else if (genericDef == typeof(IProjectionDeleter<,>) && genericArgs.Length == 2)
            {
                var eventType = genericArgs[0];
                var method = iface.GetMethod(nameof(IProjectionDeleter<object, IReadModel>.ShouldDelete))!;

                _handlers[eventType] = new ProjectionHandlerInfo(
                    ProjectionHandlerType.Deleter,
                    eventType,
                    method);
            }
        }
    }
}

/// <summary>
/// Information about a projection handler method.
/// </summary>
public readonly struct ProjectionHandlerInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionHandlerInfo"/> struct.
    /// </summary>
    /// <param name="handlerType">The type of handler.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="method">The handler method.</param>
    public ProjectionHandlerInfo(
        ProjectionHandlerType handlerType,
        Type eventType,
        MethodInfo method)
    {
        HandlerType = handlerType;
        EventType = eventType;
        Method = method;
    }

    /// <summary>
    /// Gets the handler type.
    /// </summary>
    public ProjectionHandlerType HandlerType { get; }

    /// <summary>
    /// Gets the event type.
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// Gets the handler method.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Invokes the Create method.
    /// </summary>
    public object InvokeCreate(object projection, object @event, ProjectionContext context)
    {
        return Method.Invoke(projection, [@event, context])!;
    }

    /// <summary>
    /// Invokes the Apply method.
    /// </summary>
    public object InvokeApply(object projection, object @event, object current, ProjectionContext context)
    {
        return Method.Invoke(projection, [@event, current, context])!;
    }

    /// <summary>
    /// Invokes the ShouldDelete method.
    /// </summary>
    public bool InvokeShouldDelete(object projection, object @event, object current, ProjectionContext context)
    {
        return (bool)Method.Invoke(projection, [@event, current, context])!;
    }
}

/// <summary>
/// The type of projection handler.
/// </summary>
public enum ProjectionHandlerType
{
    /// <summary>
    /// Creates a new read model.
    /// </summary>
    Creator,

    /// <summary>
    /// Updates an existing read model.
    /// </summary>
    Handler,

    /// <summary>
    /// Deletes a read model.
    /// </summary>
    Deleter
}
