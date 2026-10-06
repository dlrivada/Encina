using System.Runtime.CompilerServices;
using System.Security.Claims;
using Encina.Testing;
using Encina.Testing.Identity;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// The boundaries of a scope: forked children, successive restored messages, streams that outlive
/// or start after <c>work</c>, and principals mutated after the identity was built.
/// </summary>
public sealed class ScopeBoundaryTests
{
    public sealed record Items(int Count) : IStreamRequest<string>;

    public sealed class ItemsHandler(IRequestContextAccessor accessor) : IStreamRequestHandler<Items, string>
    {
        public async IAsyncEnumerable<Either<EncinaError, string>> Handle(Items request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < request.Count; i++)
            {
                await Task.Yield();
                yield return accessor.RequestContext?.UserId ?? "(anonymous)";
            }
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddEncinaServiceIdentity(ScopeTestHost.Job);
        services.AddScoped<IStreamRequestHandler<Items, string>, ItemsHandler>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AChildForkedInsideAScope_CannotChangeTheOwnersIdentity()
    {
        var host = new ScopeTestHost();
        var owner = await host.InUserScope("alice", async _ =>
        {
            // The child tries everything: set another user, clear, open a service scope.
            var child = await Task.Run(async () =>
            {
                var set = Record.Exception(() => host.Accessor.RequestContext = TestRequestContext.For(TestIdentity.User("mallory")));
                var clear = Record.Exception(() => host.Accessor.RequestContext = null);
                var service = await host.Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture);
                return (set, clear, service);
            });

            child.set.ShouldBeOfType<InvalidOperationException>();
            child.clear.ShouldBeOfType<InvalidOperationException>();
            child.service.IsLeft.ShouldBeTrue();
            return host.Accessor.RequestContext?.UserId;
        });

        owner.ShouldBe("alice");
    }

    [Fact]
    public async Task NoIdentityLeaksAcrossTwoRestoredMessages()
    {
        var host = new ScopeTestHost();
        var first = new PersistedRequestIdentity(IdentityKind.User, "alice", "t1", "corr-1", null);
        var second = new PersistedRequestIdentity(IdentityKind.Anonymous, null, null, "corr-2", null);

        var one = (await host.Factory.RunRestoredAsync(first, PersistedIdentitySource.Internal, ScopeTestHost.ReadAmbient(host.Accessor))).ShouldBeSuccess();
        var two = (await host.Factory.RunRestoredAsync(second, PersistedIdentitySource.Internal, ScopeTestHost.ReadAmbient(host.Accessor))).ShouldBeSuccess();

        one!.UserId.ShouldBe("alice");
        two.ShouldNotBeNull();
        two.UserId.ShouldBeNull();
        two.TenantId.ShouldBeNull();
        host.Accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task AStreamFirstEnumeratedAfterWork_RunsUnderTheEnumeratingCallersContext()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        IAsyncEnumerable<Either<EncinaError, string>>? stream = null;

        (await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsServiceAsync(ScopeTestHost.Job, (_, ct) =>
        {
            stream = encina.Stream(new Items(2), ct);
            return Task.CompletedTask;
        })).ShouldBeSuccess();

        var seen = new List<string>();
        await foreach (var item in stream!)
        {
            seen.Add(item.ShouldBeSuccess());
        }

        seen.ShouldBe(["(anonymous)", "(anonymous)"]);
    }

    [Fact]
    public async Task AStreamStartedInsideWork_ReadsAnonymous_AfterWorkEnded()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        IAsyncEnumerator<Either<EncinaError, string>>? enumerator = null;
        var seen = new List<string>();

        (await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsServiceAsync(ScopeTestHost.Job, async (_, ct) =>
        {
            enumerator = encina.Stream(new Items(2), ct).GetAsyncEnumerator(ct);
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            seen.Add(enumerator.Current.ShouldBeSuccess());
        })).ShouldBeSuccess();

        (await enumerator!.MoveNextAsync()).ShouldBeTrue();
        seen.Add(enumerator.Current.ShouldBeSuccess());
        await enumerator.DisposeAsync();

        seen.ShouldBe(["service:test-job", "(anonymous)"]);
    }

    [Fact]
    public async Task APrincipalMutatedAfterTheScopeOpened_IsNotObserved()
    {
        var host = new ScopeTestHost();
        var principal = TestIdentity.Principal("alice", roles: ["reader"]);

        async Task<Either<EncinaError, IRequestContext>> MutateThenReturn(IRequestContext scoped, CancellationToken cancellationToken)
        {
            principal.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role, "admin")], "late"));
            ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(ClaimTypes.Role, "root"));
            await Task.Yield();
            return Right<EncinaError, IRequestContext>(scoped);
        }

        var outcome = await host.Factory.RunAsPrincipalAsync(principal, MutateThenReturn);
        var context = outcome.ShouldBeSuccess();

        context.Identity.Roles.ShouldBe(["reader"]);
        context.Identity.Principal!.IsInRole("admin").ShouldBeFalse();
        context.Identity.Principal.IsInRole("root").ShouldBeFalse();
        context.Identity.HasClaim(ClaimTypes.Role, "root").ShouldBeFalse();

        // Each read returns a fresh clone: a reader cannot change what another gate sees.
        context.Identity.Principal.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role, "admin")], "reader-added"));
        context.Identity.Principal.IsInRole("admin").ShouldBeFalse();
    }
}
