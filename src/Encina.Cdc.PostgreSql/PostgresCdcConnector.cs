using System.Runtime.CompilerServices;
using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using static LanguageExt.Prelude;

namespace Encina.Cdc.PostgreSql;

/// <summary>
/// CDC connector for PostgreSQL using Logical Replication with the pgoutput plugin.
/// Streams database changes by subscribing to a replication slot.
/// </summary>
/// <remarks>
/// <para>
/// The connector consumes the WAL stream through an <see cref="IPostgresReplicationClient"/>, whose
/// Npgsql-backed implementation uses <c>LogicalReplicationConnection</c> and the pgoutput plugin,
/// and maps the decoded <see cref="InsertRecord"/>, <see cref="UpdateRecord"/> and
/// <see cref="DeleteRecord"/> values to <see cref="ChangeEvent"/> instances.
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
    private readonly IPostgresReplicationClient _replicationClient;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCdcConnector"/> class.
    /// </summary>
    /// <param name="options">PostgreSQL CDC options.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="replicationClient">The client that talks to PostgreSQL through Npgsql.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public PostgresCdcConnector(
        PostgresCdcOptions options,
        ICdcPositionStore positionStore,
        ILogger<PostgresCdcConnector> logger,
        IPostgresReplicationClient replicationClient,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(replicationClient);

        _options = options;
        _positionStore = positionStore;
        _logger = logger;
        _replicationClient = replicationClient;
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
            var result = await _replicationClient.GetCurrentWalLsnAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
            {
                return Left(CdcErrors.ConnectionFailed(
                    "Failed to get current WAL LSN. Ensure wal_level = logical is configured."));
            }

            var lsn = NpgsqlLogSequenceNumber.Parse(result);
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

        await foreach (var record in _replicationClient.StreamAsync(startLsn, cancellationToken)
            .ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var changeEvent = MapRecord(record);
            if (changeEvent is not null)
            {
                yield return changeEvent.Value;
            }

            // The client acknowledges the WAL position when this iteration resumes.
        }
    }

    // crap-exempt: single-question switch — one type pattern answers "which change event, if any, does this record map to"
    private Either<EncinaError, ChangeEvent>? MapRecord(ReplicationRecord record) => record switch
    {
        InsertRecord insert => Map(
            record, insert.SchemaName, insert.RelationName, ChangeOperation.Insert, before: null, insert.After),
        UpdateRecord update => Map(
            record, update.SchemaName, update.RelationName, ChangeOperation.Update, update.Before, update.After),
        DeleteRecord delete => Map(
            record, delete.SchemaName, delete.RelationName, ChangeOperation.Delete, delete.Before, after: null),
        DecodeFailureRecord failure => Left<EncinaError, ChangeEvent>(CdcErrors.StreamInterrupted(failure.Error)),
        _ => default(Either<EncinaError, ChangeEvent>?)
    };

    private Either<EncinaError, ChangeEvent> Map(
        ReplicationRecord record,
        string? schemaName,
        string relationName,
        ChangeOperation operation,
        IReadOnlyDictionary<string, string?>? before,
        IReadOnlyDictionary<string, string?>? after)
    {
        try
        {
            var metadata = new ChangeMetadata(
                new PostgresCdcPosition(record.WalEnd),
                _timeProvider.GetUtcNow().UtcDateTime,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: schemaName);

            return Right<EncinaError, ChangeEvent>(new ChangeEvent(
                GetTableName(schemaName, relationName),
                operation,
                ToJson(before),
                ToJson(after),
                metadata));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static JsonElement? ToJson(IReadOnlyDictionary<string, string?>? values) =>
        values is null ? null : JsonSerializer.SerializeToElement(values);

    private static string GetTableName(string? schemaName, string relationName)
    {
        return string.IsNullOrEmpty(schemaName)
            ? relationName
            : $"{schemaName}.{relationName}";
    }

    private async Task<Either<EncinaError, LanguageExt.Unit>> EnsurePublicationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _replicationClient.EnsurePublicationAsync(
                _options.PublicationName, _options.PublicationTables, cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, LanguageExt.Unit>(unit);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed(
                $"Failed to ensure publication '{_options.PublicationName}' exists", ex));
        }
    }
}
