namespace SimpleMediator.Caching;

/// <summary>
/// Generates cache keys for requests.
/// </summary>
/// <remarks>
/// <para>
/// Cache key generation is critical for effective caching. Keys must be:
/// </para>
/// <list type="bullet">
/// <item><description>Unique - Different requests produce different keys</description></item>
/// <item><description>Deterministic - Same request always produces the same key</description></item>
/// <item><description>Hierarchical - Support pattern-based invalidation</description></item>
/// </list>
/// <para>
/// The default implementation uses the format: <c>{tenant}:{request-type}:{hash}</c>
/// where the hash is computed from the request properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom cache key generator for specific request types
/// public class ProductCacheKeyGenerator : ICacheKeyGenerator
/// {
///     public string GenerateKey&lt;TRequest, TResponse&gt;(TRequest request, IRequestContext context)
///         where TRequest : IRequest&lt;TResponse&gt;
///     {
///         if (request is GetProductQuery query)
///         {
///             return $"tenant:{context.TenantId}:product:{query.ProductId}";
///         }
///
///         if (request is GetProductsQuery productsQuery)
///         {
///             return $"tenant:{context.TenantId}:products:{productsQuery.CategoryId}:{productsQuery.Page}";
///         }
///
///         // Fall back to default behavior
///         return GenerateDefaultKey(request, context);
///     }
/// }
/// </code>
/// </example>
public interface ICacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for the given request.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request to generate a key for.</param>
    /// <param name="context">The request context containing tenant, user, and other metadata.</param>
    /// <returns>A unique cache key for the request.</returns>
    string GenerateKey<TRequest, TResponse>(TRequest request, IRequestContext context)
        where TRequest : IRequest<TResponse>;

    /// <summary>
    /// Generates a cache key pattern for invalidation.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <param name="context">The request context containing tenant, user, and other metadata.</param>
    /// <returns>A pattern that matches all cache keys for the request type.</returns>
    /// <remarks>
    /// This is used when invalidating all cached responses for a specific request type.
    /// The pattern should use glob-style wildcards (e.g., <c>tenant:*:product:*</c>).
    /// </remarks>
    string GeneratePattern<TRequest>(IRequestContext context);

    /// <summary>
    /// Generates a cache key pattern from a template and request.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <param name="keyTemplate">The pattern template (e.g., "product:{ProductId}:*").</param>
    /// <param name="request">The request containing values to substitute.</param>
    /// <param name="context">The request context containing tenant, user, and other metadata.</param>
    /// <returns>A pattern with placeholders replaced by actual values.</returns>
    /// <remarks>
    /// Template placeholders are property names wrapped in braces (e.g., <c>{ProductId}</c>).
    /// The method substitutes these with the corresponding property values from the request.
    /// </remarks>
    string GeneratePatternFromTemplate<TRequest>(string keyTemplate, TRequest request, IRequestContext context);
}
