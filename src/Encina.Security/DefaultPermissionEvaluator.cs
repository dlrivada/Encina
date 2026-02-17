namespace Encina.Security;

/// <summary>
/// Default implementation of <see cref="IPermissionEvaluator"/> that checks
/// permissions against the <see cref="ISecurityContext.Permissions"/> set.
/// </summary>
/// <remarks>
/// <para>
/// This implementation performs in-memory set lookups, making it suitable
/// for applications where permissions are carried in claims or tokens.
/// </para>
/// <para>
/// For external permission systems (OPA, Casbin, Azure AD), implement
/// a custom <see cref="IPermissionEvaluator"/> and register it in DI.
/// </para>
/// </remarks>
public sealed class DefaultPermissionEvaluator : IPermissionEvaluator
{
    /// <inheritdoc />
    public ValueTask<bool> HasPermissionAsync(
        ISecurityContext context,
        string permission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return ValueTask.FromResult(context.Permissions.Contains(permission));
    }

    /// <inheritdoc />
    public ValueTask<bool> HasAnyPermissionAsync(
        ISecurityContext context,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(permissions);

        var result = permissions.Any(p => context.Permissions.Contains(p));
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask<bool> HasAllPermissionsAsync(
        ISecurityContext context,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(permissions);

        var result = permissions.All(p => context.Permissions.Contains(p));
        return ValueTask.FromResult(result);
    }
}
