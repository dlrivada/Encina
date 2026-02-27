using System.Data;

using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;

using LanguageExt;

using MySqlConnector;

using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.DataSubjectRights;

/// <summary>
/// ADO.NET implementation of <see cref="IDSRRequestStore"/> for MySQL.
/// Uses raw MySqlCommand and MySqlDataReader for maximum performance.
/// </summary>
/// <remarks>
/// <para>
/// Stores GDPR Data Subject Rights requests (Articles 15-22) in a MySQL table.
/// All SQL column names use PascalCase identifiers to match MySQL conventions.
/// </para>
/// <para>
/// Timestamps are stored as <c>DATETIME(6)</c> and converted between <see cref="DateTimeOffset"/>
/// and <see cref="DateTime"/> (UTC) for parameter binding.
/// </para>
/// </remarks>
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
                var entity = MapToEntity(reader);
                var domain = DSRRequestMapper.ToDomain(entity);
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
                var entity = MapToEntity(reader);
                var domain = DSRRequestMapper.ToDomain(entity);
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

            switch (newStatus)
            {
                case DSRRequestStatus.Completed:
                    {
                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue,
                            CompletedAtUtc = @NowUtc
                        WHERE Id = @Id";

                        using var command = _connection.CreateCommand();
                        command.CommandText = sql;
                        AddParameter(command, "@Id", id);
                        AddParameter(command, "@StatusValue", (int)newStatus);
                        AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);

                        if (_connection.State != ConnectionState.Open)
                            await OpenConnectionAsync(cancellationToken);

                        var rows = await ExecuteNonQueryAsync(command, cancellationToken);
                        if (rows == 0)
                            return Left(DSRErrors.RequestNotFound(id));

                        return Right(Unit.Default);
                    }

                case DSRRequestStatus.Rejected:
                    {
                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue,
                            RejectionReason = @RejectionReason,
                            CompletedAtUtc = @NowUtc
                        WHERE Id = @Id";

                        using var command = _connection.CreateCommand();
                        command.CommandText = sql;
                        AddParameter(command, "@Id", id);
                        AddParameter(command, "@StatusValue", (int)newStatus);
                        AddParameter(command, "@RejectionReason", (object?)reason ?? DBNull.Value);
                        AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);

                        if (_connection.State != ConnectionState.Open)
                            await OpenConnectionAsync(cancellationToken);

                        var rows = await ExecuteNonQueryAsync(command, cancellationToken);
                        if (rows == 0)
                            return Left(DSRErrors.RequestNotFound(id));

                        return Right(Unit.Default);
                    }

                case DSRRequestStatus.Extended:
                    {
                        // First, retrieve the current deadline to calculate the extended deadline
                        var selectSql = $@"
                        SELECT DeadlineAtUtc
                        FROM {_tableName}
                        WHERE Id = @Id";

                        using var selectCommand = _connection.CreateCommand();
                        selectCommand.CommandText = selectSql;
                        AddParameter(selectCommand, "@Id", id);

                        if (_connection.State != ConnectionState.Open)
                            await OpenConnectionAsync(cancellationToken);

                        DateTimeOffset deadline;
                        using (var reader = await ExecuteReaderAsync(selectCommand, cancellationToken))
                        {
                            if (!await ReadAsync(reader, cancellationToken))
                                return Left(DSRErrors.RequestNotFound(id));

                            deadline = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("DeadlineAtUtc")), TimeSpan.Zero);
                        }

                        var extendedDeadline = deadline.AddMonths(2);

                        var updateSql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue,
                            ExtensionReason = @ExtensionReason,
                            ExtendedDeadlineAtUtc = @ExtendedDeadlineAtUtc
                        WHERE Id = @Id";

                        using var updateCommand = _connection.CreateCommand();
                        updateCommand.CommandText = updateSql;
                        AddParameter(updateCommand, "@Id", id);
                        AddParameter(updateCommand, "@StatusValue", (int)newStatus);
                        AddParameter(updateCommand, "@ExtensionReason", (object?)reason ?? DBNull.Value);
                        AddParameter(updateCommand, "@ExtendedDeadlineAtUtc", extendedDeadline.UtcDateTime);

                        await ExecuteNonQueryAsync(updateCommand, cancellationToken);
                        return Right(Unit.Default);
                    }

                case DSRRequestStatus.IdentityVerified:
                    {
                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue,
                            VerifiedAtUtc = @NowUtc
                        WHERE Id = @Id";

                        using var command = _connection.CreateCommand();
                        command.CommandText = sql;
                        AddParameter(command, "@Id", id);
                        AddParameter(command, "@StatusValue", (int)newStatus);
                        AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);

                        if (_connection.State != ConnectionState.Open)
                            await OpenConnectionAsync(cancellationToken);

                        var rows = await ExecuteNonQueryAsync(command, cancellationToken);
                        if (rows == 0)
                            return Left(DSRErrors.RequestNotFound(id));

                        return Right(Unit.Default);
                    }

                default:
                    {
                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue
                        WHERE Id = @Id";

                        using var command = _connection.CreateCommand();
                        command.CommandText = sql;
                        AddParameter(command, "@Id", id);
                        AddParameter(command, "@StatusValue", (int)newStatus);

                        if (_connection.State != ConnectionState.Open)
                            await OpenConnectionAsync(cancellationToken);

                        var rows = await ExecuteNonQueryAsync(command, cancellationToken);
                        if (rows == 0)
                            return Left(DSRErrors.RequestNotFound(id));

                        return Right(Unit.Default);
                    }
            }
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
                var entity = MapToEntity(reader);
                var domain = DSRRequestMapper.ToDomain(entity);
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
            AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DSRRequest>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DSRRequestMapper.ToDomain(entity);
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
                  AND RightTypeValue = @RestrictionType
                  AND StatusValue IN (@Received, @IdentityVerified, @InProgress, @Extended)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);
            AddParameter(command, "@RestrictionType", (int)DataSubjectRight.Restriction);
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
                var entity = MapToEntity(reader);
                var domain = DSRRequestMapper.ToDomain(entity);
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
        AddParameter(command, "@ReceivedAtUtc", entity.ReceivedAtUtc.UtcDateTime);
        AddParameter(command, "@DeadlineAtUtc", entity.DeadlineAtUtc.UtcDateTime);
        AddParameter(command, "@CompletedAtUtc", entity.CompletedAtUtc.HasValue ? entity.CompletedAtUtc.Value.UtcDateTime : DBNull.Value);
        AddParameter(command, "@ExtensionReason", (object?)entity.ExtensionReason ?? DBNull.Value);
        AddParameter(command, "@ExtendedDeadlineAtUtc", entity.ExtendedDeadlineAtUtc.HasValue ? entity.ExtendedDeadlineAtUtc.Value.UtcDateTime : DBNull.Value);
        AddParameter(command, "@RejectionReason", (object?)entity.RejectionReason ?? DBNull.Value);
        AddParameter(command, "@RequestDetails", (object?)entity.RequestDetails ?? DBNull.Value);
        AddParameter(command, "@VerifiedAtUtc", entity.VerifiedAtUtc.HasValue ? entity.VerifiedAtUtc.Value.UtcDateTime : DBNull.Value);
        AddParameter(command, "@ProcessedByUserId", (object?)entity.ProcessedByUserId ?? DBNull.Value);
    }

    private static DSRRequestEntity MapToEntity(IDataReader reader)
    {
        return new DSRRequestEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            SubjectId = reader.GetString(reader.GetOrdinal("SubjectId")),
            RightTypeValue = reader.GetInt32(reader.GetOrdinal("RightTypeValue")),
            StatusValue = reader.GetInt32(reader.GetOrdinal("StatusValue")),
            ReceivedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("ReceivedAtUtc")), TimeSpan.Zero),
            DeadlineAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("DeadlineAtUtc")), TimeSpan.Zero),
            CompletedAtUtc = reader.IsDBNull(reader.GetOrdinal("CompletedAtUtc"))
                ? null
                : new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("CompletedAtUtc")), TimeSpan.Zero),
            ExtensionReason = reader.IsDBNull(reader.GetOrdinal("ExtensionReason"))
                ? null
                : reader.GetString(reader.GetOrdinal("ExtensionReason")),
            ExtendedDeadlineAtUtc = reader.IsDBNull(reader.GetOrdinal("ExtendedDeadlineAtUtc"))
                ? null
                : new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("ExtendedDeadlineAtUtc")), TimeSpan.Zero),
            RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                ? null
                : reader.GetString(reader.GetOrdinal("RejectionReason")),
            RequestDetails = reader.IsDBNull(reader.GetOrdinal("RequestDetails"))
                ? null
                : reader.GetString(reader.GetOrdinal("RequestDetails")),
            VerifiedAtUtc = reader.IsDBNull(reader.GetOrdinal("VerifiedAtUtc"))
                ? null
                : new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("VerifiedAtUtc")), TimeSpan.Zero),
            ProcessedByUserId = reader.IsDBNull(reader.GetOrdinal("ProcessedByUserId"))
                ? null
                : reader.GetString(reader.GetOrdinal("ProcessedByUserId"))
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
