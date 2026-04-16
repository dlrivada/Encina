#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Health;
using Encina.Security.ABAC.Persistence;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.UnitTests.Security.ABAC.Health;

/// <summary>
/// Unit tests for <see cref="ABACHealthCheck"/>: verifies the ABAC engine health
/// by checking for loaded policies and policy sets in the PAP, and optionally
/// verifying persistent store connectivity when <see cref="IPolicyStore"/> is registered.
/// </summary>
public sealed class ABACHealthCheckTests
{
    /// <summary>
    /// Builds a real <see cref="IServiceProvider"/> from a <see cref="ServiceCollection"/>,
    /// optionally registering a given <see cref="IPolicyStore"/> for persistent store tests.
    /// </summary>
    /// <remarks>
    /// Uses a real DI container instead of mocking <see cref="IServiceProvider"/> directly,
    /// because <c>CreateScope()</c> extension methods rely on <see cref="IServiceScopeFactory"/>
    /// resolution which is fragile to mock with NSubstitute proxies.
    /// </remarks>
    private static ServiceProvider CreateServiceProvider(IPolicyStore? policyStore = null)
    {
        var services = new ServiceCollection();

        if (policyStore is not null)
        {
            services.AddSingleton(policyStore);
        }

        return services.BuildServiceProvider();
    }

    #region Healthy — Policy Sets Loaded

    [Fact]
    public async Task CheckHealthAsync_WithPolicySets_ReturnsHealthy()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                new List<PolicySet>
                {
                    new()
                    {
                        Id = "ps-1",
                        Target = null,
                        Algorithm = global::Encina.Security.ABAC.CombiningAlgorithmId.DenyOverrides,
                        Policies = [],
                        PolicySets = [],
                        Obligations = [],
                        Advice = []
                    }
                }));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("policy sets");
    }

    #endregion

    #region Healthy — Standalone Policies Loaded

    [Fact]
    public async Task CheckHealthAsync_NoPolicySets_WithStandalonePolicies_ReturnsHealthy()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                new List<PolicySet>()));

        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<Policy>>.Right(
                new List<Policy>
                {
                    new()
                    {
                        Id = "policy-1",
                        Target = null,
                        Algorithm = global::Encina.Security.ABAC.CombiningAlgorithmId.DenyOverrides,
                        Rules = [],
                        Obligations = [],
                        Advice = [],
                        VariableDefinitions = []
                    }
                }));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("standalone policies");
    }

    #endregion

    #region Degraded — No Policies

    [Fact]
    public async Task CheckHealthAsync_NoPoliciesNoPolicySets_ReturnsDegraded()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                new List<PolicySet>()));

        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<Policy>>.Right(
                new List<Policy>()));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("No policies or policy sets loaded");
    }

    #endregion

    #region Degraded — PAP Errors (No Policies Found)

    [Fact]
    public async Task CheckHealthAsync_PapPolicySetsError_EmptyPolicies_ReturnsDegraded()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<PolicySet>>.Left(
                EncinaError.New("PAP error")));

        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<Policy>>.Right(
                new List<Policy>()));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // PAP error on policy sets → hasPolicySets=false, then checks standalone
        // No standalone policies → Degraded
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    #region Unhealthy — PAP Exception

    [Fact]
    public async Task CheckHealthAsync_PapThrows_ReturnsUnhealthy()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, IReadOnlyList<PolicySet>>>(_ =>
                throw new InvalidOperationException("Connection failed"));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Failed to query");
        result.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    #endregion

    #region Persistent Store — Healthy

    [Fact]
    public async Task CheckHealthAsync_WithPersistentStore_StoreReachable_ReturnsHealthy()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                new List<PolicySet>
                {
                    new()
                    {
                        Id = "ps-1",
                        Target = null,
                        Algorithm = CombiningAlgorithmId.DenyOverrides,
                        Policies = [],
                        PolicySets = [],
                        Obligations = [],
                        Advice = []
                    }
                }));

        var store = Substitute.For<IPolicyStore>();
        store.GetPolicySetCountAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Either<EncinaError, int>.Right(1)));
        store.GetPolicyCountAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Either<EncinaError, int>.Right(0)));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider(store));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    #endregion

    #region Persistent Store — Unhealthy (Store Error)

    [Fact]
    public async Task CheckHealthAsync_WithPersistentStore_StoreError_ReturnsUnhealthy()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        // PAP should not be queried when store fails
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                new List<PolicySet>()));

        var store = Substitute.For<IPolicyStore>();
        store.GetPolicySetCountAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(
                Either<EncinaError, int>.Left(EncinaError.New("Database connection failed"))));
        store.GetPolicyCountAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Either<EncinaError, int>.Right(0)));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider(store));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Persistent policy store connectivity check failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WithPersistentStore_PolicyCountError_ReturnsUnhealthy()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();

        var store = Substitute.For<IPolicyStore>();
        store.GetPolicySetCountAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Either<EncinaError, int>.Right(0)));
        store.GetPolicyCountAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(
                Either<EncinaError, int>.Left(EncinaError.New("Query timeout"))));

        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider(store));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Persistent policy store connectivity check failed");
    }

    #endregion

    #region No Persistent Store — Skips Store Check

    [Fact]
    public async Task CheckHealthAsync_NoPersistentStore_SkipsStoreCheck_ReturnsDegraded()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(
                new List<PolicySet>()));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<Policy>>.Right(
                new List<Policy>()));

        // No IPolicyStore registered (in-memory mode)
        var healthCheck = new ABACHealthCheck(pap, CreateServiceProvider());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Should reach PAP check and return Degraded (no policies)
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    #region Default Properties

    [Fact]
    public void DefaultName_IsEncinaAbac()
    {
        ABACHealthCheck.DefaultName.ShouldBe("encina-abac");
    }

    [Fact]
    public void Tags_ContainsExpectedTags()
    {
        ABACHealthCheck.Tags.ShouldContain("encina");
        ABACHealthCheck.Tags.ShouldContain("security");
        ABACHealthCheck.Tags.ShouldContain("abac");
        ABACHealthCheck.Tags.ShouldContain("ready");
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public void Constructor_NullPap_Throws()
    {
        var act = () => new ABACHealthCheck(null!, CreateServiceProvider());

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_NullServiceProvider_Throws()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        var act = () => new ABACHealthCheck(pap, null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion
}
