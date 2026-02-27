using System.Data;
using System.Globalization;
using Dapper;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.DataSubjectRights;

/// <summary>
/// Dapper implementation of <see cref="IDSRRequestStore"/> for SQLite.
/// Uses Dapper's parameterized queries for concise, high-performance data access.
/// </summary>
/// <remarks>
/// <para>
/// DateTime values are stored as ISO 8601 text strings using <c>.ToString("O")</c> for writes
/// and <see cref="DateTimeOffset.Parse(string, IFormatProvider?, DateTimeStyles)"/> with
/// <see cref="DateTimeStyles.RoundtripKind"/> for reads, ensuring roundtrip fidelity in SQLite.
/// </para>
/// <para>
/// Enum values (<see cref="DataSubjectRight"/> and <see cref="DSRRequestStatus"/>) are stored
/// as integers for cross-provider compatibility.
/// </para>
/// </remarks>
public sealed class DSRRequestStoreDapper : IDSRRequestStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DSRRequestStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The DSR requests table name (default: DSRRequests).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DSRRequestStoreDapper(
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
            entity.Id = request.Id;

            var sql = $@"
                INSERT INTO {_tableName}
                (Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc,
                 CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason,
                 RequestDetails, VerifiedAtUtc, ProcessedByUserId)
                VALUES
                (@Id, @SubjectId, @RightTypeValue, @StatusValue, @ReceivedAtUtc, @DeadlineAtUtc,
                 @CompletedAtUtc, @ExtensionReason, @ExtendedDeadlineAtUtc, @RejectionReason,
                 @RequestDetails, @VerifiedAtUtc, @ProcessedByUserId)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.SubjectId,
                entity.RightTypeValue,
                entity.StatusValue,
                ReceivedAtUtc = entity.ReceivedAtUtc.ToString("O"),
                DeadlineAtUtc = entity.DeadlineAtUtc.ToString("O"),
                CompletedAtUtc = entity.CompletedAtUtc?.ToString("O"),
                entity.ExtensionReason,
                ExtendedDeadlineAtUtc = entity.ExtendedDeadlineAtUtc?.ToString("O"),
                entity.RejectionReason,
                entity.RequestDetails,
                VerifiedAtUtc = entity.VerifiedAtUtc?.ToString("O"),
                entity.ProcessedByUserId
            });

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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc,
                       CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason,
                       RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE Id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = id });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<DSRRequest>>(None);
            }

            var domain = MapToDomain(row);
            return domain is not null
                ? Right<EncinaError, Option<DSRRequest>>(Some(domain))
                : Right<EncinaError, Option<DSRRequest>>(None);
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc,
                       CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason,
                       RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE SubjectId = @SubjectId";

            var rows = await _connection.QueryAsync(sql, new { SubjectId = subjectId });
            var results = rows
                .Select(r => MapToDomain(r))
                .Where(r => r is not null)
                .Cast<DSRRequest>()
                .ToList();

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
                            CompletedAtUtc = @CompletedAtUtc
                        WHERE Id = @Id";

                        await _connection.ExecuteAsync(sql, new
                        {
                            Id = id,
                            StatusValue = (int)newStatus,
                            CompletedAtUtc = nowUtc.ToString("O")
                        });
                        break;
                    }
                case DSRRequestStatus.Rejected:
                    {
                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue,
                            RejectionReason = @RejectionReason,
                            CompletedAtUtc = @CompletedAtUtc
                        WHERE Id = @Id";

                        await _connection.ExecuteAsync(sql, new
                        {
                            Id = id,
                            StatusValue = (int)newStatus,
                            RejectionReason = reason,
                            CompletedAtUtc = nowUtc.ToString("O")
                        });
                        break;
                    }
                case DSRRequestStatus.Extended:
                    {
                        var deadlineSql = $"SELECT DeadlineAtUtc FROM {_tableName} WHERE Id = @Id";
                        var deadlineRows = await _connection.QueryAsync(deadlineSql, new { Id = id });
                        var deadlineRow = deadlineRows.FirstOrDefault();

                        if (deadlineRow is null)
                        {
                            return Left(DSRErrors.RequestNotFound(id));
                        }

                        var deadline = DateTimeOffset.Parse(
                            (string)deadlineRow.DeadlineAtUtc, null, DateTimeStyles.RoundtripKind);
                        var extendedDeadline = deadline.AddMonths(2);

                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue,
                            ExtensionReason = @ExtensionReason,
                            ExtendedDeadlineAtUtc = @ExtendedDeadlineAtUtc
                        WHERE Id = @Id";

                        await _connection.ExecuteAsync(sql, new
                        {
                            Id = id,
                            StatusValue = (int)newStatus,
                            ExtensionReason = reason,
                            ExtendedDeadlineAtUtc = extendedDeadline.ToString("O")
                        });
                        break;
                    }
                case DSRRequestStatus.IdentityVerified:
                    {
                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue,
                            VerifiedAtUtc = @VerifiedAtUtc
                        WHERE Id = @Id";

                        await _connection.ExecuteAsync(sql, new
                        {
                            Id = id,
                            StatusValue = (int)newStatus,
                            VerifiedAtUtc = nowUtc.ToString("O")
                        });
                        break;
                    }
                default:
                    {
                        var sql = $@"
                        UPDATE {_tableName}
                        SET StatusValue = @StatusValue
                        WHERE Id = @Id";

                        await _connection.ExecuteAsync(sql, new
                        {
                            Id = id,
                            StatusValue = (int)newStatus
                        });
                        break;
                    }
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc,
                       CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason,
                       RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE StatusValue IN (@Received, @IdentityVerified, @InProgress, @Extended)";

            var rows = await _connection.QueryAsync(sql, new
            {
                Received = (int)DSRRequestStatus.Received,
                IdentityVerified = (int)DSRRequestStatus.IdentityVerified,
                InProgress = (int)DSRRequestStatus.InProgress,
                Extended = (int)DSRRequestStatus.Extended
            });

            var results = rows
                .Select(r => MapToDomain(r))
                .Where(r => r is not null)
                .Cast<DSRRequest>()
                .ToList();

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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc,
                       CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason,
                       RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE StatusValue IN (@Received, @IdentityVerified, @InProgress, @Extended)
                  AND COALESCE(ExtendedDeadlineAtUtc, DeadlineAtUtc) < @NowUtc";

            var rows = await _connection.QueryAsync(sql, new
            {
                Received = (int)DSRRequestStatus.Received,
                IdentityVerified = (int)DSRRequestStatus.IdentityVerified,
                InProgress = (int)DSRRequestStatus.InProgress,
                Extended = (int)DSRRequestStatus.Extended,
                NowUtc = nowUtc.ToString("O")
            });

            var results = rows
                .Select(r => MapToDomain(r))
                .Where(r => r is not null)
                .Cast<DSRRequest>()
                .ToList();

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

            var count = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                SubjectId = subjectId,
                RestrictionValue = (int)DataSubjectRight.Restriction,
                Received = (int)DSRRequestStatus.Received,
                IdentityVerified = (int)DSRRequestStatus.IdentityVerified,
                InProgress = (int)DSRRequestStatus.InProgress,
                Extended = (int)DSRRequestStatus.Extended
            });

            return Right(count > 0);
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc,
                       CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason,
                       RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var results = rows
                .Select(r => MapToDomain(r))
                .Where(r => r is not null)
                .Cast<DSRRequest>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(results);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAll", ex.Message));
        }
    }

    private static DSRRequest? MapToDomain(dynamic row)
    {
        var entity = new DSRRequestEntity
        {
            Id = (string)row.Id,
            SubjectId = (string)row.SubjectId,
            RightTypeValue = Convert.ToInt32(row.RightTypeValue),
            StatusValue = Convert.ToInt32(row.StatusValue),
            ReceivedAtUtc = DateTimeOffset.Parse((string)row.ReceivedAtUtc, null, DateTimeStyles.RoundtripKind),
            DeadlineAtUtc = DateTimeOffset.Parse((string)row.DeadlineAtUtc, null, DateTimeStyles.RoundtripKind),
            CompletedAtUtc = row.CompletedAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.CompletedAtUtc, null, DateTimeStyles.RoundtripKind),
            ExtensionReason = row.ExtensionReason is null or DBNull ? null : (string)row.ExtensionReason,
            ExtendedDeadlineAtUtc = row.ExtendedDeadlineAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.ExtendedDeadlineAtUtc, null, DateTimeStyles.RoundtripKind),
            RejectionReason = row.RejectionReason is null or DBNull ? null : (string)row.RejectionReason,
            RequestDetails = row.RequestDetails is null or DBNull ? null : (string)row.RequestDetails,
            VerifiedAtUtc = row.VerifiedAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.VerifiedAtUtc, null, DateTimeStyles.RoundtripKind),
            ProcessedByUserId = row.ProcessedByUserId is null or DBNull ? null : (string)row.ProcessedByUserId
        };

        return DSRRequestMapper.ToDomain(entity);
    }
}
