using System.Net.Http.Json;
using Encina.Testing.WireMock;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using EncinaFaultType = Encina.Testing.WireMock.FaultType;

namespace Encina.Refit.Tests;

/// <summary>
/// Example tests demonstrating comprehensive WireMock capabilities with Refit clients.
/// These tests serve as developer reference examples for various mocking patterns.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples of:
/// <list type="bullet">
///   <item>Basic request/response mocking with <see cref="EncinaRefitMockFixture{TApiClient}"/></item>
///   <item>Request matching with headers, query parameters, and body content</item>
///   <item>Response sequences for testing retry logic</item>
///   <item>Fault injection for testing error handling</item>
///   <item>Webhook testing for outbound notifications</item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
public class RefitMockingExamplesTests : IClassFixture<EncinaWireMockFixture>, IAsyncLifetime
{
    private static readonly string[] SearchResults = ["result1", "result2"];

    private readonly EncinaWireMockFixture _fixture;
    private HttpClient _httpClient = null!;

    public RefitMockingExamplesTests(EncinaWireMockFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.Reset();
        _httpClient = _fixture.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    #region Basic Mocking Patterns

    /// <summary>
    /// Demonstrates basic GET request stubbing with JSON response.
    /// </summary>
    [Fact]
    public async Task StubGet_BasicPattern_ReturnsExpectedResponse()
    {
        // Arrange
        var expectedUser = new UserResponse { Id = 1, Name = "John Doe", Email = "john@example.com" };
        _fixture.StubGet("/api/users/1", expectedUser);

        // Act
        var response = await _httpClient.GetAsync("/api/users/1");
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        user.ShouldNotBeNull();
        user.Id.ShouldBe(1);
        user.Name.ShouldBe("John Doe");
        _fixture.VerifyCallMade("/api/users/1", times: 1, method: "GET");
    }

    /// <summary>
    /// Demonstrates POST request stubbing with request body validation.
    /// </summary>
    [Fact]
    public async Task StubPost_BasicPattern_ReturnsCreatedResponse()
    {
        // Arrange
        var createdUser = new UserResponse { Id = 42, Name = "Jane Doe", Email = "jane@example.com" };
        _fixture.StubPost("/api/users", createdUser, statusCode: 201);

        var request = new { name = "Jane Doe", email = "jane@example.com" };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/users", request);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);
        user.ShouldNotBeNull();
        user.Id.ShouldBe(42);
        _fixture.VerifyCallMade("/api/users", times: 1, method: "POST");
    }

    /// <summary>
    /// Demonstrates PUT request stubbing for updates.
    /// </summary>
    [Fact]
    public async Task StubPut_BasicPattern_ReturnsUpdatedResponse()
    {
        // Arrange
        var updatedUser = new UserResponse { Id = 1, Name = "John Updated", Email = "john.updated@example.com" };
        _fixture.StubPut("/api/users/1", updatedUser);

        var request = new { name = "John Updated", email = "john.updated@example.com" };

        // Act
        var response = await _httpClient.PutAsJsonAsync("/api/users/1", request);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        _fixture.VerifyCallMade("/api/users/1", times: 1, method: "PUT");
    }

    /// <summary>
    /// Demonstrates DELETE request stubbing.
    /// </summary>
    [Fact]
    public async Task StubDelete_BasicPattern_ReturnsNoContent()
    {
        // Arrange
        _fixture.StubDelete("/api/users/1");

        // Act
        var response = await _httpClient.DeleteAsync("/api/users/1");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);
        _fixture.VerifyCallMade("/api/users/1", times: 1, method: "DELETE");
    }

    #endregion

    #region Request Matching Patterns

