using System.Runtime.CompilerServices;
using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;
using NpgsqlTypes;
using static LanguageExt.Prelude;

namespace Encina.Cdc.PostgreSql;

/// <summary>
/// CDC connector for PostgreSQL using Logical Replication with the pgoutput plugin.
/// Streams database changes by subscribing to a replication slot.
/// </summary>
/// <remarks>
/// <para>
/// This connector uses Npgsql's <see cref="LogicalReplicationConnection"/> to consume
/// the WAL stream via the pgoutput plugin. It handles <see cref="InsertMessage"/>,
/// <see cref="UpdateMessage"/>, <see cref="DeleteMessage"/>, and
/// <see cref="RelationMessage"/> for column metadata.
/// </para>
/// <para>
/// Tables require <c>REPLICA IDENTITY FULL</c> for before-values on updates/deletes.
/// </para>
/// </remarks>
internal sealed class PostgresCdcConnector : ICdcConnector
{
    private readonly PostgresCdcOptions _options;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<PostgresCdcConnector> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<uint, RelationMessage> _relations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCdcConnector"/> class.
    /// </summary>
    /// <param name="options">PostgreSQL CDC options.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public PostgresCdcConnector(
        PostgresCdcOptions options,
        ICdcPositionStore positionStore,
        ILogger<PostgresCdcConnector> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _positionStore = positionStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string ConnectorId => "encina-cdc-postgresql";

    /// <inheritdoc />
    public async Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_current_wal_lsn()::text";

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            if (result is null or DBNull)
            {
                return Left(CdcErrors.ConnectionFailed(
                    "Failed to get current WAL LSN. Ensure wal_level = logical is configured."));
            }

