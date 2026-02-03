namespace Encina.Messaging.SoftDelete;

/// <summary>
/// Provides scoped context for soft delete filter state during request processing.
/// </summary>
/// <remarks>
/// <para>
/// This service communicates the soft delete filter state from pipeline behaviors
/// (such as <c>SoftDeleteQueryFilterBehavior</c>) to repository implementations.
/// Repositories check this context to determine whether to apply soft delete filters.
/// </para>
/// <para>
/// <b>Lifetime:</b> This service is registered with scoped lifetime, meaning each HTTP
/// request or message processing scope gets its own instance. This ensures thread safety
/// and isolation between concurrent requests.
/// </para>
/// <para>
/// <b>Usage Pattern:</b>
/// <list type="number">
/// <item><description>Pipeline behavior sets <see cref="IncludeDeleted"/> based on request type</description></item>
/// <item><description>Repository reads <see cref="IncludeDeleted"/> to apply/bypass filters</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a pipeline behavior:
/// public class SoftDeleteQueryFilterBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
/// {
///     private readonly ISoftDeleteFilterContext _filterContext;
///
///     public async ValueTask&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(...)
///     {
///         if (request is IIncludeDeleted includeDeleted)
///         {
///             _filterContext.IncludeDeleted = includeDeleted.IncludeDeleted;
///         }
///         return await nextStep();
///     }
/// }
///
/// // In a repository:
/// public class SoftDeletableRepository&lt;T&gt;
/// {
///     private readonly ISoftDeleteFilterContext _filterContext;
///
///     public async Task&lt;IReadOnlyList&lt;T&gt;&gt; ListAsync()
///     {
///         if (_filterContext.IncludeDeleted)
///         {
///             return await _dbSet.ToListAsync(); // No filter
///         }
///         return await _dbSet.Where(e => !e.IsDeleted).ToListAsync();
///     }
/// }
/// </code>
/// </example>
public interface ISoftDeleteFilterContext
{
    /// <summary>
    /// Gets or sets a value indicating whether soft-deleted entities should be included in query results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, repositories should bypass soft delete filters and include entities
    /// where <c>IsDeleted = true</c>.
    /// </para>
    /// <para>
    /// When <c>false</c> (default), normal soft delete filtering applies, excluding
    /// soft-deleted entities from results.
    /// </para>
    /// <para>
    /// This value is typically set by a pipeline behavior based on whether the current
    /// request implements <see cref="Encina.IIncludeDeleted"/>.
    /// </para>
    /// </remarks>
    bool IncludeDeleted { get; set; }

    /// <summary>
    /// Resets the filter context to its default state.
    /// </summary>
    /// <remarks>
    /// This method resets <see cref="IncludeDeleted"/> to <c>false</c>.
    /// Useful when recycling scoped instances or in testing scenarios.
    /// </remarks>
    void Reset();
}
