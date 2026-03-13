using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;

using LanguageExt;

using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.ProcessorAgreements;

/// <summary>
/// Entity Framework Core implementation of <see cref="IProcessorRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic processor registry management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure processor records are never lost. The store uses
/// <see cref="ProcessorMapper"/> to convert between domain and persistence models.
/// </para>
/// <para>
/// <see cref="GetFullSubProcessorChainAsync"/> loads all processors for the tenant and
/// performs BFS traversal in memory, bounded by <see cref="MaxSubProcessorDepth"/> (DC 5).
/// </para>
/// </remarks>
public sealed class ProcessorRegistryEF : IProcessorRegistry
{
    /// <summary>
    /// Default maximum depth for sub-processor chains.
    /// </summary>
    internal const int DefaultMaxSubProcessorDepth = 5;

    private readonly DbContext _dbContext;

    /// <summary>
    /// Gets or sets the maximum allowed sub-processor depth.
    /// </summary>
    internal int MaxSubProcessorDepth { get; set; } = DefaultMaxSubProcessorDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorRegistryEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ProcessorRegistryEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RegisterProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        try
        {
            // Check for duplicate.
            var exists = await _dbContext.Set<ProcessorEntity>()
                .AnyAsync(e => e.Id == processor.Id, cancellationToken);

            if (exists)
            {
                return ProcessorAgreementErrors.AlreadyExists(processor.Id);
            }

            // Validate depth constraint.
            if (processor.Depth > MaxSubProcessorDepth)
            {
                return ProcessorAgreementErrors.SubProcessorDepthExceeded(
                    processor.Id, processor.Depth, MaxSubProcessorDepth);
            }

            // Validate parent exists and depth is consistent.
            if (processor.ParentProcessorId is not null)
            {
                var parent = await _dbContext.Set<ProcessorEntity>()
                    .FirstOrDefaultAsync(e => e.Id == processor.ParentProcessorId, cancellationToken);

                if (parent is null)
                {
                    return ProcessorAgreementErrors.NotFound(processor.ParentProcessorId);
                }

                if (processor.Depth != parent.Depth + 1)
                {
                    return ProcessorAgreementErrors.ValidationFailed(
                        processor.Id,
                        $"Depth must be {parent.Depth + 1} (parent depth + 1), but was {processor.Depth}.");
                }
            }

            var entity = ProcessorMapper.ToEntity(processor);
            await _dbContext.Set<ProcessorEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return ProcessorAgreementErrors.StoreError("RegisterProcessor", ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("RegisterProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<Processor>>> GetProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var entity = await _dbContext.Set<ProcessorEntity>()
                .FirstOrDefaultAsync(e => e.Id == processorId, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<Processor>>(None);

            var domain = ProcessorMapper.ToDomain(entity);
            return domain is not null
                ? Right<EncinaError, Option<Processor>>(Some(domain))
                : Right<EncinaError, Option<Processor>>(None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<ProcessorEntity>()
                .ToListAsync(cancellationToken);

            var processors = entities
                .Select(ProcessorMapper.ToDomain)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<Processor>>(processors);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetAllProcessors", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        try
        {
            var existing = await _dbContext.Set<ProcessorEntity>()
                .FirstOrDefaultAsync(e => e.Id == processor.Id, cancellationToken);

            if (existing is null)
            {
                return ProcessorAgreementErrors.NotFound(processor.Id);
            }

            existing.Name = processor.Name;
            existing.Country = processor.Country;
            existing.ContactEmail = processor.ContactEmail;
            existing.ParentProcessorId = processor.ParentProcessorId;
            existing.Depth = processor.Depth;
            existing.SubProcessorAuthorizationTypeValue = (int)processor.SubProcessorAuthorizationType;
            existing.TenantId = processor.TenantId;
            existing.ModuleId = processor.ModuleId;
            existing.LastUpdatedAtUtc = processor.LastUpdatedAtUtc;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return ProcessorAgreementErrors.StoreError("UpdateProcessor", ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("UpdateProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var existing = await _dbContext.Set<ProcessorEntity>()
                .FirstOrDefaultAsync(e => e.Id == processorId, cancellationToken);

            if (existing is null)
            {
                return ProcessorAgreementErrors.NotFound(processorId);
            }

            _dbContext.Set<ProcessorEntity>().Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return ProcessorAgreementErrors.StoreError("RemoveProcessor", ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("RemoveProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetSubProcessorsAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var entities = await _dbContext.Set<ProcessorEntity>()
                .Where(e => e.ParentProcessorId == processorId)
                .ToListAsync(cancellationToken);

            var processors = entities
                .Select(ProcessorMapper.ToDomain)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<Processor>>(processors);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetSubProcessors", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetFullSubProcessorChainAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            // Load all processors and perform BFS in memory (DC 5).
            // This avoids recursive SQL which is not portable across all providers.
            var allEntities = await _dbContext.Set<ProcessorEntity>()
                .ToListAsync(cancellationToken);

            var allProcessors = allEntities
                .Select(ProcessorMapper.ToDomain)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            // Build lookup by parent ID for efficient traversal.
            var childrenByParent = allProcessors
                .Where(p => p.ParentProcessorId is not null)
                .GroupBy(p => p.ParentProcessorId!)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            // BFS traversal bounded by MaxSubProcessorDepth.
            var result = new List<Processor>();
            var queue = new Queue<string>();
            queue.Enqueue(processorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                if (!childrenByParent.TryGetValue(currentId, out var children))
                    continue;

                foreach (var child in children)
                {
                    if (child.Depth <= MaxSubProcessorDepth)
                    {
                        result.Add(child);
                        queue.Enqueue(child.Id);
                    }
                }
            }

            return Right<EncinaError, IReadOnlyList<Processor>>(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetFullSubProcessorChain", ex.Message, ex);
        }
    }
}
