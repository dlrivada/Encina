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
        var sut = new PersistentPolicyAdministrationPoint(
            store, NullLogger<PersistentPolicyAdministrationPoint>.Instance, auditStore, CreateAccessor("alice"));

        var result = await sut.AddPolicyAsync(CreatePolicy(), parentPolicySetId: null);

        result.IsLeft.ShouldBeTrue();
        await store.DidNotReceive().SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPolicyAsync_NoResolvablePrincipal_ChangeIsRefused()
    {
        var store = CreateStoreForNewStandalonePolicy();
        var sut = new PersistentPolicyAdministrationPoint(
            store, NullLogger<PersistentPolicyAdministrationPoint>.Instance, auditStore: null, CreateAccessor(null));

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
