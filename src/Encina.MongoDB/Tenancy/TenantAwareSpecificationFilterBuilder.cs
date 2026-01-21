using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using Encina.Tenancy;
using MongoDB.Driver;

namespace Encina.MongoDB.Tenancy;

/// <summary>
/// Tenant-aware MongoDB filter builder that automatically composes tenant filters
/// with specification filters.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends <see cref="SpecificationFilterBuilder{TEntity}"/> to automatically
/// include tenant filtering in all queries. The tenant filter is composed with the
/// specification filter using MongoDB's <c>Builders&lt;TDocument&gt;.Filter.And</c>.
/// </para>
/// <para>
/// When <see cref="MongoDbTenancyOptions.AutoFilterTenantQueries"/> is enabled,
/// all queries will automatically include a filter like:
/// <c>{ "TenantId": "tenant-123" }</c>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var builder = new TenantAwareSpecificationFilterBuilder&lt;Order&gt;(
///     mapping, tenantProvider, tenancyOptions);
///
/// // This filter will include both the tenant filter and the specification filter
/// var filter = builder.BuildFilter(new ActiveOrdersSpec());
/// // Equivalent to: { TenantId: "tenant-123", Status: "Active" }
/// </code>
/// </example>
public sealed class TenantAwareSpecificationFilterBuilder<TEntity>
    where TEntity : class
{
    private readonly ITenantEntityMapping<TEntity, object> _mapping;
    private readonly ITenantProvider _tenantProvider;
    private readonly MongoDbTenancyOptions _options;
    private readonly SpecificationFilterBuilder<TEntity> _baseBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareSpecificationFilterBuilder{TEntity}"/> class.
    /// </summary>
    /// <param name="mapping">The tenant entity mapping.</param>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="options">The MongoDB tenancy options.</param>
    public TenantAwareSpecificationFilterBuilder(
        ITenantEntityMapping<TEntity, object> mapping,
        ITenantProvider tenantProvider,
        MongoDbTenancyOptions options)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(options);

        _mapping = mapping;
        _tenantProvider = tenantProvider;
        _options = options;
        _baseBuilder = new SpecificationFilterBuilder<TEntity>();
    }

    /// <summary>
    /// Builds a MongoDB filter from a specification, including tenant filtering.
    /// </summary>
    /// <param name="specification">The specification to convert.</param>
    /// <returns>A MongoDB filter definition with tenant filtering applied.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when tenant context is required but not available and
    /// <see cref="MongoDbTenancyOptions.ThrowOnMissingTenantContext"/> is <c>true</c>.
    /// </exception>
    public FilterDefinition<TEntity> BuildFilter(Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var specFilter = _baseBuilder.BuildFilter(specification);
        var tenantFilter = GetTenantFilter();

        if (tenantFilter is null)
        {
            return specFilter;
        }

        return Builders<TEntity>.Filter.And(tenantFilter, specFilter);
    }

    /// <summary>
    /// Builds a MongoDB filter for the current tenant only (no specification).
    /// </summary>
    /// <returns>A MongoDB filter for tenant filtering, or empty filter if no tenant context.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when tenant context is required but not available and
    /// <see cref="MongoDbTenancyOptions.ThrowOnMissingTenantContext"/> is <c>true</c>.
    /// </exception>
    public FilterDefinition<TEntity> BuildTenantFilter()
    {
        var tenantFilter = GetTenantFilter();
        return tenantFilter ?? Builders<TEntity>.Filter.Empty;
    }

    /// <summary>
    /// Builds a MongoDB filter from a specification, optionally including tenant filtering.
    /// </summary>
    /// <param name="specification">The specification to convert, or null for no spec filter.</param>
    /// <returns>A tuple containing the filter and the current tenant ID (if any).</returns>
    public (FilterDefinition<TEntity> Filter, string? TenantId) BuildFilterWithTenantId(
        Specification<TEntity>? specification)
    {
        var tenantId = GetCurrentTenantId();
        var tenantFilter = GetTenantFilter(tenantId);

        FilterDefinition<TEntity> finalFilter;

        if (specification is not null)
        {
            var specFilter = _baseBuilder.BuildFilter(specification);
            finalFilter = tenantFilter is not null
                ? Builders<TEntity>.Filter.And(tenantFilter, specFilter)
                : specFilter;
        }
        else
        {
            finalFilter = tenantFilter ?? Builders<TEntity>.Filter.Empty;
        }

        return (finalFilter, tenantId);
    }

    /// <summary>
    /// Gets the current tenant ID from the provider, validating context if required.
    /// </summary>
    /// <returns>The current tenant ID, or null if no tenant context and not required to throw.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when tenant context is required but not available.
    /// </exception>
    public string? GetCurrentTenantId()
    {
        if (!_mapping.IsTenantEntity || !_options.AutoFilterTenantQueries)
        {
            return null;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (string.IsNullOrEmpty(tenantId) && _options.ThrowOnMissingTenantContext)
        {
            throw new InvalidOperationException(
                $"Cannot execute query on tenant entity {typeof(TEntity).Name} without tenant context.");
        }

        return tenantId;
    }

    private FilterDefinition<TEntity>? GetTenantFilter()
    {
        var tenantId = GetCurrentTenantId();
        return GetTenantFilter(tenantId);
    }

    private FilterDefinition<TEntity>? GetTenantFilter(string? tenantId)
    {
        if (!_mapping.IsTenantEntity || !_options.AutoFilterTenantQueries)
        {
            return null;
        }

        if (string.IsNullOrEmpty(tenantId))
        {
            return null;
        }

        var fieldName = _mapping.TenantFieldName ?? _options.TenantFieldName;
        return Builders<TEntity>.Filter.Eq(fieldName, tenantId);
    }
}
