using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.SqlServer;

/// <summary>
/// CDC connector for SQL Server using Change Tracking.
/// Polls <c>CHANGETABLE(CHANGES ...)</c> to stream database changes as events.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server Change Tracking is a lightweight mechanism that tracks which rows
/// have changed in a table. It requires enabling Change Tracking at both the
/// database and table level.
/// </para>
/// <para>
/// <b>Limitation:</b> Change Tracking does NOT store old column values.
/// For Update operations, only the After value is available.
/// For Delete operations, only primary key columns are available.
/// </para>
/// </remarks>
internal sealed class SqlServerCdcConnector : ICdcConnector
{
    private readonly SqlServerCdcOptions _options;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<SqlServerCdcConnector> _logger;
    private readonly TimeProvider _timeProvider;
    private long _lastVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerCdcConnector"/> class.
    /// </summary>
    /// <param name="options">SQL Server CDC options.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public SqlServerCdcConnector(
        SqlServerCdcOptions options,
        ICdcPositionStore positionStore,
        ILogger<SqlServerCdcConnector> logger,
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
    public string ConnectorId => "encina-cdc-sqlserver";

    /// <inheritdoc />
    public async Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            if (result is null or DBNull)
            {
                return Left(CdcErrors.ConnectionFailed(
                    "Change Tracking is not enabled on the database. Enable it with: ALTER DATABASE [db] SET CHANGE_TRACKING = ON"));
            }

            var version = Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
            return Right<EncinaError, CdcPosition>(new SqlServerCdcPosition(version));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed("Failed to get current Change Tracking version", ex));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await InitializeVersionAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tableName in _options.TrackedTables)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var (schema, table) = ParseTableName(tableName);

            await foreach (var result in PollTableChangesAsync(schema, table, cancellationToken).ConfigureAwait(false))
            {
                yield return result;
            }
        }
    }

    private async Task InitializeVersionAsync(CancellationToken cancellationToken)
    {
        if (_lastVersion > 0)
        {
            return;
        }

        if (_options.StartFromVersion.HasValue)
        {
            _lastVersion = _options.StartFromVersion.Value;
            CdcLog.PositionRestored(_logger, ConnectorId, $"CT-Version:{_lastVersion}");
            return;
        }

        var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
            .ConfigureAwait(false);

        positionResult.Match(
            optPosition =>
            {
                optPosition.Match(
                    position =>
                    {
                        if (position is SqlServerCdcPosition sqlPosition)
                        {
                            _lastVersion = sqlPosition.Version;
                            CdcLog.PositionRestored(_logger, ConnectorId, position.ToString());
                        }
                    },
                    () => CdcLog.NoSavedPosition(_logger, ConnectorId));
            },
            _ => CdcLog.NoSavedPosition(_logger, ConnectorId));
    }

    private async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> PollTableChangesAsync(
        string schema,
        string table,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SqlConnection? connection = null;

        try
        {
            connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Validate the version is still valid
            var minValidVersion = await GetMinValidVersionAsync(connection, schema, table, cancellationToken)
                .ConfigureAwait(false);

            if (minValidVersion.HasValue && _lastVersion < minValidVersion.Value)
            {
                SqlServerCdcLog.VersionBelowMinimum(
                    _logger, _lastVersion, minValidVersion.Value, schema, table);
                _lastVersion = minValidVersion.Value;
            }

            var sql = BuildChangeTrackingQuery(schema, table);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@lastVersion", _lastVersion);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var changeEvent = ReadChangeEvent(reader, schema, table);
                yield return changeEvent;
            }
        }
        finally
        {
            if (connection is not null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<long?> GetMinValidVersionAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{schema}.{table}'))";

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (result is null or DBNull)
        {
            return null;
        }

        return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string BuildChangeTrackingQuery(string schema, string table)
    {
        // Join CHANGETABLE with the base table to get current column values
        // For Insert/Update: gets all current column values
        // For Delete: only CT columns are available (PK + operation metadata)
        return $"""
            SELECT ct.SYS_CHANGE_VERSION,
                   ct.SYS_CHANGE_OPERATION,
                   ct.SYS_CHANGE_COLUMNS,
                   ct.SYS_CHANGE_CONTEXT,
                   t.*
            FROM CHANGETABLE(CHANGES [{schema}].[{table}], @lastVersion) AS ct
            LEFT JOIN [{schema}].[{table}] AS t
                ON ct.SYS_CHANGE_PRIMARY_KEY_COLUMNS_PLACEHOLDER = t.SYS_CHANGE_PRIMARY_KEY_COLUMNS_PLACEHOLDER
            ORDER BY ct.SYS_CHANGE_VERSION
            """;
    }

    private Either<EncinaError, ChangeEvent> ReadChangeEvent(
        SqlDataReader reader,
        string schema,
        string table)
    {
        try
        {
            var version = reader.GetInt64(reader.GetOrdinal("SYS_CHANGE_VERSION"));
            var operationChar = reader.GetString(reader.GetOrdinal("SYS_CHANGE_OPERATION"));

            var operation = MapOperation(operationChar);
            var fullTableName = $"{schema}.{table}";

            // Build After value as a dictionary of column values from the joined table
            var afterData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                if (columnName.StartsWith("SYS_CHANGE_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                afterData[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            var position = new SqlServerCdcPosition(version);
            var metadata = new ChangeMetadata(
                position,
                _timeProvider.GetUtcNow().UtcDateTime,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: schema);

            // Change Tracking limitation: Before values are not available
            // For Delete, the joined table row is null, so afterData will have nulls
            object? before = operation == ChangeOperation.Delete ? JsonSerializer.SerializeToElement(afterData) : null;
            object? after = operation != ChangeOperation.Delete ? JsonSerializer.SerializeToElement(afterData) : null;

            var changeEvent = new ChangeEvent(fullTableName, operation, before, after, metadata);

            _lastVersion = version;

            return Right<EncinaError, ChangeEvent>(changeEvent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static ChangeOperation MapOperation(string operation) => operation switch
    {
        "I" => ChangeOperation.Insert,
        "U" => ChangeOperation.Update,
        "D" => ChangeOperation.Delete,
        _ => ChangeOperation.Insert
    };

    private (string Schema, string Table) ParseTableName(string tableName)
    {
        var parts = tableName.Split('.', 2);
        return parts.Length == 2
            ? (parts[0], parts[1])
            : (_options.SchemaName, parts[0]);
    }
}
