using System.Net;
using Encina.AspNetCore;
using Encina.Testing;
using Encina.Testing.Identity;
using Encina.UnitTests.Core.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="EncinaContextMiddleware"/> over a <see cref="DefaultHttpContext"/> and the
/// real scope factory (#1705, Phase 3 tasks 1 and 11): the inbound scope, the connection skip, the
/// refusals and the misordering latch.
/// </summary>
public sealed class EncinaContextMiddlewareTests
{
    private const string Sentinel = "sentinel-mw-91d0";

    private readonly ScopeTestHost _host = new();
    private readonly EncinaAspNetCoreOptions _options = new();
    private readonly FakeLogger<EncinaContextMiddleware> _logger = new();
    private IRequestContext? _seen;
    private int _nextCalls;

    private EncinaContextMiddleware CreateMiddleware(RequestDelegate? next = null) =>
        new(next ?? (_ =>
        {
            _nextCalls++;
            _seen = _host.Accessor.RequestContext;
            return Task.CompletedTask;
        }), Options.Create(_options), _logger);

    private Task InvokeAsync(EncinaContextMiddleware middleware, HttpContext context) => middleware.InvokeAsync(context, _host.Factory);

    private IReadOnlyList<int> MiddlewareEventIds => [.. _logger.Collector.GetSnapshot().Select(static record => record.Id.Id)];

    private static Endpoint HubEndpoint() =>
        new(_ => Task.CompletedTask, new EndpointMetadataCollection(new HubMetadata(typeof(Hub))), "hub");

    private static Endpoint PlainEndpoint() => new(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "plain");

    // ── The inbound scope ─────────────────────────────────────────────────

    [Fact]
    public async Task AnAuthenticatedRequest_RunsInsideAnInboundScope_WithTheMappedUser()
    {
        var context = new DefaultHttpContext { User = TestIdentity.Principal("oidc-user-1", ["Admin"], ["orders:read"]) };

        await InvokeAsync(CreateMiddleware(), context);

        _nextCalls.ShouldBe(1);
        _seen.ShouldNotBeNull();
        _seen.Issued().UserId.ShouldBe("oidc-user-1");
        _seen.Issued().Roles.ShouldContain("Admin");
        _seen.Issued().Permissions.ShouldContain("orders:read");
        ((RequestContext)_seen).Origin.ShouldBe(RequestOrigin.Inbound);
        _seen.Timestamp.ShouldBe(_host.Time.GetUtcNow());
        _host.EventIds.ShouldContain(172);
        _host.EventIds.ShouldNotContain(175);
    }

    [Fact]
    public async Task AnAnonymousRequest_RunsInsideAnInboundScope_WithTheAnonymousIdentity()
    {
        await InvokeAsync(CreateMiddleware(), new DefaultHttpContext());

        _seen.ShouldNotBeNull();
        _seen.Identity.Kind.ShouldBe(IdentityKind.Anonymous);
        ((RequestContext)_seen).Origin.ShouldBe(RequestOrigin.Inbound);
    }

    [Fact]
    public async Task TheScope_EndsWithTheRequest()
    {
        await InvokeAsync(CreateMiddleware(), new DefaultHttpContext { User = TestIdentity.Principal("alice") });

        _seen.Issued().Issuer!.IsLive.ShouldBeFalse();
        _seen!.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        _host.Accessor.RequestContext.ShouldBeNull();
        _host.EventIds.ShouldContain(168);
    }

