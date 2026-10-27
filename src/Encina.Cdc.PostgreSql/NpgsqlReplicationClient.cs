using System.Runtime.CompilerServices;
using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;
using NpgsqlTypes;

namespace Encina.Cdc.PostgreSql;

/// <summary>
/// Npgsql-backed <see cref="IPostgresReplicationClient"/>: runs the SQL, opens the
/// <see cref="LogicalReplicationConnection"/> and decodes pgoutput messages into
/// <see cref="ReplicationRecord"/> values.
/// </summary>
internal sealed class NpgsqlReplicationClient : IPostgresReplicationClient
{
    private readonly PostgresCdcOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NpgsqlReplicationClient"/> class.
    /// </summary>
    /// <param name="options">PostgreSQL CDC options.</param>
    public NpgsqlReplicationClient(PostgresCdcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <inheritdoc />
    public async Task<string?> GetCurrentWalLsnAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_current_wal_lsn()::text";

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return result is null or DBNull ? null : result.ToString();
    }

    /// <inheritdoc />
    public async Task EnsurePublicationAsync(
        string publicationName,
        string[] tables,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Check if publication exists
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT 1 FROM pg_publication WHERE pubname = @name";
        checkCommand.Parameters.AddWithValue("name", publicationName);

        var exists = await checkCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (exists is not null)
        {
            return;
        }

        // Create publication
        var publicationTables = tables.Length > 0
            ? string.Join(", ", tables)
            : "ALL TABLES";

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE PUBLICATION {publicationName} FOR {publicationTables}";
        await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ReplicationRecord> StreamAsync(
        NpgsqlLogSequenceNumber? startLsn,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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
                yield return await DecodeAsync(message).ConfigureAwait(false);

                // Acknowledge the WAL position once the consumer has taken the record
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

    private static async Task<ReplicationRecord> DecodeAsync(PgOutputReplicationMessage message)
    {
        try
        {
            switch (message)
            {
                case RelationMessage:
                    return new RelationRecord(message.WalEnd);

                case InsertMessage insert:
                    return new InsertRecord(
                        insert.WalEnd,
                        insert.Relation.Namespace,
                        insert.Relation.RelationName,
                        await ReadTupleDataAsync(insert.NewRow, insert.Relation).ConfigureAwait(false));

                case UpdateMessage update:
                    return await DecodeUpdateAsync(update).ConfigureAwait(false);

                case DeleteMessage delete:
                    return await DecodeDeleteAsync(delete).ConfigureAwait(false);

                default:
                    return new SkippedRecord(message.WalEnd);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new DecodeFailureRecord(message.WalEnd, ex);
        }
    }

    private static async Task<UpdateRecord> DecodeUpdateAsync(UpdateMessage message)
    {
        // The new row is read first, as before this seam existed (defect #2776 depends on that order).
        var after = await ReadTupleDataAsync(message.NewRow, message.Relation).ConfigureAwait(false);

        // OldRow is only available when REPLICA IDENTITY is FULL or USING INDEX
        var before = message switch
        {
            FullUpdateMessage full => await ReadTupleDataAsync(full.OldRow, message.Relation)
                .ConfigureAwait(false),
            IndexUpdateMessage index => await ReadTupleDataAsync(index.Key, message.Relation)
                .ConfigureAwait(false),
            _ => null
        };

        return new UpdateRecord(
            message.WalEnd, message.Relation.Namespace, message.Relation.RelationName, before, after);
    }

    private static async Task<DeleteRecord> DecodeDeleteAsync(DeleteMessage message)
    {
        // OldRow/Key contains the old values depending on REPLICA IDENTITY setting
        var before = message switch
        {
            FullDeleteMessage full => await ReadTupleDataAsync(full.OldRow, message.Relation)
                .ConfigureAwait(false),
            KeyDeleteMessage key => await ReadTupleDataAsync(key.Key, message.Relation)
                .ConfigureAwait(false),
            _ => null
        };

        return new DeleteRecord(
            message.WalEnd, message.Relation.Namespace, message.Relation.RelationName, before);
    }

    private static async Task<IReadOnlyDictionary<string, string?>> ReadTupleDataAsync(
        ReplicationTuple tuple,
        RelationMessage relation)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

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

        return data;
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
