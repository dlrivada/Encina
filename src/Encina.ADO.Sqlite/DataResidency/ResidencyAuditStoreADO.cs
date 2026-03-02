using System.Data;
using System.Globalization;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.DataResidency;

/// <summary>
/// ADO.NET implementation of <see cref="IResidencyAuditStore"/> for SQLite.
/// Provides an immutable audit trail for data residency enforcement decisions
/// per GDPR Article 5(2) (accountability principle) and Article 30 (records of processing).
/// </summary>
/// <remarks>
/// <para>
/// This store persists <see cref="ResidencyAuditEntry"/> records using raw ADO.NET commands
/// against a SQLite database. DateTime values are stored as ISO 8601 TEXT using the
/// round-trip ("O") format specifier, enabling correct lexicographic ordering for
/// date-range queries.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of data residency compliance and may be required during regulatory audits or
/// supervisory authority inquiries (Article 58).
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
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
                INSERT INTO {_tableName}
                (Id, EntityId, DataCategory, SourceRegion, TargetRegion, ActionValue, OutcomeValue, LegalBasis, RequestType, UserId, TimestampUtc, Details)
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
            AddParameter(command, "@TimestampUtc", entity.TimestampUtc.ToString("O"));
            AddParameter(command, "@Details", entity.Details);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
                await ExecuteNonQueryAsync(command, cancellationToken);
                return Right(Unit.Default);
            }
            finally
            {
                if (!wasOpen) _connection.Close();
            }
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record residency audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entry.EntityId, ["action"] = entry.Action.ToString() }));
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
                SELECT Id, EntityId, DataCategory, SourceRegion, TargetRegion, ActionValue, OutcomeValue, LegalBasis, RequestType, UserId, TimestampUtc, Details
                FROM {_tableName}
                WHERE EntityId = @EntityId
                ORDER BY TimestampUtc DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
                SELECT Id, EntityId, DataCategory, SourceRegion, TargetRegion, ActionValue, OutcomeValue, LegalBasis, RequestType, UserId, TimestampUtc, Details
                FROM {_tableName}
                WHERE TimestampUtc >= @FromUtc AND TimestampUtc <= @ToUtc
                ORDER BY TimestampUtc";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@FromUtc", fromUtc.ToString("O"));
            AddParameter(command, "@ToUtc", toUtc.ToString("O"));

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
                SELECT Id, EntityId, DataCategory, SourceRegion, TargetRegion, ActionValue, OutcomeValue, LegalBasis, RequestType, UserId, TimestampUtc, Details
                FROM {_tableName}
                WHERE OutcomeValue = @OutcomeValue
                ORDER BY TimestampUtc DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@OutcomeValue", (int)ResidencyOutcome.Blocked);

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();
            try
            {
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
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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
            TimestampUtc = DateTimeOffset.Parse(reader.GetString(10), null, DateTimeStyles.RoundtripKind),
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

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqliteReader)
            return await sqliteReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
