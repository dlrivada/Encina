using Encina.Testing.WireMock;

using Shouldly;

namespace Encina.GuardTests.Testing.WireMock;

/// <summary>
/// Guard tests for the public members of <see cref="EncinaWireMockFixture"/> and
/// <see cref="EncinaRefitMockFixture{TApiClient}"/> not covered by <see cref="WireMockGuardTests"/>.
/// Argument guards fire before the server is touched, so the fixtures stay uninitialized and
/// no server is started.
/// </summary>
[Trait("Category", "Guard")]
public sealed class WireMockFixtureGuardTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();
    private readonly EncinaRefitMockFixture<IGuardTestApi> _refit = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
        await _refit.DisposeAsync();
    }

    // ─── EncinaWireMockFixture ───

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyCallMade_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() => _fixture.VerifyCallMade(path!, times: 1, method: "GET"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyNoCallsMade_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() => _fixture.VerifyNoCallsMade(path!, method: "GET"));
    }

    [Fact]
    public void CreateClient_NullHandler_Throws()
    {
        Should.Throw<ArgumentNullException>(() => _fixture.CreateClient(null!));
    }

    [Fact]
    public void Server_BeforeInitialization_Throws()
    {
        Should.Throw<InvalidOperationException>(() => _fixture.Server);
        Should.Throw<InvalidOperationException>(() => _fixture.BaseUrl);
        Should.Throw<InvalidOperationException>(() => _fixture.Port);
    }

    // ─── EncinaRefitMockFixture<T> ───

    [Fact]
    public async Task Refit_DisposeAsync_BeforeInit_DoesNotThrow()
    {
        await Should.NotThrowAsync(async () => await _refit.DisposeAsync());
    }

    [Fact]
    public void Refit_Server_BeforeInitialization_Throws()
    {
        Should.Throw<InvalidOperationException>(() => _refit.Server);
        Should.Throw<InvalidOperationException>(() => _refit.BaseUrl);
        Should.Throw<InvalidOperationException>(() => _refit.Port);
    }

    [Fact]
    public void Refit_CreateClient_BeforeInitialization_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => _refit.CreateClient());

        ex.Message.ShouldContain("not initialized");
    }

    [Theory]
    [InlineData(null, "/path")]
    [InlineData("", "/path")]
    [InlineData("   ", "/path")]
    [InlineData("GET", null)]
    [InlineData("GET", "")]
    [InlineData("GET", "   ")]
    public void Refit_Stub_InvalidMethodOrPath_Throws(string? method, string? path)
    {
        Should.Throw<ArgumentException>(() => _refit.Stub(method!, path!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Refit_StubError_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() => _refit.StubError(path!, 500));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refit_StubError_BlankMethod_Throws(string method)
    {
        Should.Throw<ArgumentException>(() => _refit.StubError("/path", 500, method: method));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Refit_StubDelay_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() => _refit.StubDelay(path!, TimeSpan.FromMilliseconds(1), new { }));
    }

    [Fact]
    public void Refit_StubDelay_NullResponse_Throws()
    {
        Should.Throw<ArgumentNullException>(() => _refit.StubDelay("/path", TimeSpan.FromMilliseconds(1), null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refit_StubDelay_BlankMethod_Throws(string method)
    {
        Should.Throw<ArgumentException>(() =>
            _refit.StubDelay("/path", TimeSpan.FromMilliseconds(1), new { }, method: method));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Refit_VerifyCallMade_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() => _refit.VerifyCallMade(path!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Refit_VerifyNoCallsMade_InvalidPath_Throws(string? path)
    {
        Should.Throw<ArgumentException>(() => _refit.VerifyNoCallsMade(path!));
    }
}

/// <summary>Minimal Refit contract used to instantiate <see cref="EncinaRefitMockFixture{TApiClient}"/>.</summary>
public interface IGuardTestApi
{
    [global::Refit.Get("/ping")]
    Task PingAsync();
}
