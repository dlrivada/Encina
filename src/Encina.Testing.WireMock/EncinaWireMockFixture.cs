using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using Xunit;
using WireMockFaultType = WireMock.ResponseBuilders.FaultType;

namespace Encina.Testing.WireMock;

/// <summary>
/// xUnit fixture for WireMock server providing fluent API for HTTP mocking.
/// Implements <see cref="IAsyncLifetime"/> for automatic server lifecycle management.
/// </summary>
/// <example>
/// <code>
/// public class MyApiTests : IClassFixture&lt;EncinaWireMockFixture&gt;
/// {
///     private readonly EncinaWireMockFixture _wireMock;
///
///     public MyApiTests(EncinaWireMockFixture wireMock)
///     {
///         _wireMock = wireMock;
///         _wireMock.Reset();
///     }
///
///     [Fact]
///     public async Task Should_Get_User()
///     {
///         _wireMock.StubGet("/api/users/1", new { id = 1, name = "John" });
///
///         var client = new HttpClient { BaseAddress = new Uri(_wireMock.BaseUrl) };
///         var response = await client.GetAsync("/api/users/1");
///
///         response.StatusCode.ShouldBe(HttpStatusCode.OK);
///     }
/// }
/// </code>
/// </example>
public sealed class EncinaWireMockFixture : IAsyncLifetime
{
    private WireMockServer? _server;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Gets the underlying WireMock server instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization.</exception>
    public WireMockServer Server => _server ?? throw new InvalidOperationException("Server not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the base URL of the WireMock server.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization.</exception>
    public string BaseUrl => Server.Url ?? throw new InvalidOperationException("Server URL not available.");

    /// <summary>
    /// Gets the port the WireMock server is listening on.
    /// </summary>
    public int Port => Server.Port;

    /// <summary>
    /// Initializes a new instance of <see cref="EncinaWireMockFixture"/>.
    /// </summary>
    public EncinaWireMockFixture()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            UseSSL = false,
            StartAdminInterface = false
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        _server?.Stop();
        _server?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stubs a GET request with the specified path and response.
    /// </summary>
    /// <param name="path">The URL path to match (e.g., "/api/users").</param>
    /// <param name="response">The response object to serialize as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture StubGet(string path, object response, int statusCode = 200)
    {
        return Stub("GET", path, response: response, statusCode: statusCode);
    }

    /// <summary>
    /// Stubs a POST request with the specified path and response.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="response">The response object to serialize as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 201).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture StubPost(string path, object response, int statusCode = 201)
    {
        return Stub("POST", path, response: response, statusCode: statusCode);
    }

    /// <summary>
    /// Stubs a PUT request with the specified path and response.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="response">The response object to serialize as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture StubPut(string path, object response, int statusCode = 200)
    {
        return Stub("PUT", path, response: response, statusCode: statusCode);
    }

    /// <summary>
    /// Stubs a PATCH request with the specified path and response.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="response">The response object to serialize as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture StubPatch(string path, object response, int statusCode = 200)
    {
        return Stub("PATCH", path, response: response, statusCode: statusCode);
    }

    /// <summary>
    /// Stubs a DELETE request with the specified path.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 204).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture StubDelete(string path, int statusCode = 204)
    {
        return Stub("DELETE", path, statusCode: statusCode);
    }

    /// <summary>
    /// Stubs an HTTP request with full control over request matching and response.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, etc.).</param>
    /// <param name="path">The URL path to match.</param>
    /// <param name="requestConfig">Optional function to configure request matching.</param>
    /// <param name="response">Optional response object to serialize as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture Stub(
        string method,
        string path,
        Func<IRequestBuilder, IRequestBuilder>? requestConfig = null,
        object? response = null,
        int statusCode = 200)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var request = Request.Create()
            .WithPath(path)
            .UsingMethod(method);

        if (requestConfig is not null)
        {
            request = requestConfig(request);
        }

        var responseBuilder = Response.Create()
            .WithStatusCode(statusCode);

        if (response is not null)
        {
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            responseBuilder = responseBuilder
                .WithHeader("Content-Type", "application/json")
                .WithBody(json);
        }

        Server.Given(request).RespondWith(responseBuilder);

