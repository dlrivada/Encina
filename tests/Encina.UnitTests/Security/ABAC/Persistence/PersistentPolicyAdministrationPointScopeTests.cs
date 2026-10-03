#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Caching;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Regression tests for #1707: the singleton <see cref="PersistentPolicyAdministrationPoint"/>
/// resolves the scoped <see cref="IPolicyStore"/> per operation, in its own scope.
/// </summary>
public sealed class PersistentPolicyAdministrationPointScopeTests
{
    [Fact]
    public async Task ConcurrentOperations_UseDifferentStoreInstancesAndDisposeEachScope()
    {
        var stores = new List<IPolicyStore>();
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopesDisposed = 0;

        // Both operations are in flight before either completes.
        async ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> WaitForBothAsync()
        {
            lock (stores)
            {
                if (stores.Count == 2)
                {
                    bothStarted.TrySetResult();
                }
            }

            await bothStarted.Task;
            return Right<EncinaError, IReadOnlyList<PolicySet>>([]);
        }

        var services = new ServiceCollection();
        services.AddScoped<IPolicyStore>(_ =>
        {
            var store = Substitute.For<IPolicyStore, IAsyncDisposable>();
            ((IAsyncDisposable)store).DisposeAsync().Returns(_ =>
            {
                Interlocked.Increment(ref scopesDisposed);
                return ValueTask.CompletedTask;
            });
            store.GetAllPolicySetsAsync(Arg.Any<CancellationToken>()).Returns(_ => WaitForBothAsync());
            lock (stores)
            {
                stores.Add(store);
            }

            return store;
        });
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var pap = new PersistentPolicyAdministrationPoint(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PersistentPolicyAdministrationPoint>.Instance);

        var first = pap.GetPolicySetsAsync().AsTask();
        var second = pap.GetPolicySetsAsync().AsTask();
        await Task.WhenAll(first, second);

        stores.Count.ShouldBe(2);
        ReferenceEquals(stores[0], stores[1]).ShouldBeFalse();
        await stores[0].Received(1).GetAllPolicySetsAsync(Arg.Any<CancellationToken>());
        await stores[1].Received(1).GetAllPolicySetsAsync(Arg.Any<CancellationToken>());
        scopesDisposed.ShouldBe(2);
    }

    [Fact]
    public async Task Mutation_ResolvesAuditStoreInAScopeSeparateFromThePolicyStore()
    {
        var policyStoreScopes = new List<ScopeMarker>();
        var auditStoreScopes = new List<ScopeMarker>();
        var services = new ServiceCollection();
        services.AddScoped<ScopeMarker>();
        services.AddScoped<IPolicyStore>(sp =>
        {
            policyStoreScopes.Add(sp.GetRequiredService<ScopeMarker>());
            var store = Substitute.For<IPolicyStore>();
            store.ExistsPolicySetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));
            store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
            return store;
        });
        services.AddScoped<IAuditStore>(sp =>
        {
            auditStoreScopes.Add(sp.GetRequiredService<ScopeMarker>());
            var audit = Substitute.For<IAuditStore>();
            audit.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
            return audit;
        });
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("alice");
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        var pap = new PersistentPolicyAdministrationPoint(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PersistentPolicyAdministrationPoint>.Instance,
            accessor);

        (await pap.AddPolicySetAsync(CreatePolicySet("ps-audit"))).IsRight.ShouldBeTrue();

        policyStoreScopes.Count.ShouldBe(1);
        auditStoreScopes.Count.ShouldBe(1);
        ReferenceEquals(policyStoreScopes[0], auditStoreScopes[0]).ShouldBeFalse();
    }

    private sealed class ScopeMarker;

    [Fact]
    public async Task AddEncinaABAC_PolicyCachingEnabled_WrapsTheScopedStoreInTheDecoratorPerOperation()
    {
        var cache = Substitute.For<ICacheProvider>();
        var stores = new List<IPolicyStore>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<global::Encina.Security.ISecurityContextAccessor>());
        services.AddSingleton(cache);
        services.AddScoped<IPolicyStore>(_ =>
        {
            var store = Substitute.For<IPolicyStore>();
            store.ExistsPolicySetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));
            store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
            stores.Add(store);
            return store;
        });
        var accessor = Substitute.For<IRequestContextAccessor>();
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("alice");
        accessor.RequestContext.Returns(context);
        services.AddSingleton(accessor);
        services.AddEncinaABAC(options =>
        {
            options.UsePersistentPAP = true;
            options.PolicyCaching.Enabled = true;
            options.PolicyCaching.EnablePubSubInvalidation = false;
        });
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var pap = provider.GetRequiredService<IPolicyAdministrationPoint>();

        (await pap.AddPolicySetAsync(CreatePolicySet("ps-1"))).IsRight.ShouldBeTrue();
        (await pap.AddPolicySetAsync(CreatePolicySet("ps-2"))).IsRight.ShouldBeTrue();

        // A new scoped store per operation, each wrapped by the decorator (it invalidates the shared cache).
        stores.Count.ShouldBe(2);
        await cache.Received().RemoveAsync(Arg.Is<string>(k => k.Contains("ps-1")), Arg.Any<CancellationToken>());
        await cache.Received().RemoveAsync(Arg.Is<string>(k => k.Contains("ps-2")), Arg.Any<CancellationToken>());
    }

    private static PolicySet CreatePolicySet(string id) => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };
}
