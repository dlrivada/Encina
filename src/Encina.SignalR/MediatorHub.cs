#pragma warning disable CA1822 // Member can be static

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
/// // Client-side (JavaScript):
/// // const result = await connection.invoke("SendCommand", "CreateOrderCommand", { items: [...] });
/// // const data = await connection.invoke("SendQuery", "GetOrderQuery", { orderId: "123" });
/// </code>
/// </example>
public abstract class EncinaHub : Hub
{
    private readonly SignalROptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaHub"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="options">The SignalR options.</param>
    /// <param name="logger">The logger instance.</param>
    protected EncinaHub(
        IEncina encina,
        IOptions<SignalROptions> options,
        ILogger logger)
    {
        Encina = encina;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the Encina instance for use in derived hubs.
    /// </summary>
    protected IEncina Encina { get; }

    /// <summary>
    /// Sends a command through the Encina and returns the result.
    /// </summary>
    /// <param name="commandTypeName">The fully qualified name or simple name of the command type.</param>
    /// <param name="commandJson">The command data as a JSON object.</param>
    /// <returns>The result of the command execution.</returns>
    /// <remarks>
    /// The command type must be registered in the application's assembly.
    /// The result is returned as a JSON object containing either the success value or error details.
    /// </remarks>
    public Task<object> SendCommand(string commandTypeName, JsonElement commandJson)
        => SendRequestAsync("command", commandTypeName, commandJson, Log.ErrorExecutingCommand);

    /// <summary>
    /// Sends a query through the Encina and returns the result.
    /// </summary>
    /// <param name="queryTypeName">The fully qualified name or simple name of the query type.</param>
    /// <param name="queryJson">The query data as a JSON object.</param>
    /// <returns>The result of the query execution.</returns>
    public Task<object> SendQuery(string queryTypeName, JsonElement queryJson)
        => SendRequestAsync("query", queryTypeName, queryJson, Log.ErrorExecutingQuery);

    /// <summary>
    /// Publishes a notification through the Encina.
    /// </summary>
    /// <param name="notificationTypeName">The fully qualified name or simple name of the notification type.</param>
    /// <param name="notificationJson">The notification data as a JSON object.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishNotification(string notificationTypeName, JsonElement notificationJson)
    {
        try
        {
            var notificationType = ResolveType(notificationTypeName);
            if (notificationType == null)
            {
                Log.NotificationTypeNotFound(_logger, notificationTypeName);
                return;
            }

            var notification = JsonSerializer.Deserialize(notificationJson.GetRawText(), notificationType, _options.JsonSerializerOptions);
            if (notification == null)
            {
                Log.FailedToDeserializeNotification(_logger, notificationTypeName);
                return;
            }

            await PublishDynamicAsync(notification);
        }
        catch (Exception ex)
        {
            Log.ErrorPublishingNotification(_logger, ex.ForLogging(), notificationTypeName);
        }
    }

    /// <summary>
    /// Resolves, deserializes and sends one request; <paramref name="kind"/> ("command" or
    /// "query") names the error codes, messages and the failure log of that request kind.
    /// </summary>
    private async Task<object> SendRequestAsync(
        string kind,
        string requestTypeName,
        JsonElement requestJson,
        Action<ILogger, Exception, string> logFailure)
    {
        try
        {
            var requestType = ResolveType(requestTypeName);
            if (requestType == null)
            {
                return CreateErrorResponse($"{kind}.type_not_found", $"{char.ToUpperInvariant(kind[0])}{kind[1..]} type '{requestTypeName}' not found.");
            }

            var request = JsonSerializer.Deserialize(requestJson.GetRawText(), requestType, _options.JsonSerializerOptions);
            if (request == null)
            {
                return CreateErrorResponse($"{kind}.deserialization_failed", $"Failed to deserialize {kind}.");
            }

            return await SendDynamicAsync(request);
        }
        catch (Exception ex)
        {
            logFailure(_logger, ex.ForLogging(), requestTypeName);
            return CreateErrorResponse($"{kind}.execution_failed", GetErrorMessage(ex));
        }
    }

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
    /// Resolves a type by its name from loaded assemblies.
    /// </summary>
    /// <param name="typeName">The type name to resolve.</param>
    /// <returns>The resolved type, or null if not found.</returns>
    protected virtual Type? ResolveType(string typeName)
    {
        // Try exact match first
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // Search in all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }

                // Try simple name match
                type = assembly.GetTypes().FirstOrDefault(t =>
                    t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                    t.FullName?.Equals(typeName, StringComparison.OrdinalIgnoreCase) is true);

                if (type != null)
                {
                    return type;
                }
            }
            catch
            {
                // Ignore assemblies that can't be searched
            }
        }

        return null;
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
