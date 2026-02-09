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
/// Debezium op codes are mapped as follows:
/// <list type="bullet">
///   <item><description><c>c</c> (create) and <c>r</c> (read/snapshot) → <see cref="ChangeOperation.Insert"/></description></item>
///   <item><description><c>u</c> (update) → <see cref="ChangeOperation.Update"/></description></item>
///   <item><description><c>d</c> (delete) → <see cref="ChangeOperation.Delete"/></description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DebeziumCdcConnector : ICdcConnector
{
    private readonly DebeziumCdcOptions _options;
    private readonly ChannelReader<JsonElement> _channelReader;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<DebeziumCdcConnector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumCdcConnector"/> class.
    /// </summary>
    /// <param name="options">Debezium CDC options.</param>
    /// <param name="channel">The channel providing Debezium events.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public DebeziumCdcConnector(
        DebeziumCdcOptions options,
        Channel<JsonElement> channel,
        ICdcPositionStore positionStore,
        ILogger<DebeziumCdcConnector> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _channelReader = channel.Reader;
        _positionStore = positionStore;
        _logger = logger;
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
        CdcLog.NoSavedPosition(_logger, ConnectorId);

        await foreach (var eventJson in _channelReader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return _options.EventFormat switch
            {
                DebeziumEventFormat.CloudEvents => ParseCloudEvent(eventJson),
                DebeziumEventFormat.Flat => ParseFlatEvent(eventJson),
                _ => ParseCloudEvent(eventJson)
            };
        }
    }

    private static Either<EncinaError, ChangeEvent> ParseCloudEvent(JsonElement eventJson)
    {
        try
        {
            // CloudEvents envelope: type, source, data
            var data = eventJson.TryGetProperty("data", out var dataElement)
                ? dataElement
                : eventJson;

            return ParseDebeziumPayload(data);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static Either<EncinaError, ChangeEvent> ParseFlatEvent(JsonElement eventJson)
    {
        try
        {
            return ParseDebeziumPayload(eventJson);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static Either<EncinaError, ChangeEvent> ParseDebeziumPayload(JsonElement payload)
    {
        // Debezium envelope: { "before": {...}, "after": {...}, "source": {...}, "op": "c|u|d|r" }
        var opCode = payload.TryGetProperty("op", out var opElement)
            ? opElement.GetString() ?? "c"
            : "c";

        var operation = MapDebeziumOperation(opCode);

        // Extract source metadata
        string? database = null;
        string? schema = null;
        string? table = "unknown";
        string? offsetJson = null;

        if (payload.TryGetProperty("source", out var source))
        {
            if (source.TryGetProperty("db", out var db))
            {
                database = db.GetString();
            }

            if (source.TryGetProperty("schema", out var schemaElement))
            {
                schema = schemaElement.GetString();
            }

            if (source.TryGetProperty("table", out var tableElement))
            {
                table = tableElement.GetString() ?? "unknown";
            }

            offsetJson = source.GetRawText();
        }

        var tableName = string.IsNullOrEmpty(schema)
            ? table
            : $"{schema}.{table}";

        var position = new DebeziumCdcPosition(offsetJson ?? "{\"op\":\"" + opCode + "\"}");
        var metadata = new ChangeMetadata(
            position,
            DateTime.UtcNow,
            TransactionId: null,
            SourceDatabase: database,
            SourceSchema: schema);

        object? before = payload.TryGetProperty("before", out var beforeElement) &&
                         beforeElement.ValueKind != JsonValueKind.Null
            ? beforeElement
            : null;

        object? after = payload.TryGetProperty("after", out var afterElement) &&
                        afterElement.ValueKind != JsonValueKind.Null
            ? afterElement
            : null;

        return Right<EncinaError, ChangeEvent>(
            new ChangeEvent(tableName!, operation, before, after, metadata));
    }

    private static ChangeOperation MapDebeziumOperation(string opCode) => opCode switch
    {
        "c" => ChangeOperation.Insert,
        "r" => ChangeOperation.Snapshot,
        "u" => ChangeOperation.Update,
        "d" => ChangeOperation.Delete,
        _ => ChangeOperation.Insert
    };
}
