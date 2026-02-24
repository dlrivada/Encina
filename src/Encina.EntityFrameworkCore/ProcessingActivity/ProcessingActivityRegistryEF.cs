using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Diagnostics;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.ProcessingActivity;

/// <summary>
/// Entity Framework Core implementation of <see cref="IProcessingActivityRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic processing activity management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Uses INSERT-only semantics for <see cref="RegisterActivityAsync"/> (fails on duplicate
/// <c>RequestTypeName</c>) and a separate UPDATE for <see cref="UpdateActivityAsync"/>.
/// </para>
/// <para>
/// Register via <c>AddEncinaProcessingActivityEFCore()</c>.
/// </para>
/// </remarks>
public sealed class ProcessingActivityRegistryEF : IProcessingActivityRegistry
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingActivityRegistryEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ProcessingActivityRegistryEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RegisterActivityAsync(
        Compliance.GDPR.ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        using var trace = ProcessingActivityDiagnostics.StartRegistration(activity.RequestType);

        try
        {
            var entity = ProcessingActivityMapper.ToEntity(activity);

            await _dbContext.Set<ProcessingActivityEntity>()
                .AddAsync(entity, cancellationToken).ConfigureAwait(false);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            ProcessingActivityDiagnostics.RecordSuccess(trace, "register");
            return Right(Unit.Default);
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation on RequestTypeName
            ProcessingActivityDiagnostics.RecordFailure(trace, "register", "duplicate");
            return Left(GDPRErrors.ProcessingActivityDuplicate(activity.RequestType.AssemblyQualifiedName!));
        }
        catch (OperationCanceledException)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "register", "cancelled");
            return Left(GDPRErrors.ProcessingActivityStoreError("RegisterActivity", "Operation was cancelled"));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Compliance.GDPR.ProcessingActivity>>> GetAllActivitiesAsync(
        CancellationToken cancellationToken = default)
    {
        using var trace = ProcessingActivityDiagnostics.StartGetAll();

        try
        {
            var entities = await _dbContext.Set<ProcessingActivityEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var results = new List<Compliance.GDPR.ProcessingActivity>();
            foreach (var entity in entities)
            {
                var domain = ProcessingActivityMapper.ToDomain(entity);
                if (domain is not null)
                {
                    results.Add(domain);
                }
            }

            ProcessingActivityDiagnostics.RecordSuccess(trace, results.Count, "get_all");
            return Right<EncinaError, IReadOnlyList<Compliance.GDPR.ProcessingActivity>>(results);
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "get_all", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("GetAllActivities", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>> GetActivityByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        using var trace = ProcessingActivityDiagnostics.StartGetByRequestType(requestType);

        try
        {
            var requestTypeName = requestType.AssemblyQualifiedName!;

            var entity = await _dbContext.Set<ProcessingActivityEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.RequestTypeName == requestTypeName, cancellationToken)
                .ConfigureAwait(false);

            if (entity is not null)
            {
                var domain = ProcessingActivityMapper.ToDomain(entity);
                ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
                return domain is not null
                    ? Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(Some(domain))
                    : Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
            }

            ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
            return Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "get_by_request_type", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("GetActivityByRequestType", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateActivityAsync(
        Compliance.GDPR.ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        using var trace = ProcessingActivityDiagnostics.StartUpdate(activity.RequestType);

        try
        {
            var requestTypeName = activity.RequestType.AssemblyQualifiedName!;
            var set = _dbContext.Set<ProcessingActivityEntity>();

            var existing = await set.FirstOrDefaultAsync(
                e => e.RequestTypeName == requestTypeName,
                cancellationToken).ConfigureAwait(false);

            if (existing is null)
            {
                ProcessingActivityDiagnostics.RecordFailure(trace, "update", "not_found");
                return Left(GDPRErrors.ProcessingActivityNotFound(requestTypeName));
            }

            var updated = ProcessingActivityMapper.ToEntity(activity);

            existing.Name = updated.Name;
            existing.Purpose = updated.Purpose;
            existing.LawfulBasisValue = updated.LawfulBasisValue;
            existing.CategoriesOfDataSubjectsJson = updated.CategoriesOfDataSubjectsJson;
            existing.CategoriesOfPersonalDataJson = updated.CategoriesOfPersonalDataJson;
            existing.RecipientsJson = updated.RecipientsJson;
            existing.ThirdCountryTransfers = updated.ThirdCountryTransfers;
            existing.Safeguards = updated.Safeguards;
            existing.RetentionPeriodTicks = updated.RetentionPeriodTicks;
            existing.SecurityMeasures = updated.SecurityMeasures;
            existing.LastUpdatedAtUtc = updated.LastUpdatedAtUtc;

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            ProcessingActivityDiagnostics.RecordSuccess(trace, "update");
            return Right(Unit.Default);
        }
        catch (OperationCanceledException)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "update", "cancelled");
            return Left(GDPRErrors.ProcessingActivityStoreError("UpdateActivity", "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "update", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("UpdateActivity", ex.Message));
        }
    }
}
