using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Debezium;

/// <summary>
/// Maps Debezium JSON events into <see cref="ChangeEvent"/> instances.
/// Supports both CloudEvents and Flat (plain Debezium envelope) formats.
/// </summary>
/// <remarks>
/// <para>
/// Debezium op codes are mapped as follows:
/// <list type="bullet">
///   <item><description><c>c</c> (create) and <c>r</c> (read/snapshot) → <see cref="ChangeOperation.Insert"/></description></item>
///   <item><description><c>u</c> (update) → <see cref="ChangeOperation.Update"/></description></item>
///   <item><description><c>d</c> (delete) → <see cref="ChangeOperation.Delete"/></description></item>
/// </list>
/// </para>
/// <para>
/// Events without an <c>op</c> field (e.g., Debezium schema change events / DDL) are
/// skipped with a warning log and returned as a <see cref="Left{EncinaError}"/>.
/// </para>
/// </remarks>
internal static class DebeziumEventMapper
{
    /// <summary>
    /// Maps a raw Debezium JSON event into a <see cref="ChangeEvent"/>.
    /// </summary>
    /// <param name="eventJson">The raw JSON element received from Debezium.</param>
    /// <param name="format">The expected event format (CloudEvents or Flat).</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <returns>Either an error or a mapped change event.</returns>
    public static Either<EncinaError, ChangeEvent> MapEvent(
        JsonElement eventJson,
        DebeziumEventFormat format,
        ILogger logger)
    {
        return format switch
        {
            DebeziumEventFormat.CloudEvents => ParseCloudEvent(eventJson, logger),
            DebeziumEventFormat.Flat => ParseFlatEvent(eventJson, logger),
            _ => ParseCloudEvent(eventJson, logger)
        };
    }

    private static Either<EncinaError, ChangeEvent> ParseCloudEvent(
        JsonElement eventJson,
        ILogger logger)
    {
        try
        {
            // CloudEvents envelope: type, source, data
            var data = eventJson.TryGetProperty("data", out var dataElement)
                ? dataElement
                : eventJson;

            return ParseDebeziumPayload(data, logger);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static Either<EncinaError, ChangeEvent> ParseFlatEvent(
        JsonElement eventJson,
        ILogger logger)
    {
        try
        {
            return ParseDebeziumPayload(eventJson, logger);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static Either<EncinaError, ChangeEvent> ParseDebeziumPayload(
        JsonElement payload,
        ILogger logger)
    {
        // Check if 'op' field exists — missing op indicates a schema change / DDL event
        if (!payload.TryGetProperty("op", out var opElement) ||
            opElement.ValueKind == JsonValueKind.Null ||
            opElement.ValueKind == JsonValueKind.Undefined)
        {
            DebeziumCdcLog.SchemaChangeEventSkipped(logger);
            return Left(CdcErrors.DeserializationFailed(
                "unknown",
                typeof(ChangeEvent),
                new InvalidOperationException("Debezium event missing 'op' field (schema change / DDL event)")));
        }

        var opCode = opElement.GetString();
        if (string.IsNullOrEmpty(opCode))
        {
            DebeziumCdcLog.SchemaChangeEventSkipped(logger);
            return Left(CdcErrors.DeserializationFailed(
                "unknown",
                typeof(ChangeEvent),
                new InvalidOperationException("Debezium event has empty 'op' field")));
        }

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
                         beforeElement.ValueKind != JsonValueKind.Null &&
                         beforeElement.ValueKind != JsonValueKind.Undefined
            ? beforeElement
            : null;

        object? after = payload.TryGetProperty("after", out var afterElement) &&
                        afterElement.ValueKind != JsonValueKind.Null &&
                        afterElement.ValueKind != JsonValueKind.Undefined
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
