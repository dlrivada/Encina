namespace Encina.Security.ABAC;

/// <summary>
/// Provides attribute values for ABAC policy evaluation by collecting attributes
/// from application-specific sources.
/// </summary>
/// <remarks>
/// <para>
/// Implementations bridge the gap between the application's domain model and the
/// XACML attribute model. They extract subject, resource, and environment attributes
/// from application-specific sources (databases, HTTP context, claims, etc.) and
/// return them as key-value dictionaries.
/// </para>
/// <para>
/// The ABAC pipeline uses <see cref="IAttributeProvider"/> to populate the
/// <see cref="PolicyEvaluationContext"/> before sending it to the
/// <see cref="IPolicyDecisionPoint"/> for evaluation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomAttributeProvider : IAttributeProvider
/// {
///     public async ValueTask&lt;IReadOnlyDictionary&lt;string, object&gt;&gt; GetSubjectAttributesAsync(
///         string userId, CancellationToken ct)
///     {
///         var user = await _userService.GetAsync(userId, ct);
///         return new Dictionary&lt;string, object&gt;
///         {
///             ["department"] = user.Department,
///             ["clearanceLevel"] = user.ClearanceLevel
///         };
///     }
/// }
/// </code>
/// </example>
public interface IAttributeProvider
{
    /// <summary>
    /// Retrieves attributes describing the subject (user or service) making the access request.
    /// </summary>
    /// <param name="userId">The identifier of the subject.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A dictionary of attribute names to their values.</returns>
    ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves attributes describing the resource being accessed.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource being accessed.</typeparam>
    /// <param name="resource">The resource instance to extract attributes from.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A dictionary of attribute names to their values.</returns>
    ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves attributes describing the current environmental conditions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A dictionary of attribute names to their values (e.g., current time, IP address).</returns>
    ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default);
}
