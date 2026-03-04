using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.BreachNotification;

/// <summary>
/// Entity Framework Core implementation of <see cref="IBreachRecordStore"/>.
/// Manages breach records and phased reports for GDPR Articles 33-34 compliance.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic breach record management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure breach records are never lost. The store uses
/// <see cref="BreachRecordMapper"/> and <see cref="PhasedReportMapper"/> to convert
/// between domain and persistence models.
/// </para>
/// <para>
/// The <see cref="GetBreachAsync"/> method loads phased reports from their separate
/// table and composes them with the breach record. Other list methods return
/// records without phased reports for efficiency.
/// </para>
/// </remarks>
public sealed class BreachRecordStoreEF : IBreachRecordStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachRecordStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public BreachRecordStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        try
        {
            var entity = BreachRecordMapper.ToEntity(breach);
            await _dbContext.Set<BreachRecordEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to record breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breach.Id }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to record breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breach.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<BreachRecord>>> GetBreachAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        try
        {
            var entity = await _dbContext.Set<BreachRecordEntity>()
                .FirstOrDefaultAsync(e => e.Id == breachId, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<BreachRecord>>(None);

            var record = BreachRecordMapper.ToDomain(entity);
            if (record is null)
                return Right<EncinaError, Option<BreachRecord>>(None);

            // Load phased reports from separate table
            var reportEntities = await _dbContext.Set<PhasedReportEntity>()
                .Where(r => r.BreachId == breachId)
                .OrderBy(r => r.ReportNumber)
                .ToListAsync(cancellationToken);

            if (reportEntities.Count > 0)
            {
                var reports = reportEntities
                    .Select(PhasedReportMapper.ToDomain)
                    .ToList();
                record = record with { PhasedReports = reports };
            }

            return Right<EncinaError, Option<BreachRecord>>(Some(record));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<BreachRecord>>(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        try
        {
            var existing = await _dbContext.Set<BreachRecordEntity>()
                .FirstOrDefaultAsync(e => e.Id == breach.Id, cancellationToken);

            if (existing is null)
            {
                return Left(EncinaErrors.Create(
                    code: "breachnotification.not_found",
                    message: $"Breach record '{breach.Id}' not found",
                    details: new Dictionary<string, object?> { ["breachId"] = breach.Id }));
            }

            var updated = BreachRecordMapper.ToEntity(breach);

            existing.Nature = updated.Nature;
            existing.ApproximateSubjectsAffected = updated.ApproximateSubjectsAffected;
            existing.CategoriesOfDataAffected = updated.CategoriesOfDataAffected;
            existing.DPOContactDetails = updated.DPOContactDetails;
            existing.LikelyConsequences = updated.LikelyConsequences;
            existing.MeasuresTaken = updated.MeasuresTaken;
            existing.DetectedAtUtc = updated.DetectedAtUtc;
            existing.NotificationDeadlineUtc = updated.NotificationDeadlineUtc;
            existing.NotifiedAuthorityAtUtc = updated.NotifiedAuthorityAtUtc;
            existing.NotifiedSubjectsAtUtc = updated.NotifiedSubjectsAtUtc;
            existing.SeverityValue = updated.SeverityValue;
            existing.StatusValue = updated.StatusValue;
            existing.DelayReason = updated.DelayReason;
            existing.SubjectNotificationExemptionValue = updated.SubjectNotificationExemptionValue;
            existing.ResolvedAtUtc = updated.ResolvedAtUtc;
            existing.ResolutionSummary = updated.ResolutionSummary;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to update breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breach.Id }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to update breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breach.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetBreachesByStatusAsync(
        BreachStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusValue = (int)status;
            var entities = await _dbContext.Set<BreachRecordEntity>()
                .Where(e => e.StatusValue == statusValue)
                .ToListAsync(cancellationToken);

            var records = entities
                .Select(BreachRecordMapper.ToDomain)
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachRecord>>(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get breaches by status: {ex.Message}",
                details: new Dictionary<string, object?> { ["status"] = status.ToString() }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetOverdueBreachesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var entities = await _dbContext.Set<BreachRecordEntity>()
                .Where(e => e.NotificationDeadlineUtc < nowUtc && e.NotifiedAuthorityAtUtc == null)
                .ToListAsync(cancellationToken);

            var records = entities
                .Select(BreachRecordMapper.ToDomain)
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachRecord>>(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get overdue breaches: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DeadlineStatus>>> GetApproachingDeadlineAsync(
        int hoursRemaining,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var thresholdUtc = nowUtc.AddHours(hoursRemaining);

            var entities = await _dbContext.Set<BreachRecordEntity>()
                .Where(e => e.NotifiedAuthorityAtUtc == null
                         && e.NotificationDeadlineUtc > nowUtc
                         && e.NotificationDeadlineUtc < thresholdUtc)
                .Select(e => new { e.Id, e.DetectedAtUtc, e.NotificationDeadlineUtc, e.StatusValue })
                .ToListAsync(cancellationToken);

            var results = new List<DeadlineStatus>();
            foreach (var e in entities)
            {
                if (!Enum.IsDefined(typeof(BreachStatus), e.StatusValue))
                    continue;

                var remaining = (e.NotificationDeadlineUtc - nowUtc).TotalHours;
                results.Add(new DeadlineStatus
                {
                    BreachId = e.Id,
                    DetectedAtUtc = e.DetectedAtUtc,
                    DeadlineUtc = e.NotificationDeadlineUtc,
                    RemainingHours = remaining,
                    IsOverdue = remaining < 0,
                    Status = (BreachStatus)e.StatusValue
                });
            }

            return Right<EncinaError, IReadOnlyList<DeadlineStatus>>(results);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DeadlineStatus>>(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get approaching deadline breaches: {ex.Message}",
                details: new Dictionary<string, object?> { ["hoursRemaining"] = hoursRemaining }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
        string breachId,
        PhasedReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentNullException.ThrowIfNull(report);

        try
        {
            var entity = PhasedReportMapper.ToEntity(report);
            entity.BreachId = breachId;

            await _dbContext.Set<PhasedReportEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to add phased report: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId, ["reportId"] = report.Id }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to add phased report: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId, ["reportId"] = report.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<BreachRecordEntity>()
                .ToListAsync(cancellationToken);

            var records = entities
                .Select(BreachRecordMapper.ToDomain)
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachRecord>>(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get all breaches: {ex.Message}"));
        }
    }
}
