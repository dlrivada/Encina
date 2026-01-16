using Encina.Testing.WireMock;
namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Guard clause tests for validating argument null checks and parameter validation.
/// </summary>
public sealed class GuardClauseTests : IClassFixture<EncinaWireMockFixture>, IClassFixture<EncinaRefitMockFixture<ITestApi>>
{
    private readonly EncinaWireMockFixture _fixture;
    private readonly EncinaRefitMockFixture<ITestApi> _refitFixture;

    public GuardClauseTests(EncinaWireMockFixture fixture, EncinaRefitMockFixture<ITestApi> refitFixture)
    {
        _fixture = fixture;
        _refitFixture = refitFixture;
    }

    #region EncinaWireMockFixture Guards

    [Fact]
    public void Stub_NullMethod_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => _fixture.Stub(null!, "/path"));
        ex.ParamName.ShouldBe("method");
    }

    [Fact]
    public void Stub_EmptyMethod_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => _fixture.Stub("", "/path"));
        ex.ParamName.ShouldBe("method");
    }

    [Fact]
    public void Stub_WhitespaceMethod_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => _fixture.Stub("   ", "/path"));
        ex.ParamName.ShouldBe("method");
    }

    [Fact]
    public void Stub_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => _fixture.Stub("GET", null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void Stub_EmptyPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => _fixture.Stub("GET", ""));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void StubDelay_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.StubDelay(null!, TimeSpan.FromSeconds(1), new { }));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void StubDelay_NullResponse_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            _fixture.StubDelay("/path", TimeSpan.FromSeconds(1), null!));
        ex.ParamName.ShouldBe("response");
    }

    [Fact]
    public void StubFault_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.StubFault(null!, FaultType.Timeout));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void StubFault_EmptyPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.StubFault("", FaultType.Timeout));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void VerifyCallMade_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.VerifyCallMade(null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void VerifyCallMade_EmptyPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.VerifyCallMade(""));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void VerifyNoCallsMade_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.VerifyNoCallsMade(null!));
        ex.ParamName.ShouldBe("path");
    }

    #endregion

    #region WebhookTestingExtensions Guards

    [Fact]
    public void SetupWebhookEndpoint_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.SetupWebhookEndpoint("/path"));
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void SetupWebhookEndpoint_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.SetupWebhookEndpoint(null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void SetupWebhookEndpoint_EmptyPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.SetupWebhookEndpoint(""));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void SetupWebhookEndpoint_WhitespacePath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.SetupWebhookEndpoint("   "));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void SetupOutboxWebhook_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.SetupOutboxWebhook());
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void SetupOutboxWebhook_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.SetupOutboxWebhook(path: null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void SetupOutboxWebhook_EmptyPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.SetupOutboxWebhook(path: ""));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void SetupWebhookFailure_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.SetupWebhookFailure("/path"));
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void SetupWebhookFailure_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.SetupWebhookFailure(null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void SetupWebhookTimeout_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.SetupWebhookTimeout("/path"));
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void SetupWebhookTimeout_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.SetupWebhookTimeout(null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void VerifyWebhookReceived_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.VerifyWebhookReceived("/path"));
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void VerifyWebhookReceived_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.VerifyWebhookReceived(null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void VerifyNoWebhooksReceived_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.VerifyNoWebhooksReceived("/path"));
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void GetReceivedWebhooks_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.GetReceivedWebhooks("/path"));
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void GetReceivedWebhooks_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.GetReceivedWebhooks(null!));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void GetReceivedWebhookBodies_NullFixture_ThrowsArgumentNullException()
    {
        EncinaWireMockFixture? nullFixture = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            nullFixture!.GetReceivedWebhookBodies<object>("/path"));
        ex.ParamName.ShouldBe("fixture");
    }

    [Fact]
    public void GetReceivedWebhookBodies_NullPath_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _fixture.GetReceivedWebhookBodies<object>(null!));
        ex.ParamName.ShouldBe("path");
    }

    #endregion

    #region EncinaRefitMockFixture Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RefitFixture_Stub_InvalidMethod_ThrowsArgumentException(string? method)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _refitFixture.Stub(method!, "/path"));
        ex.ParamName.ShouldBe("method");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RefitFixture_Stub_InvalidPath_ThrowsArgumentException(string? path)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _refitFixture.Stub("GET", path!));
        ex.ParamName.ShouldBe("path");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RefitFixture_StubError_InvalidPath_ThrowsArgumentException(string? path)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _refitFixture.StubError(path!, 500));
        ex.ParamName.ShouldBe("path");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RefitFixture_StubDelay_InvalidPath_ThrowsArgumentException(string? path)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _refitFixture.StubDelay(path!, TimeSpan.FromSeconds(1), new { }));
        ex.ParamName.ShouldBe("path");
    }

    [Fact]
    public void RefitFixture_StubDelay_NullResponse_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            _refitFixture.StubDelay("/path", TimeSpan.FromSeconds(1), null!));
        ex.ParamName.ShouldBe("response");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RefitFixture_VerifyCallMade_InvalidPath_ThrowsArgumentException(string? path)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _refitFixture.VerifyCallMade(path!));
        ex.ParamName.ShouldBe("path");
    }

    #endregion
}
