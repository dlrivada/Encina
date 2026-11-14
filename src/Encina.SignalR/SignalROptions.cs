using System.Collections.Frozen;

namespace Encina.SignalR;

/// <summary>
/// Configuration options for Encina SignalR integration.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EncinaHub"/> runs only the types registered here with
/// <see cref="AllowRequest{TRequest}(bool)"/> and <see cref="AllowNotification{TNotification}(bool)"/>;
/// with no registration every client call is denied. A client names a type by its exact,
/// case-sensitive <see cref="Type.FullName"/>.
/// </para>
/// <para>
/// A <see cref="BroadcastToSignalRAttribute"/> notification with no targets reaches every connected
/// client only when its type is registered with <see cref="AllowBroadcastToAll{TNotification}"/>;
/// otherwise the broadcast is refused.
/// </para>
/// <para>
/// The registrations are frozen the first time a hub, the broadcaster or the startup check reads
/// them; register every type inside the <c>AddEncinaSignalR</c> configuration delegate.
/// </para>
/// </remarks>
public sealed class SignalROptions
{
    private readonly Dictionary<string, HubTypeRegistration> _allowList = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Type> _broadcastToAll = new(StringComparer.Ordinal);
    private readonly Lock _allowListLock = new();
    private FrozenRegistrations? _frozen;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically broadcast notifications
    /// marked with <see cref="BroadcastToSignalRAttribute"/> to SignalR clients.
    /// </summary>
    /// <remarks>
    /// When enabled, notifications with <c>[BroadcastToSignalR]</c> attribute will be
    /// automatically sent to connected SignalR clients after being published.
    /// </remarks>
    public bool EnableNotificationBroadcast { get; set; } = true;

    /// <summary>
    /// Gets or sets an authorization policy name. This property is not read by any code.
    /// </summary>
    /// <remarks>
    /// Setting it has no effect: hub calls are gated by the allow-list and by the authentication
    /// requirement of <see cref="AllowRequest{TRequest}(bool)"/> and
    /// <see cref="AllowNotification{TNotification}(bool)"/>. It is tracked for removal in #3366.
    /// </remarks>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include detailed error information
    /// in responses sent to clients.
    /// </summary>
    /// <remarks>
    /// When disabled, only generic error messages are sent to clients.
    /// Enable only in development environments.
    /// </remarks>
    public bool IncludeDetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets the JSON serializer options for serializing messages.
    /// </summary>
    public System.Text.Json.JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <inheritdoc cref="AllowRequest(Type, bool)"/>
    /// <remarks>
    /// A type that carries <c>[JsonDerivedType]</c> lets the client choose a derived type outside the
    /// allow-list, so register only types whose polymorphism the host controls.
    /// </remarks>
    /// <typeparam name="TRequest">
    /// A concrete type implementing <see cref="ICommand{TResponse}"/> (callable only through
    /// <see cref="EncinaHub.SendCommand"/>) or <see cref="IQuery{TResponse}"/> (callable only through
    /// <see cref="EncinaHub.SendQuery"/>).
    /// </typeparam>
    public SignalROptions AllowRequest<TRequest>(bool allowAnonymous = false)
        => AllowRequest(typeof(TRequest), allowAnonymous);

    /// <summary>
    /// Exposes a command or query type to <see cref="EncinaHub"/> clients.
    /// </summary>
    /// <param name="requestType">
    /// A concrete type implementing <see cref="ICommand{TResponse}"/> or <see cref="IQuery{TResponse}"/>.
    /// </param>
    /// <param name="allowAnonymous">
    /// <see langword="true"/> to let unauthenticated callers invoke the type; the opt-out is logged
    /// once at startup. By default a caller must be authenticated.
    /// </param>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requestType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="requestType"/> is abstract, open generic, has no full name, or is not exactly
    /// one of a command or a query.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A type with the same full name is already registered, or the allow-list is already frozen.
    /// </exception>
    public SignalROptions AllowRequest(Type requestType, bool allowAnonymous = false)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        EnsureInstantiable(requestType, nameof(requestType));

        var isCommand = ImplementsOpenGeneric(requestType, typeof(ICommand<>));
        var isQuery = ImplementsOpenGeneric(requestType, typeof(IQuery<>));
        if (isCommand == isQuery)
        {
            throw new ArgumentException(
                $"Type '{requestType.FullName}' must implement exactly one of ICommand<TResponse> or IQuery<TResponse> to be exposed through EncinaHub.",
                nameof(requestType));
        }

