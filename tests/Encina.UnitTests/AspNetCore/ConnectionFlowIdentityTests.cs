using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Encina.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// End-to-end tests of <c>UseEncinaContext()</c> over a real ASP.NET Core pipeline (#1705, Phase 3
/// task 9): request identity, connection flows (SignalR over WebSockets, raw WebSockets, server-sent
/// events), the misordering latch and re-routing. Every misordered pipeline runs on a fresh host with
/// the connection as its first request, so no case shares a latch with another.
/// </summary>
/// <remarks>
/// In-process hosting (<see cref="TestServer"/>, one Kestrel loopback case), no external service:
/// these live in UnitTests by the plan's Testing section.
/// </remarks>
public sealed class ConnectionFlowIdentityTests
{
    private const string UserHeader = "X-Test-User";
    private const char RecordSeparator = '\u001e';

    public enum Order
    {
        /// <summary>UseRouting, authentication, UseEncinaContext, endpoints.</summary>
        Documented,

        /// <summary>Authentication, UseEncinaContext, UseRouting, endpoints.</summary>
        BeforeRouting
    }

    // ── Hosts ─────────────────────────────────────────────────────────────

    private static Task<IHost> StartAsync(Action<IApplicationBuilder> pipeline, Action<IServiceCollection>? services = null)
    {
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(collection =>
            {
                collection.AddFakeLogging();
                collection.AddRouting();
                collection.AddSignalR();
                collection.AddEncinaAspNetCore();
                collection.AddEncinaServiceIdentity("test-job");
                services?.Invoke(collection);
            });
            web.Configure(pipeline);
        });
        return builder.StartAsync();
    }

    private static Task<IHost> StartOrderedAsync(Order order) => StartAsync(app =>
    {
        if (order == Order.Documented)
        {
            app.UseRouting();
        }

        UseTestAuthentication(app);
        app.UseEncinaContext();
        if (order == Order.BeforeRouting)
        {
            app.UseRouting();
        }

        app.UseEndpoints(MapEndpoints);
    });

    // Authentication stand-in: the X-Test-User header becomes an authenticated principal.
    private static void UseTestAuthentication(IApplicationBuilder app) =>
        Microsoft.AspNetCore.Builder.UseExtensions.Use(app, async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue(UserHeader, out var user) && !string.IsNullOrEmpty(user))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user.ToString())], "TestScheme"));
            }

            await next();
        });

    private static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<IdentityHub>("/hub");
        endpoints.MapGet("/whoami", (RequestDelegate)(context => context.Response.WriteAsync(Who(context.RequestServices))));
        endpoints.MapGet("/error", (RequestDelegate)(context => context.Response.WriteAsync("error-page")));
        endpoints.MapGet("/boom", (RequestDelegate)(_ => throw new InvalidOperationException("boom")));
        endpoints.MapGet("/service-scope", (RequestDelegate)(async context => await context.Response.WriteAsync(await TryServiceAsync(context.RequestServices, allowOverInbound: false))));
        endpoints.MapGet("/sse", (RequestDelegate)(async context =>
        {
            context.Response.ContentType = "text/event-stream";
            await context.Response.WriteAsync($"data: {Who(context.RequestServices)}\n\n");
        }));
        endpoints.MapPost("/stream", (RequestDelegate)(async context =>
        {
            context.Response.ContentType = "text/event-stream";
            for (var chunk = 0; chunk < 3; chunk++)
            {
                await context.Response.WriteAsync($"data: {Who(context.RequestServices)}\n\n");
                await context.Response.Body.FlushAsync();
                await Task.Yield();
            }
        }));
        endpoints.MapGet("/sse-optin", (RequestDelegate)(async context =>
        {
            context.Response.ContentType = "text/event-stream";
            var scopes = context.RequestServices.GetRequiredService<IRequestContextScopeFactory>();
            var info = context.CreateInboundRequestInfo();
            var events = new List<string>();
            for (var tick = 0; tick < 3; tick++)
            {
                var inside = await scopes.RunInboundAsync(info, (_, _) => Task.FromResult(Right<EncinaError, string>(Who(context.RequestServices))));
                events.Add(inside.Match(Right: value => value, Left: error => error.GetCode().IfNone("left")));
                events.Add(Who(context.RequestServices));
            }

            await context.Response.WriteAsync(string.Join(';', events));
        }));
    }

    private static string Who(IServiceProvider services)
    {
        var context = services.GetRequiredService<IRequestContextAccessor>().RequestContext;
        return $"{context?.UserId ?? "anonymous"}|{(context as RequestContext)?.Origin.ToString() ?? "none"}";
    }

    private static async Task<string> TryServiceAsync(IServiceProvider services, bool allowOverInbound)
    {
        var scopes = services.GetRequiredService<IRequestContextScopeFactory>();
        var result = await scopes.RunAsServiceAsync(
            "test-job",
            (context, _) => Task.FromResult(Right<EncinaError, string>(context.UserId!)),
            new IdentityScopeOptions(AllowOverInbound: allowOverInbound));
        return result.Match(Right: value => value, Left: error => error.GetCode().IfNone("left"));
    }

    private static List<int> EventIds(IHost host, params int[] ids) =>
        [.. host.Services.GetFakeLogCollector().GetSnapshot().Select(static record => record.Id.Id).Where(ids.Contains)];

    private static async Task<(HttpStatusCode Status, string Body)> GetAsync(IHost host, string path, string? user = null, string? accept = null, HttpMethod? method = null)
    {
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, new Uri(path, UriKind.Relative));
        if (user is not null)
        {
            request.Headers.Add(UserHeader, user);
        }

        if (accept is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    // ── Ordinary requests ─────────────────────────────────────────────────

    [Fact]
    public async Task AnAuthenticatedRequest_SeesItsUser_WithTheInboundOrigin()
    {
        using var host = await StartOrderedAsync(Order.Documented);

        var (status, body) = await GetAsync(host, "/whoami", user: "alice");

        status.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("alice|Inbound");
    }

    [Fact]
    public async Task AnAnonymousRequest_CannotOpenAServiceScope_WithoutAllowOverInbound()
    {
        // The interim Phase 2 risk is closed: an anonymous HTTP request is an inbound chain.
        using var host = await StartOrderedAsync(Order.Documented);

        var (_, body) = await GetAsync(host, "/service-scope");

        body.ShouldBe(RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task ParallelRequests_EachSeeTheirOwnUser()
    {
        using var host = await StartOrderedAsync(Order.Documented);

        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(index => GetAsync(host, "/whoami", user: $"user-{index}")));

        results.Select(static result => result.Body).ShouldBe(Enumerable.Range(0, 50).Select(static index => $"user-{index}|Inbound"));
    }

    [Fact]
    public async Task ATaskCapturedDuringARequest_RunsAnonymousAfterIt_AndCannotOpenAServiceScope()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<(string Who, string Service)>? captured = null;
        using var host = await StartAsync(app =>
        {
            app.UseRouting();
            UseTestAuthentication(app);
            app.UseEncinaContext();
            app.UseEndpoints(endpoints => endpoints.MapGet("/fire", (RequestDelegate)(context =>
            {
                var accessor = context.RequestServices.GetRequiredService<IRequestContextAccessor>();
                var scopes = context.RequestServices.GetRequiredService<IRequestContextScopeFactory>();
                captured = Task.Run(async () =>
                {
                    await gate.Task;
                    var service = await scopes.RunAsServiceAsync("test-job", (c, _) => Task.FromResult(Right<EncinaError, string>(c.UserId!)));
                    return (accessor.RequestContext?.UserId ?? "anonymous", service.Match(Right: v => v, Left: e => e.GetCode().IfNone("left")));
                });
                return context.Response.WriteAsync("started");
            })));
        });

        (await GetAsync(host, "/fire", user: "alice")).Body.ShouldBe("started");
        gate.SetResult();
        var (who, service) = await captured!;

        who.ShouldBe("anonymous");
        service.ShouldBe(RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task UseEncinaContextTwice_Answers500_WithoutRunningTheEndpoint()
    {
        var endpointRan = false;
        using var host = await StartAsync(app =>
        {
            app.UseRouting();
            app.UseEncinaContext();
            app.UseEncinaContext();
            app.UseEndpoints(endpoints => endpoints.MapGet("/whoami", (RequestDelegate)(context =>
            {
                endpointRan = true;
                return Task.CompletedTask;
            })));
        });

        var (status, _) = await GetAsync(host, "/whoami");

        status.ShouldBe(HttpStatusCode.InternalServerError);
        endpointRan.ShouldBeFalse();
    }

    // ── Server-sent events and streamed POSTs (MQ-1 (c), E3, D2) ───────────

    [Theory]
    [InlineData(Order.Documented)]
    [InlineData(Order.BeforeRouting)]
    public async Task AnSseGet_RunsAnonymousUnderTheConnectionMarker_InEitherOrder(Order order)
    {
        using var host = await StartOrderedAsync(order);

        var (status, body) = await GetAsync(host, "/sse", user: "alice", accept: "text/event-stream");

        status.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("data: anonymous|Connection\n\n");
        EventIds(host, 202).ShouldBeEmpty();
        (await GetAsync(host, "/whoami", user: "alice")).Body.ShouldBe("alice|Inbound");
    }

    [Fact]
    public async Task APostThatStreams_KeepsItsRequestIdentity_ForTheWholeStream()
    {
        using var host = await StartOrderedAsync(Order.Documented);

        var (_, body) = await GetAsync(host, "/stream", user: "mcp-user", accept: "text/event-stream", method: HttpMethod.Post);

        body.ShouldBe(string.Concat(Enumerable.Repeat("data: mcp-user|Inbound\n\n", 3)));
    }

    [Fact]
    public async Task TheSseOptIn_BindsTheUserPerEvent_AndAnonymousBetweenEvents()
    {
        using var host = await StartOrderedAsync(Order.Documented);

        var (_, body) = await GetAsync(host, "/sse-optin", user: "alice", accept: "text/event-stream");

        body.ShouldBe("alice|Inbound;anonymous|Connection;alice|Inbound;anonymous|Connection;alice|Inbound;anonymous|Connection");
        EventIds(host, 175).Count.ShouldBe(3);
        EventIds(host, 172).ShouldBeEmpty();
    }

    // ── SignalR over WebSockets, raw WebSockets ───────────────────────────

    [Fact]
    public async Task AHubMethod_ReadsAnonymous_AndCannotOpenAServiceScope_WithoutTheOptIn()
    {
        using var host = await StartOrderedAsync(Order.Documented);

        await using var hub = await HubConnection.ConnectAsync(host, "alice");
        var who = await hub.InvokeAsync("WhoAmI");
        var refused = await hub.InvokeAsync("TryService", false);
        var allowed = await hub.InvokeAsync("TryService", true);

        who.ShouldBe("anonymous|Connection");
        refused.ShouldBe(RequestIdentityErrorCodes.ScopeConflict);
        allowed.ShouldBe("service:test-job");
    }

    [Fact]
    public async Task AWebSocket_IsSkippedEvenWhenMisordered_AndItsCloseRevealsTheMisordering()
    {
        // Fresh host, no negotiate: the WebSocket connection is the first request.
        using var host = await StartOrderedAsync(Order.BeforeRouting);

        await using (var hub = await HubConnection.ConnectAsync(host, "alice"))
        {
            (await hub.InvokeAsync("WhoAmI")).ShouldBe("anonymous|Connection");
        }

        // next returns when the connection closes; the endpoint is then the hub: Critical 202.
        await WaitForAsync(() => EventIds(host, 202).Count == 1);
        (await GetAsync(host, "/whoami", user: "alice")).Status.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ARawWebSocketEndpoint_ReadsAnonymous_WhenUseWebSocketsRunsAfterUseEncinaContext()
    {
        using var host = await StartAsync(app =>
        {
            UseTestAuthentication(app);
            app.UseEncinaContext();
            app.UseWebSockets();
            Microsoft.AspNetCore.Builder.RunExtensions.Run(app, RawWebSocketEndpoint);
        });
        var client = host.GetTestServer().CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers[UserHeader] = "alice";

        using var socket = await client.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        (await ReceiveTextAsync(socket)).ShouldBe("anonymous|Connection");
    }

    [Fact]
    public async Task OverKestrel_ARawWebSocket_IsDetectedFromTheRequest_BeforeUseWebSockets()
    {
        // No TestServer feature: Kestrel exposes the upgrade only through IHttpUpgradeFeature.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddEncinaAspNetCore();
        await using var app = builder.Build();
        UseTestAuthentication(app);
        app.UseEncinaContext();
        app.UseWebSockets();
        Microsoft.AspNetCore.Builder.RunExtensions.Run(app, RawWebSocketEndpoint);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader(UserHeader, "alice");
        await socket.ConnectAsync(new Uri(address.Replace("http", "ws", StringComparison.Ordinal) + "/ws"), CancellationToken.None);

        (await ReceiveTextAsync(socket)).ShouldBe("anonymous|Connection");
        await app.StopAsync();
    }

    private static async Task RawWebSocketEndpoint(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await socket.SendAsync(Encoding.UTF8.GetBytes(Who(context.RequestServices)), WebSocketMessageType.Text, true, CancellationToken.None);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    // ── The latch (MQ-1 (b), D1) and re-routing (E1, DR-H) ─────────────────

    [Fact]
    public async Task AMisorderedPipeline_LatchesOnTheFirstNegotiate_AndAnswers500Afterwards()
    {
        using var host = await StartOrderedAsync(Order.BeforeRouting);

        var negotiate = await GetAsync(host, "/hub/negotiate?negotiateVersion=1", method: HttpMethod.Post);
        var later = await GetAsync(host, "/whoami", user: "alice");
        var connection = await GetAsync(host, "/sse", accept: "text/event-stream");

        negotiate.Status.ShouldBe(HttpStatusCode.OK);
        later.Status.ShouldBe(HttpStatusCode.InternalServerError);
        connection.Status.ShouldBe(HttpStatusCode.InternalServerError);
        EventIds(host, 202, 203).ShouldBe([202]);
    }

    [Fact]
    public async Task TheDocumentedOrder_NeverLatches_AndAnotherHostIsNotAffected()
    {
        using var misordered = await StartOrderedAsync(Order.BeforeRouting);
        using var documented = await StartOrderedAsync(Order.Documented);

        await GetAsync(misordered, "/hub/negotiate?negotiateVersion=1", method: HttpMethod.Post);
        var negotiate = await GetAsync(documented, "/hub/negotiate?negotiateVersion=1", method: HttpMethod.Post);
        var later = await GetAsync(documented, "/whoami", user: "alice");

        negotiate.Status.ShouldBe(HttpStatusCode.OK);
        later.Body.ShouldBe("alice|Inbound");
        EventIds(documented, 202, 203).ShouldBeEmpty();
        (await GetAsync(misordered, "/whoami")).Status.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TheDocumentedOrderWithRewriterAndErrorHandlers_ReRoutesWithoutAnyLatch()
    {
        // Minimal hosting: the rewriter and the error handlers re-run routing through the global router.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFakeLogging();
        builder.Services.AddSignalR();
        builder.Services.AddEncinaAspNetCore();
        builder.Services.AddEncinaServiceIdentity("test-job");
        await using var app = builder.Build();
        app.UseRouting();
        app.UseRewriter(new RewriteOptions().AddRewrite("^old$", "whoami", skipRemainingRules: true));
        app.UseStatusCodePagesWithReExecute("/error");
        app.UseExceptionHandler("/error");
        UseTestAuthentication(app);
        app.UseEncinaContext();
        MapEndpoints(app);
        await app.StartAsync();

        await AssertReRoutingAsync(app);
        await app.StopAsync();
    }

    [Fact]
    public async Task RewriterAndStatusCodeReExecuteInsideUseEncinaContext_ReRouteWithoutAnyLatch()
    {
        // Against the documented order: the re-routing runs inside next. The status-code re-execute
        // restores Request.Path when it finishes (StatusCodePagesExtensions.cs, release/10.0), so it
        // never changes the path; the rewrite lands on a plain endpoint. Neither trips the latch.
        using var host = await StartAsync(app =>
        {
            UseTestAuthentication(app);
            app.UseEncinaContext();
            app.UseRewriter(new RewriteOptions().AddRewrite("^old$", "whoami", skipRemainingRules: true));
            app.UseStatusCodePagesWithReExecute("/error");
            app.UseRouting();
            app.UseEndpoints(MapEndpoints);
        });

        (await GetAsync(host, "/old", user: "alice")).Body.ShouldBe("alice|Inbound");
        var missing = await GetAsync(host, "/missing");
        missing.Status.ShouldBe(HttpStatusCode.NotFound);
        missing.Body.ShouldBe("error-page");
        (await GetAsync(host, "/whoami", user: "bob")).Body.ShouldBe("bob|Inbound");
        EventIds(host, 202, 203).ShouldBeEmpty();
    }

    [Fact]
    public async Task ARewriteWhoseTargetIsAHub_LogsWarning203_AndLatchesNothing_WhileADirectHubRequestLatches()
    {
        using var host = await StartAsync(app =>
        {
            UseTestAuthentication(app);
            app.UseEncinaContext();
            app.UseRewriter(new RewriteOptions().AddRewrite("^legacy-hub/(.*)$", "hub/$1", skipRemainingRules: true));
            app.UseRouting();
            app.UseEndpoints(MapEndpoints);
        });

        var first = await GetAsync(host, "/legacy-hub/negotiate?negotiateVersion=1", method: HttpMethod.Post);
        var unrelated = await GetAsync(host, "/whoami", user: "alice");
        var second = await GetAsync(host, "/legacy-hub/negotiate?negotiateVersion=1", method: HttpMethod.Post);

        first.Status.ShouldBe(HttpStatusCode.OK);
        unrelated.Body.ShouldBe("alice|Inbound");
        second.Status.ShouldBe(HttpStatusCode.OK);
        EventIds(host, 202, 203).ShouldBe([203, 203]);

        // Only the unchanged-path case latches.
        (await GetAsync(host, "/hub/negotiate?negotiateVersion=1", method: HttpMethod.Post)).Status.ShouldBe(HttpStatusCode.OK);
        (await GetAsync(host, "/whoami")).Status.ShouldBe(HttpStatusCode.InternalServerError);
        EventIds(host, 202, 203).ShouldBe([203, 203, 202]);
    }

    private static async Task AssertReRoutingAsync(IHost host)
    {
        (await GetAsync(host, "/old", user: "alice")).Body.ShouldBe("alice|Inbound");
        var missing = await GetAsync(host, "/missing");
        missing.Status.ShouldBe(HttpStatusCode.NotFound);
        missing.Body.ShouldBe("error-page");
        var failed = await GetAsync(host, "/boom");
        failed.Status.ShouldBe(HttpStatusCode.InternalServerError);
        failed.Body.ShouldBe("error-page");
        (await GetAsync(host, "/whoami", user: "bob")).Body.ShouldBe("bob|Inbound");
        EventIds(host, 202, 203).ShouldBeEmpty();
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    private static async Task<string> ReceiveTextAsync(WebSocket socket)
    {
        var buffer = new byte[4096];
        var builder = new StringBuilder();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
        while (!result.EndOfMessage);

        return builder.ToString();
    }

    // ── A hub and a minimal SignalR JSON-protocol client over the TestServer WebSocket ──

    public sealed class IdentityHub(IServiceProvider services) : Hub
    {
        public string WhoAmI() => Who(services);

        public Task<string> TryService(bool allowOverInbound) => TryServiceAsync(services, allowOverInbound);
    }

    private sealed class HubConnection : IAsyncDisposable
    {
        private readonly WebSocket _socket;
        private int _invocationId;

        private HubConnection(WebSocket socket) => _socket = socket;

        public static async Task<HubConnection> ConnectAsync(IHost host, string user)
        {
            var client = host.GetTestServer().CreateWebSocketClient();
            client.ConfigureRequest = request => request.Headers[UserHeader] = user;
            var socket = await client.ConnectAsync(new Uri("ws://localhost/hub"), CancellationToken.None);
            var connection = new HubConnection(socket);
            await connection.SendAsync("{\"protocol\":\"json\",\"version\":1}");
            (await connection.ReceiveAsync()).ShouldBe("{}");
            return connection;
        }

        public async Task<string?> InvokeAsync(string target, params object[] arguments)
        {
            var id = (++_invocationId).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await SendAsync(JsonSerializer.Serialize(new { type = 1, invocationId = id, target, arguments }));
            while (true)
            {
                using var message = JsonDocument.Parse(await ReceiveAsync());
                var root = message.RootElement;
                if (root.GetProperty("type").GetInt32() == 3 && root.GetProperty("invocationId").GetString() == id)
                {
                    return root.TryGetProperty("result", out var value) ? value.GetString() : root.GetProperty("error").GetString();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            _socket.Dispose();
        }

        private Task SendAsync(string message) =>
            _socket.SendAsync(Encoding.UTF8.GetBytes(message + RecordSeparator), WebSocketMessageType.Text, true, CancellationToken.None);

        // One JSON message; SignalR frames end with the record separator and may arrive together.
        private readonly Queue<string> _pending = new();

        private async Task<string> ReceiveAsync()
        {
            while (_pending.Count == 0)
            {
                foreach (var frame in (await ReceiveTextAsync(_socket)).Split(RecordSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    _pending.Enqueue(frame);
                }
            }

            return _pending.Dequeue();
        }
    }
}
