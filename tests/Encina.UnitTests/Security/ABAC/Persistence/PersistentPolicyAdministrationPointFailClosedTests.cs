#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Regression tests for #1677: policy changes of <see cref="PersistentPolicyAdministrationPoint"/>
/// are audited fail closed, refused without a principal, and wired with the <see cref="IAuditStore"/>
/// resolved per write in its own scope, together with the policy store (#1707).
/// </summary>
public sealed class PersistentPolicyAdministrationPointFailClosedTests
{
    private static Policy CreatePolicy(string id = "p-1") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static IPolicyStore CreateStoreForNewStandalonePolicy()
    {
        var store = Substitute.For<IPolicyStore>();
        store.ExistsPolicyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Either<EncinaError, bool>.Right(false)));
        store.GetAllPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>(
                Either<EncinaError, IReadOnlyList<PolicySet>>.Right((IReadOnlyList<PolicySet>)[])));
        store.SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Prelude.unit)));
        return store;
    }

    private static IRequestContextAccessor CreateAccessor(string? userId)
    {
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns(userId);
        context.CorrelationId.Returns("corr-1");
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        return accessor;
    }

    [Fact]
    public async Task AddPolicyAsync_AuditStoreReturnsLeft_ChangeIsRejectedAndNotPersisted()
    {
        var store = CreateStoreForNewStandalonePolicy();
        var auditStore = Substitute.For<IAuditStore>();
        auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(
                Either<EncinaError, Unit>.Left(EncinaErrors.Create("audit.failed", "store down"))));
        var sut = CreateSut(store, auditStore, CreateAccessor("alice"));

        var result = await sut.AddPolicyAsync(CreatePolicy(), parentPolicySetId: null);

        result.IsLeft.ShouldBeTrue();
        await store.DidNotReceive().SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedingHostedService_WithoutPrincipal_SeedsThroughTheSystemActorScope()
    {
        var store = CreateStoreForNewStandalonePolicy();
        var auditStore = Substitute.For<IAuditStore>();
        auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Prelude.unit)));
        var pap = CreateSut(store, auditStore, accessor: null);
        var options = new ABACOptions();
        options.SeedPolicies.Add(CreatePolicy("seeded"));
        var seeder = new ABACPolicySeedingHostedService(
            pap, Microsoft.Extensions.Options.Options.Create(options), NullLogger<ABACPolicySeedingHostedService>.Instance);

        await seeder.StartAsync(CancellationToken.None);

        await store.Received(1).SavePolicyAsync(Arg.Is<Policy>(p => p.Id == "seeded"), Arg.Any<CancellationToken>());
        await auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e => e.UserId == "system" && e.EntityId == "seeded"), Arg.Any<CancellationToken>());
        (await pap.AddPolicyAsync(CreatePolicy("after"), parentPolicySetId: null)).IsLeft.ShouldBeTrue();
    }

    private static PersistentPolicyAdministrationPoint CreateSut(
        IPolicyStore store, IAuditStore? auditStore, IRequestContextAccessor? accessor)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        if (auditStore is not null)
        {
            services.AddScoped(_ => auditStore);
        }

        var provider = services.BuildServiceProvider();
        return new PersistentPolicyAdministrationPoint(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PersistentPolicyAdministrationPoint>.Instance,
            accessor);
    }

    [Fact]
    public async Task AddPolicyAsync_NoResolvablePrincipal_ChangeIsRefused()
    {
        var store = CreateStoreForNewStandalonePolicy();
        var sut = CreateSut(store, auditStore: null, CreateAccessor(null));

        var result = await sut.AddPolicyAsync(CreatePolicy(), parentPolicySetId: null);

        result.IsLeft.ShouldBeTrue();
        await store.DidNotReceive().SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEncinaABAC_PersistentPapWithScopedAuditStore_BuildsAndAuditsInItsOwnScope()
    {
        var store = CreateStoreForNewStandalonePolicy();
        var created = 0;
        var disposed = 0;
        var recorded = 0;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<global::Encina.Security.ISecurityContextAccessor>());
        services.AddSingleton(CreateAccessor("alice"));
        // Both stores are scoped, like every database provider's: the singleton PAP resolves them
        // per operation in its own scope.
        services.AddScoped(_ => store);
        services.AddScoped<IAuditStore>(_ =>
        {
            created++;
            var auditStore = Substitute.For<IAuditStore, IAsyncDisposable>();
            auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    recorded++;
                    return new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Prelude.unit));
                });
            ((IAsyncDisposable)auditStore).DisposeAsync().Returns(_ =>
            {
                disposed++;
                return ValueTask.CompletedTask;
            });
            return auditStore;
        });
        services.AddEncinaABAC(options => options.UsePersistentPAP = true);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var pap = provider.GetRequiredService<IPolicyAdministrationPoint>();
        (await pap.AddPolicyAsync(CreatePolicy("p-1"), parentPolicySetId: null)).IsRight.ShouldBeTrue();
        (await pap.AddPolicyAsync(CreatePolicy("p-2"), parentPolicySetId: null)).IsRight.ShouldBeTrue();

        created.ShouldBe(2);
        disposed.ShouldBe(2);
        recorded.ShouldBe(2);
    }
}
