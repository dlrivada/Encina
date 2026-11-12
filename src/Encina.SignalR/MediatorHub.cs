#pragma warning disable CA1822 // Member can be static

using System.Collections.Frozen;
using System.Text.Json;
using Encina.Diagnostics;
using LanguageExt;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.SignalR;

/// <summary>
/// Base SignalR hub that provides Encina integration for real-time communication.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to create hubs that can send commands and queries
/// through the Encina from SignalR clients.
/// </para>
/// <para>
/// The hub runs only the types the host registered with <see cref="SignalROptions.AllowRequest{TRequest}(bool)"/>
/// and <see cref="SignalROptions.AllowNotification{TNotification}(bool)"/> (deny-all by default), named
/// by their exact <see cref="Type.FullName"/>, and only for authenticated callers unless the type was
/// registered with <c>allowAnonymous: true</c>.
/// </para>
/// <para>
/// Clients can invoke:
/// <list type="bullet">
/// <item><description><c>SendCommand</c>: Execute a command and receive the result</description></item>
/// <item><description><c>SendQuery</c>: Execute a query and receive the result</description></item>
/// <item><description><c>PublishNotification</c>: Publish a notification (fire-and-forget)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Server-side: Create your application hub
/// public class AppHub : EncinaHub
/// {
///     public AppHub(IEncina Encina, IOptions&lt;SignalROptions&gt; options, ILogger&lt;AppHub&gt; logger)
///         : base(Encina, options, logger)
///     {
///     }
///
///     // Add custom hub methods as needed
///     public async Task JoinOrderGroup(string orderId)
///     {
///         await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");
///     }
/// }
///
/// // Registration: expose each callable type explicitly
/// services.AddEncinaSignalR(options =>
/// {
///     options.AllowRequest&lt;CreateOrderCommand&gt;();
///     options.AllowRequest&lt;GetOrderQuery&gt;();
/// });
///
/// // Client-side (JavaScript), naming the type by its full name:
/// // const result = await connection.invoke("SendCommand", "MyApp.Orders.CreateOrderCommand", { items: [...] });
/// // const data = await connection.invoke("SendQuery", "MyApp.Orders.GetOrderQuery", { orderId: "123" });
/// </code>
/// </example>
public abstract class EncinaHub : Hub
{
    private readonly SignalROptions _options;
    private readonly FrozenDictionary<string, HubTypeRegistration> _allowList;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaHub"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="options">
    /// The SignalR options; pass the <see cref="IOptions{TOptions}"/> singleton so every hub shares the
    /// allow-list the startup check freezes and logs.
    /// </param>
    /// <param name="logger">The logger instance.</param>
    protected EncinaHub(
        IEncina encina,
        IOptions<SignalROptions> options,
        ILogger logger)
    {
        Encina = encina;
        _options = options.Value;
        _allowList = _options.GetAllowList();
        _logger = logger;
    }

    /// <summary>
    /// Gets the Encina instance for use in derived hubs.
    /// </summary>
    protected IEncina Encina { get; }

    /// <summary>
    /// Sends a command through the Encina and returns the result.
    /// </summary>
    /// <param name="commandTypeName">
    /// The exact, case-sensitive <see cref="Type.FullName"/> of a command type registered with
    /// <see cref="SignalROptions.AllowRequest{TRequest}(bool)"/>.
    /// </param>
    /// <param name="commandJson">The command data as a JSON object.</param>
    /// <returns>The result of the command execution.</returns>
    /// <remarks>
    /// A name that is not registered, is registered as a query, or is not callable by an
    /// unauthenticated caller gets the same <c>command.type_not_found</c> response, and the payload
    /// is not deserialized. The result is returned as a JSON object containing either the success
    /// value or error details.
    /// </remarks>
    public Task<object> SendCommand(string commandTypeName, JsonElement commandJson)
        => SendRequestAsync(HubMessageKind.Command, "command", commandTypeName, commandJson, Log.ErrorExecutingCommand);

    /// <summary>
    /// Sends a query through the Encina and returns the result.
    /// </summary>
    /// <param name="queryTypeName">
    /// The exact, case-sensitive <see cref="Type.FullName"/> of a query type registered with
    /// <see cref="SignalROptions.AllowRequest{TRequest}(bool)"/>.
    /// </param>
    /// <param name="queryJson">The query data as a JSON object.</param>
    /// <returns>The result of the query execution.</returns>
    /// <remarks>Denials behave as in <see cref="SendCommand"/>, with <c>query.type_not_found</c>.</remarks>
    public Task<object> SendQuery(string queryTypeName, JsonElement queryJson)
        => SendRequestAsync(HubMessageKind.Query, "query", queryTypeName, queryJson, Log.ErrorExecutingQuery);

