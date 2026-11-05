using Encina.Messaging.Inbox;
using Encina.Messaging.Purge;
using Encina.TestInfrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using static Encina.IntegrationTests.Messaging.InboxPipelineScenario;

namespace Encina.IntegrationTests.Messaging;

/// <summary>
/// Provider-neutral scenario for #3046: the hosted <see cref="MessagePurgeService"/>, registered by the
/// provider's own <c>AddEncina*</c> call with <c>InboxOptions.EnableAutomaticPurge</c> on its default, runs
/// one purge cycle against the real store and removes only the processed row whose <c>ExpiresAtUtc</c> has
/// passed. An unprocessed expired row and a processed row that has not expired survive. The delays of the
/// service run on a <see cref="FakeTimeProvider"/>, and the test waits on the store state (no <c>Thread.Sleep</c>; a short <c>Task.Delay</c> yields between clock advances).
/// </summary>
public static class MessagePurgeScenario
{
    // Under the service's real-clock start-up jitter (up to 30 s): a service that ignored the fake clock would
    // fall back to real time and could not be relied on to finish inside this budget.
    private static readonly TimeSpan s_waitBudget = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan s_untouchedClockPause = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan s_pollPause = TimeSpan.FromMilliseconds(25);

    /// <summary>The ids of the three seeded rows.</summary>
    private sealed record Seeded(string ProcessedExpired, string UnprocessedExpired, string ProcessedNotExpired);

    /// <summary>One purge cycle removes only the processed-expired inbox row.</summary>
    /// <param name="harness">The provider family under test (its own registration and a fresh-connection reader).</param>
    public static async Task AssertCycleRemovesOnlyProcessedExpiredRowsAsync(Harness harness)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using var provider = BuildProvider(harness, clock);
        var seeded = await SeedAsync(provider, clock.GetUtcNow().UtcDateTime);

        var service = provider.GetServices<IHostedService>().OfType<MessagePurgeService>().Single();
        await service.StartAsync(CancellationToken.None);
        try
        {
            // The start-up delay runs on the fake clock, which has not moved: the row must still be there. A
            // service on the real clock would purge within its (random, up to 30 s) jitter.
            await Task.Delay(s_untouchedClockPause);
            (await harness.ReadRow(seeded.ProcessedExpired)).ShouldNotBeNull("no purge may run before the fake clock advances");

            var purged = await WaitUntilGoneAsync(harness, clock, seeded.ProcessedExpired);
            purged.ShouldBeTrue($"the purge cycle must remove the processed expired row. {DescribeProblems(provider)}");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        (await harness.ReadRow(seeded.ProcessedExpired)).ShouldBeNull();
        (await harness.ReadRow(seeded.UnprocessedExpired)).ShouldNotBeNull();
        (await harness.ReadRow(seeded.ProcessedNotExpired)).ShouldNotBeNull();
    }

    private static ServiceProvider BuildProvider(Harness harness, FakeTimeProvider clock)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddFakeLogging());
        services.AddEncina();
        harness.Register(services, new Setup(MaxRetries: 3, Transactions: false, new CommitSabotage()));
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));
        return services.BuildServiceProvider();
    }

    private static async Task<Seeded> SeedAsync(ServiceProvider provider, DateTime now)
    {
        await using var scope = provider.CreateAsyncScope();
        var seeded = new Seeded(NewId(), NewId(), NewId());

        await AddAsync(scope.ServiceProvider, seeded.ProcessedExpired, now.AddDays(-1), processed: true, now);
        await AddAsync(scope.ServiceProvider, seeded.UnprocessedExpired, now.AddDays(-1), processed: false, now);
        await AddAsync(scope.ServiceProvider, seeded.ProcessedNotExpired, now.AddDays(7), processed: true, now);
        return seeded;
    }

    // The warnings and errors the purge logged (error codes and exception types only), so a failing cycle is
    // visible in the failure message instead of hiding behind a timeout.
    private static string DescribeProblems(ServiceProvider provider)
    {
        var problems = provider.GetFakeLogCollector().GetSnapshot()
            .Where(static r => r.Level >= LogLevel.Warning)
            .Select(static r => $"{r.Level}: {r.Message}")
            .ToList();

        return problems.Count == 0 ? "The purge logged no warning or error." : string.Join(" | ", problems);
    }

    private static string NewId() => Guid.NewGuid().ToString();

    private static async Task AddAsync(IServiceProvider services, string id, DateTime expiresAtUtc, bool processed, DateTime now)
    {
        var store = services.GetRequiredService<IInboxStore>();
        var message = services.GetRequiredService<IInboxMessageFactory>()
            .Create(id, "PurgeScenarioRequest", now.AddDays(-2), expiresAtUtc, metadata: null);

        (await store.AddAsync(message)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        if (processed)
        {
            (await store.MarkAsProcessedAsync(id, "ok")).ShouldBeRight();
            (await store.SaveChangesAsync()).ShouldBeRight();
        }
    }

    // Each round moves the fake clock past the start-up jitter (and so past the purge interval after the
    // first cycle), then looks at the store once; the real pause only yields to the service's background task.
    private static async Task<bool> WaitUntilGoneAsync(Harness harness, FakeTimeProvider clock, string id)
    {
        using var budget = new CancellationTokenSource(s_waitBudget);
        try
        {
            while (await harness.ReadRow(id) is not null)
            {
                clock.Advance(MessagePurgeService.MaxStartupJitter + TimeSpan.FromSeconds(1));
                await Task.Delay(s_pollPause, budget.Token);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
