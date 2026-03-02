using System.Data;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IResidencyAuditStore"/> for PostgreSQL.
/// Provides an immutable audit trail for data residency enforcement decisions,
/// cross-border transfer validations, and policy violations per GDPR Articles 44-49.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 5(2) (accountability principle) requires controllers to demonstrate
/// compliance with data protection principles. This store records all residency enforcement
/// decisions as immutable audit entries, supporting regulatory audits and supervisory
/// authority inquiries (Article 58).
/// </para>
/// <para>
/// Cross-border transfers are governed by GDPR Chapter V (Articles 44-49):
/// Article 44 (general principle), Article 45 (adequacy decisions),
/// Article 46 (appropriate safeguards), Article 47 (binding corporate rules),
/// Article 48 (transfers not authorised by Union law), and
/// Article 49 (derogations for specific situations).
/// </para>
/// <para>
/// Uses lowercase column names without quotes for PostgreSQL compatibility.
/// DateTime values are written via <c>.UtcDateTime</c> and read back using
/// <c>new DateTimeOffset(reader.GetDateTime(ordinal), TimeSpan.Zero)</c>.
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
    /// <remarks>
    /// Records a new entry in the residency audit trail. The entry is mapped to a
    /// persistence entity using <see cref="ResidencyAuditEntryMapper.ToEntity"/> before
    /// insertion. Audit entries should never be modified or deleted as they serve as
    /// legal evidence of data residency compliance per GDPR Article 5(2).
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ResidencyAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ResidencyAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details)
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
    /// <remarks>
    /// Retrieves the complete residency audit trail for a specific data entity, ordered
    /// by timestamp descending (most recent first). Provides a complete history of
    /// residency actions for accountability demonstrations per GDPR Article 5(2).
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details
                FROM {_tableName}
                WHERE entityid = @EntityId
                ORDER BY timestamputc DESC";

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
                message: $"Failed to get residency audit trail by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Retrieves residency audit entries within a specific date range (inclusive boundaries),
    /// ordered by timestamp ascending. Useful for generating periodic compliance reports and
    /// for responding to supervisory authority inquiries about data transfers during a specific
    /// time period per GDPR Article 58.
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details
                FROM {_tableName}
                WHERE timestamputc >= @FromUtc AND timestamputc <= @ToUtc
                ORDER BY timestamputc";

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
    /// <remarks>
    /// Retrieves all residency audit entries where the outcome was <see cref="ResidencyOutcome.Blocked"/>
    /// (value 1), ordered by timestamp descending. Violations represent attempts to store or transfer
    /// data to non-compliant regions that were blocked by the residency enforcement system.
    /// Provides a quick overview of all compliance incidents for security reviews, DPO reporting,
    /// and supervisory authority inquiries under GDPR Articles 44-49.
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details
                FROM {_tableName}
                WHERE outcomevalue = @OutcomeValue
                ORDER BY timestamputc DESC";

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
                message: $"Failed to get residency audit violations: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static ResidencyAuditEntryEntity MapToEntity(IDataReader reader)
    {
        return new ResidencyAuditEntryEntity
        {
            Id = reader.GetString(0),
            EntityId = reader.IsDBNull(1) ? null : reader.GetString(1),
            DataCategory = reader.GetString(2),
            SourceRegion = reader.GetString(3),
            TargetRegion = reader.IsDBNull(4) ? null : reader.GetString(4),
            ActionValue = reader.GetInt32(5),
            OutcomeValue = reader.GetInt32(6),
            LegalBasis = reader.IsDBNull(7) ? null : reader.GetString(7),
            RequestType = reader.IsDBNull(8) ? null : reader.GetString(8),
            UserId = reader.IsDBNull(9) ? null : reader.GetString(9),
            TimestampUtc = new DateTimeOffset(reader.GetDateTime(10), TimeSpan.Zero),
            Details = reader.IsDBNull(11) ? null : reader.GetString(11)
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
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
