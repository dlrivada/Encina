using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.Security.Audit.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Audit;

/// <summary>
/// Decorator that records read audit entries for <see cref="IRepository{TEntity, TId}"/> operations.
/// </summary>
/// <typeparam name="TEntity">The entity type, must implement <see cref="IReadAuditable"/>.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// Intercepts read operations (GetById, GetAll, Find, GetPaged) and records
/// <see cref="ReadAuditEntry"/> records via <see cref="IReadAuditStore"/> using a
/// fire-and-forget pattern. Write operations (Add, Update, Remove) are delegated
/// directly to the inner repository without auditing — CUD operations are handled
/// by the existing <see cref="AuditPipelineBehavior{TRequest, TResponse}"/>.
/// </para>
/// <para>
/// <b>Resilience:</b> Audit failures are logged but never affect the read result.
/// A read operation should always succeed even if auditing fails.
/// </para>
/// <para>
/// <b>Sampling:</b> Supports per-entity sampling rates via <see cref="ReadAuditOptions.GetSamplingRate"/>.
/// Not every read needs to be audited — high-traffic entities can use lower sampling rates.
/// </para>
/// <para>
/// Methods that do not return entity data (<c>AnyAsync</c>, <c>CountAsync</c>) are
/// delegated directly without auditing, as they do not expose sensitive information.
/// </para>
/// </remarks>
public sealed class AuditedRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>, IReadAuditable
    where TId : notnull
{
    private readonly IRepository<TEntity, TId> _inner;
    private readonly IReadAuditStore _readAuditStore;
    private readonly IRequestContext _requestContext;
    private readonly IReadAuditContext _readAuditContext;
    private readonly ReadAuditOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditedRepository<TEntity, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditedRepository{TEntity, TId}"/>.
    /// </summary>
    /// <param name="inner">The inner repository to delegate to.</param>
    /// <param name="readAuditStore">The read audit store for recording access entries.</param>
    /// <param name="requestContext">The current request context for user information.</param>
    /// <param name="readAuditContext">The read audit context for access purpose.</param>
    /// <param name="options">The read audit options controlling auditing behavior.</param>
    /// <param name="timeProvider">The time provider for consistent timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public AuditedRepository(
        IRepository<TEntity, TId> inner,
        IReadAuditStore readAuditStore,
        IRequestContext requestContext,
        IReadAuditContext readAuditContext,
        ReadAuditOptions options,
        TimeProvider timeProvider,
        ILogger<AuditedRepository<TEntity, TId>> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(readAuditStore);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(readAuditContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _readAuditStore = readAuditStore;
        _requestContext = requestContext;
        _readAuditContext = readAuditContext;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ── Read operations (audited) ───────────────────────────────────────

    /// <inheritdoc />
    public async Task<Option<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var result = await _inner.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (ShouldAudit())
        {
            _ = LogReadAccessAsync("GetById", id?.ToString(), result.IsSome ? 1 : 0);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _inner.GetAllAsync(cancellationToken).ConfigureAwait(false);

        if (ShouldAudit())
        {
            _ = LogReadAccessAsync("GetAll", null, result.Count);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.FindAsync(specification, cancellationToken).ConfigureAwait(false);

        if (ShouldAudit())
        {
            _ = LogReadAccessAsync("Find", null, result.Count);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Option<TEntity>> FindOneAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.FindOneAsync(specification, cancellationToken).ConfigureAwait(false);

        if (ShouldAudit())
        {
            _ = LogReadAccessAsync("FindOne", null, result.IsSome ? 1 : 0);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.FindAsync(predicate, cancellationToken).ConfigureAwait(false);

        if (ShouldAudit())
        {
            _ = LogReadAccessAsync("FindByPredicate", null, result.Count);
        }

        return result;
    }

    // ── Metadata operations (not audited — no entity data exposed) ──────

    /// <inheritdoc />
    public Task<bool> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        _inner.AnyAsync(specification, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        _inner.AnyAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        _inner.CountAsync(specification, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _inner.CountAsync(cancellationToken);

    // ── Paged operations (audited) ──────────────────────────────────────

    /// <inheritdoc />
    public async Task<global::Encina.DomainModeling.PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.GetPagedAsync(pageNumber, pageSize, cancellationToken).ConfigureAwait(false);

        if (ShouldAudit())
        {
            _ = LogReadAccessAsync("GetPaged", null, result.Items.Count);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<global::Encina.DomainModeling.PagedResult<TEntity>> GetPagedAsync(
        Specification<TEntity> specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.GetPagedAsync(specification, pageNumber, pageSize, cancellationToken).ConfigureAwait(false);

        if (ShouldAudit())
        {
            _ = LogReadAccessAsync("GetPagedWithSpec", null, result.Items.Count);
        }

        return result;
    }

    // ── Write operations (delegated directly — CUD auditing is handled by AuditPipelineBehavior) ──

    /// <inheritdoc />
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        _inner.AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        _inner.AddRangeAsync(entities, cancellationToken);

    /// <inheritdoc />
    public void Update(TEntity entity) =>
        _inner.Update(entity);

    /// <inheritdoc />
    public void UpdateRange(IEnumerable<TEntity> entities) =>
        _inner.UpdateRange(entities);

    /// <inheritdoc />
    public void Remove(TEntity entity) =>
        _inner.Remove(entity);

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<TEntity> entities) =>
        _inner.RemoveRange(entities);

    /// <inheritdoc />
    public Task<bool> RemoveByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        _inner.RemoveByIdAsync(id, cancellationToken);

    // ── Private audit helpers ───────────────────────────────────────────

    private bool ShouldAudit()
    {
        if (_options.ExcludeSystemAccess && _requestContext.UserId is null)
        {
            return false;
        }

        var samplingRate = _options.GetSamplingRate(typeof(TEntity));
        return samplingRate > 0.0 && Random.Shared.NextDouble() < samplingRate;
    }

    private async Task LogReadAccessAsync(string methodName, string? entityId, int entityCount)
    {
        var entityTypeName = typeof(TEntity).Name;
        var activity = ReadAuditActivitySource.StartLogRead(entityTypeName, methodName);

        try
        {
            if (_options.RequirePurpose && _readAuditContext.Purpose is null)
            {
                ReadAuditLog.PurposeNotDeclared(_logger, entityTypeName, _requestContext.UserId);
            }

            var entry = new ReadAuditEntry
            {
                Id = Guid.NewGuid(),
                EntityType = entityTypeName,
                EntityId = entityId,
                UserId = _requestContext.UserId,
                TenantId = _requestContext.TenantId,
                AccessedAtUtc = _timeProvider.GetUtcNow(),
                CorrelationId = _requestContext.CorrelationId,
                Purpose = _readAuditContext.Purpose,
                AccessMethod = ReadAccessMethod.Repository,
                EntityCount = entityCount,
                Metadata = new Dictionary<string, object?>
                {
                    ["method"] = methodName
                }
            };

            await _readAuditStore.LogReadAsync(entry, CancellationToken.None).ConfigureAwait(false);

            ReadAuditLog.ReadAccessRecorded(_logger, entityTypeName, methodName);
            ReadAuditMeter.EntriesLoggedTotal.Add(1,
                new KeyValuePair<string, object?>(ReadAuditMeter.TagEntityType, entityTypeName),
                new KeyValuePair<string, object?>(ReadAuditMeter.TagAccessMethod, nameof(ReadAccessMethod.Repository)));
            ReadAuditActivitySource.Complete(activity);
        }
        catch (Exception ex)
        {
            // Audit failures must never block read operations
            ReadAuditLog.ReadAccessFailed(_logger, entityTypeName, methodName, ex);
            ReadAuditMeter.LogFailuresTotal.Add(1,
                new KeyValuePair<string, object?>(ReadAuditMeter.TagEntityType, entityTypeName));
            ReadAuditActivitySource.Failed(activity, ex.Message);
        }
    }
}
