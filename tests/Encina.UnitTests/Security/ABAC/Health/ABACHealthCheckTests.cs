using Encina.Security.ABAC;
using Encina.Security.ABAC.Health;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.UnitTests.Security.ABAC.Health;

/// <summary>
/// Unit tests for <see cref="ABACHealthCheck"/>: verifies the ABAC engine health
/// by checking for loaded policies and policy sets in the PAP.
/// </summary>
public sealed class ABACHealthCheckTests
{
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

        var healthCheck = new ABACHealthCheck(pap);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("policy sets");
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

        var healthCheck = new ABACHealthCheck(pap);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("standalone policies");
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

        var healthCheck = new ABACHealthCheck(pap);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("No policies or policy sets loaded");
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

        var healthCheck = new ABACHealthCheck(pap);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // PAP error on policy sets → hasPolicySets=false, then checks standalone
        // No standalone policies → Degraded
        result.Status.Should().Be(HealthStatus.Degraded);
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

        var healthCheck = new ABACHealthCheck(pap);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Failed to query");
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    #endregion

    #region Default Properties

    [Fact]
    public void DefaultName_IsEncinaAbac()
    {
        ABACHealthCheck.DefaultName.Should().Be("encina-abac");
    }

    [Fact]
    public void Tags_ContainsExpectedTags()
    {
        ABACHealthCheck.Tags.Should().Contain("encina");
        ABACHealthCheck.Tags.Should().Contain("security");
        ABACHealthCheck.Tags.Should().Contain("abac");
        ABACHealthCheck.Tags.Should().Contain("ready");
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public void Constructor_NullPap_Throws()
    {
        var act = () => new ABACHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
