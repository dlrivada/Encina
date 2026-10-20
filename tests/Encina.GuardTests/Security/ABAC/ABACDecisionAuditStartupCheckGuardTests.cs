using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard tests for the decision audit startup check and the <c>AuditDecisions</c> builder (#751 Phase 4).
/// </summary>
public sealed class ABACDecisionAuditStartupCheckGuardTests
{
    private static readonly IOptions<ABACOptions> Options = Microsoft.Extensions.Options.Options.Create(new ABACOptions());

    [Fact]
    public void StartupCheck_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACDecisionAuditStartupCheck(
            null!, Substitute.For<IServiceScopeFactory>(), NullLogger<ABACDecisionAuditStartupCheck>.Instance))
            .ParamName.ShouldBe("options");

    [Fact]
    public void StartupCheck_NullScopeFactory_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACDecisionAuditStartupCheck(
            Options, null!, NullLogger<ABACDecisionAuditStartupCheck>.Instance))
            .ParamName.ShouldBe("scopeFactory");

    [Fact]
    public void StartupCheck_NullLogger_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACDecisionAuditStartupCheck(
            Options, Substitute.For<IServiceScopeFactory>(), null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task StartupCheck_Disabled_StartsAndStopsWithoutOpeningAScope()
    {
        var scopes = Substitute.For<IServiceScopeFactory>();
        var sut = new ABACDecisionAuditStartupCheck(Options, scopes, NullLogger<ABACDecisionAuditStartupCheck>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        scopes.ReceivedCalls().ShouldBeEmpty();
    }

    private static ABACDecisionAuditStartupCheck Enabled(IOperationAuditStore? store, ABACDecisionAuditFailureMode mode = ABACDecisionAuditFailureMode.FailClosed)
    {
        var options = new ABACOptions();
        options.AuditDecisions(a => a.FailureMode = mode);
        var services = new ServiceCollection();
        if (store is not null)
        {
            services.AddScoped(_ => store);
        }

        var provider = services.BuildServiceProvider();
        return new ABACDecisionAuditStartupCheck(
            Microsoft.Extensions.Options.Options.Create(options),
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ABACDecisionAuditStartupCheck>.Instance);
    }

    [Fact]
    public async Task StartupCheck_EnabledWithoutAStore_Throws() =>
        await Should.ThrowAsync<InvalidOperationException>(() => Enabled(null).StartAsync(CancellationToken.None));

    [Fact]
    public async Task StartupCheck_EnabledWithAPersistentStore_Starts() =>
        await Should.NotThrowAsync(() => Enabled(Substitute.For<IOperationAuditStore>()).StartAsync(CancellationToken.None));

    [Fact]
    public async Task StartupCheck_EnabledWithTheInMemoryStore_Starts() =>
        await Should.NotThrowAsync(() => Enabled(new InMemoryOperationAuditStore()).StartAsync(CancellationToken.None));

    [Fact]
    public async Task StartupCheck_EnabledBestEffort_Starts() =>
        await Should.NotThrowAsync(() => Enabled(
            Substitute.For<IOperationAuditStore>(), ABACDecisionAuditFailureMode.BestEffort).StartAsync(CancellationToken.None));

    [Fact]
    public void Validator_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACOptionsValidator().Validate(null, null!))
            .ParamName.ShouldBe("options");

    [Fact]
    public void Validator_EveryRuleBroken_ReportsEachFailure()
    {
        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = TimeSpan.Zero;
        options.DecisionAudit.MaxTraceEntries = 0;
        options.DecisionAudit.Outcomes = (ABACDecisionAuditOutcomes)64;
        options.DecisionAudit.FailureMode = (ABACDecisionAuditFailureMode)42;

        new ABACOptionsValidator().Validate(null, options).Failures!.Count().ShouldBe(4);
    }

    [Fact]
    public void AuditDecisions_NullAction_IsAllowedAndEnablesTheAudit()
    {
        var options = new ABACOptions();

        Should.NotThrow(() => options.AuditDecisions(null));

        options.DecisionAudit.Enabled.ShouldBeTrue();
    }
}
