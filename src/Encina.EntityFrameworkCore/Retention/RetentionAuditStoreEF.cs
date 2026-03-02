using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// Entity Framework Core implementation of <see cref="IRetentionAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for retention-related actions as required by
/// GDPR Article 5(2) (accountability principle). Each <see cref="RecordAsync"/> call
/// immediately persists the audit entry to ensure it is never lost.
/// </para>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic audit management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// The store uses <see cref="RetentionAuditEntryMapper"/> to convert between domain
/// and persistence models.
/// </para>
/// </remarks>
public sealed class RetentionAuditStoreEF : IRetentionAuditStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public RetentionAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        RetentionAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = RetentionAuditEntryMapper.ToEntity(entry);
            await _dbContext.Set<RetentionAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to record retention audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entryId"] = entry.Id, ["action"] = entry.Action }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to record retention audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entryId"] = entry.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var entities = await _dbContext.Set<RetentionAuditEntryEntity>()
                .Where(e => e.EntityId == entityId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(RetentionAuditEntryMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionAuditEntry>>(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to retrieve retention audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<RetentionAuditEntryEntity>()
                .OrderByDescending(e => e.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(RetentionAuditEntryMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionAuditEntry>>(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to retrieve retention audit entries: {ex.Message}"));
        }
    }
}
