namespace Encina.Security;

/// <summary>
/// Evaluates whether the current user owns a given resource.
/// </summary>
/// <remarks>
/// <para>
/// Resource ownership is determined by comparing the user's identity (typically
/// <see cref="ISecurityContext.UserId"/>) against a specified property on the resource.
/// </para>
/// <para>
/// The default implementation uses cached reflection via
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>
/// to access the specified property on the resource,
/// consistent with the caching pattern used in the audit module.
/// </para>
/// <para>
/// Custom implementations can integrate with external ownership models or
/// support complex ownership hierarchies (e.g., team ownership, delegated access).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if current user owns the order via the "OwnerId" property
/// bool isOwner = await evaluator.IsOwnerAsync(context, order, "OwnerId", ct);
///
/// // Used with [RequireOwnership("OwnerId")] attribute
/// [RequireOwnership("OwnerId")]
/// public record GetOrderQuery(Guid OrderId) : IQuery&lt;OrderDto&gt;;
/// </code>
/// </example>
public interface IResourceOwnershipEvaluator
{
    /// <summary>
    /// Determines whether the current user is the owner of the specified resource.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource being checked.</typeparam>
    /// <param name="context">The security context containing user identity.</param>
    /// <param name="resource">The resource instance to verify ownership of.</param>
    /// <param name="propertyName">
    /// The name of the property on <typeparamref name="TResource"/> that contains
    /// the owner identifier (e.g., "OwnerId", "CreatedBy").
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>true</c> if the current user owns the resource; otherwise, <c>false</c>.</returns>
    ValueTask<bool> IsOwnerAsync<TResource>(
        ISecurityContext context,
        TResource resource,
        string propertyName,
        CancellationToken cancellationToken = default);
}