        return this;
    }

    /// <summary>
    /// Stubs a request to simulate a fault condition.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="faultType">The type of fault to simulate.</param>
    /// <param name="method">The HTTP method to match (default: any).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture StubFault(string path, FaultType faultType, string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var request = Request.Create().WithPath(path);
        if (method is not null)
        {
            request = request.UsingMethod(method);
        }

        var responseBuilder = faultType switch
        {
            FaultType.EmptyResponse => Response.Create().WithFault(WireMockFaultType.EMPTY_RESPONSE),
            FaultType.MalformedResponse => Response.Create().WithFault(WireMockFaultType.MALFORMED_RESPONSE_CHUNK),
            FaultType.Timeout => Response.Create().WithDelay(TimeSpan.FromSeconds(30)),
            _ => throw new ArgumentOutOfRangeException(nameof(faultType), faultType, "Unknown fault type")
        };

        Server.Given(request).RespondWith(responseBuilder);

        return this;
    }

    /// <summary>
    /// Stubs a request with a delayed response.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="delay">The delay before sending the response.</param>
    /// <param name="response">The response object to serialize as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <param name="method">The HTTP method to match (default: any).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaWireMockFixture StubDelay(
        string path,
        TimeSpan delay,
        object response,
        int statusCode = 200,
        string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(response);

        var request = Request.Create().WithPath(path);
        if (method is not null)
        {
            request = request.UsingMethod(method);
        }

        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var responseBuilder = Response.Create()
            .WithStatusCode(statusCode)
            .WithHeader("Content-Type", "application/json")
            .WithBody(json)
            .WithDelay(delay);

        Server.Given(request).RespondWith(responseBuilder);

        return this;
    }

    /// <summary>
    /// Stubs a sequence of responses for the same request path.
    /// Each subsequent call returns the next response in the sequence using WireMock scenarios.
    /// </summary>
    /// <param name="method">The HTTP method to match.</param>
    /// <param name="path">The URL path to match.</param>
    /// <param name="responses">Array of (response, statusCode) tuples.</param>
    /// <returns>This fixture for method chaining.</returns>
    /// <remarks>
    /// Uses WireMock's scenario feature to implement stateful response sequences.
    /// The scenario transitions through states "Step0", "Step1", etc.
    /// </remarks>
    public EncinaWireMockFixture StubSequence(
        string method,
        string path,
        params (object? Response, int StatusCode)[] responses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(responses);

        if (responses.Length == 0)
        {
            throw new ArgumentException("At least one response is required", nameof(responses));
        }

        // Use a unique scenario name based on method and path
        var scenarioName = $"Sequence_{method}_{path.Replace("/", "_")}";

        for (int i = 0; i < responses.Length; i++)
        {
            var (response, statusCode) = responses[i];
            var responseBuilder = Response.Create()
                .WithStatusCode(statusCode);

            if (response is not null)
            {
                var json = JsonSerializer.Serialize(response, _jsonOptions);
                responseBuilder = responseBuilder
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(json);
            }

            var request = Request.Create()
                .WithPath(path)
                .UsingMethod(method);

            var nextState = $"Step{i + 1}";

            if (i == 0)
            {
                // First response: don't use WhenStateIs - the initial state is implicit
                Server.Given(request)
                    .InScenario(scenarioName)
                    .WillSetStateTo(nextState)
                    .RespondWith(responseBuilder);
            }
            else
            {
                // Subsequent responses: match on the previous state
                var currentState = $"Step{i}";
                Server.Given(request)
                    .InScenario(scenarioName)
                    .WhenStateIs(currentState)
                    .WillSetStateTo(nextState)
                    .RespondWith(responseBuilder);
            }
        }

        return this;
    }

    /// <summary>
    /// Verifies that a request was made to the specified path.
    /// </summary>
    /// <param name="path">The URL path to check.</param>
    /// <param name="times">Expected number of calls (default: 1).</param>
    /// <param name="method">The HTTP method to check (default: any).</param>
    /// <exception cref="InvalidOperationException">Thrown when verification fails.</exception>
    public void VerifyCallMade(string path, int times = 1, string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var requests = GetReceivedRequests()
            .Where(r => r.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        if (method is not null)
        {
            requests = requests.Where(r => r.Method.Equals(method, StringComparison.OrdinalIgnoreCase));
        }

        var count = requests.Count();
        if (count != times)
        {
            var methodInfo = method is not null ? $"{method} " : "";
            throw new InvalidOperationException(
                $"Expected {times} call(s) to {methodInfo}{path}, but found {count}.");
        }
    }

    /// <summary>
    /// Verifies that no requests were made to the specified path.
    /// </summary>
    /// <param name="path">The URL path to check.</param>
    /// <param name="method">The HTTP method to check (default: any).</param>
    /// <exception cref="InvalidOperationException">Thrown when verification fails.</exception>
    public void VerifyNoCallsMade(string path, string? method = null)
    {
        VerifyCallMade(path, times: 0, method: method);
    }

    /// <summary>
    /// Gets all requests received by the WireMock server.
    /// </summary>
    /// <returns>Collection of received request information.</returns>
    public IReadOnlyList<ReceivedRequest> GetReceivedRequests()
    {
        return Server.LogEntries
            .Select(e => new ReceivedRequest(
                e.RequestMessage.Path,
                e.RequestMessage.Method,
                e.RequestMessage.Headers?.ToDictionary(
                    h => h.Key,
                    h => (IReadOnlyList<string>)h.Value.ToList().AsReadOnly())
                    ?? new Dictionary<string, IReadOnlyList<string>>(),
                e.RequestMessage.Body ?? string.Empty,
                e.RequestMessage.DateTime))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Resets all stubs and clears request history.
    /// Call this between tests to ensure isolation.
    /// </summary>
    public void Reset()
    {
        Server.Reset();
    }

    /// <summary>
    /// Resets only the request history, keeping stubs intact.
    /// </summary>
    public void ResetRequestHistory()
    {
        Server.ResetLogEntries();
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to use this WireMock server.
    /// </summary>
    /// <returns>
    /// An HttpClient with BaseAddress set to the WireMock server URL.
    /// The caller is responsible for disposing the returned HttpClient when no longer needed.
    /// </returns>
    public HttpClient CreateClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured with a custom handler.
    /// </summary>
    /// <param name="handler">The HTTP message handler to use.</param>
    /// <returns>
    /// An HttpClient with BaseAddress set to the WireMock server URL.
    /// The caller is responsible for disposing the returned HttpClient when no longer needed.
    /// </returns>
    public HttpClient CreateClient(HttpMessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }
}

/// <summary>
/// Represents a received HTTP request.
/// </summary>
/// <param name="Path">The request path.</param>
/// <param name="Method">The HTTP method.</param>
/// <param name="Headers">The request headers. Each header key maps to all its values (HTTP headers can have multiple values).</param>
/// <param name="Body">The request body.</param>
/// <param name="Timestamp">When the request was received.</param>
public sealed record ReceivedRequest(
    string Path,
    string Method,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
    string Body,
    DateTime Timestamp);