    /// <summary>
    /// Publishes a notification through the Encina.
    /// </summary>
    /// <param name="notificationTypeName">
    /// The exact, case-sensitive <see cref="Type.FullName"/> of a notification type registered with
    /// <see cref="SignalROptions.AllowNotification{TNotification}(bool)"/>.
    /// </param>
    /// <param name="notificationJson">The notification data as a JSON object.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// A name that is not registered as a notification, or is not callable by an unauthenticated
    /// caller, does nothing: no deserialization, no handler runs.
    /// </remarks>
    public async Task PublishNotification(string notificationTypeName, JsonElement notificationJson)
    {
        var registration = FindCallable(HubMessageKind.Notification, notificationTypeName);
        if (registration is null)
        {
            return;
        }

        var typeName = registration.Type.FullName!;
        try
        {
            var notification = JsonSerializer.Deserialize(notificationJson.GetRawText(), registration.Type, _options.JsonSerializerOptions);
            if (notification == null)
            {
                Log.FailedToDeserializeNotification(_logger, typeName);
                return;
            }

            await PublishDynamicAsync(notification);
        }
        catch (Exception ex)
        {
            Log.ErrorPublishingNotification(_logger, ex.ForLogging(), typeName);
        }
    }

    /// <summary>
    /// Looks up, deserializes and sends one request; <paramref name="kind"/> selects the hub
    /// method's message kind and <paramref name="code"/> ("command" or "query") names its error
    /// codes and messages.
    /// </summary>
    private async Task<object> SendRequestAsync(
        HubMessageKind kind,
        string code,
        string requestTypeName,
        JsonElement requestJson,
        Action<ILogger, Exception, string> logFailure)
    {
        // The lookup runs before any deserialization: a type that is unknown, not exposed, of the
        // wrong kind or not callable by this caller is never instantiated. Every such case gets
        // the same response, which never echoes the client string (no type-existence oracle).
        var registration = FindCallable(kind, requestTypeName);
        if (registration is null)
        {
            return CreateErrorResponse($"{code}.type_not_found", $"{char.ToUpperInvariant(code[0])}{code[1..]} type not found.");
        }

        try
        {
            var request = JsonSerializer.Deserialize(requestJson.GetRawText(), registration.Type, _options.JsonSerializerOptions);
            if (request == null)
            {
                return CreateErrorResponse($"{code}.deserialization_failed", $"Failed to deserialize {code}.");
            }

            return await SendDynamicAsync(request);
        }
        catch (Exception ex)
        {
            logFailure(_logger, ex.ForLogging(), registration.Type.FullName!);
            return CreateErrorResponse($"{code}.execution_failed", GetErrorMessage(ex));
        }
    }

    /// <summary>
    /// Returns the allow-list entry for <paramref name="typeName"/> when it exists, has the
    /// <paramref name="kind"/> of the invoked hub method and the caller may invoke it: authenticated,
    /// or the entry was registered with <c>allowAnonymous: true</c>. Otherwise logs the denial at
    /// Debug with a sanitized, truncated name and returns <see langword="null"/>.
    /// </summary>
    private HubTypeRegistration? FindCallable(HubMessageKind kind, string? typeName)
    {
        var registration = FindRegistration(kind, typeName);
        if (registration is not null && (registration.AllowAnonymous || IsCallerAuthenticated()))
        {
            return registration;
        }

        Log.HubCallDenied(_logger, kind, ClientTypeName.ForLog(typeName));
        return null;
    }

    private HubTypeRegistration? FindRegistration(HubMessageKind kind, string? typeName)
        => typeName is not null && _allowList.TryGetValue(typeName, out var registration) && registration.Kind == kind
            ? registration
            : null;

    // Fail closed: no principal or no identity counts as unauthenticated.
    private bool IsCallerAuthenticated() => Context.User?.Identity?.IsAuthenticated == true;

    // The request's runtime type is only known after deserialization, so the generic helper
    // that calls the typed IEncina.Send overload binds dynamically. A request that does not
    // implement IRequest<TResponse> fails to bind and surfaces as an exception, like before.
    private Task<object> SendDynamicAsync(object request)
        => SendTypedAsync((dynamic)request);

    private async Task<object> SendTypedAsync<TResponse>(IRequest<TResponse> request)
    {
        var result = await Encina.Send(request, Context.ConnectionAborted);

        return ConvertResult(result);
    }

    private Task PublishDynamicAsync(object notification)
        => PublishTypedAsync((dynamic)notification);

    private async Task PublishTypedAsync<TNotification>(TNotification notification)
        where TNotification : INotification
    {
        await Encina.Publish(notification, Context.ConnectionAborted);
    }

    /// <summary>
    /// Converts an Either result to a response object.
    /// </summary>
    private object ConvertResult<T>(Either<EncinaError, T> result)
    {
        return result.Match<object>(
            Right: value => new { success = true, data = value },
            Left: error => new
            {
                success = false,
                error = new
                {
                    code = error.GetCode().IfNone("unknown"),
                    message = error.Message,
                    details = _options.IncludeDetailedErrors ? error.Exception.Match(e => e.ToString(), () => (string?)null) : null
                }
            });
    }

    /// <summary>
    /// Creates an error response object.
    /// </summary>
    private object CreateErrorResponse(string code, string message)
    {
        return new
        {
            success = false,
            error = new
            {
                code,
                message
            }
        };
    }

    /// <summary>
    /// Gets the appropriate error message based on options.
    /// </summary>
    private string GetErrorMessage(Exception ex)
    {
        return _options.IncludeDetailedErrors
            ? ex.ToString()
            : "An error occurred while processing your request.";
    }
}
