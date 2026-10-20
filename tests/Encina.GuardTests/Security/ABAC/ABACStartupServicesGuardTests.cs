using Encina.Security.ABAC;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for the ABAC startup hosted services added or changed by #1705 Phase 4:
/// <see cref="ABACPolicySeedingHostedService"/> (now taking the internal scope factory) and
/// <see cref="ABACEnforcementModeStartupCheck"/> (Warning 9085).
/// </summary>
public class ABACStartupServicesGuardTests
{
    private static readonly IOptions<ABACOptions> Options = Microsoft.Extensions.Options.Options.Create(new ABACOptions());

    [Fact]
    public void Seeding_NullPap_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            null!, Substitute.For<IInternalRequestContextScopeFactory>(), Options, NullLogger<ABACPolicySeedingHostedService>.Instance))
            .ParamName.ShouldBe("pap");

    [Fact]
    public void Seeding_NullScopes_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            Substitute.For<IPolicyAdministrationPoint>(), null!, Options, NullLogger<ABACPolicySeedingHostedService>.Instance))
            .ParamName.ShouldBe("scopes");

    [Fact]
    public void Seeding_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            Substitute.For<IPolicyAdministrationPoint>(), Substitute.For<IInternalRequestContextScopeFactory>(), null!, NullLogger<ABACPolicySeedingHostedService>.Instance))
            .ParamName.ShouldBe("options");

    [Fact]
    public void Seeding_NullLogger_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            Substitute.For<IPolicyAdministrationPoint>(), Substitute.For<IInternalRequestContextScopeFactory>(), Options, null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task Seeding_NoSeeds_StartsWithoutOpeningAScope()
    {
        var scopes = Substitute.For<IInternalRequestContextScopeFactory>();
        var sut = new ABACPolicySeedingHostedService(
            Substitute.For<IPolicyAdministrationPoint>(), scopes, Options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        scopes.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void StartupCheck_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACEnforcementModeStartupCheck(
            null!, NullLogger<ABACEnforcementModeStartupCheck>.Instance))
            .ParamName.ShouldBe("options");

    [Fact]
    public void StartupCheck_NullLogger_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACEnforcementModeStartupCheck(Options, null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task StartupCheck_StartAndStop_Complete()
    {
        var sut = new ABACEnforcementModeStartupCheck(Options, NullLogger<ABACEnforcementModeStartupCheck>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);
    }
}
