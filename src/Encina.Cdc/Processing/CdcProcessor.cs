using Encina.Cdc.Abstractions;
using Encina.Cdc.DeadLetter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.Processing;

/// <summary>
/// Background service that continuously streams and processes CDC change events.
/// Consumes events from <see cref="ICdcConnector"/> and dispatches them to registered handlers
/// via <see cref="ICdcDispatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// The processor follows a poll-dispatch-save loop:
/// <list type="number">
///   <item><description>Stream changes from the connector</description></item>
///   <item><description>Dispatch each event to the appropriate handler</description></item>
///   <item><description>Save the position after successful processing</description></item>
/// </list>
/// </para>
/// <para>
/// Error handling uses exponential backoff retry with configurable
/// <see cref="CdcOptions.BaseRetryDelay"/> and <see cref="CdcOptions.MaxRetries"/>.
/// When retries are exhausted and a <see cref="ICdcDeadLetterStore"/> is configured,
/// the failed event is persisted for later inspection or replay.
/// </para>
/// </remarks>
internal sealed class CdcProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CdcProcessor> _logger;
    private readonly CdcOptions _options;
    private readonly ICdcDeadLetterStore? _deadLetterStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scopes.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="options">Configuration options for CDC processing.</param>
    /// <param name="deadLetterStore">
    /// Optional dead letter store for persisting events that fail after exhausting all retries.
    /// When <c>null</c>, failed events are logged and skipped.
    /// </param>
    public CdcProcessor(
        IServiceProvider serviceProvider,
        ILogger<CdcProcessor> logger,
        CdcOptions options,
        ICdcDeadLetterStore? deadLetterStore = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        _deadLetterStore = deadLetterStore;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            CdcLog.ProcessorDisabled(_logger);
            return;
        }

        CdcLog.ProcessorStarted(_logger, _options.PollingInterval, _options.BatchSize);

        var consecutiveErrors = 0;
        ChangeEvent? lastFailedEvent = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                lastFailedEvent = await ProcessChangesAsync(stoppingToken).ConfigureAwait(false);
                consecutiveErrors = 0;
                lastFailedEvent = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                consecutiveErrors++;

                if (consecutiveErrors <= _options.MaxRetries)
                {
                    var delay = CalculateRetryDelay(consecutiveErrors);

                    using var scope = _serviceProvider.CreateScope();
                    var connector = scope.ServiceProvider.GetService<ICdcConnector>();
                    var connectorId = connector?.ConnectorId ?? "unknown";

                    CdcLog.RetryingAfterError(_logger, ex, connectorId, consecutiveErrors, _options.MaxRetries, delay);
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    using var scope = _serviceProvider.CreateScope();
                    var connector = scope.ServiceProvider.GetService<ICdcConnector>();
                    var connectorId = connector?.ConnectorId ?? "unknown";

                    await PersistToDeadLetterAsync(
                        lastFailedEvent, ex, connectorId, stoppingToken).ConfigureAwait(false);

                    consecutiveErrors = 0;
                    lastFailedEvent = null;
                    await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
                }
            }

            await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        CdcLog.ProcessorStopped(_logger);
    }

    private async Task<ChangeEvent?> ProcessChangesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var connector = scope.ServiceProvider.GetRequiredService<ICdcConnector>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICdcDispatcher>();
        var positionStore = scope.ServiceProvider.GetRequiredService<ICdcPositionStore>();

        var successCount = 0;
        var failureCount = 0;
        var totalCount = 0;
        ChangeEvent? lastFailedEvent = null;

        await foreach (var changeResult in connector.StreamChangesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            totalCount++;

            if (changeResult.IsRight)
            {
                var changeEvent = (ChangeEvent)changeResult;

                var dispatchResult = await dispatcher.DispatchAsync(changeEvent, cancellationToken)
                    .ConfigureAwait(false);

                if (dispatchResult.IsRight)
                {
                    successCount++;

                    // Save position after each successful dispatch if tracking is enabled
                    if (_options.EnablePositionTracking)
                    {
                        await positionStore.SavePositionAsync(
                            connector.ConnectorId,
                            changeEvent.Metadata.Position,
                            cancellationToken).ConfigureAwait(false);

                        CdcLog.PositionSaved(
                            _logger,
                            connector.ConnectorId,
                            changeEvent.Metadata.Position.ToString());
                    }
                }
                else
                {
                    failureCount++;
                    lastFailedEvent = changeEvent;
                }
            }
            else
            {
                failureCount++;
            }

            if (totalCount >= _options.BatchSize)
            {
                break;
            }
        }

        if (totalCount > 0)
        {
            CdcLog.ProcessedChangeEvents(_logger, successCount, totalCount, failureCount, connector.ConnectorId);
        }

        return lastFailedEvent;
    }

    private async Task PersistToDeadLetterAsync(
        ChangeEvent? failedEvent,
        Exception exception,
        string connectorId,
        CancellationToken cancellationToken)
    {
        if (_deadLetterStore is null)
        {
            CdcLog.RetriesExhaustedNoDeadLetter(_logger, exception, connectorId);
            return;
        }

        var entry = new CdcDeadLetterEntry(
            Id: Guid.NewGuid(),
            OriginalEvent: failedEvent ?? CreatePlaceholderEvent(connectorId),
            ErrorMessage: exception.Message,
            StackTrace: exception.StackTrace ?? string.Empty,
            RetryCount: _options.MaxRetries,
            FailedAtUtc: DateTime.UtcNow,
            ConnectorId: connectorId,
            Status: CdcDeadLetterStatus.Pending);

        var result = await _deadLetterStore.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => CdcLog.EventDeadLettered(
                _logger, entry.OriginalEvent.TableName, connectorId, _options.MaxRetries, entry.Id),
            Left: _ => CdcLog.DeadLetterStoreFailed(_logger, exception, connectorId));
    }

    private static ChangeEvent CreatePlaceholderEvent(string connectorId)
        => new(
            TableName: "unknown",
            Operation: ChangeOperation.Insert,
            Before: null,
            After: null,
            Metadata: new ChangeMetadata(
                Position: new PlaceholderPosition(),
                CapturedAtUtc: DateTime.UtcNow,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: null));

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        var delay = _options.BaseRetryDelay * Math.Pow(2, retryCount - 1);
        return delay;
    }

    /// <summary>
    /// Minimal position implementation used when no real event is available.
    /// </summary>
    private sealed class PlaceholderPosition : CdcPosition
    {
        public override int CompareTo(CdcPosition? other) => 0;
        public override byte[] ToBytes() => [];
        public override string ToString() => "placeholder";
    }
}
