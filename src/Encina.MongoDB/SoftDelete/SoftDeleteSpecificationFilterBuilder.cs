using Encina.DomainModeling;
using Encina.Messaging.SoftDelete;
using Encina.MongoDB.Repository;
using MongoDB.Driver;

namespace Encina.MongoDB.SoftDelete;

/// <summary>
/// MongoDB filter builder that automatically composes soft delete filters
/// with specification filters.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends <see cref="SpecificationFilterBuilder{TEntity}"/> to automatically
/// include soft delete filtering in all queries. The soft delete filter is composed with the
/// specification filter using MongoDB's <c>Builders&lt;TDocument&gt;.Filter.And</c>.
/// </para>
/// <para>
/// When <see cref="SoftDeleteOptions.AutoFilterSoftDeletedQueries"/> is enabled,
/// all queries will automatically include a filter like:
/// <c>{ "IsDeleted": false }</c>
/// </para>
/// <para>
/// Use <see cref="IncludeDeleted"/> to bypass the soft delete filter when needed,
/// for example when querying for soft-deleted entities or implementing restore operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var builder = new SoftDeleteSpecificationFilterBuilder&lt;Order&gt;(mapping, options);
///
/// // This filter will include both the soft delete filter and the specification filter
/// var filter = builder.BuildFilter(new ActiveOrdersSpec());
/// // Equivalent to: { IsDeleted: false, Status: "Active" }
///
/// // To include deleted entities:
/// var filterWithDeleted = builder.IncludeDeleted().BuildFilter(new OrdersByDateSpec());
/// // No soft delete filter applied
/// </code>
/// </example>
public sealed class SoftDeleteSpecificationFilterBuilder<TEntity>
    where TEntity : class
{
    private readonly ISoftDeleteEntityMapping<TEntity, object> _mapping;
    private readonly SoftDeleteOptions _options;
    private readonly SpecificationFilterBuilder<TEntity> _baseBuilder;
    private bool _includeDeleted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteSpecificationFilterBuilder{TEntity}"/> class.
    /// </summary>
    /// <param name="mapping">The soft delete entity mapping.</param>
    /// <param name="options">The soft delete options.</param>
    public SoftDeleteSpecificationFilterBuilder(
        ISoftDeleteEntityMapping<TEntity, object> mapping,
        SoftDeleteOptions options)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(options);

        _mapping = mapping;
        _options = options;
        _baseBuilder = new SpecificationFilterBuilder<TEntity>();
    }

    /// <summary>
    /// Configures this builder to include soft-deleted entities in query results.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// Call this method before <see cref="BuildFilter(Specification{TEntity})"/> to bypass the automatic
    /// soft delete filter. The setting persists for subsequent builds until a new
    /// builder instance is created.
    /// </remarks>
    public SoftDeleteSpecificationFilterBuilder<TEntity> IncludeDeleted()
    {
        _includeDeleted = true;
        return this;
    }

    /// <summary>
    /// Builds a MongoDB filter from a specification, including soft delete filtering.
    /// </summary>
    /// <param name="specification">The specification to convert.</param>
    /// <returns>A MongoDB filter definition with soft delete filtering applied.</returns>
    public FilterDefinition<TEntity> BuildFilter(Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var specFilter = _baseBuilder.BuildFilter(specification);
        var softDeleteFilter = GetSoftDeleteFilter();

        if (softDeleteFilter is null)
        {
            return specFilter;
        }

        return Builders<TEntity>.Filter.And(softDeleteFilter, specFilter);
    }

    /// <summary>
    /// Builds a MongoDB filter from a query specification, including soft delete filtering.
    /// </summary>
    /// <param name="specification">The query specification to convert.</param>
    /// <returns>A MongoDB filter definition with soft delete filtering applied.</returns>
    public FilterDefinition<TEntity> BuildFilter(QuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var specFilter = _baseBuilder.BuildFilter(specification);
        var softDeleteFilter = GetSoftDeleteFilter();

        if (softDeleteFilter is null)
        {
            return specFilter;
        }

        return Builders<TEntity>.Filter.And(softDeleteFilter, specFilter);
    }

    /// <summary>
    /// Builds a MongoDB filter from a specification, or returns the soft delete filter if null.
    /// </summary>
    /// <param name="specification">The specification to convert, or null for soft delete filter only.</param>
    /// <returns>A MongoDB filter definition.</returns>
    public FilterDefinition<TEntity> BuildFilterOrEmpty(Specification<TEntity>? specification)
    {
        var softDeleteFilter = GetSoftDeleteFilter();

        if (specification is null)
        {
            return softDeleteFilter ?? Builders<TEntity>.Filter.Empty;
        }

        var specFilter = _baseBuilder.BuildFilter(specification);

        if (softDeleteFilter is null)
        {
            return specFilter;
        }

        return Builders<TEntity>.Filter.And(softDeleteFilter, specFilter);
    }

    /// <summary>
    /// Builds only the soft delete filter without any specification.
    /// </summary>
    /// <returns>
    /// A MongoDB filter for soft delete filtering, or empty filter if soft delete
    /// is not configured or <see cref="IncludeDeleted"/> was called.
    /// </returns>
    public FilterDefinition<TEntity> BuildSoftDeleteFilter()
    {
        var filter = GetSoftDeleteFilter();
        return filter ?? Builders<TEntity>.Filter.Empty;
    }

    /// <summary>
    /// Builds a MongoDB sort definition from a query specification.
    /// </summary>
    /// <param name="specification">The query specification containing ordering expressions.</param>
    /// <returns>
    /// A MongoDB sort definition, or null if no ordering is specified.
    /// </returns>
    public SortDefinition<TEntity>? BuildSortDefinition(IQuerySpecification<TEntity> specification)
    {
        return _baseBuilder.BuildSortDefinition(specification);
    }

    private FilterDefinition<TEntity>? GetSoftDeleteFilter()
    {
        // Skip soft delete filter if:
        // - IncludeDeleted was called
        // - Entity is not soft deletable
        // - Auto-filter is disabled
        if (_includeDeleted ||
            !_mapping.IsSoftDeletable ||
            !_options.AutoFilterSoftDeletedQueries)
        {
            return null;
        }

        var fieldName = _mapping.IsDeletedFieldName;
        return Builders<TEntity>.Filter.Eq(fieldName, false);
    }
}
