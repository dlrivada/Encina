using System.Net;
using Encina.Testing.WireMock;

namespace Encina.Testing.Examples.Fixtures;

/// <summary>
/// Examples demonstrating EncinaWireMockFixture for HTTP API mocking.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 9.1
/// </summary>
public sealed class WireMockFixtureExamples : IClassFixture<EncinaWireMockFixture>
{
    private readonly EncinaWireMockFixture _wireMock;

    public WireMockFixtureExamples(EncinaWireMockFixture wireMock)
    {
        _wireMock = wireMock;
        _wireMock.Reset(); // Reset between tests for isolation
    }

    /// <summary>
    /// Pattern: Stub GET request with JSON response.
    /// </summary>
    [Fact]
    public async Task StubGet_ReturnsJsonResponse()
    {
        // Arrange
        _wireMock.StubGet("/api/users/1", new { id = 1, name = "John Doe" });

        // Act
        using var client = _wireMock.CreateClient();
        var response = await client.GetAsync("/api/users/1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("John Doe");
    }

    /// <summary>
    /// Pattern: Stub POST request.
    /// </summary>
    [Fact]
    public async Task StubPost_ReturnsCreatedResponse()
    {
        // Arrange
        _wireMock.StubPost("/api/orders", new { orderId = "ORD-001" }, statusCode: 201);

        // Act
        using var client = _wireMock.CreateClient();
        var response = await client.PostAsync("/api/orders",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>
    /// Pattern: Stub PUT and PATCH requests.
    /// </summary>
    [Fact]
    public async Task StubPutAndPatch_ReturnsOkResponse()
    {
        // Arrange
        _wireMock.StubPut("/api/users/1", new { updated = true });
        _wireMock.StubPatch("/api/users/1", new { patched = true });

        // Act
        using var client = _wireMock.CreateClient();
        var putResponse = await client.PutAsync("/api/users/1",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        var patchResponse = await client.PatchAsync("/api/users/1",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        putResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Pattern: Stub DELETE request.
    /// </summary>
    [Fact]
    public async Task StubDelete_ReturnsNoContent()
    {
        // Arrange
        _wireMock.StubDelete("/api/users/1");

        // Act
        using var client = _wireMock.CreateClient();
        var response = await client.DeleteAsync("/api/users/1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Pattern: Stub fault conditions for resilience testing.
    /// </summary>
    [Fact]
    public async Task StubFault_SimulatesServerErrors()
    {
        // Arrange
        _wireMock.StubFault("/api/flaky", FaultType.EmptyResponse);

        // Act
        using var client = _wireMock.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        // Assert - Should throw or return empty response
        try
        {
            await client.GetAsync("/api/flaky");
        }
        catch (HttpRequestException)
        {
            // Expected for empty response fault
            return;
        }
    }

    /// <summary>
    /// Pattern: Stub delayed response for timeout testing.
    /// </summary>
    [Fact]
    public async Task StubDelay_SimulatesSlowResponse()
    {
        // Arrange
        _wireMock.StubDelay(
            "/api/slow",
            delay: TimeSpan.FromMilliseconds(100),
            response: new { status = "ok" });

        // Act
        using var client = _wireMock.CreateClient();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await client.GetAsync("/api/slow");
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(100);
    }

    /// <summary>
    /// Pattern: Stub response sequence for retry testing.
    /// </summary>
    [Fact]
    public async Task StubSequence_SimulatesRetryScenario()
    {
        // Arrange - First call fails, second succeeds
        _wireMock.StubSequence("GET", "/api/retry",
            (new { error = "Temporary failure" }, 503),
            (new { status = "ok" }, 200));

        // Act
        using var client = _wireMock.CreateClient();
        var firstResponse = await client.GetAsync("/api/retry");
        var secondResponse = await client.GetAsync("/api/retry");

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Pattern: Verify calls were made.
    /// </summary>
    [Fact]
    public async Task VerifyCallMade_ChecksRequestWasSent()
    {
        // Arrange
        _wireMock.StubGet("/api/users", new[] { new { id = 1 } });

        // Act
        using var client = _wireMock.CreateClient();
        await client.GetAsync("/api/users");
        await client.GetAsync("/api/users");

        // Assert
        _wireMock.VerifyCallMade("/api/users", times: 2);
    }

    /// <summary>
    /// Pattern: Verify no calls were made.
    /// </summary>
    [Fact]
    public void VerifyNoCallsMade_ChecksNoRequests()
    {
        // Arrange
        _wireMock.StubGet("/api/never-called", new { });

        // Act - Don't call the endpoint

        // Assert
        _wireMock.VerifyNoCallsMade("/api/never-called");
    }

    /// <summary>
    /// Pattern: Get received requests for inspection.
    /// </summary>
    [Fact]
    public async Task GetReceivedRequests_InspectsRequestDetails()
    {
        // Arrange
        _wireMock.StubPost("/api/logs", new { received = true });

        // Act
        using var client = _wireMock.CreateClient();
        await client.PostAsync("/api/logs",
            new StringContent("{\"level\":\"info\"}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        var requests = _wireMock.GetReceivedRequests();
        requests.ShouldNotBeEmpty();

        var request = requests[0];
        request.Method.ShouldBe("POST");
        request.Path.ShouldBe("/api/logs");
        request.Body.ShouldContain("info");
    }
}
