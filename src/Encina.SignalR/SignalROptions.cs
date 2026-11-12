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
/// The allow-list is frozen the first time a hub or the startup check reads it; register every
/// type inside the <c>AddEncinaSignalR</c> configuration delegate.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSignalR(options =>
/// {
///     options.AllowRequest&lt;GetOrderQuery&gt;();                   // authenticated callers only
///     options.AllowRequest&lt;PingQuery&gt;(allowAnonymous: true);   // explicit, logged at startup
///     options.AllowNotification&lt;OrderViewed&gt;();
/// });
/// </code>
/// </example>
public sealed class SignalROptions
{
    private readonly Dictionary<string, HubTypeRegistration> _allowList = new(StringComparer.Ordinal);
    private readonly Lock _allowListLock = new();
    private FrozenDictionary<string, HubTypeRegistration>? _frozenAllowList;

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
    /// Gets or sets the authorization policy name required for clients to invoke
    /// Encina methods through the hub.
    /// </summary>
    /// <remarks>
    /// When set, clients must satisfy this policy to call SendCommand, SendQuery, etc.
    /// Leave null to allow anonymous access (not recommended for production).
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

    /// <summary>
    /// Exposes a command or query type to <see cref="EncinaHub"/> clients.
    /// </summary>
    /// <typeparam name="TRequest">
    /// A concrete type implementing <see cref="ICommand{TResponse}"/> (callable only through
    /// <see cref="EncinaHub.SendCommand"/>) or <see cref="IQuery{TResponse}"/> (callable only through
    /// <see cref="EncinaHub.SendQuery"/>).
    /// </typeparam>
    /// <param name="allowAnonymous">
    /// <see langword="true"/> to let unauthenticated callers invoke the type; the opt-out is logged
    /// once at startup. By default a caller must be authenticated.
    /// </param>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="TRequest"/> is abstract, open generic, has no full name, or is not exactly
    /// one of a command or a query.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A type with the same full name is already registered, or the allow-list is already frozen.
    /// </exception>
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

    /// <summary>
    /// Exposes a notification type to <see cref="EncinaHub.PublishNotification"/> clients.
    /// </summary>
    /// <typeparam name="TNotification">A concrete notification type.</typeparam>
    /// <param name="allowAnonymous">
    /// <see langword="true"/> to let unauthenticated callers publish the notification; the opt-out is
    /// logged once at startup. By default a caller must be authenticated.
    /// </param>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="TNotification"/> is abstract, open generic or has no full name.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A type with the same full name is already registered, or the allow-list is already frozen.
    /// </exception>
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

    /// <summary>
    /// Returns the allow-list keyed by exact, case-sensitive <see cref="Type.FullName"/>, freezing it
    /// on the first call; later registrations throw.
    /// </summary>
    internal FrozenDictionary<string, HubTypeRegistration> GetAllowList()
    {
        var frozen = Volatile.Read(ref _frozenAllowList);
        if (frozen is not null)
        {
            return frozen;
        }

        lock (_allowListLock)
        {
            _frozenAllowList ??= _allowList.ToFrozenDictionary(StringComparer.Ordinal);
            return _frozenAllowList;
        }
    }

    private SignalROptions Add(Type type, HubMessageKind kind, bool allowAnonymous)
    {
        var key = type.FullName!;
        lock (_allowListLock)
        {
            if (_frozenAllowList is not null)
            {
                throw new InvalidOperationException(
                    "The EncinaHub allow-list is frozen once a hub or the startup check has read it; register types inside the AddEncinaSignalR configuration delegate.");
            }

            if (!_allowList.TryAdd(key, new HubTypeRegistration(type, kind, allowAnonymous)))
            {
                throw new InvalidOperationException(
                    $"A type with the full name '{key}' is already registered in the EncinaHub allow-list.");
            }
        }

        return this;
    }

    private static void EnsureInstantiable(Type type, string parameterName)
    {
        // Interfaces are abstract too; none of these can be the target of deserialization.
        if (type.IsAbstract || type.ContainsGenericParameters || type.FullName is null)
        {
            throw new ArgumentException(
                $"Type '{type}' must be a concrete, closed type to be exposed through EncinaHub.",
                parameterName);
        }
    }

    private static bool ImplementsOpenGeneric(Type type, Type openGenericInterface)
        => Array.Exists(
            type.GetInterfaces(),
            candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGenericInterface);
}
