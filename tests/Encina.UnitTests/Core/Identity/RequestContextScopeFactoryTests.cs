using System.Diagnostics;
using System.Security.Claims;
using Encina.Testing;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for <see cref="RequestContextScopeFactory"/>: the delegate-only scope, its check order
/// (accessor, cancellation, arguments, holder chain), its results, its logs (EventIds 166-175) and
/// the tenant and correlation rules.
/// </summary>
public sealed class RequestContextScopeFactoryTests
{
    private const string Sentinel = "sentinel-7f3a";

    private readonly ScopeTestHost _host = new();

    private RequestContextScopeFactory Factory => _host.Factory;

    private static Task<Either<EncinaError, int>> Ok(IRequestContext context, CancellationToken cancellationToken) =>
        Task.FromResult(Right<EncinaError, int>(1));

    private static void ShouldBeLeftWith<T>(Either<EncinaError, T> result, string code)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(code));
    }

    private IReadOnlyList<FakeLogRecord> Records => _host.Logger.Collector.GetSnapshot();

    private void NoSentinelInAnyRecord()
    {
        foreach (var record in Records.Concat(_host.IdentityLogger.Collector.GetSnapshot()))
        {
            record.Message.ShouldNotContain(Sentinel);
            foreach (var pair in record.StructuredState ?? [])
            {
                (pair.Value ?? string.Empty).ShouldNotContain(Sentinel);
            }
        }
    }

    // ── The frame: the caller's context comes back, nothing leaks ────────

    [Fact]
    public async Task AScope_IsAmbientInsideWork_AndTheCallersContextIsBackAfterwards()
    {
        await Task.Yield();
        var caller = RequestContext.CreateForTest(correlationId: "caller");
        _host.Accessor.RequestContext = caller;

        var result = await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.ReadAmbient(_host.Accessor));

        result.ShouldBeSuccess().Issued().UserId.ShouldBe("service:test-job");
        _host.Accessor.RequestContext.ShouldBeSameAs(caller);
    }

    [Fact]
    public async Task ACallNotAwaited_LeavesTheCallersAmbientUnchanged()
    {
        await Task.Yield();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var running = Factory.RunAsServiceAsync(ScopeTestHost.Job, async (context, _) =>
        {
            await gate.Task;
            return Right<EncinaError, string?>(context.UserId);
        });

        _host.Accessor.RequestContext.ShouldBeNull();
        RequestContextAccessor.CurrentFacts.ShouldBe(ChainFacts.None);
        gate.SetResult();
        (await running).ShouldBeSuccess().ShouldBe("service:test-job");
        _host.Accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task ASynchronousBody_AlsoRestoresTheCaller()
    {
        await Task.Yield();

        var result = await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture);

        result.ShouldBeSuccess().Issued().UserId.ShouldBe("service:test-job");
        _host.Accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task EveryScope_GetsANewIdentityAndIssuer_AndTheIdentityIsStaleAfterItsScope()
    {
        var first = (await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture)).ShouldBeSuccess();
        var second = (await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture)).ShouldBeSuccess();

        first.Issued().ShouldNotBeSameAs(second.Issued());
        first.Issued().Issuer.ShouldNotBeNull();
        first.Issued().Issuer.ShouldNotBeSameAs(second.Issued().Issuer);
        first.Issued().Issuer!.IsLive.ShouldBeFalse();
        first.Issued().IsSameAs(second.Issued()).ShouldBeTrue();
        first.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
    }

    // ── Results: Left and exceptions pass through after invalidation ─────

    [Fact]
    public async Task ALeftFromWork_PassesThroughUnchanged_AfterTheScopeEnded()
    {
        var error = EncinaErrors.Create("test.failure", "failure");
        IRequestContext? inside = null;

        var result = await Factory.RunAsServiceAsync<int>(ScopeTestHost.Job, (context, _) =>
        {
            inside = context;
            return Task.FromResult(Left<EncinaError, int>(error));
        });

        ShouldBeLeftWith(result, "test.failure");
        inside.Issued().Issuer!.IsLive.ShouldBeFalse();
        _host.EventIds.ShouldContain(168);
    }

    [Fact]
    public async Task AnExceptionFromWork_Propagates_AfterTheScopeEnded()
    {
        IRequestContext? inside = null;

        await Should.ThrowAsync<InvalidOperationException>(() => Factory.RunAsServiceAsync<int>(ScopeTestHost.Job, (context, _) =>
        {
            inside = context;
            throw new InvalidOperationException("boom");
        }));

        inside.Issued().Issuer!.IsLive.ShouldBeFalse();
        _host.EventIds.ShouldContain(168);
    }

    [Fact]
    public async Task AnOperationCanceledExceptionFromWork_Propagates()
    {
        using var cts = new CancellationTokenSource();

        await Should.ThrowAsync<OperationCanceledException>(() => Factory.RunAsServiceAsync<int>(ScopeTestHost.Job, (_, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Right<EncinaError, int>(1));
        }, cancellationToken: cts.Token));
    }

    // ── Rule 1 and 2: accessor and cancellation ──────────────────────────

    [Fact]
    public async Task AnUnsupportedAccessor_RefusesEveryMember_WithoutInvokingWork()
    {
        var factory = new RequestContextScopeFactory(
            Substitute.For<IRequestContextAccessor>(), _host.Catalog, _host.IdentityFactory,
            Microsoft.Extensions.Options.Options.Create(new RequestIdentityOptions()), _host.Time, _host.Logger);
        var invoked = false;
        Task<Either<EncinaError, int>> Work(IRequestContext c, CancellationToken t)
        {
            invoked = true;
            return Ok(c, t);
        }

        ShouldBeLeftWith(await factory.RunAsServiceAsync(ScopeTestHost.Job, Work), RequestIdentityErrorCodes.UnsupportedAccessor);
        ShouldBeLeftWith(await factory.RunAsPrincipalAsync(TestIdentity.Principal("alice"), Work), RequestIdentityErrorCodes.UnsupportedAccessor);
        ShouldBeLeftWith(await factory.RunInboundAsync(new InboundRequestInfo(null), Work), RequestIdentityErrorCodes.UnsupportedAccessor);
        ShouldBeLeftWith(await factory.RunHostInboundAsync(new InboundRequestInfo(null), Work), RequestIdentityErrorCodes.UnsupportedAccessor);
        ShouldBeLeftWith(await factory.RunAsBuiltInAsync(ScopeTestHost.BuiltIn, Work), RequestIdentityErrorCodes.UnsupportedAccessor);
        ShouldBeLeftWith(
            await factory.RunRestoredAsync(new PersistedRequestIdentity(IdentityKind.Anonymous, null, null, "c", null), PersistedIdentitySource.Internal, Work),
            RequestIdentityErrorCodes.UnsupportedAccessor);

        invoked.ShouldBeFalse();
        _host.EventIds.Count(id => id == 167).ShouldBe(6);
    }

    [Fact]
    public async Task ACancelledToken_ReturnsRequestCancelled_BeforePush_WithoutLogging167()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var invoked = false;

        var result = await Factory.RunAsServiceAsync(ScopeTestHost.Job, (c, t) =>
        {
            invoked = true;
            return Ok(c, t);
        }, cancellationToken: cts.Token);

        ShouldBeLeftWith(result, EncinaErrorCodes.RequestCancelled);
        invoked.ShouldBeFalse();
        _host.EventIds.ShouldBeEmpty();
    }

    // ── Rule 3: arguments ─────────────────────────────────────────────────

    [Fact]
    public async Task AnUnknownServiceName_IsRefused_AndLogs167WithTheCodeOnly()
    {
        var result = await Factory.RunAsServiceAsync("not-declared", Ok);

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.UnknownServiceIdentity);
        var record = Records.Single(r => r.Id.Id == 167);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain(RequestIdentityErrorCodes.UnknownServiceIdentity);
    }

    [Fact]
    public async Task ABuiltInName_ThroughThePublicApi_IsReserved()
    {
        ShouldBeLeftWith(await Factory.RunAsServiceAsync(ScopeTestHost.BuiltIn, Ok), RequestIdentityErrorCodes.ReservedServiceIdentity);
        ShouldBeLeftWith(await Factory.RunAsServiceAsync("encina.not-declared", Ok), RequestIdentityErrorCodes.ReservedServiceIdentity);
    }

    [Fact]
    public async Task RunAsBuiltInAsync_OpensOnlyBuiltInIdentities()
    {
        var builtIn = await Factory.RunAsBuiltInAsync(ScopeTestHost.BuiltIn, ScopeTestHost.Capture);
        var application = await Factory.RunAsBuiltInAsync(ScopeTestHost.Job, ScopeTestHost.Capture);

        var context = builtIn.ShouldBeSuccess();
        context.Issued().UserId.ShouldBe("service:encina.test.seeding");
        context.Issued().Permissions.ShouldContain("policies:seed");
        ShouldBeLeftWith(application, RequestIdentityErrorCodes.UnknownServiceIdentity);
        Records.ShouldContain(r => r.Id.Id == 166 && r.Message.Contains(ScopeTestHost.BuiltIn));
    }

    [Fact]
    public async Task RunAsPrincipalAsync_WithATenantThatDiffersFromThePrincipalClaim_IsTenantConflict()
    {
        var principal = ScopeTestHost.UserPrincipal("alice", tenant: "t1");

        var result = await Factory.RunAsPrincipalAsync(principal, Ok, new IdentityScopeOptions(TenantId: "t2"));

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.TenantConflict);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("t1")]
    [InlineData("t3")]
    public async Task RunAsPrincipalAsync_WithTwoDistinctTenantClaimsInOneIdentity_IsRefused_AndWorkNeverRuns(string? optionTenant)
    {
        var principal = TestIdentity.Principal("alice", claims: [new Claim("tenant_id", "t1-" + Sentinel), new Claim("tenant_id", "t2-" + Sentinel)]);
        var invoked = false;

        var result = await Factory.RunAsPrincipalAsync(principal, (c, t) =>
        {
            invoked = true;
            return Ok(c, t);
        }, optionTenant is null ? null : new IdentityScopeOptions(TenantId: optionTenant));

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ConflictingTenantClaims);
        invoked.ShouldBeFalse();
        var record = Records.Single(r => r.Id.Id == 167);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain(RequestIdentityErrorCodes.ConflictingTenantClaims);
        NoSentinelInAnyRecord();
    }

    [Fact]
    public async Task RunAsPrincipalAsync_WithDifferentValuesAcrossTwoConfiguredClaimTypes_IsRefused()
    {
        var principal = TestIdentity.Principal("alice", claims: [new Claim("tenant_id", "t1"), new Claim("tid", "t2")]);

        var result = await Factory.RunAsPrincipalAsync(principal, ScopeTestHost.Capture);

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ConflictingTenantClaims);
        _host.Accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsPrincipalAsync_WithTheSameTenantRepeated_OpensTheScopeWithThatTenant()
    {
        var principal = TestIdentity.Principal("alice", claims: [new Claim("tenant_id", "t1"), new Claim("tid", "t1")]);

        var result = await Factory.RunAsPrincipalAsync(principal, ScopeTestHost.Capture, new IdentityScopeOptions(TenantId: "t1"));

        var context = result.ShouldBeSuccess();
        context.TenantId.ShouldBe("t1");
        context.Issued().UserId.ShouldBe("alice");
    }

    [Fact]
    public async Task RunAsPrincipalAsync_WithConflictingTenantClaims_UnsupportedAccessorWinsOverTheConflict()
    {
        var factory = new RequestContextScopeFactory(
            Substitute.For<IRequestContextAccessor>(), _host.Catalog, _host.IdentityFactory,
            Microsoft.Extensions.Options.Options.Create(new RequestIdentityOptions()), _host.Time, _host.Logger);
        var principal = TestIdentity.Principal("alice", claims: [new Claim("tenant_id", "t1"), new Claim("tid", "t2")]);

        var result = await factory.RunAsPrincipalAsync(principal, Ok);

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.UnsupportedAccessor);
    }

    [Fact]
    public async Task RunAsPrincipalAsync_WithConflictingTenantClaims_ACancelledTokenWinsOverTheConflict()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var principal = TestIdentity.Principal("alice", claims: [new Claim("tenant_id", "t1"), new Claim("tid", "t2")]);

        var result = await Factory.RunAsPrincipalAsync(principal, Ok, cancellationToken: cts.Token);

        ShouldBeLeftWith(result, EncinaErrorCodes.RequestCancelled);
        _host.EventIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsPrincipalAsync_WithConflictingTenantClaims_TheConflictWinsOverTheHolderChainCheck()
    {
        var principal = TestIdentity.Principal("bob", claims: [new Claim("tenant_id", "t1"), new Claim("tid", "t2")]);

        await _host.InUserScope("alice", async _ =>
        {
            ShouldBeLeftWith(await Factory.RunAsPrincipalAsync(principal, Ok), RequestIdentityErrorCodes.ConflictingTenantClaims);
            return 0;
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("t9")]
    public async Task RunAsPrincipalAsync_WithoutATenantClaim_UsesTheOptionTenantOrNone(string? optionTenant)
    {
        var principal = ScopeTestHost.UserPrincipal("alice");

        var result = await Factory.RunAsPrincipalAsync(principal, ScopeTestHost.Capture, new IdentityScopeOptions(TenantId: optionTenant));

        result.ShouldBeSuccess().TenantId.ShouldBe(optionTenant);
    }

    // ── Rule 4: the holder chain ─────────────────────────────────────────

    [Fact]
    public async Task EveryMember_IsRefusedInsideAUserScope()
    {
        await _host.InUserScope("alice", async _ =>
        {
            ShouldBeLeftWith(await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok, new IdentityScopeOptions(AllowOverInbound: true)), RequestIdentityErrorCodes.ScopeConflict);
            ShouldBeLeftWith(await Factory.RunAsPrincipalAsync(TestIdentity.Principal("bob"), Ok), RequestIdentityErrorCodes.ScopeConflict);
            ShouldBeLeftWith(await Factory.RunInboundAsync(new InboundRequestInfo(null), Ok), RequestIdentityErrorCodes.ScopeConflict);
            ShouldBeLeftWith(await Factory.RunAsBuiltInAsync(ScopeTestHost.BuiltIn, Ok), RequestIdentityErrorCodes.ScopeConflict);
            ShouldBeLeftWith(
                await Factory.RunRestoredAsync(new PersistedRequestIdentity(IdentityKind.Anonymous, null, null, "c", null), PersistedIdentitySource.Internal, Ok),
                RequestIdentityErrorCodes.ScopeConflict);
            return 0;
        });
    }

    [Fact]
    public async Task AClearThenRunAsService_InsideAUserRequest_IsStillRefused()
    {
        await _host.InUserScope("alice", async _ =>
        {
            // The setter never clears; and even an anonymous set keeps the facts of the chain.
            Should.Throw<InvalidOperationException>(() => _host.Accessor.RequestContext = null);
            Should.Throw<InvalidOperationException>(() => _host.Accessor.RequestContext = RequestContext.CreateForTest());

            ShouldBeLeftWith(await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok), RequestIdentityErrorCodes.ScopeConflict);
            return 0;
        });
    }

    [Fact]
    public async Task ServiceAndPrincipalScopes_OverAnInboundRequest_AreRefused_UnlessAllowOverInbound()
    {
        await _host.InInboundScope(null, async _ =>
        {
            ShouldBeLeftWith(await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok), RequestIdentityErrorCodes.ScopeConflict);
            ShouldBeLeftWith(await Factory.RunAsPrincipalAsync(TestIdentity.Principal("bob"), Ok), RequestIdentityErrorCodes.ScopeConflict);
            ShouldBeLeftWith(await Factory.RunInboundAsync(new InboundRequestInfo(null), Ok), RequestIdentityErrorCodes.ScopeConflict);
            ShouldBeLeftWith(await Factory.RunAsBuiltInAsync(ScopeTestHost.BuiltIn, Ok), RequestIdentityErrorCodes.ScopeConflict);

            (await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok, new IdentityScopeOptions(AllowOverInbound: true))).ShouldBeSuccess();
            (await Factory.RunAsPrincipalAsync(TestIdentity.Principal("bob"), Ok, new IdentityScopeOptions(AllowOverInbound: true))).ShouldBeSuccess();
            return 0;
        });

        var overInbound = Records.Where(r => r.Id.Id == 174).ToList();
        overInbound.Count.ShouldBe(2);
        overInbound.ShouldAllBe(r => r.Level == LogLevel.Warning);
        overInbound[0].Message.ShouldContain(ScopeTestHost.Job);
        overInbound[0].Message.ShouldContain(nameof(IRequestContextScopeFactory.RunAsServiceAsync));
        Records.ShouldNotContain(r => r.Id.Id == 166 || r.Id.Id == 169);
    }

    [Fact]
    public async Task AFlowForkedFromAnEndedInboundRequest_CannotOpenAServiceScope()
    {
        Task<Either<EncinaError, int>>? forked = null;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await _host.InInboundScope(null, _ =>
        {
            forked = Task.Run(async () =>
            {
                await gate.Task;
                return await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok);
            });
            return Task.FromResult(0);
        });
        gate.SetResult();

        ShouldBeLeftWith(await forked!, RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task ServiceOverService_IsAllowed_AndLogs166AtWarning()
    {
        var result = await Factory.RunAsServiceAsync(ScopeTestHost.Job, async (_, ct) =>
            await Factory.RunAsServiceAsync(ScopeTestHost.OtherJob, ScopeTestHost.Capture, cancellationToken: ct));

        result.ShouldBeSuccess().Issued().UserId.ShouldBe("service:other-job");
        var opened = Records.Where(r => r.Id.Id == 166).ToList();
        opened[0].Level.ShouldBe(LogLevel.Information);
        opened[1].Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task AUserOverAService_IsAllowed_AndLogs169AtWarning()
    {
        var result = await Factory.RunAsServiceAsync(ScopeTestHost.Job, async (_, ct) =>
            await Factory.RunAsPrincipalAsync(TestIdentity.Principal("alice"), ScopeTestHost.Capture, cancellationToken: ct));

        result.ShouldBeSuccess().Issued().UserId.ShouldBe("alice");
        Records.Single(r => r.Id.Id == 169).Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task AnAnonymousPrincipal_OpensAnExplicitAnonymousScope_LoggedAt169Information()
    {
        var result = await Factory.RunAsPrincipalAsync(new ClaimsPrincipal(new ClaimsIdentity()), ScopeTestHost.Capture);

        result.ShouldBeSuccess().Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        Records.Single(r => r.Id.Id == 169).Level.ShouldBe(LogLevel.Information);
    }

    // ── Ending: 168 and 170 ──────────────────────────────────────────────

    [Fact]
    public async Task AScopeThatOutlivesItsEnclosingScope_ReadsAnonymous_AndLogs170()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var innerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Either<EncinaError, string>>? inner = null;

        (await Factory.RunAsServiceAsync(ScopeTestHost.Job, (_, _) =>
        {
            // A forked flow opens a nested scope that outlives the enclosing one.
            inner = Task.Run(() => Factory.RunAsServiceAsync(ScopeTestHost.OtherJob, async (_, _) =>
            {
                innerStarted.SetResult();
                await gate.Task;
                return Right<EncinaError, string>(_host.Accessor.RequestContext?.UserId ?? "(anonymous)");
            }));
            return innerStarted.Task.ContinueWith(_ => Right<EncinaError, int>(0), TaskScheduler.Default);
        })).ShouldBeSuccess();
        gate.SetResult();

        (await inner!).ShouldBeSuccess().ShouldBe("(anonymous)");
        var outlived = Records.Single(r => r.Id.Id == 170);
        outlived.Level.ShouldBe(LogLevel.Warning);
        Records.Count(r => r.Id.Id == 168).ShouldBe(2);
    }

    // ── Tenant and correlation ───────────────────────────────────────────

    [Fact]
    public async Task AServiceScope_TakesItsTenantFromTheOptionsOnly_NeverFromTheAmbient()
    {
        await Task.Yield();
        _host.Accessor.RequestContext = RequestContext.CreateForTest(tenantId: "ambient-tenant", correlationId: "ambient-correlation");

        var withTenant = await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture, new IdentityScopeOptions(TenantId: "job-tenant"));
        var withoutTenant = await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture);

        withTenant.ShouldBeSuccess().TenantId.ShouldBe("job-tenant");
        withoutTenant.ShouldBeSuccess().TenantId.ShouldBeNull();
        withTenant.ShouldBeSuccess().CorrelationId.ShouldBe("ambient-correlation");
        Records.Count(r => r.Id.Id == 173).ShouldBe(2);
        Records.Where(r => r.Id.Id == 173).ShouldAllBe(r => !r.Message.Contains("tenant-") && r.Level == LogLevel.Information);
    }

    [Fact]
    public async Task APrincipalScope_UsesThePrincipalTenant_OrTheOptionTenantWhenThePrincipalHasNone()
    {
        var claimTenant = await Factory.RunAsPrincipalAsync(ScopeTestHost.UserPrincipal("alice", tenant: "t1"), ScopeTestHost.Capture, new IdentityScopeOptions(TenantId: "t1"));
        var optionTenant = await Factory.RunAsPrincipalAsync(ScopeTestHost.UserPrincipal("alice"), ScopeTestHost.Capture, new IdentityScopeOptions(TenantId: "t9"));

        claimTenant.ShouldBeSuccess().TenantId.ShouldBe("t1");
        optionTenant.ShouldBeSuccess().TenantId.ShouldBe("t9");
    }

    [Fact]
    public async Task AScopeWithoutAmbient_TakesTheActivityIdAsCorrelation_AndTheTimestampFromTheTimeProvider()
    {
        using var activity = new Activity("scope-correlation").Start();

        var context = (await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture)).ShouldBeSuccess();

        context.CorrelationId.ShouldBe(activity.Id);
        context.Timestamp.ShouldBe(_host.Time.GetUtcNow());
        context.Metadata.ShouldBeEmpty();
        ((RequestContext)context).Origin.ShouldBe(RequestOrigin.Scope);
    }

    [Fact]
    public async Task OpeningAScope_TagsTheCurrentActivityWithTheIdentityKind()
    {
        using var activity = new Activity("scope-tag").Start();

        (await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok)).ShouldBeSuccess();

        activity.GetTagItem("encina.identity.kind").ShouldBe("service");
    }

    [Fact]
    public async Task AServiceScope_CarriesTheDeclaredAuthority_AndLogs166WithTheNameOnly()
    {
        var context = (await Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture, new IdentityScopeOptions(TenantId: Sentinel))).ShouldBeSuccess();

        context.Issued().Kind.ShouldBe(IdentityKind.Service);
        context.Issued().Roles.ShouldBe(["job"]);
        context.Issued().Permissions.ShouldBe(["jobs:run"]);
        context.Issued().Principal!.IsInRole("job").ShouldBeTrue();
        context.Issued().HasClaim(RequestIdentity.IdentityKindClaimType, "service").ShouldBeTrue();
        var record = Records.Single(r => r.Id.Id == 166);
        record.Message.ShouldContain(ScopeTestHost.Job);
        record.Message.ShouldContain("True");
        NoSentinelInAnyRecord();
    }

    // ── Inbound input: normalized, never refused; 172 and 175 ────────────

    [Fact]
    public async Task RunInboundAsync_MapsThePrincipal_AndLogs175_WhileTheHostTwinLogs172()
    {
        var info = new InboundRequestInfo(ScopeTestHost.UserPrincipal(Sentinel, tenant: Sentinel + "-t"), CorrelationId: "corr-1");

        var application = (await Factory.RunInboundAsync(info, ScopeTestHost.Capture)).ShouldBeSuccess();
        var host = (await Factory.RunHostInboundAsync(info, ScopeTestHost.Capture)).ShouldBeSuccess();

        application.Issued().UserId.ShouldBe(Sentinel);
        application.TenantId.ShouldBe(Sentinel + "-t");
        ((RequestContext)application).Origin.ShouldBe(RequestOrigin.Inbound);
        host.Issued().UserId.ShouldBe(Sentinel);
        Records.Single(r => r.Id.Id == 175).Level.ShouldBe(LogLevel.Information);
        Records.Single(r => r.Id.Id == 172).Level.ShouldBe(LogLevel.Debug);
        NoSentinelInAnyRecord();
    }

    [Fact]
    public async Task RunInboundAsync_UsesTheTenantHeader_OnlyWhenTrusted_AndThePrincipalHasNoTenant()
    {
        var anonymous = (await Factory.RunInboundAsync(
            new InboundRequestInfo(null, TenantHeaderValue: "header-tenant", TenantHeaderTrusted: true), ScopeTestHost.Capture)).ShouldBeSuccess();
        var userWithoutClaim = (await Factory.RunInboundAsync(
            new InboundRequestInfo(ScopeTestHost.UserPrincipal("alice"), TenantHeaderValue: "header-tenant", TenantHeaderTrusted: true), ScopeTestHost.Capture)).ShouldBeSuccess();

        anonymous.TenantId.ShouldBe("header-tenant");
        anonymous.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        userWithoutClaim.TenantId.ShouldBe("header-tenant");
    }

    [Fact]
    public async Task RunInboundAsync_IgnoresAnUntrustedTenantHeader_AndLogsDebug()
    {
        var anonymous = (await Factory.RunInboundAsync(new InboundRequestInfo(null, TenantHeaderValue: "header-tenant"), ScopeTestHost.Capture)).ShouldBeSuccess();
        var noHeader = (await Factory.RunInboundAsync(new InboundRequestInfo(null), ScopeTestHost.Capture)).ShouldBeSuccess();

        anonymous.TenantId.ShouldBeNull();
        noHeader.TenantId.ShouldBeNull();
        Records.Count(r => r.Id.Id == 176).ShouldBe(1);
        Records.Single(r => r.Id.Id == 176).Level.ShouldBe(LogLevel.Debug);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunInboundAsync_ClaimWinsOverAnAgreeingHeader(bool trusted)
    {
        var principal = ScopeTestHost.UserPrincipal("alice", tenant: "claim-tenant");

        var agree = (await Factory.RunInboundAsync(
            new InboundRequestInfo(principal, TenantHeaderValue: "claim-tenant", TenantHeaderTrusted: trusted), ScopeTestHost.Capture)).ShouldBeSuccess();
        var claimOnly = (await Factory.RunInboundAsync(new InboundRequestInfo(principal, TenantHeaderTrusted: trusted), ScopeTestHost.Capture)).ShouldBeSuccess();

        agree.TenantId.ShouldBe("claim-tenant");
        claimOnly.TenantId.ShouldBe("claim-tenant");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task RunInboundAsync_RefusesAHeaderThatDisagreesWithTheClaim(bool trusted, bool host)
    {
        var info = new InboundRequestInfo(
            ScopeTestHost.UserPrincipal("alice", tenant: "claim-tenant"), TenantHeaderValue: "header-tenant", TenantHeaderTrusted: trusted);
        var worked = false;
        Task<Either<EncinaError, int>> Work(IRequestContext _, CancellationToken __)
        {
            worked = true;
            return Task.FromResult(Right<EncinaError, int>(1));
        }

        var result = host ? await Factory.RunHostInboundAsync(info, Work) : await Factory.RunInboundAsync(info, Work);

        worked.ShouldBeFalse();
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.InboundTenantConflict));
        Records.Single(r => r.Id.Id == 167).Level.ShouldBe(LogLevel.Warning);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant-a")]
    public async Task RunInboundAsync_RefusesConflictingTenantClaims_WhateverTheHeader(string? header)
    {
        var first = new ClaimsIdentity([new Claim("sub", "alice"), new Claim("tenant_id", "tenant-a")], "test");
        var second = new ClaimsIdentity([new Claim("sub", "alice"), new Claim("tenant_id", "tenant-b")], "test");
        var info = new InboundRequestInfo(new ClaimsPrincipal([first, second]), TenantHeaderValue: header, TenantHeaderTrusted: true);

        var result = await Factory.RunInboundAsync(info, ScopeTestHost.Capture);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.InboundTenantConflict));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant-a")]
    public async Task RunInboundAsync_RefusesTwoDifferentTenantClaimsInOneIdentity_WhateverTheHeader(string? header)
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", "alice"), new Claim("tenant_id", "tenant-a"), new Claim("tenant_id", "tenant-b")], "test");
        var info = new InboundRequestInfo(new ClaimsPrincipal(identity), TenantHeaderValue: header, TenantHeaderTrusted: true);

        var result = await Factory.RunInboundAsync(info, ScopeTestHost.Capture);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.InboundTenantConflict));
    }

    [Fact]
    public async Task RunInboundAsync_AcceptsTheSameTenantClaimRepeatedInOneIdentity()
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", "alice"), new Claim("tenant_id", "tenant-a"), new Claim("tenant_id", "tenant-a")], "test");
        var info = new InboundRequestInfo(new ClaimsPrincipal(identity), TenantHeaderValue: "tenant-a", TenantHeaderTrusted: true);

        var context = (await Factory.RunInboundAsync(info, ScopeTestHost.Capture)).ShouldBeSuccess();

        context.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public void InboundTenantConflict_HasAFixedMessageWithNoTenant()
    {
        var error = RequestIdentityErrors.InboundTenantConflict();

        error.GetCode().IfNone("none").ShouldBe("encina.identity.inbound_tenant_conflict");
        error.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ConflictingTenantClaims_HasAFixedMessageWithNoTenant()
    {
        var error = RequestIdentityErrors.ConflictingTenantClaims();

        error.GetCode().IfNone("none").ShouldBe("encina.identity.conflicting_tenant_claims");
        error.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RunInboundAsync_NormalizesEveryClientControlledMember_AndStillInvokesWork()
    {
        var info = new InboundRequestInfo(
            null,
            CorrelationId: new string('c', 10_000),
            TenantHeaderValue: new string('t', 129),
            IdempotencyKey: new string('k', 256),
            IpAddress: "not-an-ip",
            UserAgent: "agent\r\nInjected: " + new string('u', 600),
            DataRegion: new string('r', 17));

        var context = (await Factory.RunInboundAsync(info, ScopeTestHost.Capture)).ShouldBeSuccess();

        context.CorrelationId.Length.ShouldBeLessThanOrEqualTo(InboundRequestInfo.MaxIdLength);
        context.TenantId.ShouldBeNull();
        context.IdempotencyKey.ShouldBeNull();
        context.Metadata.ContainsKey(InboundRequestNormalizer.IpAddressKey).ShouldBeFalse();
        context.Metadata.ContainsKey(InboundRequestNormalizer.DataRegionKey).ShouldBeFalse();
        var userAgent = (string)context.Metadata[InboundRequestNormalizer.UserAgentKey]!;
        userAgent.Length.ShouldBe(InboundRequestInfo.MaxUserAgentLength);
        userAgent.ShouldNotContain("\r");
        userAgent.ShouldNotContain("\n");
    }

    [Fact]
    public async Task RunInboundAsync_KeepsUsableValues_IncludingA200CharacterIdempotencyKey()
    {
        var key = new string('k', 200);
        var info = new InboundRequestInfo(null, "corr-ok", "tenant-ok", key, " 10.0.0.1 ", "agent/1.0", "eu", TenantHeaderTrusted: true);

        var context = (await Factory.RunInboundAsync(info, ScopeTestHost.Capture)).ShouldBeSuccess();

        context.CorrelationId.ShouldBe("corr-ok");
        context.TenantId.ShouldBe("tenant-ok");
        context.IdempotencyKey.ShouldBe(key);
        context.Metadata[InboundRequestNormalizer.IpAddressKey].ShouldBe("10.0.0.1");
        context.Metadata[InboundRequestNormalizer.UserAgentKey].ShouldBe("agent/1.0");
        context.Metadata[InboundRequestNormalizer.DataRegionKey].ShouldBe("eu");
    }

    [Fact]
    public async Task RunInboundAsync_WithControlCharactersInIds_DropsOrReplacesThem()
    {
        var info = new InboundRequestInfo(null, CorrelationId: "corr\u0001", TenantHeaderValue: "ten\nant", IdempotencyKey: "key\t1");

        var context = (await Factory.RunInboundAsync(info, ScopeTestHost.Capture)).ShouldBeSuccess();

        context.CorrelationId.ShouldNotContain("\u0001");
        context.TenantId.ShouldBeNull();
        context.IdempotencyKey.ShouldBeNull();
    }

    [Fact]
    public async Task AnOverLongActivityId_IsNotUsedAsCorrelation()
    {
        using var activity = new Activity("long").SetIdFormat(ActivityIdFormat.Hierarchical).SetParentId(new string('p', 300)).Start();

        var context = (await Factory.RunInboundAsync(new InboundRequestInfo(null), ScopeTestHost.Capture)).ShouldBeSuccess();

        activity.Id!.Length.ShouldBeGreaterThan(InboundRequestInfo.MaxIdLength);
        context.CorrelationId.ShouldNotBe(activity.Id);
        context.CorrelationId.Length.ShouldBeLessThanOrEqualTo(InboundRequestInfo.MaxIdLength);
    }

    [Fact]
    public async Task AReservedSubject_IsRefused_NeverOpenedAsAServiceOrAnonymous()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "service:test-job")], "bearer"));

        var result = await Factory.RunInboundAsync(new InboundRequestInfo(principal), ScopeTestHost.Capture);

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ReservedSubjectClaim);
    }

    [Fact]
    public async Task AScopeOpenedDuringADispatch_IsANewUnitOfWork_AndTheDispatchStateComesBack()
    {
        await Task.Yield();
        var dispatch = AmbientRequestContext.Enter(_host.Accessor, RequestContext.CreateForTest());
        AmbientRequestContext.IsDispatchInFlight.ShouldBeTrue();

        var inside = await Factory.RunInboundAsync(new InboundRequestInfo(null, IdempotencyKey: "k1"), (context, _) =>
            Task.FromResult(Right<EncinaError, (bool, string?)>((AmbientRequestContext.IsDispatchInFlight, context.IdempotencyKey))));

        inside.ShouldBeSuccess().ShouldBe((false, "k1"));
        AmbientRequestContext.IsDispatchInFlight.ShouldBeTrue();
        dispatch.Dispose();
    }

    // ── Guards ───────────────────────────────────────────────────────────

    [Fact]
    public async Task NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => Factory.RunAsServiceAsync<int>(null!, Ok));
        await Should.ThrowAsync<ArgumentNullException>(() => Factory.RunAsServiceAsync<int>("x", null!));
        await Should.ThrowAsync<ArgumentNullException>(() => Factory.RunAsPrincipalAsync<int>(null!, Ok));
        await Should.ThrowAsync<ArgumentNullException>(() => Factory.RunInboundAsync<int>(null!, Ok));
        await Should.ThrowAsync<ArgumentNullException>(() => Factory.RunAsBuiltInAsync<int>(null!, Ok));
        await Should.ThrowAsync<ArgumentNullException>(() => Factory.RunRestoredAsync<int>(null!, PersistedIdentitySource.Internal, Ok));
    }

    [Fact]
    public async Task TheTaskOverloads_ReturnUnit_AndPassRefusalsThrough()
    {
        var ran = 0;

        (await Factory.RunAsServiceAsync(ScopeTestHost.Job, (_, _) => { ran++; return Task.CompletedTask; })).ShouldBeSuccess();
        (await Factory.RunAsPrincipalAsync(TestIdentity.Principal("alice"), (_, _) => { ran++; return Task.CompletedTask; })).ShouldBeSuccess();
        (await Factory.RunInboundAsync(new InboundRequestInfo(null), (_, _) => { ran++; return Task.CompletedTask; })).ShouldBeSuccess();
        (await Factory.RunRestoredAsync(
            new PersistedRequestIdentity(IdentityKind.Anonymous, null, null, "c", null), PersistedIdentitySource.Internal, (_, _) => { ran++; return Task.CompletedTask; })).ShouldBeSuccess();
        var refused = await Factory.RunAsServiceAsync("unknown-job", (_, _) => { ran++; return Task.CompletedTask; });

        ran.ShouldBe(4);
        ShouldBeLeftWith(refused, RequestIdentityErrorCodes.UnknownServiceIdentity);
        Should.Throw<ArgumentNullException>(() => RequestContextScopeFactoryExtensions.RunAsServiceAsync(null!, "x", (_, _) => Task.CompletedTask));
        Should.Throw<ArgumentNullException>(() => Factory.RunAsServiceAsync("x", (Func<IRequestContext, CancellationToken, Task>)null!));
    }
}
