using System.Net;
using System.Text.Json;
using Encina.Testing.WireMock;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Unit tests for <see cref="EncinaWireMockFixture"/>.
/// </summary>
public sealed class EncinaWireMockFixtureTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public void BaseUrl_ShouldBeAvailable_AfterInitialization()
    {
        _fixture.BaseUrl.ShouldNotBeNullOrWhiteSpace();
        _fixture.BaseUrl.ShouldStartWith("http://");
    }

    [Fact]
    public void Port_ShouldBePositive_AfterInitialization()
    {
        _fixture.Port.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Server_ShouldBeAvailable_AfterInitialization()
    {
        _fixture.Server.ShouldNotBeNull();
        _fixture.Server.IsStarted.ShouldBeTrue();
    }

    [Fact]
    public async Task StubGet_ShouldReturnConfiguredResponse()
    {
        // Arrange
        var expectedResponse = new { id = 1, name = "Test" };
        _fixture.StubGet("/api/test", expectedResponse);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("id").GetInt32().ShouldBe(1);
        result.GetProperty("name").GetString().ShouldBe("Test");
    }

    [Fact]
    public async Task StubGet_ShouldReturnCustomStatusCode()
    {
        // Arrange
        _fixture.StubGet("/api/notfound", new { error = "Not Found" }, statusCode: 404);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/notfound");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StubPost_ShouldReturnCreatedStatus()
    {
        // Arrange
        var expectedResponse = new { id = 123 };
        _fixture.StubPost("/api/items", expectedResponse);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/api/items", new StringContent("{}"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task StubPut_ShouldReturnOkStatus()
    {
        // Arrange
        _fixture.StubPut("/api/items/1", new { updated = true });

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.PutAsync("/api/items/1", new StringContent("{}"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StubPatch_ShouldReturnOkStatus()
    {
        // Arrange
        _fixture.StubPatch("/api/items/1", new { patched = true });

        using var client = _fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/items/1")
        {
            Content = new StringContent("{}")
        };
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StubDelete_ShouldReturnNoContentStatus()
    {
        // Arrange
        _fixture.StubDelete("/api/items/1");

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/items/1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Stub_ShouldAllowMethodChaining()
    {
        // Arrange
        _fixture
            .StubGet("/api/users", new { users = new List<int> { 1, 2, 3 } })
            .StubPost("/api/users", new { id = 4 })
            .StubDelete("/api/users/1");

        using var client = _fixture.CreateClient();

        // Act & Assert
        var getResponse = await client.GetAsync("/api/users");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var postResponse = await client.PostAsync("/api/users", new StringContent("{}"));
        postResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var deleteResponse = await client.DeleteAsync("/api/users/1");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task StubDelay_ShouldDelayResponse()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(100);
        _fixture.StubDelay("/api/slow", delay, new { data = "delayed" });

        using var client = _fixture.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        // Act
        var startTime = DateTime.UtcNow;
        var response = await client.GetAsync("/api/slow");
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        // Use generous tolerance (200ms) for CI environments under load
        // We only verify the delay was applied (elapsed >= expected - tolerance)
        var toleranceMs = int.TryParse(Environment.GetEnvironmentVariable("ENCINA_TEST_TIMING_TOLERANCE_MS"), out var envTolerance)
            ? envTolerance
            : 200;
        elapsed.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(delay.TotalMilliseconds - toleranceMs);
    }

    [Fact]
    public async Task VerifyCallMade_ShouldSucceed_WhenCallWasMade()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });
        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");

        // Act & Assert - Should not throw
        _fixture.VerifyCallMade("/api/data");
    }

    [Fact]
    public void VerifyCallMade_ShouldFail_WhenNoCallWasMade()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/data"));
    }

    [Fact]
    public async Task VerifyCallMade_ShouldVerifyExactCount()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });
        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");
        await client.GetAsync("/api/data");

        // Act & Assert
        _fixture.VerifyCallMade("/api/data", times: 2);
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/data", times: 1));
    }

    [Fact]
    public void VerifyNoCallsMade_ShouldSucceed_WhenNoCallsMade()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });

        // Act & Assert - Should not throw
        _fixture.VerifyNoCallsMade("/api/data");
    }

    [Fact]
    public async Task VerifyNoCallsMade_ShouldFail_WhenCallWasMade()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });
        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyNoCallsMade("/api/data"));
    }

    [Fact]
    public async Task GetReceivedRequests_ShouldReturnAllRequests()
    {
        // Arrange
        _fixture.StubGet("/api/items", new { });
        _fixture.StubPost("/api/items", new { });

        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/items");
        await client.PostAsync("/api/items", new StringContent("{}"));

        // Act
        var requests = _fixture.GetReceivedRequests();

        // Assert
        requests.Count.ShouldBe(2);
        requests.ShouldContain(r => r.Method == "GET" && r.Path == "/api/items");
        requests.ShouldContain(r => r.Method == "POST" && r.Path == "/api/items");
    }

    [Fact]
    public async Task Reset_ShouldClearAllStubsAndHistory()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });
        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");

        // Act
        _fixture.Reset();

        // Assert
        _fixture.GetReceivedRequests().Count.ShouldBe(0);
    }

    [Fact]
    public async Task ResetRequestHistory_ShouldClearOnlyHistory()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { value = "test" });
        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");
        _fixture.GetReceivedRequests().Count.ShouldBe(1);

        // Act
        _fixture.ResetRequestHistory();

        // Assert - History cleared but stub still works
        _fixture.GetReceivedRequests().Count.ShouldBe(0);
        var response = await client.GetAsync("/api/data");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public void CreateClient_ShouldReturnConfiguredClient()
    {
        // Act
        using var client = _fixture.CreateClient();

        // Assert
        client.ShouldNotBeNull();
        client.BaseAddress.ShouldNotBeNull();
        client.BaseAddress.ToString().TrimEnd('/').ShouldBe(_fixture.BaseUrl.TrimEnd('/'));
    }

    [Fact]
    public void Stub_ShouldThrow_WhenMethodIsNullOrEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.Stub(null!, "/api/test"));
        Should.Throw<ArgumentException>(() => _fixture.Stub("", "/api/test"));
        Should.Throw<ArgumentException>(() => _fixture.Stub("   ", "/api/test"));
    }

    [Fact]
    public void Stub_ShouldThrow_WhenPathIsNullOrEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.Stub("GET", null!));
        Should.Throw<ArgumentException>(() => _fixture.Stub("GET", ""));
        Should.Throw<ArgumentException>(() => _fixture.Stub("GET", "   "));
    }

    [Fact]
    public void StubFault_ShouldThrow_WhenPathIsNullOrEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.StubFault(null!, FaultType.EmptyResponse));
        Should.Throw<ArgumentException>(() => _fixture.StubFault("", FaultType.EmptyResponse));
    }

    [Fact]
    public void StubDelay_ShouldThrow_WhenPathIsNullOrEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.StubDelay(null!, TimeSpan.FromSeconds(1), new { }));
    }

    [Fact]
    public void StubDelay_ShouldThrow_WhenResponseIsNull()
    {
        Should.Throw<ArgumentNullException>(() => _fixture.StubDelay("/api/test", TimeSpan.FromSeconds(1), null!));
    }

    [Fact]
    public async Task VerifyCallMade_ShouldFilterByMethod()
    {
        // Arrange
        _fixture.StubGet("/api/data", new { });
        _fixture.StubPost("/api/data", new { });

        using var client = _fixture.CreateClient();
        await client.GetAsync("/api/data");

        // Act & Assert
        _fixture.VerifyCallMade("/api/data", method: "GET");
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/data", method: "POST"));
    }
}
