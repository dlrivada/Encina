using System.Text.Json;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Encina.Testing.WireMock;

/// <summary>
/// Extension methods for testing webhook endpoints with WireMock.
/// </summary>
/// <remarks>
/// These extensions simplify testing scenarios where your application sends
/// webhook callbacks (e.g., from the Outbox pattern) to external systems.
/// </remarks>
public static class WebhookTestingExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Sets up a webhook endpoint that accepts POST requests to the specified path.
    /// </summary>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path (e.g., "/webhooks/outbox").</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <returns>The fixture for method chaining.</returns>
    /// <example>
    /// <code>
    /// _fixture.SetupWebhookEndpoint("/webhooks/notifications");
    ///
    /// // Trigger your application to send a webhook...
    ///
    /// _fixture.VerifyWebhookReceived("/webhooks/notifications");
    /// </code>
    /// </example>
    public static EncinaWireMockFixture SetupWebhookEndpoint(
        this EncinaWireMockFixture fixture,
        string path,
        int statusCode = 200)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var request = Request.Create()
            .WithPath(path)
            .UsingMethod("POST");

        var response = Response.Create()
            .WithStatusCode(statusCode);

        fixture.Server.Given(request).RespondWith(response);

        return fixture;
    }

    /// <summary>
    /// Sets up a webhook endpoint specifically for Outbox pattern testing.
    /// Accepts POST requests with JSON content containing notification data.
    /// </summary>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path (default: "/webhooks/outbox").</param>
    /// <param name="statusCode">The HTTP status code to return (default: 200).</param>
    /// <returns>The fixture for method chaining.</returns>
    /// <remarks>
    /// This sets up an endpoint that:
    /// <list type="bullet">
    /// <item>Accepts POST requests</item>
    /// <item>Expects JSON content type</item>
    /// <item>Returns the specified status code</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Setup outbox webhook endpoint
    /// _fixture.SetupOutboxWebhook();
    ///
    /// // Configure your outbox to send to this endpoint
    /// var webhookUrl = $"{_fixture.BaseUrl}/webhooks/outbox";
    ///
    /// // After processing outbox...
    /// _fixture.VerifyWebhookReceived("/webhooks/outbox");
    /// </code>
    /// </example>
    public static EncinaWireMockFixture SetupOutboxWebhook(
        this EncinaWireMockFixture fixture,
        string path = "/webhooks/outbox",
        int statusCode = 200)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var request = Request.Create()
            .WithPath(path)
            .UsingMethod("POST")
            .WithHeader("Content-Type", new ContentTypeMatcher("application/json"));

        var response = Response.Create()
            .WithStatusCode(statusCode)
            .WithHeader("Content-Type", "application/json")
            .WithBody("{}");

        fixture.Server.Given(request).RespondWith(response);

        return fixture;
    }

    /// <summary>
    /// Sets up a webhook endpoint that fails with the specified error.
    /// Useful for testing retry logic and error handling.
    /// </summary>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path.</param>
    /// <param name="statusCode">The HTTP error status code (default: 500).</param>
    /// <param name="errorMessage">Optional error message in the response body.</param>
    /// <returns>The fixture for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Setup failing webhook to test retry logic
    /// _fixture.SetupWebhookFailure("/webhooks/outbox", 503, "Service temporarily unavailable");
    ///
    /// // Your outbox processor should retry...
    /// </code>
    /// </example>
    public static EncinaWireMockFixture SetupWebhookFailure(
        this EncinaWireMockFixture fixture,
        string path,
        int statusCode = 500,
        string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var request = Request.Create()
            .WithPath(path)
            .UsingMethod("POST");

        var response = Response.Create()
            .WithStatusCode(statusCode);

        if (errorMessage is not null)
        {
            var body = JsonSerializer.Serialize(new { error = errorMessage }, DefaultJsonOptions);
            response = response
                .WithHeader("Content-Type", "application/json")
                .WithBody(body);
        }

        fixture.Server.Given(request).RespondWith(response);

        return fixture;
    }

    /// <summary>
    /// Sets up a webhook endpoint that times out (delayed response).
    /// Useful for testing timeout handling and resilience.
    /// </summary>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path.</param>
    /// <param name="delay">The delay before responding (default: 30 seconds).</param>
    /// <returns>The fixture for method chaining.</returns>
    public static EncinaWireMockFixture SetupWebhookTimeout(
        this EncinaWireMockFixture fixture,
        string path,
        TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var actualDelay = delay ?? TimeSpan.FromSeconds(30);

        var request = Request.Create()
            .WithPath(path)
            .UsingMethod("POST");

        var response = Response.Create()
            .WithStatusCode(200)
            .WithDelay(actualDelay);

        fixture.Server.Given(request).RespondWith(response);

        return fixture;
    }

    /// <summary>
    /// Verifies that a webhook was received at the specified path.
    /// </summary>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path to verify.</param>
    /// <param name="times">Expected number of webhook calls (default: 1).</param>
    /// <exception cref="InvalidOperationException">Thrown when verification fails.</exception>
    public static void VerifyWebhookReceived(
        this EncinaWireMockFixture fixture,
        string path,
        int times = 1)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        fixture.VerifyCallMade(path, times, method: "POST");
    }

    /// <summary>
    /// Verifies that no webhooks were received at the specified path.
    /// </summary>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path to verify.</param>
    /// <exception cref="InvalidOperationException">Thrown when verification fails.</exception>
    public static void VerifyNoWebhooksReceived(
        this EncinaWireMockFixture fixture,
        string path)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        fixture.VerifyNoCallsMade(path, method: "POST");
    }

    /// <summary>
    /// Gets all webhook requests received at the specified path.
    /// </summary>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path.</param>
    /// <returns>Collection of received webhook requests.</returns>
    public static IReadOnlyList<ReceivedRequest> GetReceivedWebhooks(
        this EncinaWireMockFixture fixture,
        string path)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return fixture.GetReceivedRequests()
            .Where(r => r.Path.Equals(path, StringComparison.OrdinalIgnoreCase)
                     && r.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the body content of received webhooks deserialized to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the webhook body to.</typeparam>
    /// <param name="fixture">The WireMock fixture.</param>
    /// <param name="path">The webhook endpoint path.</param>
    /// <returns>Collection of deserialized webhook bodies.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails or returns null.</exception>
    public static IReadOnlyList<T> GetReceivedWebhookBodies<T>(
        this EncinaWireMockFixture fixture,
        string path)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var webhooks = GetReceivedWebhooks(fixture, path);
        var results = new List<T>();

        for (var i = 0; i < webhooks.Count; i++)
        {
            var webhook = webhooks[i];
            if (string.IsNullOrEmpty(webhook.Body))
            {
                throw new InvalidOperationException(
                    $"Webhook [{i}] at path '{path}' has an empty body and cannot be deserialized to {typeof(T).Name}.");
            }

            T? deserialized;
            try
            {
                deserialized = JsonSerializer.Deserialize<T>(webhook.Body, DefaultJsonOptions);
            }
            catch (JsonException ex)
            {
                var bodyPreview = webhook.Body.Length > 200
                    ? webhook.Body[..200] + "..."
                    : webhook.Body;
                throw new InvalidOperationException(
                    $"Webhook [{i}] at path '{path}' failed to deserialize to {typeof(T).Name}. Body preview: {bodyPreview}",
                    ex);
            }

            if (deserialized is null)
            {
                var bodyPreview = webhook.Body.Length > 200
                    ? webhook.Body[..200] + "..."
                    : webhook.Body;
                throw new InvalidOperationException(
                    $"Webhook [{i}] at path '{path}' deserialized to null for type {typeof(T).Name}. Body preview: {bodyPreview}");
            }

            results.Add(deserialized);
        }

        return results.AsReadOnly();
    }
}
