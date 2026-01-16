using System.Net;
using System.Text.Json;
using Encina.Testing.WireMock;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Tests for verifying request/response sequences and ordering.
/// </summary>
public sealed class SequenceTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task GetReceivedRequests_ShouldMaintainOrder()
    {
        // Arrange
        _fixture
            .StubGet("/api/first", new { order = 1 })
            .StubGet("/api/second", new { order = 2 })
            .StubGet("/api/third", new { order = 3 });

        using var client = _fixture.CreateClient();

        // Act - Make requests in specific order
        await client.GetAsync("/api/first");
        await client.GetAsync("/api/second");
        await client.GetAsync("/api/third");

        // Assert
        var requests = _fixture.GetReceivedRequests();
        requests.Count.ShouldBe(3);
        requests[0].Path.ShouldBe("/api/first");
        requests[1].Path.ShouldBe("/api/second");
        requests[2].Path.ShouldBe("/api/third");
    }

    [Fact]
    public async Task GetReceivedRequests_ShouldCaptureTimestamps()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });

        using var client = _fixture.CreateClient();
        var before = DateTime.UtcNow;

        // Act
        await client.GetAsync("/api/data");

        var after = DateTime.UtcNow;

        // Assert
        var requests = _fixture.GetReceivedRequests();
        requests.Count.ShouldBe(1);
        requests[0].Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        requests[0].Timestamp.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public async Task GetReceivedRequests_ShouldCaptureRequestBody()
    {
        // Arrange
        _fixture.StubPost("/api/items", new { id = 1 });

        using var client = _fixture.CreateClient();
        var requestBody = """{"name": "Test Item", "quantity": 5}""";

        // Act
        await client.PostAsync("/api/items",
            new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        var requests = _fixture.GetReceivedRequests();
        requests.Count.ShouldBe(1);
        requests[0].Body.ShouldContain("Test Item");
        requests[0].Body.ShouldContain("quantity");
    }

    [Fact]
    public async Task GetReceivedRequests_ShouldCaptureHeaders()
    {
        // Arrange
        _fixture.StubGet("/api/auth", new { });

        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        client.DefaultRequestHeaders.Add("X-Custom-Header", "custom-value");

        // Act
        await client.GetAsync("/api/auth");

        // Assert
        var requests = _fixture.GetReceivedRequests();
        requests.Count.ShouldBe(1);

        requests[0].Headers.ShouldContainKey("Authorization");
        requests[0].Headers["Authorization"].ShouldContain("Bearer test-token");

        requests[0].Headers.ShouldContainKey("X-Custom-Header");
        requests[0].Headers["X-Custom-Header"].ShouldContain("custom-value");
    }

    [Fact]
    public async Task MultipleEndpoints_ShouldBeVerifiedIndependently()
    {
        // Arrange
        _fixture
            .StubGet("/api/users", new { })
            .StubGet("/api/orders", new { })
            .StubPost("/api/items", new { });

        using var client = _fixture.CreateClient();

        // Act
        await client.GetAsync("/api/users");
        await client.GetAsync("/api/users");
        await client.GetAsync("/api/orders");
        await client.PostAsync("/api/items", new StringContent("{}"));

        // Assert
        _fixture.VerifyCallMade("/api/users", times: 2);
        _fixture.VerifyCallMade("/api/orders", times: 1);
        _fixture.VerifyCallMade("/api/items", times: 1, method: "POST");
    }

    [Fact]
    public async Task SamePathDifferentMethods_ShouldBeTrackedSeparately()
    {
        // Arrange
        _fixture
            .StubGet("/api/resource", new { method = "GET" })
            .StubPost("/api/resource", new { method = "POST" })
            .StubPut("/api/resource", new { method = "PUT" })
            .StubDelete("/api/resource");

        using var client = _fixture.CreateClient();

        // Act
        await client.GetAsync("/api/resource");
        await client.PostAsync("/api/resource", new StringContent("{}"));
        await client.PutAsync("/api/resource", new StringContent("{}"));
        await client.DeleteAsync("/api/resource");

        // Assert
        _fixture.VerifyCallMade("/api/resource", method: "GET", times: 1);
        _fixture.VerifyCallMade("/api/resource", method: "POST", times: 1);
        _fixture.VerifyCallMade("/api/resource", method: "PUT", times: 1);
        _fixture.VerifyCallMade("/api/resource", method: "DELETE", times: 1);

        var allRequests = _fixture.GetReceivedRequests();
        allRequests.Count.ShouldBe(4);
    }

    [Fact]
    public async Task ResetRequestHistory_ShouldClearHistoryButKeepStubs()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { value = "test" });

        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");
        _fixture.VerifyCallMade("/api/data", times: 1);

        // Act
        _fixture.ResetRequestHistory();

        // Assert - History is cleared
        _fixture.VerifyNoCallsMade("/api/data");

        // But stubs still work
        var response = await client.GetAsync("/api/data");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // And new calls are tracked
        _fixture.VerifyCallMade("/api/data", times: 1);
    }

    [Fact]
    public async Task Reset_ShouldClearBothStubsAndHistory()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { value = "test" });

        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");

        // Act
        _fixture.Reset();

        // Assert - History is cleared
        _fixture.GetReceivedRequests().Count.ShouldBe(0);

        // And stub no longer works (returns 404 or similar)
        var response = await client.GetAsync("/api/data");
        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task WorkflowSequence_ShouldBeVerifiable()
    {
        // Arrange - Simulate a typical workflow: Create -> Get -> Update -> Delete
        _fixture
            .StubPost("/api/items", new { id = 123 })
            .StubGet("/api/items/123", new { id = 123, name = "New Item" })
            .StubPut("/api/items/123", new { id = 123, name = "Updated Item" })
            .StubDelete("/api/items/123");

        using var client = _fixture.CreateClient();

        // Act - Execute workflow
        await client.PostAsync("/api/items", new StringContent("""{"name": "New Item"}""", System.Text.Encoding.UTF8, "application/json"));
        await client.GetAsync("/api/items/123");
        await client.PutAsync("/api/items/123", new StringContent("""{"name": "Updated Item"}""", System.Text.Encoding.UTF8, "application/json"));
        await client.DeleteAsync("/api/items/123");

        // Assert - Verify workflow sequence
        var requests = _fixture.GetReceivedRequests();
        requests.Count.ShouldBe(4);

        requests[0].Method.ShouldBe("POST");
        requests[0].Path.ShouldBe("/api/items");
        requests[0].Body.ShouldContain("New Item");

        requests[1].Method.ShouldBe("GET");
        requests[1].Path.ShouldBe("/api/items/123");

        requests[2].Method.ShouldBe("PUT");
        requests[2].Path.ShouldBe("/api/items/123");
        requests[2].Body.ShouldContain("Updated Item");

        requests[3].Method.ShouldBe("DELETE");
        requests[3].Path.ShouldBe("/api/items/123");
    }

    [Fact]
    public async Task PollingPattern_ShouldRespectMaxAttempts()
    {
        // Arrange - Configure a fixed response (simulating a service that never completes)
        const int maxAttempts = 5;
        _fixture.Stub("GET", "/api/status", response: new { status = "pending", progress = 50 });

        using var client = _fixture.CreateClient();

        // Act - Poll until max attempts reached
        var actualAttempts = 0;
        var finalStatus = "unknown";

        for (var i = 0; i < maxAttempts; i++)
        {
            actualAttempts++;
            var response = await client.GetAsync("/api/status");
            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<JsonElement>(content);
            finalStatus = status.GetProperty("status").GetString() ?? "unknown";

            if (finalStatus == "completed")
            {
                break;
            }

            await Task.Delay(10); // Small delay between polls
        }

        // Assert - Verify polling stopped at max attempts without completing
        finalStatus.ShouldBe("pending"); // Never reached completed
        actualAttempts.ShouldBe(maxAttempts);

        // Verify actual calls made match our attempts
        var logEntries = _fixture.Server.LogEntries
            .Where(e => e.RequestMessage.Path == "/api/status")
            .ToList();
        logEntries.Count.ShouldBe(actualAttempts);
        logEntries.Count.ShouldBeLessThanOrEqualTo(maxAttempts);
    }

    [Fact]
    public async Task PollingPattern_ShouldStopEarlyWhenCompleted()
    {
        // Arrange - First call returns completed immediately
        const int maxAttempts = 10;
        _fixture.Stub("GET", "/api/status/quick", response: new { status = "completed", progress = 100 });

        using var client = _fixture.CreateClient();

        // Act - Poll until completed or max attempts
        var actualAttempts = 0;
        var finalStatus = "unknown";

        for (var i = 0; i < maxAttempts; i++)
        {
            actualAttempts++;
            var response = await client.GetAsync("/api/status/quick");
            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<JsonElement>(content);
            finalStatus = status.GetProperty("status").GetString() ?? "unknown";

            if (finalStatus == "completed")
            {
                break;
            }
        }

        // Assert - Verify we stopped early
        finalStatus.ShouldBe("completed");
        actualAttempts.ShouldBe(1); // Should stop on first attempt

        var logEntries = _fixture.Server.LogEntries
            .Where(e => e.RequestMessage.Path == "/api/status/quick")
            .ToList();
        logEntries.Count.ShouldBe(actualAttempts);
    }
}
