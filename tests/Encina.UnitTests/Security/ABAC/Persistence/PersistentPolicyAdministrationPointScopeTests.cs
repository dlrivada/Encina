#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Caching;
using Encina.Caching.Memory;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
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

            // A serialization regression fails fast instead of hanging.
            await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
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
        var pubSub = Substitute.For<IPubSubProvider>();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 3, 18, 45, 0, TimeSpan.Zero));
        var stores = new List<IPolicyStore>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<global::Encina.Security.ISecurityContextAccessor>());
        services.AddSingleton(cache);
        services.AddSingleton(pubSub);
        services.AddSingleton<TimeProvider>(clock);
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
            options.PolicyCaching.EnablePubSubInvalidation = true;
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

        // #1708 end to end: the registered TimeProvider reaches the decorator and stamps the message.
        await pubSub.Received().PublishAsync(
            Arg.Any<string>(),
            Arg.Is<PolicyCacheInvalidationMessage>(m =>
                m.EntityId == "ps-1" && m.TimestampUtc == clock.GetUtcNow().UtcDateTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mutation_PolicyStoreReturnsLeft_RecordsErrorEntryThroughTheAuditScopeAndDisposesBothScopes()
    {
        var harness = new FailingStoreHarness(failure: null);

        var result = await harness.Pap.AddPolicySetAsync(CreatePolicySet("ps-left"));

        result.IsLeft.ShouldBeTrue();
        harness.AssertErrorEntryRecordedInSeparateScopeAndScopesDisposed("store.failed");
    }

    [Fact]
    public async Task Mutation_PolicyStoreThrows_RecordsErrorEntryThroughTheAuditScopeAndDisposesBothScopes()
    {
        var harness = new FailingStoreHarness(failure: new InvalidOperationException("db crashed"));

        await Should.ThrowAsync<InvalidOperationException>(async () => await harness.Pap.AddPolicySetAsync(CreatePolicySet("ps-throws")));

        harness.AssertErrorEntryRecordedInSeparateScopeAndScopesDisposed(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task RealMemoryCache_ChangeThroughAnotherOperation_InvalidatesTheSharedCacheAcrossScopes()
    {
        var current = CreatePolicy("p-cache", "v1");
        var innerReads = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<global::Encina.Security.ISecurityContextAccessor>());
        services.AddEncinaMemoryCache();
        services.AddScoped<IPolicyStore>(_ =>
        {
            var store = Substitute.For<IPolicyStore>();
            store.GetPolicyAsync("p-cache", Arg.Any<CancellationToken>()).Returns(_ =>
            {
                Interlocked.Increment(ref innerReads);
                return new ValueTask<Either<EncinaError, Option<Policy>>>(
                    Right<EncinaError, Option<Policy>>(Some(current)));
            });
            store.ExistsPolicyAsync("p-cache", Arg.Any<CancellationToken>())
                .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(true)));
            store.SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                current = call.Arg<Policy>();
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
            });
            return store;
        });
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("alice");
        var accessor = Substitute.For<IRequestContextAccessor>();
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

        var first = await pap.GetPolicyAsync("p-cache");
        var second = await pap.GetPolicyAsync("p-cache");
        innerReads.ShouldBe(1, "the second read comes from the cache shared across scopes");
        DescriptionOf(first).ShouldBe("v1");
        DescriptionOf(second).ShouldBe("v1");

        (await pap.UpdatePolicyAsync(CreatePolicy("p-cache", "v2"))).IsRight.ShouldBeTrue();
        var third = await pap.GetPolicyAsync("p-cache");

        DescriptionOf(third).ShouldBe("v2");
        innerReads.ShouldBe(2, "the update in another scope invalidated the shared cache entry");
    }

    private static string? DescriptionOf(Either<EncinaError, Option<Policy>> result) =>
        result.Match(
            Right: option => option.Match(Some: policy => policy.Description, None: () => "none"),
            Left: _ => "error");

    private static Policy CreatePolicy(string id, string description) => new()
    {
        Id = id,
        Description = description,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    /// <summary>Tracks the DI scope it was created in and counts disposals.</summary>
    private sealed class ScopeTracker : IAsyncDisposable
    {
        public static int Disposed;

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref Disposed);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>A PAP whose policy store fails on save, with an audit store that records its entries.</summary>
    private sealed class FailingStoreHarness
    {
        private readonly List<ScopeTracker> _policyScopes = [];
        private readonly List<ScopeTracker> _auditScopes = [];
        private readonly List<AuditEntry> _entries = [];
        private readonly ServiceProvider _provider;

        public FailingStoreHarness(Exception? failure)
        {
            ScopeTracker.Disposed = 0;
            var services = new ServiceCollection();
            services.AddScoped<ScopeTracker>();
            services.AddScoped<IPolicyStore>(sp =>
            {
                _policyScopes.Add(sp.GetRequiredService<ScopeTracker>());
                var store = Substitute.For<IPolicyStore>();
                store.ExistsPolicySetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));
                store.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>()).Returns(_ =>
                    failure is null
                        ? new ValueTask<Either<EncinaError, Unit>>(
                            Left<EncinaError, Unit>(EncinaErrors.Create("store.failed", "store down")))
                        : throw failure);
                return store;
            });
            services.AddScoped<IAuditStore>(sp =>
            {
                _auditScopes.Add(sp.GetRequiredService<ScopeTracker>());
                var audit = Substitute.For<IAuditStore>();
                audit.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>()).Returns(call =>
                {
                    _entries.Add(call.Arg<AuditEntry>());
                    return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
                });
                return audit;
            });
            _provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            var context = Substitute.For<IRequestContext>();
            context.UserId.Returns("alice");
            var accessor = Substitute.For<IRequestContextAccessor>();
            accessor.RequestContext.Returns(context);
            Pap = new PersistentPolicyAdministrationPoint(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<PersistentPolicyAdministrationPoint>.Instance,
                accessor);
        }

        public PersistentPolicyAdministrationPoint Pap { get; }

        public void AssertErrorEntryRecordedInSeparateScopeAndScopesDisposed(string expectedErrorCode)
        {
            _entries.Count.ShouldBe(2);
            _entries[0].Outcome.ShouldBe(AuditOutcome.Success);
            _entries[1].Outcome.ShouldBe(AuditOutcome.Error);
            _entries[1].ErrorMessage.ShouldBe(expectedErrorCode);
            _entries[1].Metadata["writeAheadEntryId"].ShouldBe(_entries[0].Id);
            _auditScopes.Count.ShouldBe(1, "both audit entries go through the one audit scope");
            _policyScopes.Count.ShouldBe(1);
            ReferenceEquals(_policyScopes[0], _auditScopes[0]).ShouldBeFalse();
            ScopeTracker.Disposed.ShouldBe(2, "the policy-store scope and the audit scope are both disposed");
            _provider.Dispose();
        }
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
