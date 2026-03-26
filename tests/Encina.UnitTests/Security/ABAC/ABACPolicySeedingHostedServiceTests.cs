#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute .Returns() pattern for ValueTask
using Encina.Security.ABAC;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Tests for <see cref="ABACPolicySeedingHostedService"/>.
/// </summary>
public sealed class ABACPolicySeedingHostedServiceTests
{
    private readonly IPolicyAdministrationPoint _pap = Substitute.For<IPolicyAdministrationPoint>();

    [Fact]
    public void Constructor_WithNullPap_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            null!,
            Options.Create(new ABACOptions()),
            NullLogger<ABACPolicySeedingHostedService>.Instance));
    }

    [Fact]
    public async Task StartAsync_WithNoSeedData_DoesNotCallPap()
    {
        var options = Options.Create(new ABACOptions());
        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _pap.DidNotReceive().AddPolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
        await _pap.DidNotReceive().AddPolicyAsync(Arg.Any<Policy>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_SeedsPolicySets()
    {
        var policySet = CreatePolicySet("seed-ps-1");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet);

        _pap.AddPolicySetAsync(policySet, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        await _pap.Received(1).AddPolicySetAsync(policySet, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_SeedsPolicies()
    {
        var policy = CreatePolicy("seed-p-1");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);

        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        await _pap.Received(1).AddPolicyAsync(policy, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenPapFails_LogsWarningAndContinues()
    {
        var policySet1 = CreatePolicySet("fail-ps");
        var policySet2 = CreatePolicySet("success-ps");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet1);
        options.Value.SeedPolicySets.Add(policySet2);

        _pap.AddPolicySetAsync(policySet1, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(EncinaError.New("Duplicate"))));
        _pap.AddPolicySetAsync(policySet2, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        // Both should have been attempted
        await _pap.Received(2).AddPolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        var options = Options.Create(new ABACOptions());
        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await sut.StopAsync(CancellationToken.None); // Should not throw
    }

    [Fact]
    public async Task StartAsync_SeedsBothPolicySetsAndPolicies()
    {
        var policySet = CreatePolicySet("mixed-ps");
        var policy = CreatePolicy("mixed-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet);
        options.Value.SeedPolicies.Add(policy);

        _pap.AddPolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
        _pap.AddPolicyAsync(Arg.Any<Policy>(), null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        await _pap.Received(1).AddPolicySetAsync(policySet, Arg.Any<CancellationToken>());
        await _pap.Received(1).AddPolicyAsync(policy, null, Arg.Any<CancellationToken>());
    }

    private static PolicySet CreatePolicySet(string id) => new()
    {
        Id = id,
        Policies = [],
        PolicySets = [],
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Obligations = [],
        Advice = []
    };

    private static Policy CreatePolicy(string id) => new()
    {
        Id = id,
        Rules = [],
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };
}
