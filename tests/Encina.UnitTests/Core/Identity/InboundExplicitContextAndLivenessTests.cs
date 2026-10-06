using System.Runtime.CompilerServices;
using System.Security.Claims;
using Encina.Testing;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// #1892: (1) an explicit context with a different authenticated identity is refused over a chain
/// with an inbound fact (anonymous inbound request, connection marker, External restore), like
/// <c>RunAsServiceAsync</c> without <c>AllowOverInbound</c>; (2) <see cref="RequestContext.Identity"/>
/// reads Anonymous once its issuing scope has ended, so gates that read their <c>context</c>
/// parameter after the scope (a captured stream context, a fire-and-forget dispatch) deny.
/// </summary>
public sealed class InboundExplicitContextAndLivenessTests
{
    private const string Denied = "test.denied";
    private const string Sentinel = "sentinel-1892";
    private const string Anonymous = "(anonymous)";

    /// <summary>A request whose handler counts its invocations and returns the ambient user id.</summary>
    public sealed record Counted : IRequest<string?>;

    /// <summary>A request whose gate waits for <c>Hold</c> before it reads its context parameter.</summary>
    public sealed record Gated(Task Hold) : IRequest<string?>;

    /// <summary>A stream of <c>Count</c> items behind a gate that reads the captured context per item.</summary>
    public sealed record Items(int Count) : IStreamRequest<string>;

