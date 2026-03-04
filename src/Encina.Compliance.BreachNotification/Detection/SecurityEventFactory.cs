using System.Collections.Concurrent;
using System.Reflection;

using Encina.Compliance.BreachNotification.Model;

namespace Encina.Compliance.BreachNotification.Detection;

/// <summary>
/// Creates <see cref="SecurityEvent"/> instances from request metadata using cached reflection
/// for property extraction.
/// </summary>
/// <remarks>
/// <para>
/// The factory is used by the <c>BreachDetectionPipelineBehavior</c> to generate security events
/// from request objects decorated with <c>[BreachMonitored]</c>. It extracts metadata from
/// the request type's public properties (e.g., <c>UserId</c>, <c>IpAddress</c>, <c>EntityId</c>)
/// using reflection that is cached per request type for zero overhead on subsequent calls.
/// </para>
/// <para>
/// Property resolution looks for well-known property names (case-insensitive):
/// <list type="bullet">
/// <item><description><c>UserId</c> — falls back to <see cref="IRequestContext.UserId"/> if not found on request.</description></item>
/// <item><description><c>IpAddress</c> — IP address associated with the request.</description></item>
/// <item><description><c>EntityType</c> or <c>AffectedEntityType</c> — type of the affected entity.</description></item>
/// <item><description><c>EntityId</c>, <c>AffectedEntityId</c>, or <c>Id</c> — identifier of the affected entity.</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class SecurityEventFactory
{
    private static readonly ConcurrentDictionary<Type, RequestPropertyAccessors> PropertyCache = new();

    /// <summary>
    /// Creates a <see cref="SecurityEvent"/> from a request, its breach monitoring configuration,
    /// the ambient request context, and a time provider.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request being monitored.</typeparam>
    /// <param name="request">The request instance to extract metadata from.</param>
    /// <param name="eventType">The security event type to assign.</param>
    /// <param name="source">The source identifier (typically the request type's full name).</param>
    /// <param name="context">The ambient request context providing user and correlation metadata.</param>
    /// <param name="timeProvider">Time provider for the event timestamp.</param>
    /// <returns>A new <see cref="SecurityEvent"/> populated from the request metadata.</returns>
    public static SecurityEvent Create<TRequest>(
        TRequest request,
        SecurityEventType eventType,
        string source,
        IRequestContext context,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var accessors = PropertyCache.GetOrAdd(typeof(TRequest), ResolvePropertyAccessors);

        var userId = context.UserId ?? accessors.GetUserId(request);
        var ipAddress = accessors.GetIpAddress(request);
        var entityType = accessors.GetEntityType(request) ?? typeof(TRequest).Name;
        var entityId = accessors.GetEntityId(request);

        return new SecurityEvent
        {
            Id = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            Source = source,
            Description = $"Security event generated from pipeline for request '{typeof(TRequest).Name}'",
            OccurredAtUtc = timeProvider.GetUtcNow(),
            UserId = userId,
            IpAddress = ipAddress,
            AffectedEntityType = entityType,
            AffectedEntityId = entityId
        };
    }

    private static RequestPropertyAccessors ResolvePropertyAccessors(Type requestType)
    {
        var properties = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var userIdProp = FindProperty(properties, "UserId");
        var ipAddressProp = FindProperty(properties, "IpAddress");
        var entityTypeProp = FindProperty(properties, "EntityType", "AffectedEntityType");
        var entityIdProp = FindProperty(properties, "EntityId", "AffectedEntityId", "Id");

        return new RequestPropertyAccessors(userIdProp, ipAddressProp, entityTypeProp, entityIdProp);
    }

    private static PropertyInfo? FindProperty(PropertyInfo[] properties, params string[] names)
    {
        foreach (var name in names)
        {
            // Use System.Array explicitly to avoid conflict with LanguageExt.Prelude.Array<T>()
            var prop = System.Array.Find(properties,
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.CanRead);

            if (prop is not null)
            {
                return prop;
            }
        }

        return null;
    }

    /// <summary>
    /// Cached property accessors for extracting metadata from a request type.
    /// </summary>
    private sealed class RequestPropertyAccessors
    {
        private readonly PropertyInfo? _userIdProperty;
        private readonly PropertyInfo? _ipAddressProperty;
        private readonly PropertyInfo? _entityTypeProperty;
        private readonly PropertyInfo? _entityIdProperty;

        public RequestPropertyAccessors(
            PropertyInfo? userIdProperty,
            PropertyInfo? ipAddressProperty,
            PropertyInfo? entityTypeProperty,
            PropertyInfo? entityIdProperty)
        {
            _userIdProperty = userIdProperty;
            _ipAddressProperty = ipAddressProperty;
            _entityTypeProperty = entityTypeProperty;
            _entityIdProperty = entityIdProperty;
        }

        public string? GetUserId<T>(T request) =>
            _userIdProperty?.GetValue(request)?.ToString();

        public string? GetIpAddress<T>(T request) =>
            _ipAddressProperty?.GetValue(request)?.ToString();

        public string? GetEntityType<T>(T request) =>
            _entityTypeProperty?.GetValue(request)?.ToString();

        public string? GetEntityId<T>(T request) =>
            _entityIdProperty?.GetValue(request)?.ToString();
    }
}
