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

}
