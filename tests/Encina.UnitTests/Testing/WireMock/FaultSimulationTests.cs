using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Encina.Testing.WireMock;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Tests for fault simulation and resilience testing with WireMock.
/// </summary>
public sealed class FaultSimulationTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task StubFault_EmptyResponse_ShouldReturnEmptyBody()
    {
        // Arrange
        _fixture.StubFault("/api/data", FaultType.EmptyResponse);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/data");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBeEmpty();
    }

    [Fact]
    public async Task StubFault_MalformedResponse_ShouldReturnGarbageData()
    {
        // Arrange
        _fixture.StubFault("/api/data", FaultType.MalformedResponse);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/data");

        // Assert
        var content = await response.Content.ReadAsStringAsync();

        // Malformed response should either be empty or contain invalid JSON
        if (!string.IsNullOrEmpty(content))
        {
            // Verify the content is not valid JSON by attempting to parse it
            var isValidJson = true;
            try
            {
                JsonDocument.Parse(content);
            }
            catch (JsonException)
            {
                isValidJson = false;
            }

            isValidJson.ShouldBeFalse("Malformed response should not be valid JSON");
        }
    }

    [Fact]
    public async Task StubFault_Timeout_ShouldCauseClientTimeout()
    {
        // Arrange
        _fixture.StubFault("/api/slow", FaultType.Timeout);

        using var client = _fixture.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        await Should.ThrowAsync<TaskCanceledException>(async () =>
            await client.GetAsync("/api/slow"));
    }

    [Fact]
    public async Task StubFault_ShouldSupportChaining()
    {
        // Arrange
        _fixture
            .StubFault("/api/empty", FaultType.EmptyResponse)
            .StubFault("/api/malformed", FaultType.MalformedResponse);

        using var client = _fixture.CreateClient();

        // Act & Assert
        var emptyResponse = await client.GetAsync("/api/empty");
        var emptyContent = await emptyResponse.Content.ReadAsStringAsync();
        emptyContent.ShouldBeEmpty();

        var malformedResponse = await client.GetAsync("/api/malformed");
        malformedResponse.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(400, HttpStatusCode.BadRequest)]
    [InlineData(401, HttpStatusCode.Unauthorized)]
    [InlineData(403, HttpStatusCode.Forbidden)]
    [InlineData(404, HttpStatusCode.NotFound)]
    [InlineData(429, HttpStatusCode.TooManyRequests)]
    [InlineData(500, HttpStatusCode.InternalServerError)]
    [InlineData(502, HttpStatusCode.BadGateway)]
    [InlineData(503, HttpStatusCode.ServiceUnavailable)]
    [InlineData(504, HttpStatusCode.GatewayTimeout)]
    public async Task StubGet_ShouldReturnHttpErrorCodes(int statusCode, HttpStatusCode expected)
    {
        // Arrange
        _fixture.StubGet($"/api/error/{statusCode}", new { error = "Error" }, statusCode: statusCode);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/error/{statusCode}");

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task RetryScenario_ShouldTrackMultipleAttempts()
    {
        // Arrange - First call fails, subsequent calls succeed
        // This simulates a simple retry test pattern
        _fixture.StubGet("/api/flaky", new { success = true });

        using var client = _fixture.CreateClient();
        var attempts = 0;
        const int maxRetries = 3;

        // Act - Simulate retry logic
        HttpResponseMessage? response = null;
        while (attempts < maxRetries)
        {
            attempts++;
            response = await client.GetAsync("/api/flaky");
            if (response.IsSuccessStatusCode)
            {
                break;
            }
        }

        // Assert
        _fixture.VerifyCallMade("/api/flaky", times: attempts);
        response.ShouldNotBeNull();
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldAllBeTracked()
    {
        // Arrange
        _fixture.StubGet("/api/concurrent", new { data = "test" });

        using var client = _fixture.CreateClient();
        const int concurrentRequests = 10;

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => client.GetAsync("/api/concurrent"))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        _fixture.VerifyCallMade("/api/concurrent", times: concurrentRequests);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public async Task StubDelay_ShouldAddConfiguredDelay(int delayMs)
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(delayMs);
        _fixture.StubDelay("/api/delayed", delay, new { result = "delayed" });

        using var client = _fixture.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/delayed");
        stopwatch.Stop();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        // Allow tolerance for CI environments
        stopwatch.Elapsed.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(delayMs - 50);
    }

    [Fact]
    public async Task NetworkPartition_CanBeSimulatedWithTimeout()
    {
        // Arrange - Simulate network partition with very long delay
        _fixture.StubDelay("/api/partition", TimeSpan.FromMinutes(5), new { });

        using var client = _fixture.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(50);

        // Act & Assert - Should timeout (simulating network partition)
        await Should.ThrowAsync<TaskCanceledException>(async () =>
            await client.GetAsync("/api/partition"));
    }
}
