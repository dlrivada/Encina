using System.Data;
using Dapper;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.DataSubjectRights;

/// <summary>
/// Dapper implementation of <see cref="IDSRRequestStore"/> for MySQL.
/// Provides DSR request lifecycle persistence using Dapper with MySQL-specific SQL syntax.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses MySQL-specific features:
/// <list type="bullet">
/// <item><description>PascalCase column identifiers</description></item>
/// <item><description>DATETIME for UTC datetime storage</description></item>
/// <item><description>TINYINT(1) for boolean columns</description></item>
/// </list>
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
                (Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId)
                VALUES
                (@Id, @SubjectId, @RightTypeValue, @StatusValue, @ReceivedAtUtc, @DeadlineAtUtc, @CompletedAtUtc, @ExtensionReason, @ExtendedDeadlineAtUtc, @RejectionReason, @RequestDetails, @VerifiedAtUtc, @ProcessedByUserId)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.SubjectId,
                entity.RightTypeValue,
                entity.StatusValue,
                ReceivedAtUtc = entity.ReceivedAtUtc.UtcDateTime,
                DeadlineAtUtc = entity.DeadlineAtUtc.UtcDateTime,
                CompletedAtUtc = entity.CompletedAtUtc?.UtcDateTime,
                entity.ExtensionReason,
                ExtendedDeadlineAtUtc = entity.ExtendedDeadlineAtUtc?.UtcDateTime,
                entity.RejectionReason,
                entity.RequestDetails,
                VerifiedAtUtc = entity.VerifiedAtUtc?.UtcDateTime,
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE SubjectId = @SubjectId";

            var rows = await _connection.QueryAsync(sql, new { SubjectId = subjectId });
            var results = rows
                .Select(r => MapToDomain(r))
                .Where(d => d is not null)
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
                    var sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, CompletedAtUtc = @NowUtc WHERE Id = @Id";
                    var rowsAffected = await _connection.ExecuteAsync(sql, new
                    {
                        StatusValue = (int)newStatus,
                        NowUtc = nowUtc.UtcDateTime,
                        Id = id
                    });
                    if (rowsAffected == 0)
                        return Left(DSRErrors.RequestNotFound(id));
                    break;
                }

                case DSRRequestStatus.Rejected:
                {
                    var sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, RejectionReason = @Reason, CompletedAtUtc = @NowUtc WHERE Id = @Id";
                    var rowsAffected = await _connection.ExecuteAsync(sql, new
                    {
                        StatusValue = (int)newStatus,
                        Reason = reason,
                        NowUtc = nowUtc.UtcDateTime,
                        Id = id
                    });
                    if (rowsAffected == 0)
                        return Left(DSRErrors.RequestNotFound(id));
                    break;
                }

                case DSRRequestStatus.Extended:
                {
                    var selectSql = $"SELECT DeadlineAtUtc FROM {_tableName} WHERE Id = @Id";
                    var deadlineRow = await _connection.QuerySingleOrDefaultAsync<dynamic>(selectSql, new { Id = id });

                    if (deadlineRow is null)
                        return Left(DSRErrors.RequestNotFound(id));

                    var deadline = new DateTimeOffset((DateTime)deadlineRow.DeadlineAtUtc, TimeSpan.Zero);
                    var extendedDeadline = deadline.AddMonths(2);

                    var sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, ExtensionReason = @Reason, ExtendedDeadlineAtUtc = @ExtendedDeadline WHERE Id = @Id";
                    await _connection.ExecuteAsync(sql, new
                    {
                        StatusValue = (int)newStatus,
                        Reason = reason,
                        ExtendedDeadline = extendedDeadline.UtcDateTime,
                        Id = id
                    });
                    break;
                }

                case DSRRequestStatus.IdentityVerified:
                {
                    var sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue, VerifiedAtUtc = @NowUtc WHERE Id = @Id";
                    var rowsAffected = await _connection.ExecuteAsync(sql, new
                    {
                        StatusValue = (int)newStatus,
                        NowUtc = nowUtc.UtcDateTime,
                        Id = id
                    });
                    if (rowsAffected == 0)
                        return Left(DSRErrors.RequestNotFound(id));
                    break;
                }

                default:
                {
                    var sql = $@"UPDATE {_tableName} SET StatusValue = @StatusValue WHERE Id = @Id";
                    var rowsAffected = await _connection.ExecuteAsync(sql, new
                    {
                        StatusValue = (int)newStatus,
                        Id = id
                    });
                    if (rowsAffected == 0)
                        return Left(DSRErrors.RequestNotFound(id));
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
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
                .Where(d => d is not null)
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}
                WHERE StatusValue IN (@Received, @IdentityVerified, @InProgress, @Extended)
                  AND COALESCE(ExtendedDeadlineAtUtc, DeadlineAtUtc) < @NowUtc";

            var rows = await _connection.QueryAsync(sql, new
            {
                Received = (int)DSRRequestStatus.Received,
                IdentityVerified = (int)DSRRequestStatus.IdentityVerified,
                InProgress = (int)DSRRequestStatus.InProgress,
                Extended = (int)DSRRequestStatus.Extended,
                NowUtc = nowUtc.UtcDateTime
            });

            var results = rows
                .Select(r => MapToDomain(r))
                .Where(d => d is not null)
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
                SELECT Id, SubjectId, RightTypeValue, StatusValue, ReceivedAtUtc, DeadlineAtUtc, CompletedAtUtc, ExtensionReason, ExtendedDeadlineAtUtc, RejectionReason, RequestDetails, VerifiedAtUtc, ProcessedByUserId
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var results = rows
                .Select(r => MapToDomain(r))
                .Where(d => d is not null)
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
            ReceivedAtUtc = new DateTimeOffset((DateTime)row.ReceivedAtUtc, TimeSpan.Zero),
            DeadlineAtUtc = new DateTimeOffset((DateTime)row.DeadlineAtUtc, TimeSpan.Zero),
            CompletedAtUtc = row.CompletedAtUtc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.CompletedAtUtc, TimeSpan.Zero),
            ExtensionReason = row.ExtensionReason is null or DBNull ? null : (string)row.ExtensionReason,
            ExtendedDeadlineAtUtc = row.ExtendedDeadlineAtUtc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.ExtendedDeadlineAtUtc, TimeSpan.Zero),
            RejectionReason = row.RejectionReason is null or DBNull ? null : (string)row.RejectionReason,
            RequestDetails = row.RequestDetails is null or DBNull ? null : (string)row.RequestDetails,
            VerifiedAtUtc = row.VerifiedAtUtc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.VerifiedAtUtc, TimeSpan.Zero),
            ProcessedByUserId = row.ProcessedByUserId is null or DBNull ? null : (string)row.ProcessedByUserId
        };

        return DSRRequestMapper.ToDomain(entity);
    }
}
