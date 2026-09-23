using Encina.Messaging.Diagnostics;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Outbox;

/// <summary>
/// Background service that periodically delivers pending outbox messages through
/// <see cref="IEncina.Publish{TNotification}(TNotification, CancellationToken)"/>. Every provider's <c>OutboxProcessor</c>
/// derives from it, so the delivery, retry and exhaustion rules live in one place.
/// </summary>
/// <remarks>
/// <para>
/// Each cycle creates a scope, resolves the <see cref="IOutboxStore"/>
/// (see <see cref="ResolveOutboxStore"/>), <see cref="IEncina"/> and, when registered,
/// <see cref="IMessageSerializer"/>, and processes one batch of up to
/// <see cref="OutboxOptions.BatchSize"/> messages:
/// <list type="bullet">
/// <item><description>A <c>Right</c> from <c>Publish</c> marks the message processed.</description></item>
/// <item><description>A <c>Left</c> from <c>Publish</c>, a thrown exception, an unknown type or an
/// undeserializable payload marks it failed, with the next retry computed by
/// <see cref="OutboxRetryBackoff"/> (exponential, capped by <see cref="OutboxOptions.MaxRetryDelay"/>,
/// with <see cref="OutboxOptions.RetryJitterRatio"/> jitter).</description></item>
/// <item><description>When the failure uses up <see cref="OutboxOptions.MaxRetries"/>, the message is
/// logged with <see cref="OutboxErrorCodes.MaxRetriesExceeded"/> (EventId 2958) and counted in
/// <c>encina.outbox.processor.messages_total{outcome="exhausted"}</c>.</description></item>
/// <item><description>A cancellation (the host stopping, or <see cref="EncinaErrorCodes.NotificationCancelled"/>
/// from the dispatcher) stops the batch without marking the interrupted message failed.</description></item>
/// </list>
/// </para>
/// <para>
/// After the batch, the outcomes are committed with <see cref="IOutboxStore.SaveChangesAsync"/>. When the
/// save returns <c>Left</c>, nothing was persisted: the processor logs EventId 2960 instead of the batch
/// summary (2833) and counts every message of the batch as
/// <c>encina.outbox.processor.messages_total{outcome="unsaved"}</c>; the messages are delivered again in a
/// later cycle. The per-outcome counts are recorded only for a batch that was saved.
/// </para>
/// </remarks>
public abstract class OutboxProcessorBase : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly OutboxOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessorBase"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider used to create one scope per processing cycle.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="options">Configuration options for outbox processing.</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/>, <paramref name="logger"/> or <paramref name="options"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="OutboxOptions.MaxRetryDelay"/> is less than <see cref="OutboxOptions.BaseRetryDelay"/>.
    /// </exception>
    protected OutboxProcessorBase(
        IServiceProvider serviceProvider,
        ILogger logger,
        OutboxOptions options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate(nameof(options));

        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the time provider used for retry scheduling.
    /// </summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableProcessor)
        {
            MessagingLog.OutboxProcessorDisabled(_logger);
            return;
        }

        MessagingLog.OutboxProcessorStarted(_logger, _options.ProcessingInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                MessagingLog.ErrorProcessingOutboxMessages(_logger, ex);
            }

            await Task.Delay(_options.ProcessingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resolves the outbox store used for one processing cycle.
    /// </summary>
    /// <param name="scopedServices">The services of the cycle's scope.</param>
    /// <returns>The outbox store. The default implementation resolves <see cref="IOutboxStore"/>.</returns>
    protected virtual IOutboxStore ResolveOutboxStore(IServiceProvider scopedServices)
        => scopedServices.GetRequiredService<IOutboxStore>();

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var store = ResolveOutboxStore(services);
        var encina = services.GetRequiredService<IEncina>();
        var serializer = services.GetService<IMessageSerializer>();

        var batchProcessor = new OutboxBatchProcessor(store, _options, _logger, serializer, TimeProvider);

        var batchResult = await batchProcessor.ProcessAsync(
            (message, _, notification) => PublishAsync(encina, message, notification, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (batchResult.IsLeft)
        {
            var error = batchResult.LeftToArray()[0];
            // Only the error code: EncinaError.Message can carry personal data (#1259 review).
            MessagingLog.ErrorProcessingOutboxMessages(
                _logger,
                new InvalidOperationException($"Fetching pending outbox messages failed with error code {error.GetCode().IfNone("encina.unknown")}"));
            return;
        }

        var result = batchResult.Match(Right: r => r, Left: _ => default);
        if (result.Total == 0)
        {
            return;
        }

        // The outcomes are saved even when the host is stopping: they describe deliveries that
        // already happened, and dropping them would redeliver those messages.
        var saveResult = await store.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        if (saveResult.IsLeft)
        {
            var error = saveResult.LeftToArray()[0];
            OutboxProcessorMetrics.Instance.RecordUnsavedBatch(result);
            MessagingLog.OutboxBatchSaveFailed(_logger, result.Total, error.GetCode().IfNone("encina.unknown"));
            return;
        }

        OutboxProcessorMetrics.Instance.RecordBatch(result);
        MessagingLog.ProcessedOutboxMessages(_logger, result.Total, result.Succeeded, result.NotDelivered);
    }

    private static ValueTask<Either<EncinaError, Unit>> PublishAsync(
        IEncina encina,
        IOutboxMessage message,
        object notification,
        CancellationToken cancellationToken)
    {
        if (notification is not INotification typedNotification)
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create(
                OutboxErrorCodes.UnknownNotificationType,
                $"Type {message.NotificationType} does not implement {nameof(INotification)}"));
        }

        return encina.Publish(typedNotification, cancellationToken);
    }
}
