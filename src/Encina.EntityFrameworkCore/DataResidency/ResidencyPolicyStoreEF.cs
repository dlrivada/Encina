using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DataResidency;

/// <summary>
/// Entity Framework Core implementation of <see cref="IResidencyPolicyStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages the lifecycle and persistence of <see cref="ResidencyPolicyDescriptor"/> records
/// that define which regions are allowed for storing and processing data of a given category.
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data to
/// a third country shall take place only if the conditions of Chapter V (Articles 44-49)
/// are complied with by the controller and processor.
/// </para>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic residency policy management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure policy records are never lost. The store uses
/// <see cref="ResidencyPolicyMapper"/> to convert between domain and persistence models.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyStoreEF : IResidencyPolicyStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyPolicyStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public ResidencyPolicyStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = ResidencyPolicyMapper.ToEntity(policy);
            await _dbContext.Set<ResidencyPolicyEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to create residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = policy.DataCategory }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to create residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = policy.DataCategory }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var entity = await _dbContext.Set<ResidencyPolicyEntity>()
                .FirstOrDefaultAsync(e => e.DataCategory == dataCategory, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None);

            var domain = ResidencyPolicyMapper.ToDomain(entity);
            return Right<EncinaError, Option<ResidencyPolicyDescriptor>>(domain is null ? None : Some(domain));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<ResidencyPolicyDescriptor>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve residency policy by category: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<ResidencyPolicyEntity>()
                .ToListAsync(cancellationToken);

            var policies = entities
                .Select(ResidencyPolicyMapper.ToDomain)
                .Where(p => p is not null)
                .Cast<ResidencyPolicyDescriptor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>(policies);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve residency policies: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var existing = await _dbContext.Set<ResidencyPolicyEntity>()
                .FirstOrDefaultAsync(e => e.DataCategory == policy.DataCategory, cancellationToken);

            if (existing is null)
            {
                return Left(EncinaErrors.Create(
                    code: "residency.not_found",
                    message: $"Residency policy for category '{policy.DataCategory}' not found",
                    details: new Dictionary<string, object?> { ["dataCategory"] = policy.DataCategory }));
            }

            var entity = ResidencyPolicyMapper.ToEntity(policy);
            existing.AllowedRegionCodes = entity.AllowedRegionCodes;
            existing.RequireAdequacyDecision = entity.RequireAdequacyDecision;
            existing.AllowedTransferBasesValue = entity.AllowedTransferBasesValue;
            existing.LastModifiedAtUtc = _timeProvider.GetUtcNow();

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to update residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = policy.DataCategory }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to update residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = policy.DataCategory }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var existing = await _dbContext.Set<ResidencyPolicyEntity>()
                .FirstOrDefaultAsync(e => e.DataCategory == dataCategory, cancellationToken);

            if (existing is null)
                return Right(Unit.Default);

            _dbContext.Set<ResidencyPolicyEntity>().Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to delete residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to delete residency policy: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }
}
