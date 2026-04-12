using System.Diagnostics.CodeAnalysis;
using Encina.Messaging.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Scheduling;

/// <summary>
/// Background service that polls <see cref="IScheduledMessageStore"/> for due messages
/// and dispatches them through <see cref="IEncina"/> via the registered
/// <see cref="IScheduledMessageDispatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Processing algorithm</b>:
/// <list type="number">
/// <item><description>Create a DI scope so scoped services (orchestrator, dispatcher) are fresh per cycle.</description></item>
/// <item><description>Resolve <see cref="SchedulerOrchestrator"/> and <see cref="IScheduledMessageDispatcher"/> from the scope.</description></item>
/// <item><description>Call <see cref="SchedulerOrchestrator.ProcessDueMessagesAsync"/> with a callback that delegates to <see cref="IScheduledMessageDispatcher.DispatchAsync"/>.</description></item>
/// <item><description>The orchestrator handles deserialization, retry policy delegation, recurring rescheduling, and store updates. The processor owns only the outer loop timing and scope management.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Enablement gating</b>: the processor checks
/// <see cref="SchedulingOptions.EnableProcessor"/> at both DI registration time (Phase 4)
/// and at the start of <see cref="ExecuteAsync"/>. If disabled, the processor logs once
/// and returns without entering the loop.
/// </para>
/// <para>
/// <b>Exception isolation</b>: the outer loop catches all non-<see cref="OperationCanceledException"/>
/// exceptions to prevent crash-looping. Individual message failures are handled inside
/// the orchestrator via the <see cref="IScheduledMessageRetryPolicy"/>.
/// </para>
/// <para>
/// <b>Deferred cross-cutting integrations</b> (tracked as separate issues):
/// <list type="bullet">
/// <item><description><b>Distributed locks</b> — Without a lock, multi-replica deployments may process the same message concurrently. See Issue #716.</description></item>
/// <item><description><b>Idempotency</b> — A message dispatched successfully but failed-to-mark could be re-executed. See Issue #735.</description></item>
/// <item><description><b>Multi-tenancy</b> — <see cref="IScheduledMessage"/> does not carry TenantId yet. See Issue #739.</description></item>
/// <item><description><b>Audit trail</b> — Background processor audit events. See Issue #749.</description></item>
/// <item><description><b>Health check</b> — Processor lag health check deferred to a follow-up issue.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ScheduledMessageProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulingOptions _options;
    private readonly ILogger<ScheduledMessageProcessor> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SchedulingProcessorMetrics _metrics = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The root service provider. A new scope is created per processing cycle so that
    /// scoped services (<see cref="SchedulerOrchestrator"/>,
    /// <see cref="IScheduledMessageDispatcher"/>) are fresh per cycle.
    /// </param>
    /// <param name="options">
    /// The scheduling options controlling interval, batch size, and enablement.
    /// </param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/>, <paramref name="options"/>,
    /// or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public ScheduledMessageProcessor(
        IServiceProvider serviceProvider,
        SchedulingOptions options,
        ILogger<ScheduledMessageProcessor> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableProcessor)
        {
            SchedulingProcessorLog.ProcessorDisabled(_logger);
            return;
        }

        SchedulingProcessorLog.ProcessorStarting(
            _logger, _options.ProcessingInterval, _options.BatchSize, _options.MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — exit the loop
                break;
            }
#pragma warning disable CA1031 // Do not catch general exception types — intentional: prevent crash-looping
            catch (Exception ex)
#pragma warning restore CA1031
            {
                SchedulingProcessorLog.CycleFailed(_logger, ex);
            }

            try
            {
                await Task.Delay(_options.ProcessingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        SchedulingProcessorLog.ProcessorStopping(_logger);
    }

    /// <summary>
    /// Executes a single processing cycle: creates a scope, resolves the orchestrator
    /// and dispatcher, and processes all due messages.
    /// </summary>
    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        var cycleStart = _timeProvider.GetTimestamp();

        await using var scope = _serviceProvider.CreateAsyncScope();

        var orchestrator = scope.ServiceProvider.GetRequiredService<SchedulerOrchestrator>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IScheduledMessageDispatcher>();

        // Do NOT use 'using var' — CompleteProcessingCycle/Failed already dispose the activity.
        var activity = SchedulingActivitySource.StartProcessingCycle(_options.BatchSize);

        var result = await orchestrator.ProcessDueMessagesAsync(
            (msg, type, req, ct) => dispatcher.DispatchAsync(type, req, ct),
            cancellationToken).ConfigureAwait(false);

        var elapsed = _timeProvider.GetElapsedTime(cycleStart);
        _metrics.RecordCycleDuration(elapsed);

        result.Match(
            Right: count =>
            {
                if (count > 0)
                {
                    SchedulingProcessorLog.BatchCompleted(_logger, count);
                    _metrics.RecordBatch(successCount: count, failureCount: 0);
                }

                SchedulingActivitySource.CompleteProcessingCycle(activity, count);
            },
            Left: error =>
            {
                var errorCode = error.GetCode().IfNone("unknown");
                SchedulingProcessorLog.BatchFailed(_logger, errorCode, error.Message);
                // Do NOT record batch metrics here — Left means the store retrieval
                // itself failed, not that individual messages failed dispatch.
                // Per-message failures are tracked inside the orchestrator loop.

                SchedulingActivitySource.Failed(activity, errorCode, error.Message);
            });
    }
}
