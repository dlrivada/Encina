using Encina.Testing.WireMock;

using Shouldly;

namespace Encina.GuardTests.Testing.WireMock;

/// <summary>
/// Guard tests for Encina.Testing.WireMock covering null/empty/whitespace guard clauses
/// on <see cref="EncinaWireMockFixture"/>, <see cref="EncinaRefitMockFixture{T}"/>,
/// and <see cref="WebhookTestingExtensions"/>.
///
/// Tests call methods on uninitialized fixtures — the ThrowIfNull/ThrowIfNullOrWhiteSpace
/// guards fire BEFORE the server is accessed, so no Docker or WireMock process is needed.
/// </summary>
[Trait("Category", "Guard")]
public sealed class WireMockGuardTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    // ─── EncinaWireMockFixture.Stub guards ───

    [Theory]
    [InlineData(null, "/path")]
    [InlineData("", "/path")]
    [InlineData("   ", "/path")]
    [InlineData("GET", null)]
    [InlineData("GET", "")]
    [InlineData("GET", "   ")]
    public void Stub_InvalidMethodOrPath_Throws(string? method, string? path)
    {
        Should.Throw<ArgumentException>(() => _fixture.Stub(method!, path!));
    }

    // ─── EncinaWireMockFixture.StubFault guards ───

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StubFault_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() => _fixture.StubFault(path!, FaultType.Timeout));
    }

    // ─── EncinaWireMockFixture.StubDelay guards ───

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StubDelay_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            _fixture.StubDelay(path!, TimeSpan.FromSeconds(1), new { }));
    }

    [Fact]
    public void StubDelay_NullResponse_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _fixture.StubDelay("/path", TimeSpan.FromSeconds(1), null!));
    }

    // ─── EncinaWireMockFixture.StubSequence guards ───

    [Theory]
    [InlineData(null, "/path")]
    [InlineData("", "/path")]
    [InlineData("GET", null)]
    [InlineData("GET", "")]
    public void StubSequence_InvalidMethodOrPath_Throws(string? method, string? path)
    {
        Should.Throw<ArgumentException>(() =>
            _fixture.StubSequence(method!, path!, (new { }, 200)));
    }

    [Fact]
    public void StubSequence_NullResponses_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _fixture.StubSequence("GET", "/path", ((object?, int)[])null!));
    }

    [Fact]
    public void StubSequence_EmptyResponses_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            _fixture.StubSequence("GET", "/path", Array.Empty<(object?, int)>()));
    }

    // ─── EncinaWireMockFixture construction + disposal ───

    [Fact]
    public void Constructor_CreatesFixture()
    {
        var fixture = new EncinaWireMockFixture();
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public async Task DisposeAsync_BeforeInit_DoesNotThrow()
    {
        var fixture = new EncinaWireMockFixture();
        await fixture.DisposeAsync();
    }

    // ─── WebhookTestingExtensions null guards ───

    [Fact]
    public void SetupWebhookEndpoint_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.SetupWebhookEndpoint(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetupWebhookEndpoint_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.SetupWebhookEndpoint(_fixture, path!));
    }

    [Fact]
    public void SetupOutboxWebhook_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.SetupOutboxWebhook(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetupOutboxWebhook_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.SetupOutboxWebhook(_fixture, path!));
    }

    [Fact]
    public void SetupWebhookFailure_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.SetupWebhookFailure(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetupWebhookFailure_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.SetupWebhookFailure(_fixture, path!));
    }

    [Fact]
    public void SetupWebhookTimeout_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.SetupWebhookTimeout(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetupWebhookTimeout_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.SetupWebhookTimeout(_fixture, path!));
    }

    [Fact]
    public void VerifyWebhookReceived_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.VerifyWebhookReceived(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void VerifyWebhookReceived_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.VerifyWebhookReceived(_fixture, path!));
    }

    [Fact]
    public void VerifyNoWebhooksReceived_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.VerifyNoWebhooksReceived(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void VerifyNoWebhooksReceived_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.VerifyNoWebhooksReceived(_fixture, path!));
    }

    [Fact]
    public void GetReceivedWebhooks_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.GetReceivedWebhooks(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetReceivedWebhooks_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.GetReceivedWebhooks(_fixture, path!));
    }

    [Fact]
    public void GetReceivedWebhookBodies_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookTestingExtensions.GetReceivedWebhookBodies<object>(null!, "/webhook"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetReceivedWebhookBodies_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() =>
            WebhookTestingExtensions.GetReceivedWebhookBodies<object>(_fixture, path!));
    }

    // ─── ReceivedRequest record ───

    [Fact]
    public void ReceivedRequest_PropertiesAssignable()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Content-Type"] = new[] { "application/json" }
        };
        var req = new ReceivedRequest(
            Path: "/webhook",
            Method: "POST",
            Headers: headers,
            Body: "{}",
            Timestamp: DateTime.UtcNow);

        req.Method.ShouldBe("POST");
        req.Path.ShouldBe("/webhook");
        req.Body.ShouldBe("{}");
        req.Headers.Count.ShouldBe(1);
    }
}