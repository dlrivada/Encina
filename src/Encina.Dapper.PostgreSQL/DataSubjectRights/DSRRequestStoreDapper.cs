using System.Data;
using Dapper;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.DataSubjectRights;

/// <summary>
/// Dapper implementation of <see cref="IDSRRequestStore"/> for PostgreSQL.
/// Provides DSR request lifecycle persistence using Dapper with PostgreSQL-specific SQL syntax.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses PostgreSQL-specific features:
/// <list type="bullet">
/// <item><description>Lowercase unquoted column identifiers (PostgreSQL folds to lowercase)</description></item>
/// <item><description>TIMESTAMP for UTC datetime storage</description></item>
/// <item><description>Native BOOLEAN for boolean columns</description></item>
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
    /// <param name="tableName">The DSR requests table name (default: dsrrequests).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DSRRequestStoreDapper(
        IDbConnection connection,
        string tableName = "dsrrequests",
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
                (id, subjectid, righttypevalue, statusvalue, receivedatutc, deadlineatutc, completedatutc, extensionreason, extendeddeadlineatutc, rejectionreason, requestdetails, verifiedatutc, processedbyuserid)
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
                SELECT id, subjectid, righttypevalue, statusvalue, receivedatutc, deadlineatutc, completedatutc, extensionreason, extendeddeadlineatutc, rejectionreason, requestdetails, verifiedatutc, processedbyuserid
                FROM {_tableName}
                WHERE id = @Id";

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
                SELECT id, subjectid, righttypevalue, statusvalue, receivedatutc, deadlineatutc, completedatutc, extensionreason, extendeddeadlineatutc, rejectionreason, requestdetails, verifiedatutc, processedbyuserid
                FROM {_tableName}
                WHERE subjectid = @SubjectId";

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
                    var sql = $@"UPDATE {_tableName} SET statusvalue = @StatusValue, completedatutc = @NowUtc WHERE id = @Id";
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
                    var sql = $@"UPDATE {_tableName} SET statusvalue = @StatusValue, rejectionreason = @Reason, completedatutc = @NowUtc WHERE id = @Id";
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
                    var selectSql = $"SELECT deadlineatutc FROM {_tableName} WHERE id = @Id";
                    var deadlineRow = await _connection.QuerySingleOrDefaultAsync<dynamic>(selectSql, new { Id = id });

                    if (deadlineRow is null)
                        return Left(DSRErrors.RequestNotFound(id));

                    var deadline = new DateTimeOffset((DateTime)deadlineRow.deadlineatutc, TimeSpan.Zero);
                    var extendedDeadline = deadline.AddMonths(2);

                    var sql = $@"UPDATE {_tableName} SET statusvalue = @StatusValue, extensionreason = @Reason, extendeddeadlineatutc = @ExtendedDeadline WHERE id = @Id";
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
                    var sql = $@"UPDATE {_tableName} SET statusvalue = @StatusValue, verifiedatutc = @NowUtc WHERE id = @Id";
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
                    var sql = $@"UPDATE {_tableName} SET statusvalue = @StatusValue WHERE id = @Id";
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
                SELECT id, subjectid, righttypevalue, statusvalue, receivedatutc, deadlineatutc, completedatutc, extensionreason, extendeddeadlineatutc, rejectionreason, requestdetails, verifiedatutc, processedbyuserid
                FROM {_tableName}
                WHERE statusvalue IN (@Received, @IdentityVerified, @InProgress, @Extended)";

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
                SELECT id, subjectid, righttypevalue, statusvalue, receivedatutc, deadlineatutc, completedatutc, extensionreason, extendeddeadlineatutc, rejectionreason, requestdetails, verifiedatutc, processedbyuserid
                FROM {_tableName}
                WHERE statusvalue IN (@Received, @IdentityVerified, @InProgress, @Extended)
                  AND COALESCE(extendeddeadlineatutc, deadlineatutc) < @NowUtc";

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
                WHERE subjectid = @SubjectId
                  AND righttypevalue = @RestrictionValue
                  AND statusvalue IN (@Received, @IdentityVerified, @InProgress, @Extended)";

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
                SELECT id, subjectid, righttypevalue, statusvalue, receivedatutc, deadlineatutc, completedatutc, extensionreason, extendeddeadlineatutc, rejectionreason, requestdetails, verifiedatutc, processedbyuserid
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
            Id = (string)row.id,
            SubjectId = (string)row.subjectid,
            RightTypeValue = Convert.ToInt32(row.righttypevalue),
            StatusValue = Convert.ToInt32(row.statusvalue),
            ReceivedAtUtc = new DateTimeOffset((DateTime)row.receivedatutc, TimeSpan.Zero),
            DeadlineAtUtc = new DateTimeOffset((DateTime)row.deadlineatutc, TimeSpan.Zero),
            CompletedAtUtc = row.completedatutc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.completedatutc, TimeSpan.Zero),
            ExtensionReason = row.extensionreason is null or DBNull ? null : (string)row.extensionreason,
            ExtendedDeadlineAtUtc = row.extendeddeadlineatutc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.extendeddeadlineatutc, TimeSpan.Zero),
            RejectionReason = row.rejectionreason is null or DBNull ? null : (string)row.rejectionreason,
            RequestDetails = row.requestdetails is null or DBNull ? null : (string)row.requestdetails,
            VerifiedAtUtc = row.verifiedatutc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.verifiedatutc, TimeSpan.Zero),
            ProcessedByUserId = row.processedbyuserid is null or DBNull ? null : (string)row.processedbyuserid
        };

        return DSRRequestMapper.ToDomain(entity);
    }
}
