using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.Consent;

/// <summary>
/// Dapper implementation of <see cref="IConsentStore"/> for SQLite.
/// Uses Dapper's parameterized queries for concise, high-performance data access.
/// </summary>
public sealed class ConsentStoreDapper : IConsentStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The consent records table name (default: ConsentRecords).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public ConsentStoreDapper(
        IDbConnection connection,
        string tableName = "ConsentRecords",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordConsentAsync(
        ConsentRecord consent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consent);

        try
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, SubjectId, Purpose, Status, ConsentVersionId, GivenAtUtc, WithdrawnAtUtc, ExpiresAtUtc, Source, IpAddress, ProofOfConsent, Metadata)
                VALUES
                (@Id, @SubjectId, @Purpose, @Status, @ConsentVersionId, @GivenAtUtc, @WithdrawnAtUtc, @ExpiresAtUtc, @Source, @IpAddress, @ProofOfConsent, @Metadata)";

            await _connection.ExecuteAsync(sql, new
            {
                Id = consent.Id.ToString(),
                consent.SubjectId,
                consent.Purpose,
                Status = (int)consent.Status,
                consent.ConsentVersionId,
                GivenAtUtc = consent.GivenAtUtc.ToString("O"),
                WithdrawnAtUtc = consent.WithdrawnAtUtc?.ToString("O"),
                ExpiresAtUtc = consent.ExpiresAtUtc?.ToString("O"),
                consent.Source,
                consent.IpAddress,
                consent.ProofOfConsent,
                Metadata = JsonSerializer.Serialize(consent.Metadata)
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to record consent: {ex.Message}",
                details: new Dictionary<string, object?>
                {
                    ["subjectId"] = consent.SubjectId,
                    ["purpose"] = consent.Purpose
                }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<ConsentRecord>>> GetConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var sql = $@"
                SELECT Id, SubjectId, Purpose, Status, ConsentVersionId, GivenAtUtc, WithdrawnAtUtc, ExpiresAtUtc, Source, IpAddress, ProofOfConsent, Metadata
                FROM {_tableName}
                WHERE SubjectId = @SubjectId AND Purpose = @Purpose";

            var rows = await _connection.QueryAsync(sql, new { SubjectId = subjectId, Purpose = purpose });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<ConsentRecord>>(None);
            }

            return Right<EncinaError, Option<ConsentRecord>>(Some(MapToConsentRecord(row)));
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to get consent: {ex.Message}",
                details: new Dictionary<string, object?>
                {
                    ["subjectId"] = subjectId,
                    ["purpose"] = purpose
                }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentRecord>>> GetAllConsentsAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var sql = $@"
                SELECT Id, SubjectId, Purpose, Status, ConsentVersionId, GivenAtUtc, WithdrawnAtUtc, ExpiresAtUtc, Source, IpAddress, ProofOfConsent, Metadata
                FROM {_tableName}
                WHERE SubjectId = @SubjectId";

            var rows = await _connection.QueryAsync(sql, new { SubjectId = subjectId });
            var records = rows.Select(MapToConsentRecord).ToList();

            return Right<EncinaError, IReadOnlyList<ConsentRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to get all consents: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                UPDATE {_tableName}
                SET Status = @WithdrawnStatus,
                    WithdrawnAtUtc = @NowUtc
                WHERE SubjectId = @SubjectId
                  AND Purpose = @Purpose
                  AND Status = @ActiveStatus";

            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                SubjectId = subjectId,
                Purpose = purpose,
                WithdrawnStatus = (int)ConsentStatus.Withdrawn,
                ActiveStatus = (int)ConsentStatus.Active,
                NowUtc = nowUtc.ToString("O")
            });

            if (rowsAffected == 0)
            {
                return Left(ConsentErrors.MissingConsent(subjectId, purpose));
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to withdraw consent: {ex.Message}",
                details: new Dictionary<string, object?>
                {
                    ["subjectId"] = subjectId,
                    ["purpose"] = purpose
                }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                SELECT COUNT(1)
                FROM {_tableName}
                WHERE SubjectId = @SubjectId
                  AND Purpose = @Purpose
                  AND Status = @ActiveStatus
                  AND (ExpiresAtUtc IS NULL OR ExpiresAtUtc > @NowUtc)";

            var count = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                SubjectId = subjectId,
                Purpose = purpose,
                ActiveStatus = (int)ConsentStatus.Active,
                NowUtc = nowUtc.ToString("O")
            });

            return Right(count > 0);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to check consent validity: {ex.Message}",
                details: new Dictionary<string, object?>
                {
                    ["subjectId"] = subjectId,
                    ["purpose"] = purpose
                }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkRecordConsentAsync(
        IEnumerable<ConsentRecord> consents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consents);

        var consentList = consents.ToList();
        var successCount = 0;
        var errors = new List<BulkOperationError>();

        foreach (var consent in consentList)
        {
            var result = await RecordConsentAsync(consent, cancellationToken);
            result.Match(
                Right: _ => successCount++,
                Left: error => errors.Add(new BulkOperationError(
                    $"{consent.SubjectId}:{consent.Purpose}", error)));
        }

        return Right<EncinaError, BulkOperationResult>(errors.Count == 0
            ? BulkOperationResult.Success(successCount)
            : BulkOperationResult.Partial(successCount, errors));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkWithdrawConsentAsync(
        string subjectId,
        IEnumerable<string> purposes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(purposes);

        var purposeList = purposes.ToList();
        var successCount = 0;
        var errors = new List<BulkOperationError>();

        foreach (var purpose in purposeList)
        {
            var result = await WithdrawConsentAsync(subjectId, purpose, cancellationToken);
            result.Match(
                Right: _ => successCount++,
                Left: error => errors.Add(new BulkOperationError(
                    $"{subjectId}:{purpose}", error)));
        }

        return Right<EncinaError, BulkOperationResult>(errors.Count == 0
            ? BulkOperationResult.Success(successCount)
            : BulkOperationResult.Partial(successCount, errors));
    }

    private static ConsentRecord MapToConsentRecord(dynamic row)
    {
        var metadataJson = (string)row.Metadata;
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson)
            ?? new Dictionary<string, object?>();

        return new ConsentRecord
        {
            Id = Guid.Parse((string)row.Id),
            SubjectId = (string)row.SubjectId,
            Purpose = (string)row.Purpose,
            Status = (ConsentStatus)Convert.ToInt32(row.Status),
            ConsentVersionId = (string)row.ConsentVersionId,
            GivenAtUtc = DateTimeOffset.Parse((string)row.GivenAtUtc, null, DateTimeStyles.RoundtripKind),
            WithdrawnAtUtc = row.WithdrawnAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.WithdrawnAtUtc, null, DateTimeStyles.RoundtripKind),
            ExpiresAtUtc = row.ExpiresAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.ExpiresAtUtc, null, DateTimeStyles.RoundtripKind),
            Source = (string)row.Source,
            IpAddress = row.IpAddress is null or DBNull ? null : (string)row.IpAddress,
            ProofOfConsent = row.ProofOfConsent is null or DBNull ? null : (string)row.ProofOfConsent,
            Metadata = metadata
        };
    }
}
