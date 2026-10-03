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
/// are audited fail closed, refused without a principal, and wired without a captive scoped
/// <see cref="IAuditStore"/>.
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
        if (auditStore is not null)
        {
            services.AddScoped(_ => auditStore);
        }

        var provider = services.BuildServiceProvider();
        return new PersistentPolicyAdministrationPoint(
            store,
            NullLogger<PersistentPolicyAdministrationPoint>.Instance,
            provider.GetRequiredService<IServiceScopeFactory>(),
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
        var auditStore = Substitute.For<IAuditStore>();
        auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Prelude.unit)));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<global::Encina.Security.ISecurityContextAccessor>());
        services.AddSingleton(CreateAccessor("alice"));
        services.AddSingleton(store);
        services.AddScoped(_ => auditStore);
        services.AddEncinaABAC(options => options.UsePersistentPAP = true);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var pap = provider.GetRequiredService<IPolicyAdministrationPoint>();
        var result = await pap.AddPolicyAsync(CreatePolicy(), parentPolicySetId: null);

        result.IsRight.ShouldBeTrue();
        await auditStore.Received(1).RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }
}
