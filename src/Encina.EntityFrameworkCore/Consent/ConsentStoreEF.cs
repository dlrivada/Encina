using System.Text.Json;
using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// Entity Framework Core implementation of <see cref="IConsentStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic consent record management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure consent records are never lost, matching the pattern used by
/// <see cref="Auditing.AuditStoreEF"/>.
/// </para>
/// </remarks>
public sealed class ConsentStoreEF : IConsentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public ConsentStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordConsentAsync(
        ConsentRecord consent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consent);

        try
        {
            var entity = MapToEntity(consent);
            await _dbContext.Set<ConsentRecordEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to record consent: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = consent.SubjectId, ["purpose"] = consent.Purpose }));
        }
        catch (OperationCanceledException)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<ConsentRecord>>> GetConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var entity = await _dbContext.Set<ConsentRecordEntity>()
                .FirstOrDefaultAsync(e => e.SubjectId == subjectId && e.Purpose == purpose, cancellationToken);

            return Right<EncinaError, Option<ConsentRecord>>(entity is null ? None : Some(MapToRecord(entity)));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, Option<ConsentRecord>>(EncinaErrors.Create(
                code: "consent.store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentRecord>>> GetAllConsentsAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var entities = await _dbContext.Set<ConsentRecordEntity>()
                .Where(e => e.SubjectId == subjectId)
                .ToListAsync(cancellationToken);

            var records = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<ConsentRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<ConsentRecord>>(EncinaErrors.Create(
                code: "consent.store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var entity = await _dbContext.Set<ConsentRecordEntity>()
                .FirstOrDefaultAsync(
                    e => e.SubjectId == subjectId && e.Purpose == purpose && e.Status == ConsentStatus.Active,
                    cancellationToken);

            if (entity is null)
            {
                return Left(ConsentErrors.MissingConsent(subjectId, purpose));
            }

            entity.Status = ConsentStatus.Withdrawn;
            entity.WithdrawnAtUtc = _timeProvider.GetUtcNow();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to withdraw consent: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
        catch (OperationCanceledException)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var now = _timeProvider.GetUtcNow();

            // Two-step query: filter by SubjectId/Purpose/Status in SQL, then check
            // expiry in memory. This avoids SQLite's inability to translate DateTimeOffset
            // comparisons in LINQ-to-SQL.
            var entity = await _dbContext.Set<ConsentRecordEntity>()
                .FirstOrDefaultAsync(
                    e => e.SubjectId == subjectId &&
                         e.Purpose == purpose &&
                         e.Status == ConsentStatus.Active,
                    cancellationToken);

            if (entity is null)
                return Right<EncinaError, bool>(false);

            var isValid = entity.ExpiresAtUtc is null || entity.ExpiresAtUtc > now;
            return Right<EncinaError, bool>(isValid);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, bool>(EncinaErrors.Create(
                code: "consent.store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkRecordConsentAsync(
        IEnumerable<ConsentRecord> consents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consents);

        try
        {
            var consentList = consents.ToList();
            var errors = new List<BulkOperationError>();
            var successCount = 0;

            foreach (var consent in consentList)
            {
                var result = await RecordConsentAsync(consent, cancellationToken);
                result.Match(
                    Right: _ => successCount++,
                    Left: error => errors.Add(new BulkOperationError($"{consent.SubjectId}:{consent.Purpose}", error)));
            }

            return errors.Count == 0
                ? Right<EncinaError, BulkOperationResult>(BulkOperationResult.Success(successCount))
                : Right<EncinaError, BulkOperationResult>(BulkOperationResult.Partial(successCount, errors));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, BulkOperationResult>(EncinaErrors.Create(
                code: "consent.store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkWithdrawConsentAsync(
        string subjectId,
        IEnumerable<string> purposes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(purposes);

        try
        {
            var purposeList = purposes.ToList();
            var errors = new List<BulkOperationError>();
            var successCount = 0;

            foreach (var purpose in purposeList)
            {
                var result = await WithdrawConsentAsync(subjectId, purpose, cancellationToken);
                result.Match(
                    Right: _ => successCount++,
                    Left: error => errors.Add(new BulkOperationError($"{subjectId}:{purpose}", error)));
            }

            return errors.Count == 0
                ? Right<EncinaError, BulkOperationResult>(BulkOperationResult.Success(successCount))
                : Right<EncinaError, BulkOperationResult>(BulkOperationResult.Partial(successCount, errors));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, BulkOperationResult>(EncinaErrors.Create(
                code: "consent.store_error",
                message: "Operation was cancelled"));
        }
    }

    internal static ConsentRecordEntity MapToEntity(ConsentRecord record) => new()
    {
        Id = record.Id,
        SubjectId = record.SubjectId,
        Purpose = record.Purpose,
        Status = record.Status,
        ConsentVersionId = record.ConsentVersionId,
        GivenAtUtc = record.GivenAtUtc,
        WithdrawnAtUtc = record.WithdrawnAtUtc,
        ExpiresAtUtc = record.ExpiresAtUtc,
        Source = record.Source,
        IpAddress = record.IpAddress,
        ProofOfConsent = record.ProofOfConsent,
        Metadata = SerializeMetadata(record.Metadata)
    };

    internal static ConsentRecord MapToRecord(ConsentRecordEntity entity) => new()
    {
        Id = entity.Id,
        SubjectId = entity.SubjectId,
        Purpose = entity.Purpose,
        Status = entity.Status,
        ConsentVersionId = entity.ConsentVersionId,
        GivenAtUtc = entity.GivenAtUtc,
        WithdrawnAtUtc = entity.WithdrawnAtUtc,
        ExpiresAtUtc = entity.ExpiresAtUtc,
        Source = entity.Source,
        IpAddress = entity.IpAddress,
        ProofOfConsent = entity.ProofOfConsent,
        Metadata = DeserializeMetadata(entity.Metadata)
    };

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
            return null;

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    private static Dictionary<string, object?> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object?>();

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            return dict ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
