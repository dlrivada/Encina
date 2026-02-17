namespace Encina.Security;

/// <summary>
/// Requires the user to have at least one of the specified roles (OR logic).
/// </summary>
/// <remarks>
/// <para>
/// The user must have at least one of the listed roles in <see cref="ISecurityContext.Roles"/>
/// to proceed. For AND logic (all roles required), use <see cref="RequireAllRolesAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // User needs "Admin" OR "Manager" role
/// [RequireRole("Admin", "Manager")]
/// public sealed record ApproveOrderCommand(Guid OrderId) : ICommand;
/// </code>
/// </example>
public sealed class RequireRoleAttribute : SecurityAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireRoleAttribute"/> class.
    /// </summary>
    /// <param name="roles">One or more roles, any of which grants access.</param>
    public RequireRoleAttribute(params string[] roles)
    {
        Roles = roles;
    }

    /// <summary>
    /// Gets the roles, any of which grants access to the request.
    /// </summary>
    public string[] Roles { get; }
}
