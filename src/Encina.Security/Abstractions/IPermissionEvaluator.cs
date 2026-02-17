namespace Encina.Security;

/// <summary>
/// Evaluates permissions for a given security context.
/// </summary>
/// <remarks>
/// <para>
/// Implementations determine whether a user has specific permissions based on the
/// application's authorization model. The default implementation checks the
/// <see cref="ISecurityContext.Permissions"/> set directly.
/// </para>
/// <para>
/// Custom implementations can integrate with external authorization services
/// (e.g., OPA, Casbin, Azure AD) or implement hierarchical permission models.
/// </para>
/// <para>
/// Methods return <see cref="ValueTask{TResult}"/> for efficiency when the evaluation
/// can be completed synchronously (e.g., in-memory permission checks).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple permission check
/// bool canRead = await evaluator.HasPermissionAsync(context, "orders:read", ct);
///
/// // Check if user has any of the specified permissions (OR logic)
/// bool canModify = await evaluator.HasAnyPermissionAsync(
///     context, ["orders:update", "orders:admin"], ct);
///
/// // Check if user has all specified permissions (AND logic)
/// bool canFullAccess = await evaluator.HasAllPermissionsAsync(
///     context, ["orders:read", "orders:write", "orders:delete"], ct);
/// </code>
/// </example>
public interface IPermissionEvaluator
{
    /// <summary>
    /// Determines whether the user in the given security context has the specified permission.
    /// </summary>
    /// <param name="context">The security context containing user identity and permissions.</param>
    /// <param name="permission">The permission to check (e.g., "orders:read").</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>true</c> if the user has the specified permission; otherwise, <c>false</c>.</returns>
    ValueTask<bool> HasPermissionAsync(
        ISecurityContext context,
        string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the user has at least one of the specified permissions (OR logic).
    /// </summary>
    /// <param name="context">The security context containing user identity and permissions.</param>
    /// <param name="permissions">The permissions to check. Returns <c>true</c> if the user has any one of them.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>true</c> if the user has at least one of the specified permissions; otherwise, <c>false</c>.</returns>
    ValueTask<bool> HasAnyPermissionAsync(
        ISecurityContext context,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the user has all of the specified permissions (AND logic).
    /// </summary>
    /// <param name="context">The security context containing user identity and permissions.</param>
    /// <param name="permissions">The permissions to check. Returns <c>true</c> only if the user has all of them.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>true</c> if the user has all of the specified permissions; otherwise, <c>false</c>.</returns>
    ValueTask<bool> HasAllPermissionsAsync(
        ISecurityContext context,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);
}
