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
/// service run on a <see cref="FakeTimeProvider"/>, and the test waits on the store state (no
/// <c>Thread.Sleep</c>; a short <c>Task.Delay</c> yields between clock advances).
/// </summary>
public static class MessagePurgeScenario
{
    // Real-time ceiling for slow CI; the fake clock makes the purge itself immediate.
    private static readonly TimeSpan s_waitBudget = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan s_timerBudget = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_pollPause = TimeSpan.FromMilliseconds(25);

    /// <summary>The ids of the three seeded rows.</summary>
    private sealed record Seeded(string ProcessedExpired, string UnprocessedExpired, string ProcessedNotExpired);

    /// <summary>One purge cycle removes only the processed-expired inbox row.</summary>
    /// <param name="harness">The provider family under test (its own registration and a fresh-connection reader).</param>
    public static async Task AssertCycleRemovesOnlyProcessedExpiredRowsAsync(Harness harness)
    {
        var clock = new CountingTimeProvider(new FakeTimeProvider(DateTimeOffset.UtcNow));
        await using var provider = BuildProvider(harness, clock);
        var seeded = await SeedAsync(provider, clock.GetUtcNow().UtcDateTime);

        var service = provider.GetServices<IHostedService>().OfType<MessagePurgeService>().Single();
        var timersBefore = clock.TimersCreated;
        await service.StartAsync(CancellationToken.None);
        try
        {
            // The service must schedule its start-up delay as a timer of the injected provider
            // (Task.Delay(delay, timeProvider) calls CreateTimer; the host may run ExecuteAsync on another
            // thread, so the timer is awaited with a short bound). A service that ignored the injected
            // TimeProvider creates none, so it fails here with a clear message, whatever its real jitter is.
            (await WaitForTimerAsync(clock, timersBefore)).ShouldBeTrue(
                "the purge service must schedule its start-up delay on the injected TimeProvider");

            // The fake clock has not moved, so no cycle may have run.
            (await harness.ReadRow(seeded.ProcessedExpired)).ShouldNotBeNull("no purge may run before the fake clock advances");

            var purged = await WaitUntilGoneAsync(harness, provider, clock, seeded.ProcessedExpired);
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

    /// <summary>
    /// Delegates every member to the fake clock and counts the timers created on it, which is the signal that
    /// the service under test scheduled its delays on the injected provider.
    /// </summary>
    private sealed class CountingTimeProvider(FakeTimeProvider inner) : TimeProvider
    {
        private int _timersCreated;

        public int TimersCreated => Volatile.Read(ref _timersCreated);

        public void Advance(TimeSpan delta) => inner.Advance(delta);

        public override DateTimeOffset GetUtcNow() => inner.GetUtcNow();

        public override TimeZoneInfo LocalTimeZone => inner.LocalTimeZone;

        public override long TimestampFrequency => inner.TimestampFrequency;

        public override long GetTimestamp() => inner.GetTimestamp();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Interlocked.Increment(ref _timersCreated);
            return inner.CreateTimer(callback, state, dueTime, period);
        }
    }

    private static ServiceProvider BuildProvider(Harness harness, TimeProvider clock)
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

    private static async Task<bool> WaitForTimerAsync(CountingTimeProvider clock, int timersBefore)
    {
        using var budget = new CancellationTokenSource(s_timerBudget);
        try
        {
            while (clock.TimersCreated <= timersBefore)
            {
                await Task.Delay(s_pollPause, budget.Token);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // Each round moves the fake clock past the start-up jitter plus the purge interval (so also past the pause
    // after the first cycle), then looks at the store once; the real pause only yields to the service's task.
    private static async Task<bool> WaitUntilGoneAsync(
        Harness harness, ServiceProvider provider, CountingTimeProvider clock, string id)
    {
        var step = MessagePurgeService.MaxStartupJitter + provider.GetRequiredService<InboxOptions>().PurgeInterval;
        using var budget = new CancellationTokenSource(s_waitBudget);
        try
        {
            while (await harness.ReadRow(id) is not null)
            {
                clock.Advance(step);
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
