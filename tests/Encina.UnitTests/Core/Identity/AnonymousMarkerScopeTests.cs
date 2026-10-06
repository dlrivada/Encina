using Encina.Testing;
using Encina.Testing.Identity;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for <see cref="IInternalRequestContextScopeFactory.RunAnonymousMarkerAsync"/> (#1705,
/// Phase 3 task 0): the connection marker of long-lived connections and the masking scope of the
/// Blazor circuit handler, and the holder-chain rules over them (MQ-2).
/// </summary>
public sealed class AnonymousMarkerScopeTests
{
    private readonly ScopeTestHost _host = new();

    private RequestContextScopeFactory Factory => _host.Factory;

    private static Task<Either<EncinaError, int>> Ok(IRequestContext context, CancellationToken cancellationToken) =>
        Task.FromResult(Right<EncinaError, int>(1));

    private static void ShouldBeLeftWith<T>(Either<EncinaError, T> result, string code)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(code));
    }

    private static PersistedRequestIdentity AnonymousRow() => new(IdentityKind.Anonymous, null, null, "restored-correlation", null);

    private async Task<T> UnderMarker<T>(AnonymousMarker marker, Func<Task<T>> inside)
    {
        var value = default(T);
        var result = await Factory.RunAnonymousMarkerAsync(marker, async _ => value = await inside());
        result.IsRight.ShouldBeTrue();
        return value!;
    }

    // ── The connection marker ─────────────────────────────────────────────

    [Fact]
    public async Task TheConnectionMarker_ReadsAnonymous_WithTheConnectionOrigin()
    {
        var context = await UnderMarker(AnonymousMarker.Connection, () => Task.FromResult(_host.Accessor.RequestContext));

        context.ShouldNotBeNull();
        context.Identity.Kind.ShouldBe(IdentityKind.Anonymous);
        context.UserId.ShouldBeNull();
        ((RequestContext)context).Origin.ShouldBe(RequestOrigin.Connection);
        context.CorrelationId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TheConnectionMarker_RecordsTheConnectionFact_AndEndsWithItsWork()
    {
        IRequestContext? captured = null;
        var facts = await UnderMarker(AnonymousMarker.Connection, () =>
        {
            captured = _host.Accessor.RequestContext;
            return Task.FromResult(RequestContextAccessor.CurrentFacts);
        });

        facts.ShouldBe(ChainFacts.Connection);
        captured.ShouldNotBeNull();
        _host.Accessor.RequestContext.ShouldBeNull();
        RequestContextAccessor.CurrentFacts.ShouldBe(ChainFacts.None);
        _host.EventIds.ShouldContain(168);
    }

    [Fact]
    public async Task OverTheConnectionMarker_AServiceScope_IsRefusedWithoutTheOptIn()
    {
        var result = await UnderMarker(AnonymousMarker.Connection, () => Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok));

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ScopeConflict);
        _host.EventIds.ShouldContain(167);
    }

    [Fact]
    public async Task OverTheConnectionMarker_AServiceScope_OpensWithTheOptIn_AndLogs174()
    {
        var result = await UnderMarker(AnonymousMarker.Connection, () => Factory.RunAsServiceAsync(
            ScopeTestHost.Job, ScopeTestHost.Capture, new IdentityScopeOptions(AllowOverInbound: true)));

        result.ShouldBeSuccess().UserId.ShouldBe("service:test-job");
        _host.EventIds.ShouldContain(174);
        _host.EventIds.ShouldNotContain(166);
    }

    [Fact]
    public async Task OverTheConnectionMarker_APrincipalScope_IsRefusedWithoutTheOptIn()
    {
        var result = await UnderMarker(AnonymousMarker.Connection, () => Factory.RunAsPrincipalAsync(TestIdentity.Principal("alice"), Ok));

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task OverTheConnectionMarker_RestoredAndBuiltInScopes_AreRefused()
    {
        var restored = await UnderMarker(AnonymousMarker.Connection, () => Factory.RunRestoredAsync(AnonymousRow(), PersistedIdentitySource.Internal, Ok));
        var builtIn = await UnderMarker(AnonymousMarker.Connection, () => Factory.RunAsBuiltInAsync(ScopeTestHost.BuiltIn, Ok));

        ShouldBeLeftWith(restored, RequestIdentityErrorCodes.ScopeConflict);
        ShouldBeLeftWith(builtIn, RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task OverTheConnectionMarker_TheHostInboundScope_IsPermitted_AndReadsThePrincipal()
    {
        var result = await UnderMarker(AnonymousMarker.Connection, () => Factory.RunHostInboundAsync(
            InboundRequestInfo.ForCircuit(TestIdentity.Principal("alice"), "circuit-1"), ScopeTestHost.Capture));

        var context = result.ShouldBeSuccess();
        context.UserId.ShouldBe("alice");
        ((RequestContext)context).Origin.ShouldBe(RequestOrigin.Inbound);
        _host.EventIds.ShouldContain(172);
    }

    [Fact]
    public async Task OverTheConnectionMarker_TheApplicationInboundScope_IsPermitted_AndLogs175()
    {
        var result = await UnderMarker(AnonymousMarker.Connection, () => Factory.RunInboundAsync(
            new InboundRequestInfo(TestIdentity.Principal("alice")), ScopeTestHost.Capture));

        result.ShouldBeSuccess().UserId.ShouldBe("alice");
        _host.EventIds.ShouldContain(175);
        _host.EventIds.ShouldNotContain(172);
    }

    [Fact]
    public async Task InsideAnInboundScopeOverTheConnectionMarker_AServiceScope_AndASecondInboundScope_AreRefused()
    {
        var results = await UnderMarker(AnonymousMarker.Connection, async () =>
        {
            var inner = await Factory.RunHostInboundAsync(
                InboundRequestInfo.ForCircuit(null, "circuit-1"),
                async (_, ct) =>
                {
                    var service = await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok, cancellationToken: ct);
                    var inbound = await Factory.RunInboundAsync(new InboundRequestInfo(null), Ok, ct);
                    return Right<EncinaError, (Either<EncinaError, int>, Either<EncinaError, int>)>((service, inbound));
                });
            return inner.ShouldBeSuccess();
        });

        ShouldBeLeftWith(results.Item1, RequestIdentityErrorCodes.ScopeConflict);
        ShouldBeLeftWith(results.Item2, RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task TheConnectionMarker_InsideAnInboundRequest_KeepsTheInboundFact()
    {
        var result = await _host.InInboundScope(null, _ => UnderMarker(AnonymousMarker.Connection, () => Factory.RunHostInboundAsync(
            new InboundRequestInfo(TestIdentity.Principal("alice")), Ok)));

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ScopeConflict);
    }

    // ── The masking scope ─────────────────────────────────────────────────

    [Fact]
    public async Task TheMask_OverAUserScope_ReadsAnonymous_AndStillRefusesEveryScope()
    {
        var (context, service) = await _host.InUserScope("alice", _ => UnderMarker(AnonymousMarker.Mask, async () =>
            (_host.Accessor.RequestContext, await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok, new IdentityScopeOptions(AllowOverInbound: true)))));

        context.ShouldNotBeNull();
        context.Identity.Kind.ShouldBe(IdentityKind.Anonymous);
        ShouldBeLeftWith(service, RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task TheMask_OverAnInboundScope_ReadsAnonymous_AndKeepsTheInboundRefusals()
    {
        var (context, service, inbound) = await _host.InInboundScope(TestIdentity.Principal("alice"), _ => UnderMarker(AnonymousMarker.Mask, async () =>
            (Context: _host.Accessor.RequestContext,
             Service: await Factory.RunAsServiceAsync(ScopeTestHost.Job, Ok),
             Inbound: await Factory.RunHostInboundAsync(new InboundRequestInfo(null), Ok))));

        context.ShouldNotBeNull();
        context.UserId.ShouldBeNull();
        ((RequestContext)context).Origin.ShouldBe(RequestOrigin.Scope);
        ShouldBeLeftWith(service, RequestIdentityErrorCodes.ScopeConflict);
        ShouldBeLeftWith(inbound, RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task TheMask_OverNothing_RecordsNoFact()
    {
        var facts = await UnderMarker(AnonymousMarker.Mask, () => Task.FromResult(RequestContextAccessor.CurrentFacts));

        facts.ShouldBe(ChainFacts.None);
    }

    // ── Refusals and results ──────────────────────────────────────────────

    [Fact]
    public async Task ACancelledToken_ReturnsRequestCancelled_WithoutInvokingWork_OrLogging167()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var invoked = false;

        var result = await Factory.RunAnonymousMarkerAsync(AnonymousMarker.Connection, _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        }, cts.Token);

        ShouldBeLeftWith(result, EncinaErrorCodes.RequestCancelled);
        invoked.ShouldBeFalse();
        _host.EventIds.ShouldNotContain(167);
    }

    [Fact]
    public async Task AnUnsupportedAccessor_RefusesTheMarker_WithoutInvokingWork()
    {
        var factory = new RequestContextScopeFactory(
            Substitute.For<IRequestContextAccessor>(), _host.Catalog, _host.IdentityFactory,
            Microsoft.Extensions.Options.Options.Create(new RequestIdentityOptions()), _host.Time, _host.Logger);
        var invoked = false;

        var result = await factory.RunAnonymousMarkerAsync(AnonymousMarker.Mask, _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.UnsupportedAccessor);
        invoked.ShouldBeFalse();
        _host.EventIds.ShouldContain(167);
    }

    [Fact]
    public async Task AnExceptionFromWork_Propagates_AfterTheMarkerEnded()
    {
        IRequestContext? captured = null;

        await Should.ThrowAsync<InvalidOperationException>(() => Factory.RunAnonymousMarkerAsync(AnonymousMarker.Connection, _ =>
        {
            captured = _host.Accessor.RequestContext;
            throw new InvalidOperationException("boom");
        }));

        captured.ShouldNotBeNull();
        _host.Accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task TheWork_ReceivesTheCallersToken()
    {
        using var cts = new CancellationTokenSource();
        var received = CancellationToken.None;

        await Factory.RunAnonymousMarkerAsync(AnonymousMarker.Connection, token =>
        {
            received = token;
            return Task.CompletedTask;
        }, cts.Token);

        received.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task AnUndefinedMarker_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => Factory.RunAnonymousMarkerAsync((AnonymousMarker)99, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task AFlowForkedInsideTheConnectionMarker_ReadsNoContext_AfterItEnded()
    {
        Task<IRequestContext?>? forked = null;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await Factory.RunAnonymousMarkerAsync(AnonymousMarker.Connection, _ =>
        {
            forked = Task.Run(async () =>
            {
                await release.Task;
                return _host.Accessor.RequestContext;
            });
            return Task.CompletedTask;
        });
        release.SetResult();

        (await forked!).ShouldBeNull();
    }
}
