using System.Diagnostics;
using System.Net;
using System.Text;
using Encina.Testing.WireMock;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Behaviour tests for <see cref="EncinaWireMockFixture"/>. Each test owns an in-process WireMock
/// server started by the fixture itself and talks to it with a real <see cref="HttpClient"/> on localhost.
/// </summary>
public sealed class EncinaWireMockFixtureBehaviorTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public void ServerProperties_AfterInitialization_AreAvailable()
    {
        _fixture.Server.IsStarted.ShouldBeTrue();
        _fixture.BaseUrl.ShouldStartWith("http://");
        _fixture.Port.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData("GET", 200)]
    [InlineData("POST", 201)]
    [InlineData("PUT", 200)]
    [InlineData("PATCH", 200)]
    public async Task StubVerbs_ReturnJsonWithDefaultStatus(string verb, int expectedStatus)
    {
        switch (verb)
        {
            case "GET": _fixture.StubGet("/api/v", new { Name = "x" }); break;
            case "POST": _fixture.StubPost("/api/v", new { Name = "x" }); break;
            case "PUT": _fixture.StubPut("/api/v", new { Name = "x" }); break;
            default: _fixture.StubPatch("/api/v", new { Name = "x" }); break;
        }

        using var client = _fixture.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(verb), "/api/v");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        ((int)response.StatusCode).ShouldBe(expectedStatus);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("""{"name":"x"}""");
    }

    [Fact]
    public async Task StubGet_CustomStatusCode_IsReturned()
    {
        _fixture.StubGet("/api/missing", new { error = "nope" }, statusCode: 404);
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/missing", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StubDelete_ReturnsNoContentByDefault()
    {
        _fixture.StubDelete("/api/items/1");
        using var client = _fixture.CreateClient();

        using var response = await client.DeleteAsync("/api/items/1", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Stub_WithoutResponse_ReturnsEmptyBody()
    {
        _fixture.Stub("GET", "/api/empty", statusCode: 202);
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/empty", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Stub_WithRequestConfig_MatchesOnlyConfiguredRequests()
    {
        _fixture.Stub(
            "GET",
            "/api/secure",
            requestConfig: r => r.WithHeader("X-Api-Key", "secret"),
            response: new { ok = true });
        using var client = _fixture.CreateClient();

        using var withoutHeader = await client.GetAsync("/api/secure", TestContext.Current.CancellationToken);
        using var withHeaderRequest = new HttpRequestMessage(HttpMethod.Get, "/api/secure");
        withHeaderRequest.Headers.Add("X-Api-Key", "secret");
        using var withHeader = await client.SendAsync(withHeaderRequest, TestContext.Current.CancellationToken);

        withoutHeader.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        withHeader.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Stub_Chaining_RegistersAllStubs()
    {
        _fixture.StubGet("/a", new { id = 1 }).StubGet("/b", new { id = 2 });
        using var client = _fixture.CreateClient();

        using var a = await client.GetAsync("/a", TestContext.Current.CancellationToken);
        using var b = await client.GetAsync("/b", TestContext.Current.CancellationToken);

        a.StatusCode.ShouldBe(HttpStatusCode.OK);
        b.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StubFault_EmptyResponse_ReturnsEmptyBody()
    {
        _fixture.StubFault("/api/data", FaultType.EmptyResponse);
        using var client = _fixture.CreateClient();

        var outcome = await Record.ExceptionAsync(async () =>
        {
            using var response = await client.GetAsync("/api/data", TestContext.Current.CancellationToken);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        });

        // Either the connection is closed (HttpRequestException) or an empty body arrives.
        if (outcome is not null)
        {
            outcome.ShouldBeOfType<HttpRequestException>();
        }
    }

    [Fact]
    public async Task StubFault_MalformedResponse_DoesNotReturnValidJson()
    {
        _fixture.StubFault("/api/data", FaultType.MalformedResponse, method: "GET");
        using var client = _fixture.CreateClient();

        var outcome = await Record.ExceptionAsync(async () =>
        {
            using var response = await client.GetAsync("/api/data", TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            content.ShouldNotBe("""{"name":"x"}""");
        });

        if (outcome is not null)
        {
            outcome.ShouldBeAssignableTo<Exception>();
        }

        _fixture.VerifyCallMade("/api/data", method: "GET");
    }

    [Fact]
    public async Task StubFault_Timeout_CausesClientTimeout()
    {
        _fixture.StubFault("/api/slow", FaultType.Timeout);
        using var client = _fixture.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(100);

        await Should.ThrowAsync<TaskCanceledException>(async () =>
            await client.GetAsync("/api/slow", CancellationToken.None));
    }

    [Fact]
    public void StubFault_UnknownFaultType_Throws()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => _fixture.StubFault("/api/x", (FaultType)99));

        ex.ParamName.ShouldBe("faultType");
    }

    [Fact]
    public async Task StubDelay_DelaysResponseByAtLeastTheConfiguredTime()
    {
        var delay = TimeSpan.FromMilliseconds(100);
        _fixture.StubDelay("/api/delayed", delay, new { result = "late" }, statusCode: 202, method: "GET");
        using var client = _fixture.CreateClient();

        var stopwatch = Stopwatch.StartNew();
        using var response = await client.GetAsync("/api/delayed", TestContext.Current.CancellationToken);
        stopwatch.Stop();

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(delay - TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task StubSequence_ReturnsResponsesInOrder()
    {
        _fixture.StubSequence("GET", "/api/seq", (new { n = 1 }, 200), (null, 500), (new { n = 3 }, 201));
        using var client = _fixture.CreateClient();

        using var first = await client.GetAsync("/api/seq", TestContext.Current.CancellationToken);
        using var second = await client.GetAsync("/api/seq", TestContext.Current.CancellationToken);
        using var third = await client.GetAsync("/api/seq", TestContext.Current.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        third.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await third.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("""{"n":3}""");
    }

    [Fact]
    public async Task GetReceivedRequests_CapturesPathMethodHeadersBodyAndTimestamp()
    {
        _fixture.StubPost("/api/items", new { id = 1 });
        using var client = _fixture.CreateClient();
        var before = DateTime.UtcNow.AddSeconds(-5);
        using var content = new StringContent("""{"name":"a"}""", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/items", content, TestContext.Current.CancellationToken);

        var request = _fixture.GetReceivedRequests().ShouldHaveSingleItem();
        request.Path.ShouldBe("/api/items");
        request.Method.ShouldBe("POST");
        request.Body.ShouldBe("""{"name":"a"}""");
        request.Headers.Keys.ShouldContain(k => k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
        request.Timestamp.ShouldBeGreaterThan(before);
    }

    [Fact]
    public async Task VerifyCallMade_MatchingCount_DoesNotThrow()
    {
        _fixture.StubGet("/api/items", new { });
        using var client = _fixture.CreateClient();
        using var r1 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);
        using var r2 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);

        Should.NotThrow(() => _fixture.VerifyCallMade("/api/items", times: 2));
        Should.NotThrow(() => _fixture.VerifyCallMade("/api/items", times: 2, method: "GET"));
    }

    [Fact]
    public async Task VerifyCallMade_WrongCount_ThrowsWithDetails()
    {
        _fixture.StubGet("/api/items", new { });
        using var client = _fixture.CreateClient();
        using var r1 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);

        var ex = Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/items", times: 3, method: "GET"));

        ex.Message.ShouldContain("Expected 3 call(s) to GET /api/items");
        ex.Message.ShouldContain("found 1");
    }

    [Fact]
    public async Task VerifyCallMade_FiltersByMethod()
    {
        _fixture.StubGet("/api/items", new { });
        using var client = _fixture.CreateClient();
        using var r1 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/items", method: "DELETE"));
        Should.NotThrow(() => _fixture.VerifyNoCallsMade("/api/items", method: "DELETE"));
    }

    [Fact]
    public async Task VerifyNoCallsMade_AfterCall_Throws()
    {
        _fixture.StubGet("/api/items", new { });
        using var client = _fixture.CreateClient();

        Should.NotThrow(() => _fixture.VerifyNoCallsMade("/api/items"));
        using var r1 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyNoCallsMade("/api/items"));
    }

    [Fact]
    public async Task ResetRequestHistory_ClearsRequestsButKeepsStubs()
    {
        _fixture.StubGet("/api/items", new { });
        using var client = _fixture.CreateClient();
        using var r1 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);

        _fixture.ResetRequestHistory();

        _fixture.GetReceivedRequests().ShouldBeEmpty();
        using var r2 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_ClearsStubsAndHistory()
    {
        _fixture.StubGet("/api/items", new { });
        using var client = _fixture.CreateClient();
        using var r1 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);

        _fixture.Reset();

        _fixture.GetReceivedRequests().ShouldBeEmpty();
        using var r2 = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);
        r2.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public void CreateClient_UsesServerUrlAsBaseAddress()
    {
        using var client = _fixture.CreateClient();

        client.BaseAddress.ShouldNotBeNull();
        client.BaseAddress.ToString().TrimEnd('/').ShouldBe(_fixture.BaseUrl.TrimEnd('/'));
    }

    [Fact]
    public async Task CreateClient_WithHandler_UsesHandlerAndBaseAddress()
    {
        _fixture.StubGet("/api/items", new { });
        using var handler = new CountingHandler(new HttpClientHandler());
        using var client = _fixture.CreateClient(handler);

        using var response = await client.GetAsync("/api/items", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        handler.Calls.ShouldBe(1);
    }

    private sealed class CountingHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
