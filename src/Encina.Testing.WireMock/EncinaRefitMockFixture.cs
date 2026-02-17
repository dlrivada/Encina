using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using Xunit;
using WireMockRequestBuilder = WireMock.RequestBuilders.IRequestBuilder;

namespace Encina.Testing.WireMock;

/// <summary>
/// xUnit fixture for testing Refit API clients with WireMock.
/// Provides automatic Refit client configuration and fluent stubbing API.
/// </summary>
/// <typeparam name="TApiClient">The Refit API client interface type.</typeparam>
/// <example>
/// <code>
/// public interface IWeatherApi
/// {
///     [Get("/weather/{city}")]
///     Task&lt;WeatherResponse&gt; GetWeatherAsync(string city);
/// }
///
/// public class WeatherApiTests : IClassFixture&lt;EncinaRefitMockFixture&lt;IWeatherApi&gt;&gt;
/// {
///     private readonly EncinaRefitMockFixture&lt;IWeatherApi&gt; _fixture;
///
///     public WeatherApiTests(EncinaRefitMockFixture&lt;IWeatherApi&gt; fixture)
///     {
///         _fixture = fixture;
///         _fixture.Reset();
///     }
///
///     [Fact]
///     public async Task Should_Get_Weather()
///     {
///         _fixture.StubGet("/weather/London", new { temp = 20, city = "London" });
///
///         var api = _fixture.CreateClient();
///         var result = await api.GetWeatherAsync("London");
///
///         result.Temp.ShouldBe(20);
///     }
/// }
/// </code>
/// </example>
public sealed class EncinaRefitMockFixture<TApiClient> : IAsyncLifetime
    where TApiClient : class
{
    private WireMockServer? _server;
    private IServiceProvider? _serviceProvider;
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
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization.</exception>
    public int Port => Server.Port;

    /// <summary>
    /// Initializes a new instance of <see cref="EncinaRefitMockFixture{TApiClient}"/>.
    /// </summary>
    public EncinaRefitMockFixture()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc/>
    public ValueTask InitializeAsync()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            UseSSL = false,
            StartAdminInterface = false
        });

        var services = new ServiceCollection();
        services.AddRefitClient<TApiClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(BaseUrl));

        _serviceProvider = services.BuildServiceProvider();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _server?.Stop();
        _server?.Dispose();
    }

    /// <summary>
    /// Creates a configured Refit API client connected to the WireMock server.
    /// </summary>
    /// <returns>The Refit API client instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization.</exception>
    public TApiClient CreateClient()
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
        }

        return _serviceProvider.GetRequiredService<TApiClient>();
    }

    /// <summary>
    /// Stubs a GET request with the specified path and response.
    /// </summary>
    /// <param name="path">The URL path to match (e.g., "/api/users").</param>
    /// <param name="response">The response object to serialize as JSON.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaRefitMockFixture<TApiClient> StubGet(string path, object response, int statusCode = 200)
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
    public EncinaRefitMockFixture<TApiClient> StubPost(string path, object response, int statusCode = 201)
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
    public EncinaRefitMockFixture<TApiClient> StubPut(string path, object response, int statusCode = 200)
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
    public EncinaRefitMockFixture<TApiClient> StubPatch(string path, object response, int statusCode = 200)
    {
        return Stub("PATCH", path, response: response, statusCode: statusCode);
    }

    /// <summary>
    /// Stubs a DELETE request with the specified path.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="statusCode">The HTTP status code to return (default: 204).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaRefitMockFixture<TApiClient> StubDelete(string path, int statusCode = 204)
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
    public EncinaRefitMockFixture<TApiClient> Stub(
        string method,
        string path,
        Func<WireMockRequestBuilder, WireMockRequestBuilder>? requestConfig = null,
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
    /// Stubs a request to return an error response.
    /// </summary>
    /// <param name="path">The URL path to match.</param>
    /// <param name="statusCode">The HTTP error status code.</param>
    /// <param name="errorResponse">Optional error response body.</param>
    /// <param name="method">The HTTP method to match (default: any).</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaRefitMockFixture<TApiClient> StubError(
        string path,
        int statusCode,
        object? errorResponse = null,
        string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var request = Request.Create().WithPath(path);
        if (method is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(method);
            request = request.UsingMethod(method);
        }

        var responseBuilder = Response.Create()
            .WithStatusCode(statusCode);

        if (errorResponse is not null)
        {
            var json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            responseBuilder = responseBuilder
                .WithHeader("Content-Type", "application/json")
                .WithBody(json);
        }

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
    public EncinaRefitMockFixture<TApiClient> StubDelay(
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
            ArgumentException.ThrowIfNullOrWhiteSpace(method);
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
    /// Verifies that a request was made to the specified path.
    /// </summary>
    /// <param name="path">The URL path to check.</param>
    /// <param name="times">Expected number of calls (default: 1).</param>
    /// <param name="method">The HTTP method to check (default: any).</param>
    /// <exception cref="InvalidOperationException">Thrown when verification fails.</exception>
    public void VerifyCallMade(string path, int times = 1, string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var requests = Server.LogEntries
            .Where(e => e.RequestMessage.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        if (method is not null)
        {
            requests = requests.Where(e => e.RequestMessage.Method.Equals(method, StringComparison.OrdinalIgnoreCase));
        }

        var matchedRequests = requests.ToList();
        if (matchedRequests.Count != times)
        {
            var methodInfo = method is not null ? $"{method} " : "";
            throw new InvalidOperationException(
                $"Expected {times} call(s) to {methodInfo}{path}, but found {matchedRequests.Count}.");
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
}
