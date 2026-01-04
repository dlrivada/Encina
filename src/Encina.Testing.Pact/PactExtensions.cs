using System.Net.Http.Json;
using System.Text.Json;
using LanguageExt;

namespace Encina.Testing.Pact;

/// <summary>
/// Extension methods for working with Pact contracts and Encina.
/// </summary>
public static class PactExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Converts an Either result to a Pact-compatible response object.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="result">The Either result.</param>
    /// <returns>A Pact-compatible response object.</returns>
    public static object ToPactResponse<TResponse>(this Either<EncinaError, TResponse> result)
    {
        return result.Match<object>(
            Right: response => new PactSuccessResponse<TResponse>(true, response),
            Left: error => new PactErrorResponseWrapper(
                false,
                error.GetCode().IfNone("encina.unknown"),
                error.Message));
    }

    /// <summary>
    /// Creates an HTTP client configured for Pact mock server testing.
    /// </summary>
    /// <param name="mockServerUri">The mock server URI.</param>
    /// <param name="configureClient">Optional action to configure the client.</param>
    /// <returns>A configured HttpClient. Caller is responsible for disposing the returned instance.</returns>
    /// <remarks>
    /// This method creates a new <see cref="HttpClient"/> instance that must be disposed by the caller.
    /// For production code, consider using <c>IHttpClientFactory</c> instead to manage client lifetimes.
    /// This extension is primarily intended for test scenarios with Pact mock servers.
    /// </remarks>
    public static HttpClient CreatePactHttpClient(
        this Uri mockServerUri,
        Action<HttpClient>? configureClient = null)
    {
        ArgumentNullException.ThrowIfNull(mockServerUri);

        var client = new HttpClient
        {
            BaseAddress = mockServerUri
        };

        client.DefaultRequestHeaders.Add("Accept", "application/json");
        configureClient?.Invoke(client);

        return client;
    }

    /// <summary>
    /// Sends a command to the mock server and returns the response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the mock server.</returns>
    public static async Task<HttpResponseMessage> SendCommandAsync<TCommand, TResponse>(
        this HttpClient client,
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(command);

        var requestTypeName = typeof(TCommand).Name;
        var path = $"/api/commands/{requestTypeName}";

        return await client.PostAsJsonAsync(path, command, DefaultJsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a query to the mock server and returns the response.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="query">The query to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the mock server.</returns>
    public static async Task<HttpResponseMessage> SendQueryAsync<TQuery, TResponse>(
        this HttpClient client,
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(query);

        var requestTypeName = typeof(TQuery).Name;
        var path = $"/api/queries/{requestTypeName}";

        return await client.PostAsJsonAsync(path, query, DefaultJsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a notification to the mock server.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the mock server.</returns>
    public static async Task<HttpResponseMessage> PublishNotificationAsync<TNotification>(
        this HttpClient client,
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = typeof(TNotification).Name;
        var path = $"/api/notifications/{notificationType}";

        return await client.PostAsJsonAsync(path, notification, DefaultJsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes the response as an Either result.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="response">The HTTP response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized Either result.</returns>
    /// <remarks>
    /// This method is railway-oriented and will not throw exceptions for deserialization failures.
    /// JSON parsing errors are captured and returned as <c>pact.deserialization</c> errors.
    /// A 204 NoContent response is explicitly handled and returned as an error.
    /// </remarks>
    public static async Task<Either<EncinaError, TResponse>> ReadAsEitherAsync<TResponse>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        try
        {
            if (response.IsSuccessStatusCode)
            {
                // Explicitly handle 204 NoContent - no body to deserialize
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return Either<EncinaError, TResponse>.Left(
                        EncinaErrors.Create("pact.deserialization", "No content to deserialize"));
                }

                var content = await response.Content.ReadFromJsonAsync<TResponse>(
                    DefaultJsonOptions,
                    cancellationToken).ConfigureAwait(false);

                return content is not null
                    ? Either<EncinaError, TResponse>.Right(content)
                    : Either<EncinaError, TResponse>.Left(
                        EncinaErrors.Create("pact.deserialization", "Failed to deserialize response"));
            }

            var errorContent = await response.Content.ReadFromJsonAsync<PactErrorResponse>(
                DefaultJsonOptions,
                cancellationToken).ConfigureAwait(false);

            return Either<EncinaError, TResponse>.Left(
                EncinaErrors.Create(
                    errorContent?.ErrorCode ?? "pact.error",
                    errorContent?.ErrorMessage ?? "Unknown error"));
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation - don't swallow cancellation tokens
            throw;
        }
        catch (JsonException ex)
        {
            return Either<EncinaError, TResponse>.Left(
                EncinaErrors.Create("pact.deserialization", $"JSON deserialization failed: {ex.Message}"));
        }
        catch (NotSupportedException ex)
        {
            return Either<EncinaError, TResponse>.Left(
                EncinaErrors.Create("pact.deserialization", $"Unsupported content type: {ex.Message}"));
        }
        catch (HttpRequestException ex)
        {
            return Either<EncinaError, TResponse>.Left(
                EncinaErrors.Create("pact.network", $"Network/IO error during response read: {ex.Message}"));
        }
    }
}

/// <summary>
/// Pact success response wrapper.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="Data">The response data.</param>
public sealed record PactSuccessResponse<T>(bool IsSuccess, T Data);

/// <summary>
/// Internal wrapper for error responses.
/// </summary>
internal sealed record PactErrorResponseWrapper(
    bool IsSuccess,
    string ErrorCode,
    string ErrorMessage);
