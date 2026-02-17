namespace Encina.Security;

/// <summary>
/// Requires the user to have all of the specified roles (AND logic).
/// </summary>
/// <remarks>
/// <para>
/// The user must have every listed role in <see cref="ISecurityContext.Roles"/>
/// to proceed. For OR logic (any role suffices), use <see cref="RequireRoleAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // User needs BOTH "Admin" AND "Auditor" roles
/// [RequireAllRoles("Admin", "Auditor")]
/// public sealed record ViewSensitiveDataQuery(Guid EntityId) : IQuery&lt;SensitiveDataDto&gt;;
/// </code>
/// </example>
public sealed class RequireAllRolesAttribute : SecurityAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireAllRolesAttribute"/> class.
    /// </summary>
    /// <param name="roles">Roles that are all required for access.</param>
    public RequireAllRolesAttribute(params string[] roles)
    {
        Roles = roles;
    }

    /// <summary>
    /// Gets the roles that are all required for access to the request.
    /// </summary>
    public string[] Roles { get; }
}
