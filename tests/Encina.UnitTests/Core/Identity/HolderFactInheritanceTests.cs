using Encina.Testing;
using Encina.Testing.Identity;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Every new holder inherits the facts of the holder current when it is created (PR #1862 review,
/// finding 3): nested dispatch from a dead flow and successive sets never lose a user or inbound fact.
/// </summary>
public sealed class HolderFactInheritanceTests
{
    public sealed record Outer : IRequest<string>;

    public sealed record Inner : IRequest<string>;

    public sealed class OuterHandler(IEncina encina) : IRequestHandler<Outer, string>
    {
        public async Task<Either<EncinaError, string>> Handle(Outer request, CancellationToken cancellationToken)
            => await encina.Send(new Inner(), cancellationToken);
    }

    public sealed class InnerHandler(IRequestContextScopeFactory scopes) : IRequestHandler<Inner, string>
    {
        public async Task<Either<EncinaError, string>> Handle(Inner request, CancellationToken cancellationToken)
        {
            var opened = await scopes.RunAsServiceAsync(ScopeTestHost.Job, (_, _) => Task.FromResult(Right<EncinaError, int>(0)), cancellationToken: cancellationToken);
            return opened.Match(Right: _ => "opened", Left: error => error.GetCode().IfNone("none"));
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddEncinaServiceIdentity(ScopeTestHost.Job);
        services.AddScoped<IRequestHandler<Outer, string>, OuterHandler>();
        services.AddScoped<IRequestHandler<Inner, string>, InnerHandler>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task NestedDispatchFromADeadFlow_CannotOpenAServiceScope()
    {
        await using var provider = BuildProvider();
        var scopes = provider.GetRequiredService<IInternalRequestContextScopeFactory>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Either<EncinaError, string>>? forked = null;

        // A user request forks a task, then ends; the task dispatches r1 whose handler sends r2,
        // whose handler calls RunAsServiceAsync.
        (await scopes.RunHostInboundAsync(new InboundRequestInfo(TestIdentity.Principal("alice"), "corr"), (_, _) =>
        {
            forked = Task.Run(async () =>
            {
                await gate.Task;
                return await provider.GetRequiredService<IEncina>().Send(new Outer());
            });
            return Task.FromResult(Right<EncinaError, int>(0));
        })).ShouldBeSuccess();
        gate.SetResult();

        (await forked!).ShouldBeSuccess().ShouldBe(RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task TwoSuccessiveSetsOverAnEndedInboundChain_KeepTheRefusal()
    {
        var host = new ScopeTestHost();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Either<EncinaError, int>>? forked = null;

        await host.InInboundScope(null, _ =>
        {
            forked = Task.Run(async () =>
            {
                await gate.Task;
                host.Accessor.RequestContext = RequestContext.CreateForTest(correlationId: "first");
                host.Accessor.RequestContext = host.Accessor.RequestContext!.WithMetadata("k", "second");
                return await host.Factory.RunAsServiceAsync(ScopeTestHost.Job, (_, _) => Task.FromResult(Right<EncinaError, int>(1)));
            });
            return Task.FromResult(0);
        });
        gate.SetResult();

        (await forked!).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ASetOverALiveNonScopeHolderWithNoParent_KeepsThatHoldersFacts()
    {
        await Task.Yield();

        // A non-scope holder with an inbound context and no parent (as the dispatcher installs one).
        var inbound = ((RequestContext)RequestContext.CreateForTest()).WithOrigin(RequestOrigin.Inbound);
        RequestContextAccessor.SetUnchecked(inbound);
        RequestContextAccessor.Current!.Parent.ShouldBeNull();

        RequestContextAccessor.SetUnchecked(RequestContext.CreateForTest());

        RequestContextAccessor.CurrentFacts.HasFlag(ChainFacts.Inbound).ShouldBeTrue();
        var host = new ScopeTestHost();
        (await host.Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task FactsSurviveInvalidate()
    {
        await Task.Yield();
        var holder = RequestContextAccessor.Push(TestRequestContext.For(TestIdentity.User("alice")));

        RequestContextAccessor.End(holder);

        holder.Kind.ShouldBe(IdentityKind.User);
        holder.Facts.ShouldBe(ChainFacts.User);
        RequestContextAccessor.CurrentFacts.ShouldBe(ChainFacts.User);
    }
}
