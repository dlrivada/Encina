using System.Diagnostics;
using System.Net;
using System.Text;
using Encina.Testing.WireMock;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Behaviour tests for <see cref="WebhookTestingExtensions"/> through real POST requests to an
/// in-process WireMock server.
/// </summary>
public sealed class WebhookTestingExtensionsBehaviorTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task SetupWebhookEndpoint_AcceptsPostWithDefaultAndCustomStatus()
    {
        _fixture.SetupWebhookEndpoint("/hooks/a").SetupWebhookEndpoint("/hooks/b", statusCode: 202);
        using var client = _fixture.CreateClient();

        using var a = await client.PostAsync("/hooks/a", Json("{}"), TestContext.Current.CancellationToken);
        using var b = await client.PostAsync("/hooks/b", Json("{}"), TestContext.Current.CancellationToken);

        a.StatusCode.ShouldBe(HttpStatusCode.OK);
        b.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SetupOutboxWebhook_AcceptsJsonPostsAtDefaultPath()
    {
        _fixture.SetupOutboxWebhook();
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsync("/webhooks/outbox", Json("""{"a":1}"""), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("{}");
    }

    [Fact]
    public async Task SetupOutboxWebhook_RejectsNonJsonContent_AndHonorsCustomPathAndStatus()
    {
        _fixture.SetupOutboxWebhook("/custom/outbox", statusCode: 201);
        using var client = _fixture.CreateClient();

        using var plain = await client.PostAsync(
            "/custom/outbox",
            new StringContent("x", Encoding.UTF8, "text/plain"),
            TestContext.Current.CancellationToken);
        using var json = await client.PostAsync("/custom/outbox", Json("{}"), TestContext.Current.CancellationToken);

        plain.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        json.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SetupWebhookFailure_WithoutMessage_ReturnsStatusOnly()
    {
        _fixture.SetupWebhookFailure("/hooks/fail");
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsync("/hooks/fail", Json("{}"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task SetupWebhookFailure_WithMessage_ReturnsJsonError()
    {
        _fixture.SetupWebhookFailure("/hooks/fail", 503, "unavailable");
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsync("/hooks/fail", Json("{}"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("""{"error":"unavailable"}""");
    }

    [Fact]
    public async Task SetupWebhookTimeout_CustomDelay_DelaysResponse()
    {
        var delay = TimeSpan.FromMilliseconds(100);
        _fixture.SetupWebhookTimeout("/hooks/slow", delay);
        using var client = _fixture.CreateClient();

        var stopwatch = Stopwatch.StartNew();
        using var response = await client.PostAsync("/hooks/slow", Json("{}"), TestContext.Current.CancellationToken);
        stopwatch.Stop();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(delay - TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task SetupWebhookTimeout_ShortClientTimeout_TimesOut()
    {
        _fixture.SetupWebhookTimeout("/hooks/slow", TimeSpan.FromMilliseconds(500));
        using var client = _fixture.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(100);

        await Should.ThrowAsync<TaskCanceledException>(async () =>
            await client.PostAsync("/hooks/slow", Json("{}"), CancellationToken.None));
    }

    [Fact]
    public void SetupWebhookTimeout_DefaultDelay_RegistersThirtySecondDelay()
    {
        // No request is sent: the server would hold the 30 s delay on shutdown.
        _fixture.SetupWebhookTimeout("/hooks/slow");

        var mapping = _fixture.Server.MappingModels.ShouldHaveSingleItem();
        mapping.Response.Delay.ShouldBe((int)TimeSpan.FromSeconds(30).TotalMilliseconds);
    }

    [Fact]
    public async Task VerifyWebhookReceived_AndVerifyNoWebhooksReceived_ReflectPostCalls()
    {
        _fixture.SetupWebhookEndpoint("/hooks/a");
        using var client = _fixture.CreateClient();

        Should.NotThrow(() => _fixture.VerifyNoWebhooksReceived("/hooks/a"));
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyWebhookReceived("/hooks/a"));

        using var r1 = await client.PostAsync("/hooks/a", Json("{}"), TestContext.Current.CancellationToken);
        using var r2 = await client.PostAsync("/hooks/a", Json("{}"), TestContext.Current.CancellationToken);

        Should.NotThrow(() => _fixture.VerifyWebhookReceived("/hooks/a", times: 2));
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyWebhookReceived("/hooks/a"));
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyNoWebhooksReceived("/hooks/a"));
    }

    [Fact]
    public async Task GetReceivedWebhooks_ReturnsOnlyPostsToThePath()
    {
        _fixture.SetupWebhookEndpoint("/hooks/a").SetupWebhookEndpoint("/hooks/b").StubGet("/hooks/a", new { });
        using var client = _fixture.CreateClient();

        using var r1 = await client.PostAsync("/hooks/a", Json("""{"n":1}"""), TestContext.Current.CancellationToken);
        using var r2 = await client.PostAsync("/hooks/b", Json("""{"n":2}"""), TestContext.Current.CancellationToken);
        using var r3 = await client.GetAsync("/hooks/a", TestContext.Current.CancellationToken);

        var webhooks = _fixture.GetReceivedWebhooks("/hooks/a");

        var webhook = webhooks.ShouldHaveSingleItem();
        webhook.Method.ShouldBe("POST");
        webhook.Body.ShouldBe("""{"n":1}""");
    }

    [Fact]
    public async Task GetReceivedWebhookBodies_ValidJson_DeserializesCaseInsensitively()
    {
        _fixture.SetupWebhookEndpoint("/hooks/orders");
        using var client = _fixture.CreateClient();
        using var r1 = await client.PostAsync("/hooks/orders", Json("""{"orderId":1,"Status":"New"}"""), TestContext.Current.CancellationToken);
        using var r2 = await client.PostAsync("/hooks/orders", Json("""{"OrderId":2,"status":"Paid"}"""), TestContext.Current.CancellationToken);

        var bodies = _fixture.GetReceivedWebhookBodies<OrderPayload>("/hooks/orders");

        bodies.Count.ShouldBe(2);
        bodies[0].ShouldBe(new OrderPayload(1, "New"));
        bodies[1].ShouldBe(new OrderPayload(2, "Paid"));
    }

    [Fact]
    public void GetReceivedWebhookBodies_NoWebhooks_ReturnsEmpty()
    {
        _fixture.GetReceivedWebhookBodies<OrderPayload>("/hooks/none").ShouldBeEmpty();
    }

    [Fact]
    public async Task GetReceivedWebhookBodies_EmptyBody_ThrowsWithIndexAndPath()
    {
        _fixture.SetupWebhookEndpoint("/hooks/orders");
        using var client = _fixture.CreateClient();
        using var r1 = await client.PostAsync("/hooks/orders", Json("""{"orderId":1,"status":"New"}"""), TestContext.Current.CancellationToken);
        using var r2 = await client.PostAsync("/hooks/orders", Json(string.Empty), TestContext.Current.CancellationToken);

        var ex = Should.Throw<InvalidOperationException>(() => _fixture.GetReceivedWebhookBodies<OrderPayload>("/hooks/orders"));

        ex.Message.ShouldContain("Webhook [1]");
        ex.Message.ShouldContain("empty body");
        ex.Message.ShouldContain("OrderPayload");
    }

    [Fact]
    public async Task GetReceivedWebhookBodies_InvalidJson_ThrowsWithPreviewAndInnerJsonException()
    {
        _fixture.SetupWebhookEndpoint("/hooks/orders");
        using var client = _fixture.CreateClient();
        using var r1 = await client.PostAsync("/hooks/orders", Json("not-json"), TestContext.Current.CancellationToken);

        var ex = Should.Throw<InvalidOperationException>(() => _fixture.GetReceivedWebhookBodies<OrderPayload>("/hooks/orders"));

        ex.Message.ShouldContain("failed to deserialize");
        ex.Message.ShouldContain("Body preview: not-json");
        ex.InnerException.ShouldBeAssignableTo<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task GetReceivedWebhookBodies_LongInvalidJson_TruncatesPreview()
    {
        _fixture.SetupWebhookEndpoint("/hooks/orders");
        using var client = _fixture.CreateClient();
        var longBody = new string('x', 300);
        using var r1 = await client.PostAsync("/hooks/orders", Json(longBody), TestContext.Current.CancellationToken);

        var ex = Should.Throw<InvalidOperationException>(() => _fixture.GetReceivedWebhookBodies<OrderPayload>("/hooks/orders"));

        ex.Message.ShouldContain(new string('x', 200) + "...");
        ex.Message.ShouldNotContain(new string('x', 201));
    }

    [Fact]
    public async Task GetReceivedWebhookBodies_JsonNull_ThrowsDeserializedToNull()
    {
        _fixture.SetupWebhookEndpoint("/hooks/orders");
        using var client = _fixture.CreateClient();
        using var r1 = await client.PostAsync("/hooks/orders", Json("null"), TestContext.Current.CancellationToken);

        var ex = Should.Throw<InvalidOperationException>(() => _fixture.GetReceivedWebhookBodies<OrderPayload>("/hooks/orders"));

        ex.Message.ShouldContain("deserialized to null");
        ex.Message.ShouldContain("Body preview: null");
    }

    [Fact]
    public async Task GetReceivedWebhookBodies_LongJsonNullLikeBody_TruncatesNullPreview()
    {
        _fixture.SetupWebhookEndpoint("/hooks/orders");
        using var client = _fixture.CreateClient();
        // Valid JSON that deserializes to null: "null" followed by >200 chars of whitespace.
        var body = "null" + new string(' ', 250);
        using var r1 = await client.PostAsync("/hooks/orders", Json(body), TestContext.Current.CancellationToken);

        var ex = Should.Throw<InvalidOperationException>(() => _fixture.GetReceivedWebhookBodies<OrderPayload>("/hooks/orders"));

        ex.Message.ShouldContain("deserialized to null");
        ex.Message.ShouldEndWith("...");
    }

    private sealed record OrderPayload(int OrderId, string Status);
}
