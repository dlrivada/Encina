using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Encina.Testing.WireMock;
using Encina.UnitTests.Testing.WireMock.Fixtures;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Unit tests for <see cref="WebhookTestingExtensions"/>.
/// </summary>
public sealed class WebhookTestingExtensionsTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task SetupWebhookEndpoint_ShouldAcceptPostRequests()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/webhooks/notifications", new StringContent("{}"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetupWebhookEndpoint_ShouldReturnCustomStatusCode()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/custom", statusCode: 202);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/webhooks/custom", new StringContent("{}"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SetupOutboxWebhook_ShouldAcceptJsonPosts()
    {
        // Arrange
        _fixture.SetupOutboxWebhook();

        using var client = _fixture.CreateClient();
        var content = new StringContent(
            """{"eventType": "OrderCreated", "orderId": 123}""",
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await client.PostAsync("/webhooks/outbox", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task SetupOutboxWebhook_ShouldUseCustomPath()
    {
        // Arrange
        _fixture.SetupOutboxWebhook(path: "/api/events");

        using var client = _fixture.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/events", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetupWebhookFailure_ShouldReturnErrorStatus()
    {
        // Arrange
        _fixture.SetupWebhookFailure("/webhooks/outbox", statusCode: 503, errorMessage: "Service unavailable");

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/webhooks/outbox", new StringContent("{}"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Service unavailable");
    }

    [Fact]
    public async Task SetupWebhookFailure_ShouldWorkWithoutErrorMessage()
    {
        // Arrange
        _fixture.SetupWebhookFailure("/webhooks/outbox", statusCode: 500);

        using var client = _fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/webhooks/outbox", new StringContent("{}"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SetupWebhookTimeout_ShouldDelayResponse()
    {
        // Arrange
        const double DelayToleranceMs = 50;
        var delay = TimeSpan.FromMilliseconds(100);
        _fixture.SetupWebhookTimeout("/webhooks/slow", delay);

        using var client = _fixture.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await client.PostAsync("/webhooks/slow", new StringContent("{}"));
        stopwatch.Stop();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        stopwatch.Elapsed.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(delay.TotalMilliseconds - DelayToleranceMs);
    }

    [Fact]
    public async Task VerifyWebhookReceived_ShouldSucceed_WhenWebhookWasSent()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        using var client = _fixture.CreateClient();
        await client.PostAsync("/webhooks/notifications", new StringContent("{}"));

        // Act & Assert - Should not throw
        _fixture.VerifyWebhookReceived("/webhooks/notifications");
    }

    [Fact]
    public void VerifyWebhookReceived_ShouldFail_WhenNoWebhookWasSent()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _fixture.VerifyWebhookReceived("/webhooks/notifications"));
    }

    [Fact]
    public async Task VerifyWebhookReceived_ShouldVerifyExactCount()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        using var client = _fixture.CreateClient();
        await client.PostAsync("/webhooks/notifications", new StringContent("{}"));
        await client.PostAsync("/webhooks/notifications", new StringContent("{}"));

        // Act & Assert
        _fixture.VerifyWebhookReceived("/webhooks/notifications", times: 2);
        Should.Throw<InvalidOperationException>(() =>
            _fixture.VerifyWebhookReceived("/webhooks/notifications", times: 1));
    }

    [Fact]
    public void VerifyNoWebhooksReceived_ShouldSucceed_WhenNoWebhooksSent()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        // Act & Assert - Should not throw
        _fixture.VerifyNoWebhooksReceived("/webhooks/notifications");
    }

    [Fact]
    public async Task VerifyNoWebhooksReceived_ShouldFail_WhenWebhookWasSent()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        using var client = _fixture.CreateClient();
        await client.PostAsync("/webhooks/notifications", new StringContent("{}"));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _fixture.VerifyNoWebhooksReceived("/webhooks/notifications"));
    }

    [Fact]
    public async Task GetReceivedWebhooks_ShouldReturnAllWebhooks()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");

        using var client = _fixture.CreateClient();
        await client.PostAsync("/webhooks/notifications", new StringContent("""{"id": 1}""", Encoding.UTF8, "application/json"));
        await client.PostAsync("/webhooks/notifications", new StringContent("""{"id": 2}""", Encoding.UTF8, "application/json"));

        // Act
        var webhooks = _fixture.GetReceivedWebhooks("/webhooks/notifications");

        // Assert
        webhooks.Count.ShouldBe(2);
        webhooks.ShouldAllBe(w => w.Method == "POST");
        webhooks.ShouldAllBe(w => w.Path == "/webhooks/notifications");
    }

    [Fact]
    public async Task GetReceivedWebhooks_ShouldNotIncludeGetRequests()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/notifications");
        _fixture.StubGet("/webhooks/notifications", new { status = "ok" });

        using var client = _fixture.CreateClient();
        await client.GetAsync("/webhooks/notifications");
        await client.PostAsync("/webhooks/notifications", new StringContent("{}"));

        // Act
        var webhooks = _fixture.GetReceivedWebhooks("/webhooks/notifications");

        // Assert
        webhooks.Count.ShouldBe(1);
        webhooks[0].Method.ShouldBe("POST");
    }

    [Fact]
    public async Task GetReceivedWebhookBodies_ShouldDeserializeBodies()
    {
        // Arrange
        _fixture.SetupWebhookEndpoint("/webhooks/orders");

        using var client = _fixture.CreateClient();
        await client.PostAsync("/webhooks/orders",
            new StringContent("""{"orderId": 123, "status": "created"}""", Encoding.UTF8, "application/json"));
        await client.PostAsync("/webhooks/orders",
            new StringContent("""{"orderId": 456, "status": "shipped"}""", Encoding.UTF8, "application/json"));

        // Act
        var bodies = _fixture.GetReceivedWebhookBodies<OrderWebhookPayload>("/webhooks/orders");

        // Assert
        bodies.Count.ShouldBe(2);
        bodies[0].OrderId.ShouldBe(123);
        bodies[0].Status.ShouldBe("created");
        bodies[1].OrderId.ShouldBe(456);
        bodies[1].Status.ShouldBe("shipped");
    }

    [Fact]
    public void SetupWebhookEndpoint_ShouldThrow_WhenFixtureIsNull()
    {
        EncinaWireMockFixture? nullFixture = null;

        Should.Throw<ArgumentNullException>(() =>
            nullFixture!.SetupWebhookEndpoint("/webhooks/test"));
    }

    [Fact]
    public void SetupWebhookEndpoint_ShouldThrow_WhenPathIsEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.SetupWebhookEndpoint(""));
        Should.Throw<ArgumentException>(() => _fixture.SetupWebhookEndpoint("   "));
    }

    [Fact]
    public void SetupOutboxWebhook_ShouldThrow_WhenFixtureIsNull()
    {
        EncinaWireMockFixture? nullFixture = null;

        Should.Throw<ArgumentNullException>(() =>
            nullFixture!.SetupOutboxWebhook());
    }

    [Fact]
    public void SetupWebhookFailure_ShouldThrow_WhenPathIsEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.SetupWebhookFailure(""));
    }

    [Fact]
    public void SetupWebhookTimeout_ShouldThrow_WhenPathIsEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.SetupWebhookTimeout(""));
    }

    [Fact]
    public void VerifyWebhookReceived_ShouldThrow_WhenFixtureIsNull()
    {
        EncinaWireMockFixture? nullFixture = null;

        Should.Throw<ArgumentNullException>(() =>
            nullFixture!.VerifyWebhookReceived("/webhooks/test"));
    }

    [Fact]
    public void VerifyWebhookReceived_ShouldThrow_WhenPathIsEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.VerifyWebhookReceived(""));
    }

    [Fact]
    public void GetReceivedWebhooks_ShouldThrow_WhenFixtureIsNull()
    {
        EncinaWireMockFixture? nullFixture = null;

        Should.Throw<ArgumentNullException>(() =>
            nullFixture!.GetReceivedWebhooks("/webhooks/test"));
    }

    [Fact]
    public async Task SetupWebhookEndpoint_ShouldSupportMethodChaining()
    {
        // Arrange & Act
        _fixture
            .SetupWebhookEndpoint("/webhooks/a")
            .SetupWebhookEndpoint("/webhooks/b")
            .SetupOutboxWebhook("/webhooks/c");

        using var client = _fixture.CreateClient();

        // Assert
        var responseA = await client.PostAsync("/webhooks/a", new StringContent("{}"));
        var responseB = await client.PostAsync("/webhooks/b", new StringContent("{}"));
        var responseC = await client.PostAsync("/webhooks/c", new StringContent("{}", Encoding.UTF8, "application/json"));

        responseA.StatusCode.ShouldBe(HttpStatusCode.OK);
        responseB.StatusCode.ShouldBe(HttpStatusCode.OK);
        responseC.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
