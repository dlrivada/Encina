using System.Diagnostics;
using System.Net;
using Encina.Testing.WireMock;
using Refit;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Behaviour tests for <see cref="EncinaRefitMockFixture{TApiClient}"/>. Each test owns an in-process
/// WireMock server and calls it through a real Refit-generated client.
/// </summary>
public sealed class EncinaRefitMockFixtureBehaviorTests : IAsyncLifetime
{
    private readonly EncinaRefitMockFixture<IRefitBehaviorApi> _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public void ServerProperties_AfterInitialization_AreAvailable()
    {
        _fixture.Server.IsStarted.ShouldBeTrue();
        _fixture.BaseUrl.ShouldStartWith("http://");
        _fixture.Port.ShouldBeGreaterThan(0);
        _fixture.CreateClient().ShouldNotBeNull();
    }

    [Fact]
    public async Task StubGet_ReturnsConfiguredBody()
    {
        _fixture.StubGet("/api/items/1", new { id = 1, name = "One" });

        var item = await _fixture.CreateClient().GetItemAsync(1);

        item.Id.ShouldBe(1);
        item.Name.ShouldBe("One");
    }

    [Fact]
    public async Task StubPost_ReturnsCreated()
    {
        _fixture.StubPost("/api/items", new { id = 2, name = "Two" });

        var item = await _fixture.CreateClient().CreateItemAsync(new BehaviorItem { Id = 0, Name = "Two" });

        item.Id.ShouldBe(2);
    }

    [Fact]
    public async Task StubPut_ReturnsUpdated()
    {
        _fixture.StubPut("/api/items/3", new { id = 3, name = "Three" });

        var item = await _fixture.CreateClient().UpdateItemAsync(3, new BehaviorItem { Id = 3, Name = "Three" });

        item.Name.ShouldBe("Three");
    }

    [Fact]
    public async Task StubPatch_ReturnsPatched()
    {
        _fixture.StubPatch("/api/items/4", new { id = 4, name = "Four" });

        var item = await _fixture.CreateClient().PatchItemAsync(4, new BehaviorItem { Id = 4, Name = "Four" });

        item.Id.ShouldBe(4);
    }

    [Fact]
    public async Task StubDelete_ReturnsNoContent()
    {
        _fixture.StubDelete("/api/items/5");

        await _fixture.CreateClient().DeleteItemAsync(5);

        _fixture.VerifyCallMade("/api/items/5", method: "DELETE");
    }

    [Fact]
    public async Task Stub_WithRequestConfig_AppliesMatching()
    {
        _fixture.Stub(
            "GET",
            "/api/items/6",
            requestConfig: r => r.WithHeader("X-Never", "sent"),
            response: new { id = 6, name = "Six" });

        var ex = await Should.ThrowAsync<ApiException>(() => _fixture.CreateClient().GetItemAsync(6));

        ex.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stub_WithoutResponse_ReturnsConfiguredStatus()
    {
        _fixture.Stub("DELETE", "/api/items/7", statusCode: 204);

        await _fixture.CreateClient().DeleteItemAsync(7);

        _fixture.VerifyCallMade("/api/items/7", method: "DELETE");
    }

    [Fact]
    public async Task StubError_WithBodyAndMethod_ReturnsErrorStatus()
    {
        _fixture.StubError("/api/items/8", 500, new { message = "boom" }, method: "GET");

        var ex = await Should.ThrowAsync<ApiException>(() => _fixture.CreateClient().GetItemAsync(8));

        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (ex.Content ?? string.Empty).ShouldContain("boom");
    }

    [Fact]
    public async Task StubError_WithoutBodyOrMethod_ReturnsErrorStatus()
    {
        _fixture.StubError("/api/items/9", 503);

        var ex = await Should.ThrowAsync<ApiException>(() => _fixture.CreateClient().GetItemAsync(9));

        ex.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task StubDelay_DelaysByAtLeastTheConfiguredTime()
    {
        var delay = TimeSpan.FromMilliseconds(100);
        _fixture.StubDelay("/api/items/10", delay, new { id = 10, name = "Late" }, method: "GET");

        var stopwatch = Stopwatch.StartNew();
        var item = await _fixture.CreateClient().GetItemAsync(10);
        stopwatch.Stop();

        item.Name.ShouldBe("Late");
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(delay - TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task StubDelay_WithoutMethod_MatchesAnyMethod()
    {
        _fixture.StubDelay("/api/items/11", TimeSpan.FromMilliseconds(10), new { id = 11, name = "Any" });

        var item = await _fixture.CreateClient().GetItemAsync(11);

        item.Id.ShouldBe(11);
    }

    [Fact]
    public async Task VerifyCallMade_CountAndMethod_AreChecked()
    {
        _fixture.StubGet("/api/items/12", new { id = 12, name = "x" });
        var api = _fixture.CreateClient();
        await api.GetItemAsync(12);
        await api.GetItemAsync(12);

        Should.NotThrow(() => _fixture.VerifyCallMade("/api/items/12", times: 2));
        Should.NotThrow(() => _fixture.VerifyCallMade("/api/items/12", times: 2, method: "GET"));
        var ex = Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/items/12", times: 1, method: "GET"));
        ex.Message.ShouldContain("Expected 1 call(s) to GET /api/items/12");
        Should.NotThrow(() => _fixture.VerifyNoCallsMade("/api/items/12", method: "POST"));
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyNoCallsMade("/api/items/12"));
    }

    [Fact]
    public async Task ResetRequestHistory_ClearsHistoryButKeepsStubs()
    {
        _fixture.StubGet("/api/items/13", new { id = 13, name = "x" });
        var api = _fixture.CreateClient();
        await api.GetItemAsync(13);

        _fixture.ResetRequestHistory();

        _fixture.VerifyNoCallsMade("/api/items/13");
        (await api.GetItemAsync(13)).Id.ShouldBe(13);
    }

    [Fact]
    public async Task Reset_ClearsStubsAndHistory()
    {
        _fixture.StubGet("/api/items/14", new { id = 14, name = "x" });
        var api = _fixture.CreateClient();
        await api.GetItemAsync(14);

        _fixture.Reset();

        _fixture.VerifyNoCallsMade("/api/items/14");
        var ex = await Should.ThrowAsync<ApiException>(() => api.GetItemAsync(14));
        ex.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DisposeAsync_StopsServer()
    {
        var fixture = new EncinaRefitMockFixture<IRefitBehaviorApi>();
        await fixture.InitializeAsync();
        var server = fixture.Server;

        await fixture.DisposeAsync();

        server.IsStarted.ShouldBeFalse();
    }
}

/// <summary>Refit API used by the behaviour tests.</summary>
public interface IRefitBehaviorApi
{
    [Get("/api/items/{id}")]
    Task<BehaviorItem> GetItemAsync(int id);

    [Post("/api/items")]
    Task<BehaviorItem> CreateItemAsync([Body] BehaviorItem item);

    [Put("/api/items/{id}")]
    Task<BehaviorItem> UpdateItemAsync(int id, [Body] BehaviorItem item);

    [Patch("/api/items/{id}")]
    Task<BehaviorItem> PatchItemAsync(int id, [Body] BehaviorItem item);

    [Delete("/api/items/{id}")]
    Task DeleteItemAsync(int id);
}

/// <summary>Payload used by <see cref="IRefitBehaviorApi"/>.</summary>
public sealed record BehaviorItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