    public sealed class Invocations
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Add() => Interlocked.Increment(ref _count);
    }

    public sealed class CountedHandler(IRequestContextAccessor accessor, Invocations invocations) : IRequestHandler<Counted, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(Counted request, CancellationToken cancellationToken)
        {
            invocations.Add();
            return Task.FromResult<Either<EncinaError, string?>>(accessor.RequestContext?.UserId ?? Anonymous);
        }
    }

    public sealed class GatedHandler(Invocations invocations) : IRequestHandler<Gated, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(Gated request, CancellationToken cancellationToken)
        {
            invocations.Add();
            return Task.FromResult<Either<EncinaError, string?>>("handled");
        }
    }

    /// <summary>A notification whose handler counts its invocations.</summary>
    public sealed record Ping : INotification;

    public sealed class PingHandler(Invocations invocations) : INotificationHandler<Ping>
    {
        public Task<Either<EncinaError, Unit>> Handle(Ping notification, CancellationToken cancellationToken)
        {
            invocations.Add();
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }
    }

    public sealed class ItemsHandler(Invocations invocations) : IStreamRequestHandler<Items, string>
    {
        public async IAsyncEnumerable<Either<EncinaError, string>> Handle(Items request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            invocations.Add();
            for (var i = 0; i < request.Count; i++)
            {
                await Task.Yield();
                yield return $"item-{i}";
            }
        }
    }

    /// <summary>A gate in the style of the authorization behaviors: it reads its context parameter.</summary>
    public sealed class AuthenticatedGate : IPipelineBehavior<Gated, string?>
    {
        public async ValueTask<Either<EncinaError, string?>> Handle(
            Gated request, IRequestContext context, RequestHandlerCallback<string?> nextStep, CancellationToken cancellationToken)
        {
            await request.Hold;
            return context.Identity is { IsAuthenticated: true }
                ? await nextStep()
                : Left<EncinaError, string?>(EncinaErrors.Create(Denied, "denied"));
        }
    }

    /// <summary>A stream gate that keeps its context parameter for the whole enumeration.</summary>
    public sealed class AuthenticatedStreamGate : IStreamPipelineBehavior<Items, string>
    {
        public async IAsyncEnumerable<Either<EncinaError, string>> Handle(
            Items request, IRequestContext context, StreamHandlerCallback<string> nextStep, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return context.Identity is { IsAuthenticated: true }
                    ? item
                    : Left<EncinaError, string>(EncinaErrors.Create(Denied, "denied"));
            }
        }
    }

    private readonly FakeLogger<global::Encina.Encina> _logger = new();
    private readonly Invocations _invocations = new();

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddEncinaServiceIdentity(ScopeTestHost.Job, id => id.WithRoles("job"));
        services.AddSingleton<ILogger<global::Encina.Encina>>(_logger);
        services.AddSingleton(_invocations);
        services.AddScoped<IRequestHandler<Counted, string?>, CountedHandler>();
        services.AddScoped<IRequestHandler<Gated, string?>, GatedHandler>();
        services.AddScoped<IStreamRequestHandler<Items, string>, ItemsHandler>();
        services.AddScoped<INotificationHandler<Ping>, PingHandler>();
        services.AddScoped<IPipelineBehavior<Gated, string?>, AuthenticatedGate>();
        services.AddScoped<IStreamPipelineBehavior<Items, string>, AuthenticatedStreamGate>();
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal Principal(string id) => TestIdentity.Principal(id);

    /// <summary>
    /// Opens a user scope in another flow and keeps it live until the returned release is called, so
    /// its context is issued and live while the caller's flow does not carry it.
    /// </summary>
    private static async Task<(IRequestContext Context, Func<Task> Release)> LiveScopeElsewhere(ServiceProvider provider, ClaimsPrincipal principal)
    {
        var captured = new TaskCompletionSource<IRequestContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopes = provider.GetRequiredService<IRequestContextScopeFactory>();
        var running = Task.Run(() => scopes.RunAsPrincipalAsync(principal, async (context, _) =>
        {
            captured.SetResult(context);
            await gate.Task;
            return Right<EncinaError, Unit>(unit);
        }));

        var context = await captured.Task;

        async Task Release()
        {
            gate.TrySetResult();
            (await running).ShouldBeSuccess();
        }

        return (context, Release);
    }

    private static void ShouldBeLeftWith<T>(Either<EncinaError, T> result, string code)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(code));
    }

    private void Expect167WithTheCodeOnly()
    {
        var record = _logger.Collector.GetSnapshot().Last(r => r.Id.Id == 167);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain(RequestIdentityErrorCodes.ScopeConflict);
        record.Message.ShouldNotContain(Sentinel);
    }

    // ── Decision 1: refused over an inbound fact ────────────────────────

    [Fact]
    public async Task Send_AnotherLiveIdentity_OverAnAnonymousInboundRequest_IsRefused_WithoutInvokingWork_AndLogs167()
    {
        await using var provider = BuildProvider();
        var (other, release) = await LiveScopeElsewhere(provider, Principal(Sentinel));

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunInboundAsync(
            new InboundRequestInfo(null),
            async (_, ct) => await provider.GetRequiredService<IEncina>().Send(new Counted(), other, ct));

        ShouldBeLeftWith(outcome, RequestIdentityErrorCodes.ScopeConflict);
        _invocations.Count.ShouldBe(0);
        Expect167WithTheCodeOnly();
        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
        await release();
    }

    [Fact]
    public async Task Send_AnotherLiveIdentity_OverAConnectionMarker_IsRefused_WithoutInvokingWork()
    {
        await using var provider = BuildProvider();
        var (other, release) = await LiveScopeElsewhere(provider, Principal("mallory"));
        Either<EncinaError, string?> result = default;

        var marker = await provider.GetRequiredService<IInternalRequestContextScopeFactory>().RunAnonymousMarkerAsync(
            AnonymousMarker.Connection,
            async ct => result = await provider.GetRequiredService<IEncina>().Send(new Counted(), other, ct));

        marker.ShouldBeSuccess();
        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ScopeConflict);
        _invocations.Count.ShouldBe(0);
        Expect167WithTheCodeOnly();
        await release();
    }

    [Fact]
    public async Task Send_AnotherLiveIdentity_OverAnExternalRestore_IsRefused_WithoutInvokingWork()
    {
        await using var provider = BuildProvider();
        var (other, release) = await LiveScopeElsewhere(provider, Principal("mallory"));
        var row = new PersistedRequestIdentity(IdentityKind.User, "alice", null, "corr-1", null);

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunRestoredAsync(
            row,
            PersistedIdentitySource.External,
            async (_, ct) => await provider.GetRequiredService<IEncina>().Send(new Counted(), other, ct));

        ShouldBeLeftWith(outcome, RequestIdentityErrorCodes.ScopeConflict);
        _invocations.Count.ShouldBe(0);
        Expect167WithTheCodeOnly();
        await release();
    }

    [Fact]
    public async Task Publish_WithAnotherLiveIdentity_OverAnInboundRequest_IsRefused_WithoutInvokingHandlers()
    {
        await using var provider = BuildProvider();
        var (other, release) = await LiveScopeElsewhere(provider, Principal("mallory"));

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunInboundAsync(
            new InboundRequestInfo(null),
            async (_, ct) => await provider.GetRequiredService<IEncina>().Publish(new Ping(), other, ct));

        ShouldBeLeftWith(outcome, RequestIdentityErrorCodes.ScopeConflict);
        _invocations.Count.ShouldBe(0);
        Expect167WithTheCodeOnly();
        await release();
    }

    [Fact]
    public async Task Stream_WithAnotherLiveIdentity_OverAnInboundRequest_IsRefused_WithoutInvokingTheHandler()
    {
        await using var provider = BuildProvider();
        var (other, release) = await LiveScopeElsewhere(provider, Principal("mallory"));
        var items = new List<Either<EncinaError, string>>();

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunInboundAsync(
            new InboundRequestInfo(null),
            async (_, ct) =>
            {
                await foreach (var item in provider.GetRequiredService<IEncina>().Stream(new Items(1), other, ct))
                {
                    items.Add(item);
                }

                return Right<EncinaError, Unit>(unit);
            });

        outcome.ShouldBeSuccess();
        var refused = items.ShouldHaveSingleItem();
        ShouldBeLeftWith(refused, RequestIdentityErrorCodes.ScopeConflict);
        _invocations.Count.ShouldBe(0);
        await release();
    }

    [Fact]
    public async Task Send_AnAnonymousExplicitContext_OverAnInboundRequest_IsAccepted()
    {
        await using var provider = BuildProvider();

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunInboundAsync(
            new InboundRequestInfo(null),
            async (_, ct) => await provider.GetRequiredService<IEncina>().Send(new Counted(), RequestContext.CreateForTest(), ct));

        outcome.ShouldBeSuccess().ShouldBe(Anonymous);
        _invocations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Send_TheSameIdentity_ThroughRunAsServiceAsyncWithAllowOverInbound_IsAccepted()
    {
        await using var provider = BuildProvider();
        var scopes = provider.GetRequiredService<IRequestContextScopeFactory>();

        var outcome = await scopes.RunInboundAsync(
            new InboundRequestInfo(null),
            (_, ct) => scopes.RunAsServiceAsync(
                ScopeTestHost.Job,
                async (service, innerCt) => await provider.GetRequiredService<IEncina>().Send(new Counted(), service, innerCt),
                new IdentityScopeOptions(AllowOverInbound: true),
                ct));

        outcome.ShouldBeSuccess().ShouldBe("service:test-job");
        _invocations.Count.ShouldBe(1);
        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165 || r.Id.Id == 167);
    }

    [Fact]
    public async Task Resolve_WithASubstitutedAccessor_ReadsTheInboundFactFromTheAmbientOrigin()
    {
        await using var provider = BuildProvider();
        var (other, release) = await LiveScopeElsewhere(provider, Principal("mallory"));
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(((RequestContext)RequestContext.CreateForTest()).WithOrigin(RequestOrigin.Inbound));

        var result = AmbientRequestContext.Resolve(accessor, other, TimeProvider.System, NullLogger.Instance);

        ShouldBeLeftWith(result, RequestIdentityErrorCodes.ScopeConflict);
        await release();
    }

    // ── Decision 2: centralised liveness ────────────────────────────────

    [Fact]
    public async Task AStreamGateThatCapturedItsContext_ReadsAnonymous_AfterTheScopeEnds_AndDenies()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        IAsyncEnumerator<Either<EncinaError, string>>? enumerator = null;
        Either<EncinaError, string> first = default;

        (await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsServiceAsync(ScopeTestHost.Job, async (_, ct) =>
        {
            enumerator = encina.Stream(new Items(2), ct).GetAsyncEnumerator(ct);
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            first = enumerator.Current;
        })).ShouldBeSuccess();

        (await enumerator!.MoveNextAsync()).ShouldBeTrue();
        var second = enumerator.Current;
        await enumerator.DisposeAsync();

        first.ShouldBeSuccess().ShouldBe("item-0");
        ShouldBeLeftWith(second, Denied);
    }

    [Fact]
    public async Task AFireAndForgetSend_ResolvedInsideTheScope_WhoseGateRunsAfterTheScopeEnded_Denies()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Either<EncinaError, string?>>? pending = null;

        (await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsServiceAsync(ScopeTestHost.Job, (_, ct) =>
        {
            // Not awaited: the context is resolved now, the gate reads it once hold is released.
            pending = encina.Send(new Gated(hold.Task), ct).AsTask();
            return Task.CompletedTask;
        })).ShouldBeSuccess();

        hold.SetResult();
        var result = await pending!;

        ShouldBeLeftWith(result, Denied);
        _invocations.Count.ShouldBe(0);
    }

    [Fact]
    public async Task AContextReadInsideTheScope_IsUnchanged_AndItsGatePasses()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsServiceAsync(ScopeTestHost.Job, async (context, ct) =>
        {
            var issued = ((RequestContext)context).IssuedIdentity;
            context.Identity.ShouldBeSameAs(issued);
            context.Identity.Kind.ShouldBe(IdentityKind.Service);
            context.WithTenantId("t1").Identity.ShouldBeSameAs(issued);
            return await encina.Send(new Gated(Task.CompletedTask), ct);
        });

        outcome.ShouldBeSuccess().ShouldBe("handled");
        _invocations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CopiesOfAnEndedContext_NeverResurrectItsIdentity()
    {
        var host = new ScopeTestHost();
        var context = (await host.Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture)).ShouldBeSuccess();

        context.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        context.UserId.ShouldBeNull();
        context.WithMetadata("k", "v").Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        context.WithTenantId("t").Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        RequestContext.ForNestedDispatch(context, TimeProvider.System.GetUtcNow()).Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        RequestContext.CopyOf(context).Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        context.ToString()!.ShouldContain("IdentityKind = Anonymous");
    }

    [Fact]
    public async Task TheSetter_RefusesAContextWhoseScopeHasEnded()
    {
        var host = new ScopeTestHost();
        var ended = (await host.Factory.RunAsPrincipalAsync(ScopeTestHost.UserPrincipal("alice"), ScopeTestHost.Capture)).ShouldBeSuccess();

        await host.InUserScope("alice", context =>
        {
            Should.Throw<InvalidOperationException>(() => host.Accessor.RequestContext = ended);
            host.Accessor.RequestContext.ShouldBeSameAs(context);
            return Task.FromResult(0);
        });
    }

    [Fact]
    public async Task APushedScope_KeepsItsUserFact_EvenThoughItsIssuerIsBoundAfterThePush()
    {
        var host = new ScopeTestHost();

        var facts = await host.InUserScope("alice", _ => Task.FromResult(RequestContextAccessor.CurrentFacts));

        facts.HasFlag(ChainFacts.User).ShouldBeTrue();
    }
}
