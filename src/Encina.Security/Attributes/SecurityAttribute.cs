namespace Encina.Security;

/// <summary>
/// Base class for all Encina security attributes applied to request classes.
/// </summary>
/// <remarks>
/// <para>
/// Security attributes are discovered by <c>SecurityPipelineBehavior</c> at runtime
/// and evaluated in <see cref="Order"/> sequence (lowest first). Multiple attributes
/// can be combined on a single request class for layered security checks.
/// </para>
/// <para>
/// <b>Evaluation Order:</b>
/// <list type="number">
/// <item><description><see cref="DenyAnonymousAttribute"/> — authentication gate</description></item>
/// <item><description><see cref="RequireRoleAttribute"/> / <see cref="RequireAllRolesAttribute"/> — role checks</description></item>
/// <item><description><see cref="RequirePermissionAttribute"/> — permission checks</description></item>
/// <item><description><see cref="RequireClaimAttribute"/> — claim checks</description></item>
/// <item><description><see cref="RequireOwnershipAttribute"/> — resource ownership</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Combine multiple security attributes
/// [DenyAnonymous]
/// [RequirePermission("orders:read")]
/// [RequireRole("Manager", "Admin")]
/// public sealed record GetOrderQuery(Guid OrderId) : IQuery&lt;OrderDto&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class SecurityAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the evaluation order for this security attribute.
    /// </summary>
    /// <remarks>
    /// Lower values are evaluated first. Default is <c>0</c>.
    /// Use this to control the sequence when multiple security attributes are applied.
    /// </remarks>
    public int Order { get; set; }
}
