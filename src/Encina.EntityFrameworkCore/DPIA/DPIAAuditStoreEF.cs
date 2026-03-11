using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DPIA;

/// <summary>
/// Entity Framework Core implementation of <see cref="IDPIAAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for DPIA assessment actions as required by
/// GDPR Article 5(2) (accountability principle). Each <see cref="RecordAuditEntryAsync"/> call
/// immediately persists the audit entry to ensure it is never lost.
/// </para>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic audit management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// The store uses <see cref="DPIAAuditEntryMapper"/> to convert between domain
/// and persistence models.
/// </para>
/// </remarks>
public sealed class DPIAAuditStoreEF : IDPIAAuditStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public DPIAAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAuditEntryAsync(
        DPIAAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = DPIAAuditEntryMapper.ToEntity(entry);
            await _dbContext.Set<DPIAAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to record DPIA audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = entry.AssessmentId, ["action"] = entry.Action }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to record DPIA audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = entry.AssessmentId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>> GetAuditTrailAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var assessmentIdStr = assessmentId.ToString("D");
            var entities = await _dbContext.Set<DPIAAuditEntryEntity>()
                .Where(e => e.AssessmentId == assessmentIdStr)
                .OrderBy(e => e.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities
                .Select(DPIAAuditEntryMapper.ToDomain)
                .Where(e => e is not null)
                .Cast<DPIAAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DPIAAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DPIAAuditEntry>>(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to retrieve DPIA audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }
}
