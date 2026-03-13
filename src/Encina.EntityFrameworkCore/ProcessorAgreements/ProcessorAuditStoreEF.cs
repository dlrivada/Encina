using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;

using LanguageExt;

using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.ProcessorAgreements;

/// <summary>
/// Entity Framework Core implementation of <see cref="IProcessorAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for processor and DPA actions as required by
/// GDPR Article 5(2) (accountability principle). Each <see cref="RecordAsync"/> call
/// immediately persists the audit entry to ensure it is never lost.
/// </para>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic audit management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// The store uses <see cref="ProcessorAgreementAuditEntryMapper"/> to convert between domain
/// and persistence models.
/// </para>
/// </remarks>
public sealed class ProcessorAuditStoreEF : IProcessorAuditStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ProcessorAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ProcessorAgreementAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ProcessorAgreementAuditEntryMapper.ToEntity(entry);
            await _dbContext.Set<ProcessorAgreementAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return ProcessorAgreementErrors.StoreError("RecordAuditEntry", ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("RecordAuditEntry", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>> GetAuditTrailAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var entities = await _dbContext.Set<ProcessorAgreementAuditEntryEntity>()
                .Where(e => e.ProcessorId == processorId)
                .OrderBy(e => e.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities
                .Select(ProcessorAgreementAuditEntryMapper.ToDomain)
                .ToList();

            return Right<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetAuditTrail", ex.Message, ex);
        }
    }
}
