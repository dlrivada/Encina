using System.Data;

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;

using LanguageExt;

using MySqlConnector;

using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IResidencyAuditStore"/> for MySQL.
/// Provides an immutable audit trail for GDPR Article 5(2) accountability.
/// </summary>
/// <remarks>
/// <para>
/// Records all residency enforcement decisions, cross-border transfer validations,
/// region routing outcomes, and policy violations. Audit entries should never be
/// modified or deleted as they serve as legal evidence of data residency compliance.
/// </para>
/// <para>
/// Uses <see cref="MySqlConnector"/> for native async MySQL operations and
/// <see cref="ResidencyAuditEntryMapper"/> for domain-entity type transformations.
/// </para>
/// </remarks>
public sealed class ResidencyAuditStoreADO : IResidencyAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The residency audit entries table name (default: ResidencyAuditEntries).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public ResidencyAuditStoreADO(
        IDbConnection connection,
        string tableName = "ResidencyAuditEntries",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ResidencyAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ResidencyAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO `{_tableName}`
                (`Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`)
                VALUES
                (@Id, @EntityId, @DataCategory, @SourceRegion, @TargetRegion, @ActionValue, @OutcomeValue, @LegalBasis, @RequestType, @UserId, @TimestampUtc, @Details)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@EntityId", entity.EntityId);
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@SourceRegion", entity.SourceRegion);
            AddParameter(command, "@TargetRegion", entity.TargetRegion);
            AddParameter(command, "@ActionValue", entity.ActionValue);
            AddParameter(command, "@OutcomeValue", entity.OutcomeValue);
            AddParameter(command, "@LegalBasis", entity.LegalBasis);
            AddParameter(command, "@RequestType", entity.RequestType);
            AddParameter(command, "@UserId", entity.UserId);
            AddParameter(command, "@TimestampUtc", entity.TimestampUtc.UtcDateTime);
            AddParameter(command, "@Details", entity.Details);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record residency audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entry.EntityId, ["action"] = entry.Action }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT `Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`
                FROM `{_tableName}`
                WHERE `EntityId` = @EntityId
                ORDER BY `TimestampUtc` DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<ResidencyAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ResidencyAuditEntryMapper.ToDomain(entity);
                if (domain is not null)
                    entries.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get residency audit entries by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT `Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`
                FROM `{_tableName}`
                WHERE `TimestampUtc` >= @FromUtc AND `TimestampUtc` <= @ToUtc
                ORDER BY `TimestampUtc`";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@FromUtc", fromUtc.UtcDateTime);
            AddParameter(command, "@ToUtc", toUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<ResidencyAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ResidencyAuditEntryMapper.ToDomain(entity);
                if (domain is not null)
                    entries.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get residency audit entries by date range: {ex.Message}",
                details: new Dictionary<string, object?> { ["fromUtc"] = fromUtc, ["toUtc"] = toUtc }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT `Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`
                FROM `{_tableName}`
                WHERE `OutcomeValue` = @OutcomeValue
                ORDER BY `TimestampUtc` DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@OutcomeValue", (int)ResidencyOutcome.Blocked);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<ResidencyAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ResidencyAuditEntryMapper.ToDomain(entity);
                if (domain is not null)
                    entries.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get residency violation entries: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static ResidencyAuditEntryEntity MapToEntity(IDataReader reader)
    {
        var entityIdOrd = reader.GetOrdinal("EntityId");
        var targetRegionOrd = reader.GetOrdinal("TargetRegion");
        var legalBasisOrd = reader.GetOrdinal("LegalBasis");
        var requestTypeOrd = reader.GetOrdinal("RequestType");
        var userIdOrd = reader.GetOrdinal("UserId");
        var detailsOrd = reader.GetOrdinal("Details");

        return new ResidencyAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            EntityId = reader.IsDBNull(entityIdOrd) ? null : reader.GetString(entityIdOrd),
            DataCategory = reader.GetString(reader.GetOrdinal("DataCategory")),
            SourceRegion = reader.GetString(reader.GetOrdinal("SourceRegion")),
            TargetRegion = reader.IsDBNull(targetRegionOrd) ? null : reader.GetString(targetRegionOrd),
            ActionValue = reader.GetInt32(reader.GetOrdinal("ActionValue")),
            OutcomeValue = reader.GetInt32(reader.GetOrdinal("OutcomeValue")),
            LegalBasis = reader.IsDBNull(legalBasisOrd) ? null : reader.GetString(legalBasisOrd),
            RequestType = reader.IsDBNull(requestTypeOrd) ? null : reader.GetString(requestTypeOrd),
            UserId = reader.IsDBNull(userIdOrd) ? null : reader.GetString(userIdOrd),
            TimestampUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("TimestampUtc")), TimeSpan.Zero),
            Details = reader.IsDBNull(detailsOrd) ? null : reader.GetString(detailsOrd)
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader mysqlReader)
            return await mysqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
