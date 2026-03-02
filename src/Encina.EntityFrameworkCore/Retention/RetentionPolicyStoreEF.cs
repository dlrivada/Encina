using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// Entity Framework Core implementation of <see cref="IRetentionPolicyStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic retention policy management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure retention policy records are never lost. The store uses
/// <see cref="RetentionPolicyMapper"/> to convert between domain and persistence models.
/// </para>
/// </remarks>
public sealed class RetentionPolicyStoreEF : IRetentionPolicyStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionPolicyStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public RetentionPolicyStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = RetentionPolicyMapper.ToEntity(policy);
            await _dbContext.Set<RetentionPolicyEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policy.Id, ["dataCategory"] = policy.DataCategory }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policy.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByIdAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        try
        {
            var entity = await _dbContext.Set<RetentionPolicyEntity>()
                .FirstOrDefaultAsync(e => e.Id == policyId, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<RetentionPolicy>>(None);

            var domain = RetentionPolicyMapper.ToDomain(entity);
            return Right<EncinaError, Option<RetentionPolicy>>(domain is null ? None : Some(domain));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<RetentionPolicy>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policyId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var entity = await _dbContext.Set<RetentionPolicyEntity>()
                .FirstOrDefaultAsync(e => e.DataCategory == dataCategory, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<RetentionPolicy>>(None);

            var domain = RetentionPolicyMapper.ToDomain(entity);
            return Right<EncinaError, Option<RetentionPolicy>>(domain is null ? None : Some(domain));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<RetentionPolicy>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve retention policy by category: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicy>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<RetentionPolicyEntity>()
                .ToListAsync(cancellationToken);

            var policies = entities
                .Select(RetentionPolicyMapper.ToDomain)
                .Where(p => p is not null)
                .Cast<RetentionPolicy>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionPolicy>>(policies);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionPolicy>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve retention policies: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var existing = await _dbContext.Set<RetentionPolicyEntity>()
                .FirstOrDefaultAsync(e => e.Id == policy.Id, cancellationToken);

            if (existing is null)
            {
                return Left(EncinaErrors.Create(
                    code: "retention.not_found",
                    message: $"Retention policy '{policy.Id}' not found",
                    details: new Dictionary<string, object?> { ["policyId"] = policy.Id }));
            }

            existing.DataCategory = policy.DataCategory;
            existing.RetentionPeriodTicks = policy.RetentionPeriod.Ticks;
            existing.AutoDelete = policy.AutoDelete;
            existing.Reason = policy.Reason;
            existing.LegalBasis = policy.LegalBasis;
            existing.PolicyTypeValue = (int)policy.PolicyType;
            existing.LastModifiedAtUtc = _timeProvider.GetUtcNow();

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to update retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policy.Id }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to update retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policy.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        try
        {
            var existing = await _dbContext.Set<RetentionPolicyEntity>()
                .FirstOrDefaultAsync(e => e.Id == policyId, cancellationToken);

            if (existing is null)
                return Right(Unit.Default);

            _dbContext.Set<RetentionPolicyEntity>().Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to delete retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policyId }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to delete retention policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["policyId"] = policyId }));
        }
    }
}
