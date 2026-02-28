using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Anonymization;

/// <summary>
/// EF Core implementation of <see cref="ITokenMappingStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is provider-agnostic — it works with any EF Core database provider
/// (SQLite, SQL Server, PostgreSQL, MySQL) through the configured <see cref="DbContext"/>.
/// </para>
/// <para>
/// Read operations use <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>
/// for optimal performance. The Token column should have a UNIQUE index and the
/// OriginalValueHash column should have an INDEX configured in the DbContext model.
/// </para>
/// </remarks>
public sealed class TokenMappingStoreEF : ITokenMappingStore
{
    private readonly DbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenMappingStoreEF"/> class.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public TokenMappingStoreEF(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> StoreAsync(
        TokenMapping mapping,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        try
        {
            var entity = TokenMappingMapper.ToEntity(mapping);
            _context.Set<TokenMappingEntity>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(AnonymizationErrors.StoreError("Store", ex.Message));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("Store", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        try
        {
            var entity = await _context.Set<TokenMappingEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Token == token, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<TokenMapping>>(None);

            return Right<EncinaError, Option<TokenMapping>>(Some(TokenMappingMapper.ToDomain(entity)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetByToken", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByOriginalValueHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        try
        {
            var entity = await _context.Set<TokenMappingEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.OriginalValueHash == hash, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<TokenMapping>>(None);

            return Right<EncinaError, Option<TokenMapping>>(Some(TokenMappingMapper.ToDomain(entity)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetByOriginalValueHash", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        try
        {
            var entities = await _context.Set<TokenMappingEntity>()
                .Where(e => e.KeyId == keyId)
                .ToListAsync(cancellationToken);

            if (entities.Count > 0)
            {
                _context.Set<TokenMappingEntity>().RemoveRange(entities);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(AnonymizationErrors.StoreError("DeleteByKeyId", ex.Message));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("DeleteByKeyId", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<TokenMapping>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.Set<TokenMappingEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var results = entities
                .Select(TokenMappingMapper.ToDomain)
                .ToList();

            return Right<EncinaError, IReadOnlyList<TokenMapping>>(results);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetAll", ex.Message));
        }
    }
}
