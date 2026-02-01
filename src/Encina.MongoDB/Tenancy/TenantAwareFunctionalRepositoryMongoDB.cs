using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Tenancy;
using LanguageExt;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Tenancy;

/// <summary>
/// Tenant-aware MongoDB implementation of <see cref="IFunctionalRepository{TEntity, TId}"/>
/// with automatic tenant filtering, assignment, and validation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository extends the standard MongoDB repository with multi-tenancy support:
/// </para>
/// <list type="bullet">
/// <item><b>Query Filtering:</b> All queries automatically include tenant filter using <c>Filter.Eq("TenantId", tenantId)</c></item>
/// <item><b>Insert Assignment:</b> New entities automatically get <c>TenantId</c> set from current context</item>
/// <item><b>Modify Validation:</b> Updates/deletes validate that the entity belongs to the current tenant</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register tenant-aware repository
/// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =&gt;
///     mapping.ToCollection("orders")
///            .HasId(o =&gt; o.Id)
///            .HasTenantId(o =&gt; o.TenantId)
///            .MapField(o =&gt; o.Total));
///
/// // Use in service - all operations are automatically tenant-scoped
/// public class OrderService(IFunctionalRepository&lt;Order, Guid&gt; repository)
/// {
///     public Task&lt;Either&lt;EncinaError, IReadOnlyList&lt;Order&gt;&gt;&gt; GetOrdersAsync(CancellationToken ct)
///         =&gt; repository.ListAsync(ct);
/// }
/// </code>
/// </example>
public sealed class TenantAwareFunctionalRepositoryMongoDB<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly ITenantEntityMapping<TEntity, TId> _mapping;
    private readonly ITenantProvider _tenantProvider;
    private readonly MongoDbTenancyOptions _options;
    private readonly Expression<Func<TEntity, TId>> _idSelector;
    private readonly Func<TEntity, TId> _compiledIdSelector;
    private readonly TenantAwareSpecificationFilterBuilder<TEntity> _filterBuilder;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareFunctionalRepositoryMongoDB{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection.</param>
    /// <param name="mapping">The tenant-aware entity mapping.</param>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="options">The MongoDB tenancy options.</param>
    /// <param name="idSelector">Expression to select the ID property from an entity.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    public TenantAwareFunctionalRepositoryMongoDB(
        IMongoCollection<TEntity> collection,
        ITenantEntityMapping<TEntity, TId> mapping,
        ITenantProvider tenantProvider,
        MongoDbTenancyOptions options,
        Expression<Func<TEntity, TId>> idSelector,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(idSelector);

        _collection = collection;
        _mapping = mapping;
        _tenantProvider = tenantProvider;
        _options = options;
        _idSelector = idSelector;
        _compiledIdSelector = idSelector.Compile();
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;

        // Create generic mapping adapter for filter builder
        var genericMapping = new GenericTenantMappingAdapter<TEntity, TId>(mapping);
        _filterBuilder = new TenantAwareSpecificationFilterBuilder<TEntity>(genericMapping, tenantProvider, options);
    }

    #region Read Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var idFilter = BuildIdFilter(id);
            var tenantFilter = _filterBuilder.BuildTenantFilter();
            var combinedFilter = Builders<TEntity>.Filter.And(idFilter, tenantFilter);

            var entity = await _collection.Find(combinedFilter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity, TId>(id, "GetById", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = _filterBuilder.BuildTenantFilter();
            var entities = await _collection.Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("List", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var entities = await _collection.Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.InvalidOperation<TEntity>("List", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("List", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> FirstOrDefaultAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var entity = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(
                    RepositoryErrors.NotFound<TEntity>($"specification: {specification.GetType().Name}"));
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.InvalidOperation<TEntity>("FirstOrDefault", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("FirstOrDefault", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = _filterBuilder.BuildTenantFilter();
            var count = await _collection.CountDocumentsAsync(
                filter,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>((int)count);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("Count", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var count = await _collection.CountDocumentsAsync(
                filter,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>((int)count);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.InvalidOperation<TEntity>("Count", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("Count", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var count = await _collection.CountDocumentsAsync(
                filter,
                new CountOptions { Limit = 1 },
                cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(count > 0);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.InvalidOperation<TEntity>("Any", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.PersistenceError<TEntity>("Any", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = _filterBuilder.BuildTenantFilter();
            var count = await _collection.CountDocumentsAsync(
                filter,
                new CountOptions { Limit = 1 },
                cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(count > 0);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.PersistenceError<TEntity>("Any", ex));
        }
    }

    #endregion

    #region Write Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Auto-assign tenant ID if enabled
            AssignTenantIdIfNeeded(entity);

            // Populate audit fields before persistence
            AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);

            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return Right<EncinaError, TEntity>(entity);
        }
        catch (MongoWriteException ex) when (IsDuplicateKeyException(ex))
        {
            var id = _compiledIdSelector(entity);
            return Left<EncinaError, TEntity>(RepositoryErrors.AlreadyExists<TEntity, TId>(id));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Add", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Validate tenant ownership if enabled
            var validationResult = ValidateTenantOwnership(entity);
            if (validationResult.IsLeft)
            {
                return validationResult.Map(_ => entity);
            }

            // Populate audit fields before persistence
            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);

            var id = _compiledIdSelector(entity);
            var idFilter = BuildIdFilter(id);
            var tenantFilter = _filterBuilder.BuildTenantFilter();
            var combinedFilter = Builders<TEntity>.Filter.And(idFilter, tenantFilter);

            var result = await _collection.ReplaceOneAsync(
                combinedFilter,
                entity,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            return Right<EncinaError, TEntity>(entity);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Update", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var idFilter = BuildIdFilter(id);
            var tenantFilter = _filterBuilder.BuildTenantFilter();
            var combinedFilter = Builders<TEntity>.Filter.And(idFilter, tenantFilter);

            var result = await _collection.DeleteOneAsync(combinedFilter, cancellationToken)
                .ConfigureAwait(false);

            if (result.DeletedCount == 0)
            {
                return Left<EncinaError, Unit>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity, TId>(id, "Delete", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Validate tenant ownership if enabled
        var validationResult = ValidateTenantOwnership(entity);
        if (validationResult.IsLeft)
        {
            return validationResult;
        }

        var id = _compiledIdSelector(entity);
        return await DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            // Auto-assign tenant ID to all entities if enabled
            foreach (var entity in entityList)
            {
                AssignTenantIdIfNeeded(entity);
                AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);
            }

            await _collection.InsertManyAsync(entityList, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entityList);
        }
        catch (MongoBulkWriteException ex) when (HasDuplicateKeyError(ex))
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.AlreadyExists<TEntity>("One or more entities already exist"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("AddRange", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            // Validate tenant ownership for all entities if enabled
            foreach (var entity in entityList)
            {
                var validationResult = ValidateTenantOwnership(entity);
                if (validationResult.IsLeft)
                {
                    return validationResult;
                }
            }

            // Populate audit fields before persistence
            foreach (var entity in entityList)
            {
                AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);
            }

            var tenantFilter = _filterBuilder.BuildTenantFilter();

            var bulkOps = entityList.Select(entity =>
            {
                var id = _compiledIdSelector(entity);
                var idFilter = BuildIdFilter(id);
                var combinedFilter = Builders<TEntity>.Filter.And(idFilter, tenantFilter);
                return new ReplaceOneModel<TEntity>(combinedFilter, entity) { IsUpsert = false };
            }).ToList();

            if (bulkOps.Count > 0)
            {
                await _collection.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("UpdateRange", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);

            // Safety check: prevent accidental deletion of all documents
            if (filter == Builders<TEntity>.Filter.Empty)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.InvalidOperation<TEntity>("DeleteRange",
                        "DELETE requires a filter to prevent accidental data loss."));
            }

            var result = await _collection.DeleteManyAsync(filter, cancellationToken)
                .ConfigureAwait(false);
            return Right<EncinaError, int>((int)result.DeletedCount);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.InvalidOperation<TEntity>("DeleteRange", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("DeleteRange", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation is not supported in MongoDB providers because they don't have change tracking.
    /// Use EF Core providers for immutable record support.
    /// </remarks>
    public Task<Either<EncinaError, Unit>> UpdateImmutableAsync(
        TEntity modified,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modified);
        return Task.FromResult<Either<EncinaError, Unit>>(
            RepositoryErrors.OperationNotSupported<TEntity>("UpdateImmutableAsync"));
    }

    #endregion

    #region Private Helpers

    private FilterDefinition<TEntity> BuildIdFilter(TId id)
    {
        // Build a filter using the ID selector expression
        var parameter = _idSelector.Parameters[0];
        var body = Expression.Equal(_idSelector.Body, Expression.Constant(id, typeof(TId)));
        var lambda = Expression.Lambda<Func<TEntity, bool>>(body, parameter);

        return Builders<TEntity>.Filter.Where(lambda);
    }

    private void AssignTenantIdIfNeeded(TEntity entity)
    {
        if (!_mapping.IsTenantEntity || !_options.AutoAssignTenantId)
        {
            return;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (string.IsNullOrEmpty(tenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot add tenant entity {typeof(TEntity).Name} without tenant context.");
            }

            return;
        }

        _mapping.SetTenantId(entity, tenantId);
    }

    private Either<EncinaError, Unit> ValidateTenantOwnership(TEntity entity)
    {
        if (!_mapping.IsTenantEntity || !_options.ValidateTenantOnModify)
        {
            return Right<EncinaError, Unit>(Unit.Default);
        }

        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        var entityTenantId = _mapping.GetTenantId(entity);

        if (string.IsNullOrEmpty(currentTenantId))
        {
            if (_options.ThrowOnMissingTenantContext)
            {
                return Left<EncinaError, Unit>(
                    RepositoryErrors.InvalidOperation<TEntity>(
                        "Modify",
                        $"Cannot modify tenant entity {typeof(TEntity).Name} without tenant context."));
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }

        if (!string.Equals(currentTenantId, entityTenantId, StringComparison.Ordinal))
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.InvalidOperation<TEntity>(
                    "Modify",
                    $"Tenant mismatch: entity belongs to tenant '{entityTenantId}' but current tenant is '{currentTenantId}'."));
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }

    private static bool IsDuplicateKeyException(MongoWriteException ex)
    {
        return ex.WriteError?.Category == ServerErrorCategory.DuplicateKey;
    }

    private static bool HasDuplicateKeyError(MongoBulkWriteException ex)
    {
        return ex.WriteErrors?.Any(e => e.Category == ServerErrorCategory.DuplicateKey) == true;
    }

    #endregion
}

/// <summary>
/// Adapter to convert generic TId mapping to object-based mapping for filter builder.
/// </summary>
internal sealed class GenericTenantMappingAdapter<TEntity, TId> : ITenantEntityMapping<TEntity, object>
    where TEntity : class
    where TId : notnull
{
    private readonly ITenantEntityMapping<TEntity, TId> _innerMapping;

    public GenericTenantMappingAdapter(ITenantEntityMapping<TEntity, TId> innerMapping)
    {
        _innerMapping = innerMapping;
    }

    public string CollectionName => _innerMapping.CollectionName;
    public string IdFieldName => _innerMapping.IdFieldName;
    public IReadOnlyDictionary<string, string> FieldMappings => _innerMapping.FieldMappings;
    public bool IsTenantEntity => _innerMapping.IsTenantEntity;
    public string? TenantFieldName => _innerMapping.TenantFieldName;
    public string? TenantPropertyName => _innerMapping.TenantPropertyName;

    public object GetId(TEntity entity) => _innerMapping.GetId(entity)!;
    public string? GetTenantId(TEntity entity) => _innerMapping.GetTenantId(entity);
    public void SetTenantId(TEntity entity, string tenantId) => _innerMapping.SetTenantId(entity, tenantId);
}
