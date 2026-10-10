using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// The decision audit startup check (#751 Phase 4): Critical 9087 and a failed start without an
/// operation audit store, Warning 9086 for the in-memory store, Warning 9084 for BestEffort, and a
/// no-op that never resolves the store while the audit is disabled.
/// </summary>
public sealed class ABACDecisionAuditStartupCheckTests
{
    private static (ABACDecisionAuditStartupCheck Check, FakeLogCollector Logs, ServiceProvider Provider) Create(
        Action<ABACDecisionAuditOptions> configure, IOperationAuditStore? store)
    {
        var options = new ABACOptions();
        configure(options.DecisionAudit);

        var services = new ServiceCollection();
        if (store is not null)
        {
            services.AddScoped(_ => store);
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var logs = new FakeLogCollector();
        var logger = new FakeLogger<ABACDecisionAuditStartupCheck>(logs);
        var check = new ABACDecisionAuditStartupCheck(
            Options.Create(options), provider.GetRequiredService<IServiceScopeFactory>(), logger);
        return (check, logs, provider);
    }

    [Fact]
    public async Task StartAsync_Disabled_NeverResolvesTheStoreAndLogsNothing()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var logs = new FakeLogCollector();
        var check = new ABACDecisionAuditStartupCheck(
            Options.Create(new ABACOptions()), scopeFactory, new FakeLogger<ABACDecisionAuditStartupCheck>(logs));

        await check.StartAsync(CancellationToken.None);
        await check.StopAsync(CancellationToken.None);

        scopeFactory.ReceivedCalls().ShouldBeEmpty();
        logs.GetSnapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task StartAsync_EnabledWithoutAStore_LogsCritical9087AndThrows()
    {
        var (check, logs, provider) = Create(o => o.Enabled = true, store: null);
        using var _ = provider;

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => check.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("IOperationAuditStore");
        var record = logs.GetSnapshot().Single();
        record.Id.Id.ShouldBe(9087);
        record.Level.ShouldBe(LogLevel.Critical);
    }

    [Fact]
    public async Task StartAsync_EnabledWithTheInMemoryStore_LogsWarning9086Only()
    {
        var (check, logs, provider) = Create(o => o.Enabled = true, new InMemoryOperationAuditStore());
        using var _ = provider;

        await check.StartAsync(CancellationToken.None);

        var record = logs.GetSnapshot().Single();
        record.Id.Id.ShouldBe(9086);
        record.Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task StartAsync_EnabledBestEffortWithAPersistentStore_LogsWarning9084Only()
    {
        var (check, logs, provider) = Create(
            o =>
            {
                o.Enabled = true;
                o.FailureMode = ABACDecisionAuditFailureMode.BestEffort;
            },
            Substitute.For<IOperationAuditStore>());
        using var _ = provider;

        await check.StartAsync(CancellationToken.None);

        var record = logs.GetSnapshot().Single();
        record.Id.Id.ShouldBe(9084);
        record.Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task StartAsync_EnabledBestEffortWithTheInMemoryStore_LogsBothWarnings()
    {
        var (check, logs, provider) = Create(
            o =>
            {
                o.Enabled = true;
                o.FailureMode = ABACDecisionAuditFailureMode.BestEffort;
            },
            new InMemoryOperationAuditStore());
        using var _ = provider;

        await check.StartAsync(CancellationToken.None);

        logs.GetSnapshot().Select(r => r.Id.Id).ShouldBe([9086, 9084], ignoreOrder: true);
    }

    [Fact]
    public async Task StartAsync_EnabledFailClosedWithAPersistentStore_LogsNothing()
    {
        var (check, logs, provider) = Create(o => o.Enabled = true, Substitute.For<IOperationAuditStore>());
        using var _ = provider;

        await check.StartAsync(CancellationToken.None);

        logs.GetSnapshot().ShouldBeEmpty();
    }

    // ── Options builder ─────────────────────────────────────────────

    [Fact]
    public void AuditDecisions_WithoutAnAction_EnablesTheAudit()
    {
        var options = new ABACOptions();

        options.AuditDecisions().ShouldBeSameAs(options);

        options.DecisionAudit.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void AuditDecisions_WithAnAction_EnablesAndConfiguresTheAudit()
    {
        var options = new ABACOptions();

        options.AuditDecisions(audit =>
        {
            audit.Outcomes = ABACDecisionAuditOutcomes.Denied;
            audit.WriteTimeout = TimeSpan.FromSeconds(2);
        });

        options.DecisionAudit.Enabled.ShouldBeTrue();
        options.DecisionAudit.Outcomes.ShouldBe(ABACDecisionAuditOutcomes.Denied);
        options.DecisionAudit.WriteTimeout.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AuditDecisions_ActionTurningTheAuditOff_Wins()
    {
        var options = new ABACOptions();

        options.AuditDecisions(audit => audit.Enabled = false);

        options.DecisionAudit.Enabled.ShouldBeFalse();
    }

    // ── Validator: enums ────────────────────────────────────────────

    [Fact]
    public void ABACOptionsValidator_RejectsAnUndefinedFailureMode()
    {
        var options = new ABACOptions();
        options.DecisionAudit.FailureMode = (ABACDecisionAuditFailureMode)42;

        var result = new ABACOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("FailureMode");
    }

    [Fact]
    public void ABACOptionsValidator_RejectsAnOutcomesValueWithUnknownBits()
    {
        var options = new ABACOptions();
        options.DecisionAudit.Outcomes = (ABACDecisionAuditOutcomes)64;

        var result = new ABACOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Outcomes");
    }

    [Theory]
    [InlineData(ABACDecisionAuditOutcomes.None)]
    [InlineData(ABACDecisionAuditOutcomes.Denied)]
    [InlineData(ABACDecisionAuditOutcomes.Granted | ABACDecisionAuditOutcomes.NotEnforced)]
    [InlineData(ABACDecisionAuditOutcomes.All)]
    public void ABACOptionsValidator_AcceptsEveryCombinationOfTheDefinedFlags(ABACDecisionAuditOutcomes outcomes)
    {
        var options = new ABACOptions();
        options.DecisionAudit.Outcomes = outcomes;

        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ABACOptionsValidator_RejectsTheInfiniteTimeout()
    {
        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = Timeout.InfiniteTimeSpan;

        new ABACOptionsValidator().Validate(null, options).Failed.ShouldBeTrue();
    }

    [Fact]
    public void ABACOptionsValidator_AcceptsTheLargestTimeoutATokenSourceAccepts()
    {
        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }
}
