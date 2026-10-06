using System.Collections.Frozen;

namespace Encina;

/// <summary>
/// A service identity declared at startup with <c>AddEncinaServiceIdentity</c>: its name and the
/// roles, permissions and claims it holds.
/// </summary>
/// <remarks>
/// <para>
/// The declaration is the whole authority of the service: a scope opened with
/// <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/> carries exactly these roles,
/// permissions and claims, under the subject <c>service:&lt;name&gt;</c>. Declarations are
/// reviewable in one place and validated at startup.
/// </para>
/// <para>
/// Instances are built by <see cref="ServiceIdentityBuilder"/>; application code reads them through
/// <see cref="IServiceIdentityCatalog"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// if (catalog.TryGet("billing-reconciliation", out var definition))
/// {
///     var canReconcile = definition.Permissions.Contains("invoices:reconcile");
/// }
/// </code>
/// </example>
public sealed class ServiceIdentityDefinition
{
    internal ServiceIdentityDefinition(
        string name,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        IEnumerable<KeyValuePair<string, string>> claims,
        bool isBuiltIn)
    {
        Name = name;
        Roles = roles.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        Permissions = permissions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        Claims = [.. claims];
        IsBuiltIn = isBuiltIn;
    }

    /// <summary>
    /// Gets the declared name (pattern <c>^[a-z0-9][a-z0-9.-]{0,62}$</c>). The subject of the
    /// identity is <c>service:</c> followed by this name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the declared roles (case-insensitive).
    /// </summary>
    public IReadOnlySet<string> Roles { get; }

    /// <summary>
    /// Gets the declared permissions (case-insensitive).
    /// </summary>
    public IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// Gets the declared extra claims (type and value), in declaration order.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> Claims { get; }

    /// <summary>
    /// Gets a value indicating whether this is a built-in identity of an Encina package (name prefix
    /// <c>encina.</c>), opened only through the internal scope API.
    /// </summary>
    internal bool IsBuiltIn { get; }

    /// <summary>
    /// Returns the declared name only; roles, permissions and claims are not printed.
    /// </summary>
    /// <returns>A string such as <c>ServiceIdentityDefinition { Name = billing-reconciliation }</c>.</returns>
    public override string ToString() => $"ServiceIdentityDefinition {{ Name = {Name} }}";

    /// <summary>
    /// Determines whether <paramref name="other"/> declares the same identity (same name, roles,
    /// permissions, claims and built-in flag), so a repeated identical declaration is not a conflict.
    /// </summary>
    internal bool IsEquivalentTo(ServiceIdentityDefinition other) =>
        string.Equals(Name, other.Name, StringComparison.Ordinal)
        && IsBuiltIn == other.IsBuiltIn
        && Roles.SetEquals(other.Roles)
        && Permissions.SetEquals(other.Permissions)
        && Claims.ToHashSet().SetEquals(other.Claims);
}
