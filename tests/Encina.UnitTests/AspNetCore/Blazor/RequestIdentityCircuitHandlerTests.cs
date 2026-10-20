using System.Security.Claims;
using Encina.AspNetCore.Blazor;
using Encina.Testing.Identity;
using Encina.UnitTests.Core.Identity;
using LanguageExt;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore.Blazor;

/// <summary>
/// Unit tests for <see cref="RequestIdentityCircuitHandler"/> (#1705, Phase 3 task 4): one inbound
/// scope per circuit activity from the current authentication state, over the connection marker the
/// circuit's connection runs under; the masking scope when the inbound scope is refused.
/// </summary>
public sealed class RequestIdentityCircuitHandlerTests
{
    private readonly ScopeTestHost _host = new();
    private readonly MutableAuthenticationStateProvider _authentication = new();

    private RequestIdentityCircuitHandler Handler() => new(_authentication, _host.Factory);

    // An activity of the circuit: what the component code reads while it runs.
    private async Task<IRequestContext?> RunActivity(RequestIdentityCircuitHandler handler, Func<Task>? inside = null)
    {
        IRequestContext? seen = null;
        var activityHandler = handler.CreateInboundActivityHandler(async _ =>
        {
            seen = _host.Accessor.RequestContext;
            if (inside is not null)
            {
                await inside();
            }
        });
        await activityHandler(null!);
        return seen;
    }

    // The circuit's flow: the connection marker opened by UseEncinaContext for /_blazor.
    private async Task<T> InCircuit<T>(Func<Task<T>> inside)
    {
        var value = default(T)!;
        var result = await _host.Factory.RunAnonymousMarkerAsync(AnonymousMarker.Connection, async _ => value = await inside());
        result.IsRight.ShouldBeTrue();
        return value;
    }

    [Fact]
    public async Task InsideAnActivity_TheCircuitUserIsAmbient_WithTheInboundOrigin()
    {
        _authentication.Set(TestIdentity.Principal("circuit-user", ["Admin"]));
        var handler = Handler();

        var seen = await InCircuit(() => RunActivity(handler));

        seen.ShouldNotBeNull();
        seen.Issued().UserId.ShouldBe("circuit-user");
        seen.Issued().Roles.ShouldContain("Admin");
        ((RequestContext)seen).Origin.ShouldBe(RequestOrigin.Inbound);
        _host.EventIds.ShouldContain(172);
        _host.EventIds.ShouldNotContain(167);
    }

    [Fact]
    public async Task OutsideAnActivity_TheCircuitFlowReadsAnonymous()
    {
        _authentication.Set(TestIdentity.Principal("circuit-user"));
        var handler = Handler();

        var (inside, outside) = await InCircuit(async () =>
        {
            var during = await RunActivity(handler);
            return (during, _host.Accessor.RequestContext);
        });

        inside.Issued().UserId.ShouldBe("circuit-user");
        outside.ShouldNotBeNull();
        outside.Identity.Kind.ShouldBe(IdentityKind.Anonymous);
    }

    [Fact]
    public async Task AChangedAuthenticationState_AppliesAtTheNextActivity()
    {
        _authentication.Set(TestIdentity.Principal("before"));
        var handler = Handler();

        var (first, second) = await InCircuit(async () =>
        {
            var one = await RunActivity(handler);
            _authentication.Set(TestIdentity.Principal("after", ["Admin"]));
            var two = await RunActivity(handler);
            return (one, two);
        });

        first.Issued().UserId.ShouldBe("before");
        second.ShouldNotBeNull();
        second.Issued().UserId.ShouldBe("after");
        second.Issued().Roles.ShouldContain("Admin");
    }

    [Fact]
    public async Task ASignedOutCircuit_RunsItsActivitiesAnonymous()
    {
        _authentication.Set(new ClaimsPrincipal(new ClaimsIdentity()));
        var handler = Handler();

        var seen = await InCircuit(() => RunActivity(handler));

        seen!.Identity.Kind.ShouldBe(IdentityKind.Anonymous);
        ((RequestContext)seen).Origin.ShouldBe(RequestOrigin.Inbound);
    }

    [Fact]
    public async Task InsideAnActivity_AServiceScopeNeedsTheOptIn()
    {
        _authentication.Set(new ClaimsPrincipal(new ClaimsIdentity()));
        var handler = Handler();
        Either<EncinaError, int> service = default;

        await InCircuit(async () => await RunActivity(handler, async () =>
            service = await _host.Factory.RunAsServiceAsync(ScopeTestHost.Job, (_, _) => Task.FromResult(Right<EncinaError, int>(1)))));

        service.IsLeft.ShouldBeTrue();
        service.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.ScopeConflict));
    }

    [Fact]
    public async Task WhenTheInboundScopeIsRefused_TheActivityRunsAnonymousUnderTheMask()
    {
        // The circuit flow carries an inbound user context (the first long poll of a misordered
        // pipeline): the per-activity inbound scope is refused, the activity still runs, masked.
        _authentication.Set(TestIdentity.Principal("circuit-user"));
        var handler = Handler();
        Either<EncinaError, int> service = default;

        var seen = await _host.InInboundScope(TestIdentity.Principal("connect-time-user"), _ => RunActivity(handler, async () =>
            service = await _host.Factory.RunAsServiceAsync(
                ScopeTestHost.Job,
                (_, _) => Task.FromResult(Right<EncinaError, int>(1)),
                new IdentityScopeOptions(AllowOverInbound: true))));

        seen.ShouldNotBeNull();
        seen.Identity.Kind.ShouldBe(IdentityKind.Anonymous);
        ((RequestContext)seen).Origin.ShouldBe(RequestOrigin.Scope);
        _host.EventIds.ShouldContain(167);
        service.IsLeft.ShouldBeTrue("the mask keeps the user fact of the chain it hides");
    }

    [Fact]
    public async Task AnExceptionFromTheActivity_Propagates()
    {
        _authentication.Set(TestIdentity.Principal("circuit-user"));
        var handler = Handler();

        await Should.ThrowAsync<InvalidOperationException>(() => InCircuit(() =>
            RunActivity(handler, () => throw new InvalidOperationException("component failure"))));
    }

    [Fact]
    public async Task AnUnsupportedAccessor_FailsTheActivityLoudly()
    {
        var factory = new RequestContextScopeFactory(
            Substitute.For<IRequestContextAccessor>(), _host.Catalog, _host.IdentityFactory,
            Microsoft.Extensions.Options.Options.Create(new RequestIdentityOptions()), _host.Time, _host.Logger);
        _authentication.Set(TestIdentity.Principal("circuit-user"));
        var handler = new RequestIdentityCircuitHandler(_authentication, factory);
        var ran = false;

        var activity = handler.CreateInboundActivityHandler(_ =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        await Should.ThrowAsync<InvalidOperationException>(() => activity(null!));
        ran.ShouldBeFalse();
    }

    private sealed class MutableAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _user = new(new ClaimsIdentity());

        public void Set(ClaimsPrincipal user) => _user = user;

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(_user));
    }
}