    [Fact]
    public async Task AnAnonymousRequest_CannotOpenAServiceScope_WithoutTheOptIn()
    {
        Either<EncinaError, int> refused = default;
        Either<EncinaError, int> optedIn = default;
        var middleware = CreateMiddleware(async _ =>
        {
            refused = await _host.Factory.RunAsServiceAsync(ScopeTestHost.Job, (_, _) => Task.FromResult(Right<EncinaError, int>(1)));
            optedIn = await _host.Factory.RunAsServiceAsync(
                ScopeTestHost.Job, (_, _) => Task.FromResult(Right<EncinaError, int>(2)), new IdentityScopeOptions(AllowOverInbound: true));
        });

        await InvokeAsync(middleware, new DefaultHttpContext());

        refused.IsLeft.ShouldBeTrue();
        refused.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.ScopeConflict));
        optedIn.ShouldBeSuccess().ShouldBe(2);
        _host.EventIds.ShouldContain(167);
        _host.EventIds.ShouldContain(174);
    }

    // ── What the request carries (task 11 builder) ────────────────────────

    [Fact]
    public async Task TheCorrelationIdHeader_IsUsed_AndEchoedInTheResponse()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "test-corr-123";

        await InvokeAsync(CreateMiddleware(), context);

        _seen!.CorrelationId.ShouldBe("test-corr-123");
        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe("test-corr-123");
    }

    [Fact]
    public async Task ACustomCorrelationIdHeader_IsUsed()
    {
        _options.CorrelationIdHeader = "X-Request-ID";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "custom-123";

        await InvokeAsync(CreateMiddleware(), context);

        _seen!.CorrelationId.ShouldBe("custom-123");
        context.Response.Headers["X-Request-ID"].ToString().ShouldBe("custom-123");
    }

    [Fact]
    public async Task AnOverlongCorrelationId_IsReplaced_AndTheRequestRunsNormally()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = new string('c', 10_000);

        await InvokeAsync(CreateMiddleware(), context);

        _nextCalls.ShouldBe(1);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        _seen!.CorrelationId.Length.ShouldBeLessThanOrEqualTo(InboundRequestInfo.MaxIdLength);
        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe(_seen.CorrelationId);
    }

    [Fact]
    public async Task TheTenant_ComesFromTheClaim_ThenFromTheHeader()
    {
        var withClaim = new DefaultHttpContext { User = ScopeTestHost.UserPrincipal("alice", tenant: "claim-tenant") };
        withClaim.Request.Headers["X-Tenant-ID"] = "header-tenant";
        var anonymous = new DefaultHttpContext();
        anonymous.Request.Headers["X-Tenant-ID"] = "header-tenant";

        await InvokeAsync(CreateMiddleware(), withClaim);
        var claimTenant = _seen!.TenantId;
        await InvokeAsync(CreateMiddleware(), anonymous);

        claimTenant.ShouldBe("claim-tenant");
        _seen!.TenantId.ShouldBe("header-tenant");
    }

    [Fact]
    public async Task TheIdempotencyKey_IsKeptUpTo255Characters_AndDroppedAbove()
    {
        var kept = new DefaultHttpContext();
        kept.Request.Headers["X-Idempotency-Key"] = new string('k', 200);
        var dropped = new DefaultHttpContext();
        dropped.Request.Headers["X-Idempotency-Key"] = new string('k', 300);

        await InvokeAsync(CreateMiddleware(), kept);
        var keptKey = _seen!.IdempotencyKey;
        await InvokeAsync(CreateMiddleware(), dropped);

        keptKey.ShouldBe(new string('k', 200));
        _seen!.IdempotencyKey.ShouldBeNull();
    }

    [Fact]
    public async Task TheUserAgent_LosesItsControlCharacters()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["User-Agent"] = "Agent/1.0\u0001\u0007 (forged\u0002)";

        await InvokeAsync(CreateMiddleware(), context);

        _seen!.GetUserAgent().ShouldBe("Agent/1.0 (forged)");
    }

    [Fact]
    public async Task TheIpAddress_IsTheConnectionAddress_AndXForwardedForIsIgnored()
    {
        // X-Forwarded-For is client-controlled; trusted proxies go through UseForwardedHeaders().
        var spoofed = new DefaultHttpContext();
        spoofed.Request.Headers["X-Forwarded-For"] = "203.0.113.7, 10.0.0.1";
        spoofed.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.4");
        var headerOnly = new DefaultHttpContext();
        headerOnly.Request.Headers["X-Forwarded-For"] = "203.0.113.7";

        await InvokeAsync(CreateMiddleware(), spoofed);
        var spoofedIp = _seen!.GetIpAddress();
        await InvokeAsync(CreateMiddleware(), headerOnly);

        spoofedIp.ShouldBe("198.51.100.4");
        _seen!.GetIpAddress().ShouldBeNull();
    }

    [Fact]
    public async Task TheDataRegionHeader_ReachesTheContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Data-Region"] = "eu-west-1";

        await InvokeAsync(CreateMiddleware(), context);

        _seen!.GetDataRegion().ShouldBe("eu-west-1");
    }

    [Fact]
    public void CreateInboundRequestInfo_ReadsTheRegisteredOptions_OrTheDefaults()
    {
        var withOptions = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().Configure<EncinaAspNetCoreOptions>(o => o.TenantIdHeader = "X-Org").BuildServiceProvider()
        };
        withOptions.Request.Headers["X-Org"] = "org-1";
        var withoutServices = new DefaultHttpContext { RequestServices = null! };
        withoutServices.Request.Headers["X-Tenant-ID"] = "tenant-1";

        withOptions.CreateInboundRequestInfo().TenantHeaderValue.ShouldBe("org-1");
        withoutServices.CreateInboundRequestInfo().TenantHeaderValue.ShouldBe("tenant-1");
        Should.Throw<ArgumentNullException>(() => ((HttpContext)null!).CreateInboundRequestInfo());
    }

    [Fact]
    public void CreateInboundRequestInfo_TakesTheIpFromTheConnection_AfterUseForwardedHeadersRewroteIt()
    {
        // UseForwardedHeaders with a trusted proxy rewrites RemoteIpAddress; the builder reads that.
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::7");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.99";

        context.CreateInboundRequestInfo().IpAddress.ShouldBe("2001:db8::7");
    }

    [Fact]
    public void CreateInboundRequestInfo_CopiesThePrincipal_AndBlankHeadersAreAbsent()
    {
        var principal = TestIdentity.Principal("alice");
        var context = new DefaultHttpContext { User = principal };
        context.Request.Headers["X-Idempotency-Key"] = "   ";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7";

        var info = context.CreateInboundRequestInfo();

        info.Principal.ShouldBeSameAs(principal);
        info.IdempotencyKey.ShouldBeNull();
        info.IpAddress.ShouldBeNull("X-Forwarded-For is never read; only RemoteIpAddress");
        info.ToString().ShouldBe(nameof(InboundRequestInfo));
    }

    // ── Connection requests ───────────────────────────────────────────────

    [Fact]
    public async Task AConnectionRequest_RunsUnderTheConnectionMarker_WithNoIdentity()
    {
        var context = new DefaultHttpContext { User = TestIdentity.Principal("connect-time-user") };
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Accept = "text/event-stream";

        await InvokeAsync(CreateMiddleware(), context);

        _nextCalls.ShouldBe(1);
        _seen.ShouldNotBeNull();
        _seen.Identity.Kind.ShouldBe(IdentityKind.Anonymous);
        ((RequestContext)_seen).Origin.ShouldBe(RequestOrigin.Connection);
        _host.EventIds.ShouldNotContain(172);
        context.Response.Headers.ContainsKey("X-Correlation-ID").ShouldBeFalse();
    }

    [Fact]
    public async Task AHubEndpoint_RunsUnderTheConnectionMarker()
    {
        var context = new DefaultHttpContext { User = TestIdentity.Principal("connect-time-user") };
        context.SetEndpoint(HubEndpoint());

        await InvokeAsync(CreateMiddleware(), context);

        ((RequestContext)_seen!).Origin.ShouldBe(RequestOrigin.Connection);
        MiddlewareEventIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task APostThatStreams_KeepsItsRequestIdentity()
    {
        var context = new DefaultHttpContext { User = TestIdentity.Principal("mcp-user") };
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.Accept = "application/json, text/event-stream";

        await InvokeAsync(CreateMiddleware(), context);

        var seen = _seen.ShouldBeOfType<RequestContext>();
        seen.Issued().UserId.ShouldBe("mcp-user");
        seen.Origin.ShouldBe(RequestOrigin.Inbound);
    }

    // ── Refusals ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AnAbortedRequest_GetsNoResponse_AndNextIsNotCalled()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = new DefaultHttpContext { RequestAborted = aborted.Token };

        await InvokeAsync(CreateMiddleware(), context);

        _nextCalls.ShouldBe(0);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        _host.EventIds.ShouldNotContain(167);
        MiddlewareEventIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAbortedConnectionRequest_GetsNoResponse_AndNextIsNotCalled()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = new DefaultHttpContext { RequestAborted = aborted.Token };
        context.SetEndpoint(HubEndpoint());

        await InvokeAsync(CreateMiddleware(), context);

        _nextCalls.ShouldBe(0);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task AMiddlewareRegisteredTwice_Answers500_WithoutCallingNext()
    {
        var inner = CreateMiddleware();
        var outer = CreateMiddleware(context => InvokeAsync(inner, context));
        var context = new DefaultHttpContext { User = TestIdentity.Principal("alice") };

        await InvokeAsync(outer, context);

        _nextCalls.ShouldBe(0);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        _host.EventIds.ShouldContain(167);
    }

    [Fact]
    public async Task AnUnsupportedAccessor_Answers500_ForEveryRequestKind()
    {
        var factory = new RequestContextScopeFactory(
            Substitute.For<IRequestContextAccessor>(), _host.Catalog, _host.IdentityFactory,
            Options.Create(new RequestIdentityOptions()), _host.Time, _host.Logger);
        var middleware = CreateMiddleware();
        var request = new DefaultHttpContext();
        var connection = new DefaultHttpContext();
        connection.SetEndpoint(HubEndpoint());

        await middleware.InvokeAsync(request, factory);
        await middleware.InvokeAsync(connection, factory);

        _nextCalls.ShouldBe(0);
        request.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        connection.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        middleware.IsMisordered.ShouldBeFalse();
    }

    // ── The misordering latch (D1, E1) ────────────────────────────────────

    [Fact]
    public async Task AHubReachedWithNoEndpointBefore_AndAnUnchangedPath_LogsCritical202Once_AndLatches()
    {
        var middleware = CreateMiddleware(context =>
        {
            _nextCalls++;
            context.SetEndpoint(HubEndpoint());
            return Task.CompletedTask;
        });
        var first = new DefaultHttpContext();
        first.Request.Path = "/hub/negotiate";
        var second = new DefaultHttpContext();
        var third = new DefaultHttpContext();
        third.SetEndpoint(PlainEndpoint());

        await InvokeAsync(middleware, first);
        await InvokeAsync(middleware, second);
        await InvokeAsync(middleware, third);

        _nextCalls.ShouldBe(1);
        middleware.IsMisordered.ShouldBeTrue();
        first.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        second.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        third.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(202);
        record.Level.ShouldBe(LogLevel.Critical);
    }

    [Fact]
    public async Task TheLatch_IsLoggedOnce_EvenWhenTwoRequestsRevealIt()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var middleware = CreateMiddleware(async context =>
        {
            await gate.Task;
            context.SetEndpoint(HubEndpoint());
        });

        var one = InvokeAsync(middleware, new DefaultHttpContext());
        var two = InvokeAsync(middleware, new DefaultHttpContext());
        gate.SetResult();
        await Task.WhenAll(one, two);

        MiddlewareEventIds.ShouldBe([202]);
    }

    [Fact]
    public async Task AHubReachedThroughAChangedPath_LogsWarning203_AndDoesNotLatch()
    {
        var middleware = CreateMiddleware(context =>
        {
            _nextCalls++;
            context.Request.Path = "/hub";
            context.SetEndpoint(HubEndpoint());
            return Task.CompletedTask;
        });
        var rewritten = new DefaultHttpContext();
        rewritten.Request.Path = "/legacy-hub";
        var again = new DefaultHttpContext();
        again.Request.Path = "/legacy-hub";

        await InvokeAsync(middleware, rewritten);
        await InvokeAsync(middleware, again);

        _nextCalls.ShouldBe(2);
        middleware.IsMisordered.ShouldBeFalse();
        again.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        MiddlewareEventIds.ShouldBe([203, 203]);
        _logger.Collector.GetSnapshot().ShouldAllBe(record => record.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task ANonHubEndpointOrA404_AfterNext_NeverLatches()
    {
        var routed = CreateMiddleware(context =>
        {
            context.SetEndpoint(PlainEndpoint());
            return Task.CompletedTask;
        });
        var notFound = CreateMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });

        await InvokeAsync(routed, new DefaultHttpContext());
        await InvokeAsync(notFound, new DefaultHttpContext());

        routed.IsMisordered.ShouldBeFalse();
        notFound.IsMisordered.ShouldBeFalse();
        MiddlewareEventIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheLatch_IsPerInstance()
    {
        var misordered = CreateMiddleware(context =>
        {
            context.SetEndpoint(HubEndpoint());
            return Task.CompletedTask;
        });
        var other = CreateMiddleware();

        await InvokeAsync(misordered, new DefaultHttpContext());
        var context = new DefaultHttpContext();
        await InvokeAsync(other, context);

        misordered.IsMisordered.ShouldBeTrue();
        other.IsMisordered.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData(true, true, false, "None")]
    [InlineData(false, false, false, "None")]
    [InlineData(false, true, false, "BeforeRouting")]
    [InlineData(false, true, true, "PathChanged")]
    public void Classify_ReadsTheEndpointBefore_TheEndpointAfter_AndThePath(bool endpointBefore, bool hubAfter, bool pathChanged, string expected)
    {
        var signal = EncinaContextMiddleware.Classify(
            endpointBefore ? PlainEndpoint() : null,
            hubAfter ? HubEndpoint() : PlainEndpoint(),
            "/hub",
            pathChanged ? "/elsewhere" : "/HUB");

        signal.ToString().ShouldBe(expected);
    }

    // ── No request data in the middleware logs ────────────────────────────

    [Fact]
    public async Task TheMiddlewareLogs_CarryNoRequestData()
    {
        var middleware = CreateMiddleware(context =>
        {
            context.Request.Path = "/hub";
            context.SetEndpoint(HubEndpoint());
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext { User = TestIdentity.Principal(Sentinel) };
        context.Request.Path = "/" + Sentinel;
        context.Request.Headers["X-Tenant-ID"] = Sentinel;

        await InvokeAsync(middleware, context);

        MiddlewareEventIds.ShouldBe([203]);
        foreach (var record in _logger.Collector.GetSnapshot().Concat(_host.Logger.Collector.GetSnapshot()))
        {
            record.Message.ShouldNotContain(Sentinel);
            foreach (var pair in record.StructuredState ?? [])
            {
                (pair.Value ?? string.Empty).ShouldNotContain(Sentinel);
            }
        }
    }

    [Fact]
    public void TheConstructor_RejectsNullArguments()
    {
        Should.Throw<ArgumentNullException>(() => new EncinaContextMiddleware(null!, Options.Create(_options), _logger));
        Should.Throw<ArgumentNullException>(() => new EncinaContextMiddleware(_ => Task.CompletedTask, null!, _logger));
        Should.Throw<ArgumentNullException>(() => new EncinaContextMiddleware(_ => Task.CompletedTask, Options.Create(_options), null!));
    }
}
