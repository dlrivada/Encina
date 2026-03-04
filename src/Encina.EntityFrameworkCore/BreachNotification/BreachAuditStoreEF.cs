using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.BreachNotification;

/// <summary>
/// Entity Framework Core implementation of <see cref="IBreachAuditStore"/>.
/// Provides immutable audit trail for GDPR Article 33(5) accountability.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the breach notification measures applied and may be required during supervisory
/// authority inquiries (Article 58).
/// </para>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic audit management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// </remarks>
public sealed class BreachAuditStoreEF : IBreachAuditStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public BreachAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        BreachAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = BreachAuditEntryMapper.ToEntity(entry);
            await _dbContext.Set<BreachAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.audit_store_error",
                message: $"Failed to record breach audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = entry.BreachId, ["action"] = entry.Action }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.audit_store_error",
                message: $"Failed to record breach audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = entry.BreachId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachAuditEntry>>> GetAuditTrailAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        try
        {
            var entities = await _dbContext.Set<BreachAuditEntryEntity>()
                .Where(e => e.BreachId == breachId)
                .OrderBy(e => e.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities
                .Select(BreachAuditEntryMapper.ToDomain)
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachAuditEntry>>(EncinaErrors.Create(
                code: "breachnotification.audit_store_error",
                message: $"Failed to get breach audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId }));
        }
    }
}
