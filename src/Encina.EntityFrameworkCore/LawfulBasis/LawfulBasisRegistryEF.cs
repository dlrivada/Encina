using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.LawfulBasis;

/// <summary>
/// Entity Framework Core implementation of <see cref="ILawfulBasisRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic lawful basis record management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure lawful basis records are never lost.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistryEF : ILawfulBasisRegistry
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisRegistryEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public LawfulBasisRegistryEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RegisterAsync(
        LawfulBasisRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);

        try
        {
            var entity = LawfulBasisRegistrationMapper.ToEntity(registration);
            var set = _dbContext.Set<LawfulBasisRegistrationEntity>();

            var existing = await set.FirstOrDefaultAsync(
                e => e.RequestTypeName == entity.RequestTypeName,
                cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                existing.BasisValue = entity.BasisValue;
                existing.Purpose = entity.Purpose;
                existing.LIAReference = entity.LIAReference;
                existing.LegalReference = entity.LegalReference;
                existing.ContractReference = entity.ContractReference;
                existing.RegisteredAtUtc = entity.RegisteredAtUtc;
            }
            else
            {
                await set.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("Register", ex.Message));
        }
        catch (OperationCanceledException)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("Register", "Operation was cancelled"));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        return GetByRequestTypeNameAsync(requestType.AssemblyQualifiedName!, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeNameAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        try
        {
            var entity = await _dbContext.Set<LawfulBasisRegistrationEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.RequestTypeName == requestTypeName, cancellationToken)
                .ConfigureAwait(false);

            if (entity is not null)
            {
                var domain = LawfulBasisRegistrationMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<LawfulBasisRegistration>>(Some(domain))
                    : Right<EncinaError, Option<LawfulBasisRegistration>>(None);
            }

            return Right<EncinaError, Option<LawfulBasisRegistration>>(None);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("GetByRequestTypeName", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisRegistration>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<LawfulBasisRegistrationEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var results = new List<LawfulBasisRegistration>();
            foreach (var entity in entities)
            {
                var domain = LawfulBasisRegistrationMapper.ToDomain(entity);
                if (domain is not null)
                {
                    results.Add(domain);
                }
            }

            return Right<EncinaError, IReadOnlyList<LawfulBasisRegistration>>(results);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("GetAll", ex.Message));
        }
    }
}
