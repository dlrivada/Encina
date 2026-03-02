using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DataResidency;

/// <summary>
/// Entity Framework Core implementation of <see cref="IResidencyAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for data residency enforcement decisions as required
/// by GDPR Article 5(2) (accountability principle). Per Article 30, controllers must maintain
/// records of processing activities including transfers of personal data to a third country.
/// Per Articles 44-49 (Chapter V), all cross-border transfers must be documented with their
/// legal basis, outcome, and any safeguards applied.
/// </para>
/// <para>
/// Audit entries recorded by this store serve as legal evidence of data residency compliance
/// and may be required during regulatory audits or supervisory authority inquiries
/// (Article 58). Entries should never be modified or deleted.
/// </para>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic audit management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// The store uses <see cref="ResidencyAuditEntryMapper"/> to convert between domain
/// and persistence models.
/// </para>
/// </remarks>
public sealed class ResidencyAuditStoreEF : IResidencyAuditStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public ResidencyAuditStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ResidencyAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ResidencyAuditEntryMapper.ToEntity(entry);
            await _dbContext.Set<ResidencyAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record residency audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entryId"] = entry.Id, ["action"] = entry.Action }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record residency audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entryId"] = entry.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var entities = await _dbContext.Set<ResidencyAuditEntryEntity>()
                .Where(e => e.EntityId == entityId)
                .OrderByDescending(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities
                .Select(ResidencyAuditEntryMapper.ToDomain)
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve residency audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<ResidencyAuditEntryEntity>()
                .Where(e => e.TimestampUtc >= fromUtc && e.TimestampUtc <= toUtc)
                .OrderBy(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities
                .Select(ResidencyAuditEntryMapper.ToDomain)
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve residency audit entries by date range: {ex.Message}",
                details: new Dictionary<string, object?> { ["fromUtc"] = fromUtc, ["toUtc"] = toUtc }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<ResidencyAuditEntryEntity>()
                .Where(e => e.OutcomeValue == (int)ResidencyOutcome.Blocked)
                .OrderByDescending(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities
                .Select(ResidencyAuditEntryMapper.ToDomain)
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve residency violations: {ex.Message}"));
        }
    }
}
