using System.Security.Cryptography;
using Encina.Diagnostics;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Purge;

/// <summary>
/// Background service that purges expired messages of the patterns whose automatic purge is on
/// (today the inbox, <see cref="InboxOptions.EnableAutomaticPurge"/>).
/// </summary>
/// <remarks>
/// <para>
/// Each pattern runs its own loop: one cycle shortly after start-up, then one every purge interval. A cycle
/// removes expired messages in batches through the pattern's store until a batch comes back short. A store
/// <c>Left</c> or an exception fails that cycle only: it is logged by error code or exception type and
/// counted, and the next cycle still runs. No distributed lock is taken: the deletes are bounded and
/// idempotent, so several hosts can run the loop. The purge is deployment-wide and independent of any
/// processor being enabled.
/// </para>
/// <para>
/// Registered by <c>RegisterMessagePurge</c> from the shared messaging registration, only when at least one
/// pattern has its automatic purge on.
/// </para>
/// </remarks>
internal sealed class MessagePurgeService : BackgroundService
{
    /// <summary>
    /// The upper bound of the random pause before the first cycle, so hosts started together do not all
    /// purge in the same second.
    /// </summary>
    internal static readonly TimeSpan MaxStartupJitter = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessagePurgeSchedule _schedule;
    private readonly ILogger<MessagePurgeService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePurgeService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory (the stores are scoped).</param>
    /// <param name="schedule">The purge loops to run.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">
    /// Optional time provider for the delays. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public MessagePurgeService(
        IServiceScopeFactory scopeFactory,
        MessagePurgeSchedule schedule,
        ILogger<MessagePurgeService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _schedule = schedule;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_schedule.InboxPurge is not { } inbox)
        {
            MessagePurgeLog.NothingToPurge(_logger);
            return Task.CompletedTask;
        }

        return RunLoopAsync(MessagePurgeCycle.InboxPattern, inbox, PurgeInboxAsync, stoppingToken);
    }

    private static Task<Either<EncinaError, int>> PurgeInboxAsync(
        IServiceProvider services, int batchSize, CancellationToken cancellationToken)
        => MessagePurgeCycle.PurgeInboxAsync(
            services.GetRequiredService<IInboxStore>(), batchSize, cancellationToken);

    private async Task RunLoopAsync(
        string pattern,
        PurgeEntry entry,
        Func<IServiceProvider, int, CancellationToken, Task<Either<EncinaError, int>>> purge,
        CancellationToken stoppingToken)
    {
        MessagePurgeLog.Started(_logger, pattern, entry.Interval, entry.BatchSize);

        try
        {
            await Task.Delay(NextStartupJitter(), _timeProvider, stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCycleAsync(pattern, entry.BatchSize, purge, stoppingToken).ConfigureAwait(false);
                await Task.Delay(entry.Interval, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
    }

    private async Task RunCycleAsync(
        string pattern,
        int batchSize,
        Func<IServiceProvider, int, CancellationToken, Task<Either<EncinaError, int>>> purge,
        CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var result = await purge(scope.ServiceProvider, batchSize, stoppingToken).ConfigureAwait(false);

            result.Match(
                Right: count => ReportPurged(pattern, count),
                Left: error => ReportFailed(pattern, error));
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && stoppingToken.IsCancellationRequested))
        {
            MessagePurgeMetrics.RecordCycleFailure(pattern, ex.GetType().Name);
            MessagePurgeLog.CycleException(_logger, pattern, ex.ForLogging());
        }
    }

    private void ReportPurged(string pattern, int count)
    {
        if (count > 0)
        {
            MessagePurgeLog.Purged(_logger, pattern, count);
        }
    }

    // Only the error code: EncinaError.Message can carry personal data (AGENTS.md section 3).
    private void ReportFailed(string pattern, EncinaError error)
    {
        var errorCode = error.GetCode().IfNone("encina.unknown");
        MessagePurgeMetrics.RecordCycleFailure(pattern, errorCode);
        MessagePurgeLog.CycleFailed(_logger, pattern, errorCode);
    }

    private static TimeSpan NextStartupJitter()
        => TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(0, (int)MaxStartupJitter.TotalMilliseconds));
}
