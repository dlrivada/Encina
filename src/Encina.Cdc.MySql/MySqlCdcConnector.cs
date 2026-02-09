using System.Runtime.CompilerServices;
using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using MySqlCdc;
using MySqlCdc.Constants;
using MySqlCdc.Events;
using static LanguageExt.Prelude;

namespace Encina.Cdc.MySql;

/// <summary>
/// CDC connector for MySQL using Binary Log Replication.
/// Streams database changes by consuming binlog events via the MySqlCdc library.
/// </summary>
/// <remarks>
/// <para>
/// This connector requires:
/// <list type="bullet">
///   <item><description><c>binlog_format=ROW</c> (default in MySQL 8+)</description></item>
///   <item><description><c>REPLICATION SLAVE</c> and <c>REPLICATION CLIENT</c> privileges</description></item>
/// </list>
/// </para>
/// <para>
/// Supports both GTID-based and file/position-based replication modes.
/// </para>
/// </remarks>
internal sealed class MySqlCdcConnector : ICdcConnector
{
    private readonly MySqlCdcOptions _options;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<MySqlCdcConnector> _logger;
    private readonly Dictionary<long, TableMapEvent> _tableMap = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlCdcConnector"/> class.
    /// </summary>
    /// <param name="options">MySQL CDC options.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public MySqlCdcConnector(
        MySqlCdcOptions options,
        ICdcPositionStore positionStore,
        ILogger<MySqlCdcConnector> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _positionStore = positionStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ConnectorId => "encina-cdc-mysql";

    /// <inheritdoc />
    public async Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new MySqlConnector.MySqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "SHOW MASTER STATUS";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return Left(CdcErrors.ConnectionFailed(
                    "Binary logging is not enabled. Enable it with: SET GLOBAL log_bin = ON"));
            }

            var file = reader.GetString(reader.GetOrdinal("File"));
            var position = reader.GetInt64(reader.GetOrdinal("Position"));

            return Right<EncinaError, CdcPosition>(new MySqlCdcPosition(file, position));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed("Failed to get MySQL binlog position", ex));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new BinlogClient(options =>
        {
            options.Hostname = _options.Hostname;
            options.Port = _options.Port;
            options.Username = _options.Username;
            options.Password = _options.Password;
            options.ServerId = _options.ServerId;
            options.Blocking = true;
        });

        // Restore position
        var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
            .ConfigureAwait(false);

        if (positionResult.IsRight)
        {
            var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
            optPosition.IfSome(position =>
            {
                if (position is MySqlCdcPosition mysqlPosition)
                {
                    if (mysqlPosition.GtidSet is not null)
                    {
                        // GTID mode: set starting GTID
                        CdcLog.PositionRestored(_logger, ConnectorId, position.ToString());
                    }
                    else if (mysqlPosition.BinlogFileName is not null)
                    {
                        // File/position mode
                        CdcLog.PositionRestored(_logger, ConnectorId, position.ToString());
                    }
                }
            });
        }
        else
        {
            CdcLog.NoSavedPosition(_logger, ConnectorId);
        }

        await foreach (var (header, binlogEvent) in client.Replicate(cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var result = ProcessBinlogEvent(binlogEvent);
            if (result is not null)
            {
                foreach (var changeEvent in result)
                {
                    yield return changeEvent;
                }
            }
        }
    }

    private IEnumerable<Either<EncinaError, ChangeEvent>>? ProcessBinlogEvent(IBinlogEvent binlogEvent)
    {
        switch (binlogEvent)
        {
            case TableMapEvent tableMap:
                _tableMap[tableMap.TableId] = tableMap;
                return null;

            case WriteRowsEvent writeRows:
                return MapWriteRowsEvent(writeRows);

            case UpdateRowsEvent updateRows:
                return MapUpdateRowsEvent(updateRows);

            case DeleteRowsEvent deleteRows:
                return MapDeleteRowsEvent(deleteRows);

            default:
                return null;
        }
    }

    private IEnumerable<Either<EncinaError, ChangeEvent>> MapWriteRowsEvent(WriteRowsEvent writeRows)
    {
        var tableName = GetTableName(writeRows.TableId);

        foreach (var row in writeRows.Rows)
        {
            var position = CreatePosition();
            var afterData = ConvertRowToJsonElement(row.Cells);

            var metadata = new ChangeMetadata(
                position,
                DateTime.UtcNow,
                TransactionId: null,
                SourceDatabase: GetDatabaseName(writeRows.TableId),
                SourceSchema: null);

            yield return Right<EncinaError, ChangeEvent>(
                new ChangeEvent(tableName, ChangeOperation.Insert, Before: null, afterData, metadata));
        }
    }

    private IEnumerable<Either<EncinaError, ChangeEvent>> MapUpdateRowsEvent(UpdateRowsEvent updateRows)
    {
        var tableName = GetTableName(updateRows.TableId);

        foreach (var row in updateRows.Rows)
        {
            var position = CreatePosition();
            var beforeData = ConvertRowToJsonElement(row.BeforeUpdate.Cells);
            var afterData = ConvertRowToJsonElement(row.AfterUpdate.Cells);

            var metadata = new ChangeMetadata(
                position,
                DateTime.UtcNow,
                TransactionId: null,
                SourceDatabase: GetDatabaseName(updateRows.TableId),
                SourceSchema: null);

            yield return Right<EncinaError, ChangeEvent>(
                new ChangeEvent(tableName, ChangeOperation.Update, beforeData, afterData, metadata));
        }
    }

    private IEnumerable<Either<EncinaError, ChangeEvent>> MapDeleteRowsEvent(DeleteRowsEvent deleteRows)
    {
        var tableName = GetTableName(deleteRows.TableId);

        foreach (var row in deleteRows.Rows)
        {
            var position = CreatePosition();
            var beforeData = ConvertRowToJsonElement(row.Cells);

            var metadata = new ChangeMetadata(
                position,
                DateTime.UtcNow,
                TransactionId: null,
                SourceDatabase: GetDatabaseName(deleteRows.TableId),
                SourceSchema: null);

            yield return Right<EncinaError, ChangeEvent>(
                new ChangeEvent(tableName, ChangeOperation.Delete, beforeData, After: null, metadata));
        }
    }

    private MySqlCdcPosition CreatePosition()
    {
        // Return a generic position since we don't have file/position from the event directly
        return _options.UseGtid
            ? new MySqlCdcPosition("current")
            : new MySqlCdcPosition("binlog", 0);
    }

    private string GetTableName(long tableId)
    {
        if (_tableMap.TryGetValue(tableId, out var tableMap))
        {
            return $"{tableMap.DatabaseName}.{tableMap.TableName}";
        }

        return $"unknown.table_{tableId}";
    }

    private string? GetDatabaseName(long tableId)
    {
        return _tableMap.TryGetValue(tableId, out var tableMap)
            ? tableMap.DatabaseName
            : null;
    }

    private static JsonElement ConvertRowToJsonElement(IReadOnlyList<object?> cells)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < cells.Count; i++)
        {
            data[$"column_{i}"] = cells[i];
        }

        return JsonSerializer.SerializeToElement(data);
    }
}