            var lsn = NpgsqlLogSequenceNumber.Parse(result.ToString()!);
            return Right<EncinaError, CdcPosition>(new PostgresCdcPosition(lsn));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed("Failed to connect to PostgreSQL for CDC", ex));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        NpgsqlLogSequenceNumber? startLsn = null;

        // Try to restore position
        var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
            .ConfigureAwait(false);

        if (positionResult.IsRight)
        {
            var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
            optPosition.IfSome(position =>
            {
                if (position is PostgresCdcPosition pgPosition)
                {
                    startLsn = pgPosition.Lsn;
                    CdcLog.PositionRestored(_logger, ConnectorId, position.ToString());
                }
            });
        }

        if (startLsn is null)
        {
            CdcLog.NoSavedPosition(_logger, ConnectorId);
        }

        // Setup replication
        if (_options.CreatePublicationIfNotExists)
        {
            var pubResult = await EnsurePublicationAsync(cancellationToken).ConfigureAwait(false);
            if (pubResult.IsLeft)
            {
                var error = pubResult.Match(_ => default!, Left: e => e);
                yield return Left(error);
                yield break;
            }
        }

        LogicalReplicationConnection? replicationConnection = null;

        try
        {
            replicationConnection = new LogicalReplicationConnection(_options.ConnectionString);
            await replicationConnection.Open(cancellationToken).ConfigureAwait(false);

            if (_options.CreateSlotIfNotExists)
            {
                await EnsureReplicationSlotAsync(replicationConnection, cancellationToken)
                    .ConfigureAwait(false);
            }

            var slot = new PgOutputReplicationSlot(_options.ReplicationSlotName);
            var replicationOptions = new PgOutputReplicationOptions(
                _options.PublicationName,
                protocolVersion: PgOutputProtocolVersion.V1,
                binary: null,
                streamingMode: null,
                messages: null,
                twoPhase: null);

            await foreach (var message in replicationConnection.StartReplication(
                slot, replicationOptions, cancellationToken, startLsn).ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                var changeEvent = await ProcessMessageAsync(message).ConfigureAwait(false);
                if (changeEvent is not null)
                {
                    yield return changeEvent.Value;
                }

                // Acknowledge the WAL position
                replicationConnection.SetReplicationStatus(message.WalEnd);
            }
        }
        finally
        {
            if (replicationConnection is not null)
            {
                await replicationConnection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task<Either<EncinaError, ChangeEvent>?> ProcessMessageAsync(
        PgOutputReplicationMessage message)
    {
        switch (message)
        {
            case RelationMessage relation:
                _relations[relation.RelationId] = relation;
                return null;

            case InsertMessage insert:
                return await MapInsertMessageAsync(insert).ConfigureAwait(false);

            case UpdateMessage update:
                return await MapUpdateMessageAsync(update).ConfigureAwait(false);

            case DeleteMessage delete:
                return await MapDeleteMessageAsync(delete).ConfigureAwait(false);

            default:
                return null;
        }
    }

    private async Task<Either<EncinaError, ChangeEvent>> MapInsertMessageAsync(InsertMessage message)
    {
        try
        {
            var tableName = GetTableName(message.Relation);
            var position = new PostgresCdcPosition(message.WalEnd);
            var after = await ReadTupleDataAsync(message.NewRow, message.Relation).ConfigureAwait(false);

            var metadata = new ChangeMetadata(
                position,
                _timeProvider.GetUtcNow().UtcDateTime,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: message.Relation.Namespace);

            return Right<EncinaError, ChangeEvent>(
                new ChangeEvent(tableName, ChangeOperation.Insert, Before: null, after, metadata));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private async Task<Either<EncinaError, ChangeEvent>> MapUpdateMessageAsync(UpdateMessage message)
    {
        try
        {
            var tableName = GetTableName(message.Relation);
            var position = new PostgresCdcPosition(message.WalEnd);
            var after = await ReadTupleDataAsync(message.NewRow, message.Relation).ConfigureAwait(false);

            // OldRow is only available when REPLICA IDENTITY is FULL or USING INDEX
            JsonElement? before = message switch
            {
                FullUpdateMessage full => await ReadTupleDataAsync(full.OldRow, message.Relation)
                    .ConfigureAwait(false),
                IndexUpdateMessage index => await ReadTupleDataAsync(index.Key, message.Relation)
                    .ConfigureAwait(false),
                _ => null
            };

            var metadata = new ChangeMetadata(
                position,
                _timeProvider.GetUtcNow().UtcDateTime,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: message.Relation.Namespace);

            return Right<EncinaError, ChangeEvent>(
                new ChangeEvent(tableName, ChangeOperation.Update, before, after, metadata));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private async Task<Either<EncinaError, ChangeEvent>> MapDeleteMessageAsync(DeleteMessage message)
    {
        try
        {
            var tableName = GetTableName(message.Relation);
            var position = new PostgresCdcPosition(message.WalEnd);

            // OldRow/Key contains the old values depending on REPLICA IDENTITY setting
            JsonElement? before = message switch
            {
                FullDeleteMessage full => await ReadTupleDataAsync(full.OldRow, message.Relation)
                    .ConfigureAwait(false),
                KeyDeleteMessage key => await ReadTupleDataAsync(key.Key, message.Relation)
                    .ConfigureAwait(false),
                _ => null
            };

            var metadata = new ChangeMetadata(
                position,
                _timeProvider.GetUtcNow().UtcDateTime,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: message.Relation.Namespace);

            return Right<EncinaError, ChangeEvent>(
                new ChangeEvent(tableName, ChangeOperation.Delete, before, After: null, metadata));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static async Task<JsonElement> ReadTupleDataAsync(ReplicationTuple tuple, RelationMessage relation)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var columnIndex = 0;
        await foreach (var value in tuple.ConfigureAwait(false))
        {
            var columnName = columnIndex < relation.Columns.Count
                ? relation.Columns[columnIndex].ColumnName
                : $"column_{columnIndex}";

            data[columnName] = value.IsDBNull
                ? null
                : await value.Get<string>().ConfigureAwait(false);

            columnIndex++;
        }

        return JsonSerializer.SerializeToElement(data);
    }

    private static string GetTableName(RelationMessage relation)
    {
        return string.IsNullOrEmpty(relation.Namespace)
            ? relation.RelationName
            : $"{relation.Namespace}.{relation.RelationName}";
    }

    private async Task<Either<EncinaError, LanguageExt.Unit>> EnsurePublicationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Check if publication exists
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT 1 FROM pg_publication WHERE pubname = @name";
            checkCommand.Parameters.AddWithValue("name", _options.PublicationName);

            var exists = await checkCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            if (exists is not null)
            {
                return Right<EncinaError, LanguageExt.Unit>(unit);
            }

            // Create publication
            var tables = _options.PublicationTables.Length > 0
                ? string.Join(", ", _options.PublicationTables)
                : "ALL TABLES";

            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = $"CREATE PUBLICATION {_options.PublicationName} FOR {tables}";
            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, LanguageExt.Unit>(unit);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed(
                $"Failed to ensure publication '{_options.PublicationName}' exists", ex));
        }
    }

    private static async Task EnsureReplicationSlotAsync(
        LogicalReplicationConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            await connection.CreatePgOutputReplicationSlot(
                "encina_cdc_slot",
                slotSnapshotInitMode: LogicalSlotSnapshotInitMode.Export,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (ex.SqlState == "42710")
        {
            // Slot already exists - this is fine
        }
    }
}
