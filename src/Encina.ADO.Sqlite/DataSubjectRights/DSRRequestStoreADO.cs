using System.Data;
using System.Globalization;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.DataSubjectRights;

/// <summary>
/// ADO.NET implementation of <see cref="IDSRRequestStore"/> for SQLite.
/// Uses raw SqliteCommand and SqliteDataReader for maximum performance.
/// </summary>
public sealed class DSRRequestStoreADO : IDSRRequestStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DSRRequestStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The DSR requests table name (default: DSRRequests).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DSRRequestStoreADO(
        IDbConnection connection,
        string tableName = "DSRRequests",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        DSRRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var entity = DSRRequestMapper.ToEntity(request);
            entity.Id = request.Id; // Preserve original Id, don't generate new one

            var sql = $@"
                INSERT INTO {_tableName}
                (Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId)
                VALUES
                (@Id, @SubjectId, @RightTypeValue, @StatusValue, @ReceivedAtUtc, @DeadlineAtUtc, @CompletedAtUtc, @ExtensionReason, @ExtendedDeadlineAtUtc, @RejectionReason, @RequestDetails, @VerifiedAtUtc, @ProcessedByUserId)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddEntityParameters(command, entity);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("Create", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DSRRequest>>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var sql = $@"
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", id);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var domain = MapToDomain(reader);
                return domain is not null
                    ? Right<EncinaError, Option<DSRRequest>>(Some(domain))
                    : Right<EncinaError, Option<DSRRequest>>(None);
            }

            return Right<EncinaError, Option<DSRRequest>>(None);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetById", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetBySubjectIdAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var sql = $@"
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE SubjectId = @SubjectId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DSRRequest>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var domain = MapToDomain(reader);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(results);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetBySubjectId", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string id,
        DSRRequestStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();

            // For Extended status, we need the current DeadlineAtUtc to calculate the extended deadline
            string sql;
            Action<IDbCommand> addParams;

            switch (newStatus)
            {
                case DSRRequestStatus.Completed:
                    sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, CompletedAtUtc = @NowUtc WHERE Id = @Id";
                    addParams = cmd =>
                    {
                        AddParameter(cmd, "@StatusValue", (int)newStatus);
                        AddParameter(cmd, "@NowUtc", nowUtc.ToString("O"));
                        AddParameter(cmd, "@Id", id);
                    };
                    break;

                case DSRRequestStatus.Rejected:
                    sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, RejectionReason = @Reason, CompletedAtUtc = @NowUtc WHERE Id = @Id";
                    addParams = cmd =>
                    {
                        AddParameter(cmd, "@StatusValue", (int)newStatus);
                        AddParameter(cmd, "@Reason", reason);
                        AddParameter(cmd, "@NowUtc", nowUtc.ToString("O"));
                        AddParameter(cmd, "@Id", id);
                    };
                    break;

                case DSRRequestStatus.Extended:
                    {
                        // First read the current deadline
                        var selectSql = $"SELECT DeadlineAtUtc FROM {_tableName} WHERE Id = @Id";
                        using var selectCmd = _connection.CreateCommand();
                        selectCmd.CommandText = selectSql;
                        AddParameter(selectCmd, "@Id", id);

                        if (_connection.State != ConnectionState.Open)
                            await OpenConnectionAsync(cancellationToken);

                        using var selectReader = await ExecuteReaderAsync(selectCmd, cancellationToken);
                        if (!await ReadAsync(selectReader, cancellationToken))
                        {
                            return Left(DSRErrors.RequestNotFound(id));
                        }

                        var deadlineStr = selectReader.GetString(0);
                        var deadline = DateTimeOffset.Parse(deadlineStr, null, DateTimeStyles.RoundtripKind);
                        var extendedDeadline = deadline.AddMonths(2);

                        sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, ExtensionReason = @Reason, ExtendedDeadlineAtUtc = @ExtendedDeadline WHERE Id = @Id";
                        addParams = cmd =>
                        {
                            AddParameter(cmd, "@StatusValue", (int)newStatus);
                            AddParameter(cmd, "@Reason", reason);
                            AddParameter(cmd, "@ExtendedDeadline", extendedDeadline.ToString("O"));
                            AddParameter(cmd, "@Id", id);
                        };
                        break;
                    }

                case DSRRequestStatus.IdentityVerified:
                    sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, VerifiedAtUtc = @NowUtc WHERE Id = @Id";
                    addParams = cmd =>
                    {
                        AddParameter(cmd, "@StatusValue", (int)newStatus);
                        AddParameter(cmd, "@NowUtc", nowUtc.ToString("O"));
                        AddParameter(cmd, "@Id", id);
                    };
                    break;

                default:
                    sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue WHERE Id = @Id";
                    addParams = cmd =>
                    {
                        AddParameter(cmd, "@StatusValue", (int)newStatus);
                        AddParameter(cmd, "@Id", id);
                    };
                    break;
            }

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            addParams(command);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken);
            if (rowsAffected == 0)
            {
                return Left(DSRErrors.RequestNotFound(id));
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("UpdateStatus", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE StatusValue IN (@Received, @IdentityVerified, @InProgress, @Extended)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Received", (int)DSRRequestStatus.Received);
            AddParameter(command, "@IdentityVerified", (int)DSRRequestStatus.IdentityVerified);
            AddParameter(command, "@InProgress", (int)DSRRequestStatus.InProgress);
            AddParameter(command, "@Extended", (int)DSRRequestStatus.Extended);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DSRRequest>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var domain = MapToDomain(reader);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(results);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetPendingRequests", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE StatusValue IN (@Received, @IdentityVerified, @InProgress, @Extended)
                  AND COALESCE(ExtendedDeadlineAtUtc, DeadlineAtUtc) < @NowUtc";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Received", (int)DSRRequestStatus.Received);
            AddParameter(command, "@IdentityVerified", (int)DSRRequestStatus.IdentityVerified);
            AddParameter(command, "@InProgress", (int)DSRRequestStatus.InProgress);
            AddParameter(command, "@Extended", (int)DSRRequestStatus.Extended);
            AddParameter(command, "@NowUtc", nowUtc.ToString("O"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DSRRequest>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var domain = MapToDomain(reader);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(results);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetOverdueRequests", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var sql = $@"
                SELECT COUNT(1)
                FROM {_tableName}
                WHERE SubjectId = @SubjectId
                  AND RightTypeValue = @RestrictionValue
                  AND StatusValue IN (@Received, @IdentityVerified, @InProgress, @Extended)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);
            AddParameter(command, "@RestrictionValue", (int)DataSubjectRight.Restriction);
            AddParameter(command, "@Received", (int)DSRRequestStatus.Received);
            AddParameter(command, "@IdentityVerified", (int)DSRRequestStatus.IdentityVerified);
            AddParameter(command, "@InProgress", (int)DSRRequestStatus.InProgress);
            AddParameter(command, "@Extended", (int)DSRRequestStatus.Extended);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var count = reader.GetInt32(0);
                return Right(count > 0);
            }

            return Right(false);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("HasActiveRestriction", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DSRRequest>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var domain = MapToDomain(reader);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(results);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAll", ex.Message));
        }
    }

    private static void AddEntityParameters(IDbCommand command, DSRRequestEntity entity)
    {
        AddParameter(command, "@Id", entity.Id);
        AddParameter(command, "@SubjectId", entity.SubjectId);
        AddParameter(command, "@RightTypeValue", entity.RightTypeValue);
        AddParameter(command, "@StatusValue", entity.StatusValue);
        AddParameter(command, "@ReceivedAtUtc", entity.ReceivedAtUtc.ToString("O"));
        AddParameter(command, "@DeadlineAtUtc", entity.DeadlineAtUtc.ToString("O"));
        AddParameter(command, "@CompletedAtUtc", entity.CompletedAtUtc?.ToString("O"));
        AddParameter(command, "@ExtensionReason", entity.ExtensionReason);
        AddParameter(command, "@ExtendedDeadlineAtUtc", entity.ExtendedDeadlineAtUtc?.ToString("O"));
        AddParameter(command, "@RejectionReason", entity.RejectionReason);
        AddParameter(command, "@RequestDetails", entity.RequestDetails);
        AddParameter(command, "@VerifiedAtUtc", entity.VerifiedAtUtc?.ToString("O"));
        AddParameter(command, "@ProcessedByUserId", entity.ProcessedByUserId);
    }

    private static DSRRequest? MapToDomain(IDataReader reader)
    {
        var entity = new DSRRequestEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            SubjectId = reader.GetString(reader.GetOrdinal("SubjectId")),
            RightTypeValue = reader.GetInt32(reader.GetOrdinal("RightTypeValue")),
            StatusValue = reader.GetInt32(reader.GetOrdinal("StatusValue")),
            ReceivedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("ReceivedAtUtc")), null, DateTimeStyles.RoundtripKind),
            DeadlineAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("DeadlineAtUtc")), null, DateTimeStyles.RoundtripKind),
            CompletedAtUtc = reader.IsDBNull(reader.GetOrdinal("CompletedAtUtc"))
                ? null
                : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("CompletedAtUtc")), null, DateTimeStyles.RoundtripKind),
            ExtensionReason = reader.IsDBNull(reader.GetOrdinal("ExtensionReason"))
                ? null
                : reader.GetString(reader.GetOrdinal("ExtensionReason")),
            ExtendedDeadlineAtUtc = reader.IsDBNull(reader.GetOrdinal("ExtendedDeadlineAtUtc"))
                ? null
                : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("ExtendedDeadlineAtUtc")), null, DateTimeStyles.RoundtripKind),
            RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                ? null
                : reader.GetString(reader.GetOrdinal("RejectionReason")),
            RequestDetails = reader.IsDBNull(reader.GetOrdinal("RequestDetails"))
                ? null
                : reader.GetString(reader.GetOrdinal("RequestDetails")),
            VerifiedAtUtc = reader.IsDBNull(reader.GetOrdinal("VerifiedAtUtc"))
                ? null
                : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("VerifiedAtUtc")), null, DateTimeStyles.RoundtripKind),
            ProcessedByUserId = reader.IsDBNull(reader.GetOrdinal("ProcessedByUserId"))
                ? null
                : reader.GetString(reader.GetOrdinal("ProcessedByUserId"))
        };

        return DSRRequestMapper.ToDomain(entity);
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
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