    /// <summary>
    /// Demonstrates request matching with custom headers.
    /// </summary>
    [Fact]
    public async Task Stub_WithHeaderMatching_MatchesCorrectRequest()
    {
        // Arrange - Only match requests with specific header
        _fixture.Server.Given(
            Request.Create()
                .WithPath("/api/protected")
                .UsingMethod("GET")
                .WithHeader("Authorization", "Bearer valid-token"))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new { message = "Protected data" }));

        // Arrange - Requests without header should get 401
        _fixture.Server.Given(
            Request.Create()
                .WithPath("/api/protected")
                .UsingMethod("GET"))
            .AtPriority(100)  // Lower priority = matched last
            .RespondWith(
                Response.Create()
                    .WithStatusCode(401)
                    .WithBodyAsJson(new { error = "Unauthorized" }));

        // Act - With auth header
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer valid-token");
        var authorizedResponse = await _httpClient.GetAsync("/api/protected");

        // Assert
        authorizedResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Demonstrates request matching with query parameters.
    /// </summary>
    [Fact]
    public async Task Stub_WithQueryParameters_MatchesCorrectRequest()
    {
        // Arrange
        _fixture.Server.Given(
            Request.Create()
                .WithPath("/api/search")
                .UsingMethod("GET")
                .WithParam("q", "test")
                .WithParam("limit", "10"))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new { results = SearchResults, total = 2 }));

        // Act
        var response = await _httpClient.GetAsync("/api/search?q=test&limit=10");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    #endregion

    #region Response Sequences

    /// <summary>
    /// Demonstrates response sequences for testing retry logic.
    /// First request fails, second succeeds.
    /// </summary>
    /// <remarks>
    /// This test uses unique paths to avoid state conflicts when tests run in parallel or sequence.
    /// WireMock scenarios are stateful, so each test should use a unique path.
    /// </remarks>
    [Fact]
    public async Task StubSequence_RetryPattern_SucceedsOnSecondAttempt()
    {
        // Arrange - First call fails with 503, second succeeds
        // Use unique path to ensure scenario isolation
        var path = $"/api/flaky/{Guid.NewGuid():N}";
        _fixture.StubSequence("GET", path,
            (new { error = "Service temporarily unavailable" }, 503),
            (new { data = "Success on retry" }, 200));

        // Act - First call
        var response1 = await _httpClient.GetAsync(path);

        // Assert - First call fails
        response1.StatusCode.ShouldBe(System.Net.HttpStatusCode.ServiceUnavailable);

        // Act - Second call (retry)
        var response2 = await _httpClient.GetAsync(path);

        // Assert - Second call succeeds
        response2.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Demonstrates response sequences for testing rate limiting scenarios.
    /// </summary>
    /// <remarks>
    /// This test uses unique paths to avoid state conflicts when tests run in parallel or sequence.
    /// WireMock scenarios are stateful, so each test should use a unique path.
    /// </remarks>
    [Fact]
    public async Task StubSequence_RateLimiting_ReturnsThrottleResponse()
    {
        // Arrange - Allow 2 calls, then throttle
        // Use unique path to ensure scenario isolation
        var path = $"/api/rate-limited/{Guid.NewGuid():N}";
        _fixture.StubSequence("GET", path,
            (new { data = "First call OK" }, 200),
            (new { data = "Second call OK" }, 200),
            (new { error = "Too many requests", retryAfter = 60 }, 429));

        // Act & Assert
        var response1 = await _httpClient.GetAsync(path);
        response1.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var response2 = await _httpClient.GetAsync(path);
        response2.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var response3 = await _httpClient.GetAsync(path);
        response3.StatusCode.ShouldBe(System.Net.HttpStatusCode.TooManyRequests);
    }

    #endregion

    #region Fault Injection

    /// <summary>
    /// Demonstrates timeout fault injection for testing timeout handling.
    /// </summary>
    [Fact]
    public async Task StubFault_Timeout_CausesClientTimeout()
    {
        // Arrange - Configure short timeout on client
        using var timeoutClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = TimeSpan.FromMilliseconds(500)
        };

        _fixture.StubFault("/api/slow", EncinaFaultType.Timeout, method: "GET");

        // Act & Assert
        await Should.ThrowAsync<TaskCanceledException>(async () =>
        {
            await timeoutClient.GetAsync("/api/slow");
        });
    }

    /// <summary>
    /// Demonstrates delayed response for testing timeout handling with specific delay.
    /// </summary>
    [Fact]
    public async Task StubDelay_SlowResponse_CausesTimeout()
    {
        // Arrange - Configure short timeout on client
        using var timeoutClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = TimeSpan.FromMilliseconds(500)
        };

        _fixture.StubDelay("/api/delayed", TimeSpan.FromSeconds(5), new { data = "Delayed response" }, method: "GET");

        // Act & Assert
        await Should.ThrowAsync<TaskCanceledException>(async () =>
        {
            await timeoutClient.GetAsync("/api/delayed");
        });
    }

    /// <summary>
    /// Demonstrates empty response fault injection.
    /// </summary>
    [Fact]
    public async Task StubFault_EmptyResponse_ReturnsEmptyBody()
    {
        // Arrange
        _fixture.StubFault("/api/empty", EncinaFaultType.EmptyResponse, method: "GET");

        // Act
        var response = await _httpClient.GetAsync("/api/empty");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Response body should be empty
        content.ShouldBeEmpty();
    }

    #endregion

    #region Webhook Testing

    /// <summary>
    /// Demonstrates webhook endpoint setup for testing outbound notifications.
    /// </summary>
    [Fact]
    public async Task WebhookEndpoint_ReceivesNotification_CanBeVerified()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        // Act - Simulate application sending webhook
        var notification = new { eventType = "user.created", userId = 123 };
        var response = await _httpClient.PostAsJsonAsync("/webhooks/notifications", notification);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        _fixture.VerifyWebhookReceived("/webhooks/notifications", times: 1);
    }

    /// <summary>
    /// Demonstrates webhook body inspection for validating payload content.
    /// </summary>
    [Fact]
    public async Task WebhookEndpoint_InspectPayload_ValidatesContent()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/orders");

        // Act - Send order webhook
        var orderWebhook = new OrderWebhookPayload
        {
            OrderId = "ORD-123",
            Status = "completed",
            Total = 99.99m
        };
        await _httpClient.PostAsJsonAsync("/webhooks/orders", orderWebhook);

        // Assert - Verify and inspect payload
        var receivedWebhooks = _fixture.GetReceivedWebhookBodies<OrderWebhookPayload>("/webhooks/orders");
        receivedWebhooks.Count.ShouldBe(1);
        receivedWebhooks[0].OrderId.ShouldBe("ORD-123");
        receivedWebhooks[0].Status.ShouldBe("completed");
    }

    /// <summary>
    /// Demonstrates webhook failure setup for testing retry logic.
    /// </summary>
    [Fact]
    public async Task WebhookFailure_Returns503_AllowsRetryTesting()
    {
        // Arrange - Webhook endpoint fails first, then succeeds
        _fixture.SetupWebhookFailure("/webhooks/retry-test", statusCode: 503, errorMessage: "Service unavailable");

        // Act
        var notification = new { eventType = "test" };
        var response = await _httpClient.PostAsJsonAsync("/webhooks/retry-test", notification);

        // Assert - First call fails
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.ServiceUnavailable);

        // Now reset and setup success for retry
        _fixture.Reset();
        _fixture.SetupWebhookEndpoint("/webhooks/retry-test");

        // Retry
        var retryResponse = await _httpClient.PostAsJsonAsync("/webhooks/retry-test", notification);
        retryResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    #endregion

    #region Error Response Patterns

    /// <summary>
    /// Demonstrates various HTTP error status code handling.
    /// </summary>
    [Theory]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(502, "Bad Gateway")]
    [InlineData(503, "Service Unavailable")]
    public async Task StubError_VariousStatusCodes_ReturnsCorrectError(int statusCode, string errorMessage)
    {
        // Arrange
        _fixture.Server.Given(
            Request.Create()
                .WithPath($"/api/error/{statusCode}")
                .UsingMethod("GET"))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(statusCode)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new { error = errorMessage, statusCode }));

        // Act
        var response = await _httpClient.GetAsync($"/api/error/{statusCode}");

        // Assert
        ((int)response.StatusCode).ShouldBe(statusCode);
    }

    /// <summary>
    /// Demonstrates validation error response pattern (400 with details).
    /// </summary>
    [Fact]
    public async Task StubError_ValidationErrors_ReturnsDetailedErrors()
    {
        // Arrange
        var validationErrors = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "Validation Failed",
            status = 400,
            errors = new
            {
                name = new[] { "Name is required", "Name must be at least 3 characters" },
                email = new[] { "Invalid email format" }
            }
        };

        _fixture.Server.Given(
            Request.Create()
                .WithPath("/api/users")
                .UsingMethod("POST"))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(400)
                    .WithHeader("Content-Type", "application/problem+json")
                    .WithBodyAsJson(validationErrors));

        // Act
        var invalidUser = new { name = "", email = "invalid" };
        var response = await _httpClient.PostAsJsonAsync("/api/users", invalidUser);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    #endregion

    #region Request History and Verification

    /// <summary>
    /// Demonstrates verifying no unwanted calls were made.
    /// </summary>
    [Fact]
    public async Task VerifyNoCallsMade_WhenNotCalled_Succeeds()
    {
        // Arrange
        _fixture.StubGet("/api/should-not-call", new { data = "test" });

        // Act - Don't make any calls

        // Assert - Verify no calls were made
        _fixture.VerifyNoCallsMade("/api/should-not-call");
    }

    /// <summary>
    /// Demonstrates verifying exact number of calls.
    /// </summary>
    [Fact]
    public async Task VerifyCallMade_ExactCount_MatchesExpectation()
    {
        // Arrange
        _fixture.StubGet("/api/count-test", new { data = "test" });

        // Act - Make exactly 3 calls
        await _httpClient.GetAsync("/api/count-test");
        await _httpClient.GetAsync("/api/count-test");
        await _httpClient.GetAsync("/api/count-test");

        // Assert - Verify exactly 3 calls
        _fixture.VerifyCallMade("/api/count-test", times: 3, method: "GET");
    }

    /// <summary>
    /// Demonstrates inspecting all received requests.
    /// </summary>
    [Fact]
    public async Task GetReceivedRequests_InspectsAllCalls_ProvidesDetails()
    {
        // Arrange
        _fixture.StubGet("/api/inspect", new { data = "test" });

        // Act
        _httpClient.DefaultRequestHeaders.Add("X-Trace-Id", "trace-123");
        await _httpClient.GetAsync("/api/inspect");

        // Assert
        var requests = _fixture.GetReceivedRequests();
        requests.Count.ShouldBe(1);
        requests[0].Path.ShouldBe("/api/inspect");
        requests[0].Method.ShouldBe("GET");
        requests[0].Headers.ShouldContainKey("X-Trace-Id");
    }

    #endregion

    #region Test Models

    private sealed class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class OrderWebhookPayload
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    #endregion
}
