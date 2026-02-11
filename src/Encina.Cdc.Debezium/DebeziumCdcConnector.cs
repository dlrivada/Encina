using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Debezium;

/// <summary>
/// CDC connector for Debezium Server's HTTP Client sink.
/// Reads change events from an internal <see cref="Channel{T}"/> that is populated
/// by the <see cref="DebeziumHttpListener"/> hosted service.
/// </summary>
/// <remarks>
/// <para>
/// This connector does not directly connect to any database. Instead, it receives
/// pre-processed change events from Debezium Server via HTTP POST, which are written
/// to a <see cref="Channel{T}"/> by <see cref="DebeziumHttpListener"/>.
/// </para>
/// <para>
/// On startup, the connector retrieves the last saved position from the
/// <see cref="ICdcPositionStore"/> and skips events that have already been processed,
/// enabling resume-from-position after restarts.
/// </para>
/// <para>
/// Event parsing is delegated to <see cref="DebeziumEventMapper"/> which supports
/// both CloudEvents and Flat formats with proper validation.
/// </para>
/// </remarks>
internal sealed class DebeziumCdcConnector : ICdcConnector
{
    private readonly DebeziumCdcOptions _options;
    private readonly ChannelReader<JsonElement> _channelReader;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<DebeziumCdcConnector> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumCdcConnector"/> class.
    /// </summary>
    /// <param name="options">Debezium CDC options.</param>
    /// <param name="channel">The channel providing Debezium events.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public DebeziumCdcConnector(
        DebeziumCdcOptions options,
        Channel<JsonElement> channel,
        ICdcPositionStore positionStore,
        ILogger<DebeziumCdcConnector> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _channelReader = channel.Reader;
        _positionStore = positionStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string ConnectorId => "encina-cdc-debezium";

    /// <inheritdoc />
    public async Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
                .ConfigureAwait(false);

            if (positionResult.IsRight)
            {
                var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
                if (optPosition.IsSome)
                {
                    return Right<EncinaError, CdcPosition>((CdcPosition)optPosition);
                }
            }

            return Right<EncinaError, CdcPosition>(
                new DebeziumCdcPosition("{\"status\":\"awaiting_events\"}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed("Failed to get Debezium position", ex));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Retrieve saved position for resume-from-position logic
        var resumePosition = await GetResumePositionAsync(cancellationToken).ConfigureAwait(false);
        var passedResumePoint = resumePosition is null;

        await foreach (var eventJson in _channelReader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var result = DebeziumEventMapper.MapEvent(eventJson, _options.EventFormat, _logger, _timeProvider);

            // Skip events that were already processed before restart
            if (!passedResumePoint && result.IsRight)
            {
                var changeEvent = (ChangeEvent)result;
                if (changeEvent.Metadata.Position is DebeziumCdcPosition pos &&
                    pos.CompareTo(resumePosition) <= 0)
                {
                    DebeziumCdcLog.EventSkippedAlreadyProcessed(_logger);
                    continue;
                }

                passedResumePoint = true;
            }

            yield return result;
        }
    }

    /// <summary>
    /// Retrieves the last saved position from the position store for resume logic.
    /// Returns <c>null</c> if no saved position exists or if retrieval fails.
    /// </summary>
    private async Task<DebeziumCdcPosition?> GetResumePositionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
                .ConfigureAwait(false);

            if (positionResult.IsRight)
            {
                var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
                if (optPosition.IsSome)
                {
                    var position = (CdcPosition)optPosition;
                    if (position is DebeziumCdcPosition debeziumPosition)
                    {
                        DebeziumCdcLog.ResumingFromPosition(_logger, debeziumPosition.ToString());
                        return debeziumPosition;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log but don't fail — start from beginning if position retrieval fails
            DebeziumCdcLog.PositionRetrievalFailed(_logger, ex, ConnectorId);
        }

        CdcLog.NoSavedPosition(_logger, ConnectorId);
        return null;
    }
}