        return Add(requestType, isCommand ? HubMessageKind.Command : HubMessageKind.Query, allowAnonymous);
    }

    /// <inheritdoc cref="AllowNotification(Type, bool)"/>
    /// <typeparam name="TNotification">A concrete notification type.</typeparam>
    public SignalROptions AllowNotification<TNotification>(bool allowAnonymous = false)
        where TNotification : INotification
        => AllowNotification(typeof(TNotification), allowAnonymous);

    /// <summary>
    /// Exposes a notification type to <see cref="EncinaHub.PublishNotification"/> clients.
    /// </summary>
    /// <param name="notificationType">A concrete type implementing <see cref="INotification"/>.</param>
    /// <param name="allowAnonymous">
    /// <see langword="true"/> to let unauthenticated callers publish the notification; the opt-out is
    /// logged once at startup. By default a caller must be authenticated.
    /// </param>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notificationType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="notificationType"/> is abstract, open generic, has no full name, or does not
    /// implement <see cref="INotification"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A type with the same full name is already registered, or the allow-list is already frozen.
    /// </exception>
    public SignalROptions AllowNotification(Type notificationType, bool allowAnonymous = false)
    {
        ArgumentNullException.ThrowIfNull(notificationType);
        EnsureInstantiable(notificationType, nameof(notificationType));

        if (!typeof(INotification).IsAssignableFrom(notificationType))
        {
            throw new ArgumentException(
                $"Type '{notificationType.FullName}' must implement INotification to be exposed through EncinaHub.",
                nameof(notificationType));
        }

        return Add(notificationType, HubMessageKind.Notification, allowAnonymous);
    }

    /// <inheritdoc cref="AllowBroadcastToAll(Type)"/>
    /// <typeparam name="TNotification">A concrete notification type.</typeparam>
    public SignalROptions AllowBroadcastToAll<TNotification>()
        where TNotification : INotification
        => AllowBroadcastToAll(typeof(TNotification));

    /// <summary>
    /// Lets a <see cref="BroadcastToSignalRAttribute"/> notification that declares no
    /// <see cref="BroadcastToSignalRAttribute.TargetUsers"/> or <see cref="BroadcastToSignalRAttribute.TargetGroups"/>
    /// reach every connected client.
    /// </summary>
    /// <remarks>
    /// Without this registration such a notification is refused and an error is logged. A notification
    /// whose declared targets resolve to no user or group is refused even when registered.
    /// </remarks>
    /// <param name="notificationType">A concrete type implementing <see cref="INotification"/>.</param>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notificationType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="notificationType"/> is abstract, open generic, has no full name, or does not
    /// implement <see cref="INotification"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A type with the same full name is already registered for broadcast to all clients, or the
    /// registrations are already frozen.
    /// </exception>
    public SignalROptions AllowBroadcastToAll(Type notificationType)
    {
        ArgumentNullException.ThrowIfNull(notificationType);
        EnsureInstantiable(notificationType, nameof(notificationType), "broadcast to all clients");

        if (!typeof(INotification).IsAssignableFrom(notificationType))
        {
            throw new ArgumentException(
                $"Type '{notificationType.FullName}' must implement INotification to be broadcast to all clients.",
                nameof(notificationType));
        }

        var key = notificationType.FullName!;
        lock (_allowListLock)
        {
            ThrowIfFrozen();
            if (!_broadcastToAll.TryAdd(key, notificationType))
            {
                throw new InvalidOperationException(
                    $"A type with the full name '{key}' is already registered for broadcast to all clients.");
            }
        }

        return this;
    }

    /// <summary>
    /// Returns the allow-list keyed by exact, case-sensitive <see cref="Type.FullName"/>, freezing every
    /// registration on the first call; later registrations throw.
    /// </summary>
    internal FrozenDictionary<string, HubTypeRegistration> GetAllowList() => Freeze().AllowList;

    /// <summary>
    /// Returns whether <paramref name="notificationType"/> itself was registered with
    /// <see cref="AllowBroadcastToAll(Type)"/>, freezing every registration on the first call.
    /// </summary>
    internal bool IsBroadcastToAllAllowed(Type notificationType)
        => Freeze().BroadcastToAll.TryGetValue(notificationType.FullName!, out var registered)
            && registered == notificationType;

    private FrozenRegistrations Freeze()
    {
        var frozen = Volatile.Read(ref _frozen);
        if (frozen is not null)
        {
            return frozen;
        }

        lock (_allowListLock)
        {
            _frozen ??= new FrozenRegistrations(
                _allowList.ToFrozenDictionary(StringComparer.Ordinal),
                _broadcastToAll.ToFrozenDictionary(StringComparer.Ordinal));
            return _frozen;
        }
    }

    private SignalROptions Add(Type type, HubMessageKind kind, bool allowAnonymous)
    {
        var key = type.FullName!;
        lock (_allowListLock)
        {
            ThrowIfFrozen();
            if (!_allowList.TryAdd(key, new HubTypeRegistration(type, kind, allowAnonymous)))
            {
                throw new InvalidOperationException(
                    $"A type with the full name '{key}' is already registered in the EncinaHub allow-list.");
            }
        }

        return this;
    }

    // Callers hold _allowListLock.
    private void ThrowIfFrozen()
    {
        if (_frozen is not null)
        {
            throw new InvalidOperationException(
                "The SignalR registrations are frozen once a hub, the broadcaster or the startup check has read them; register types inside the AddEncinaSignalR configuration delegate.");
        }
    }

    private static void EnsureInstantiable(Type type, string parameterName, string usage = "exposed through EncinaHub")
    {
        // Interfaces are abstract too; none of these can be the target of deserialization.
        if (type.IsAbstract || type.ContainsGenericParameters || type.FullName is null)
        {
            throw new ArgumentException(
                $"Type '{type}' must be a concrete, closed type to be {usage}.",
                parameterName);
        }
    }

    private static bool ImplementsOpenGeneric(Type type, Type openGenericInterface)
        => Array.Exists(
            type.GetInterfaces(),
            candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGenericInterface);

    private sealed record FrozenRegistrations(
        FrozenDictionary<string, HubTypeRegistration> AllowList,
        FrozenDictionary<string, Type> BroadcastToAll);
}
