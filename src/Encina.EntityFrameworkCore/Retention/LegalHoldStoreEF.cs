using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// Entity Framework Core implementation of <see cref="ILegalHoldStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic legal hold management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Per GDPR Article 17(3)(e), the right to erasure does not apply when processing is
/// necessary for the establishment, exercise, or defence of legal claims. This store
/// manages the persistence of legal holds that implement this exemption.
/// </para>
/// <para>
/// Active holds are identified by <c>ReleasedAtUtc IS NULL</c>. The
/// <see cref="IsUnderHoldAsync"/> method is designed to be lightweight for use during
/// enforcement cycles before every deletion attempt.
/// </para>
/// </remarks>
public sealed class LegalHoldStoreEF : ILegalHoldStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalHoldStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public LegalHoldStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        LegalHold hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);

        try
        {
            var entity = LegalHoldMapper.ToEntity(hold);
            await _dbContext.Set<LegalHoldEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = hold.Id, ["entityId"] = hold.EntityId }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = hold.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<LegalHold>>> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var entity = await _dbContext.Set<LegalHoldEntity>()
                .FirstOrDefaultAsync(e => e.Id == holdId, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<LegalHold>>(None);

            return Right<EncinaError, Option<LegalHold>>(Some(LegalHoldMapper.ToDomain(entity)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<LegalHold>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = holdId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var entities = await _dbContext.Set<LegalHoldEntity>()
                .Where(e => e.EntityId == entityId)
                .ToListAsync(cancellationToken);

            var holds = entities.Select(LegalHoldMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<LegalHold>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve legal holds by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, bool>> IsUnderHoldAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var isHeld = await _dbContext.Set<LegalHoldEntity>()
                .AnyAsync(e => e.EntityId == entityId && e.ReleasedAtUtc == null, cancellationToken);

            return Right<EncinaError, bool>(isHeld);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to check legal hold status: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<LegalHoldEntity>()
                .Where(e => e.ReleasedAtUtc == null)
                .ToListAsync(cancellationToken);

            var holds = entities.Select(LegalHoldMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<LegalHold>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve active legal holds: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> ReleaseAsync(
        string holdId,
        string? releasedByUserId,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var existing = await _dbContext.Set<LegalHoldEntity>()
                .FirstOrDefaultAsync(e => e.Id == holdId, cancellationToken);

            if (existing is null)
            {
                return Left(EncinaErrors.Create(
                    code: "retention.not_found",
                    message: $"Legal hold '{holdId}' not found",
                    details: new Dictionary<string, object?> { ["holdId"] = holdId }));
            }

            if (existing.ReleasedAtUtc is not null)
            {
                return Left(EncinaErrors.Create(
                    code: "retention.already_released",
                    message: $"Legal hold '{holdId}' has already been released",
                    details: new Dictionary<string, object?> { ["holdId"] = holdId, ["releasedAtUtc"] = existing.ReleasedAtUtc }));
            }

            existing.ReleasedAtUtc = releasedAtUtc;
            existing.ReleasedByUserId = releasedByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to release legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = holdId }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to release legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = holdId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<LegalHoldEntity>()
                .ToListAsync(cancellationToken);

            var holds = entities.Select(LegalHoldMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<LegalHold>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve legal holds: {ex.Message}"));
        }
    }
}
