namespace Encina;

/// <summary>
/// Declares the roles, permissions and claims of a service identity in
/// <c>AddEncinaServiceIdentity</c>.
/// </summary>
/// <remarks>
/// Declare the least authority the job needs. Wildcard entries (<c>*</c>) are rejected at startup.
/// The tenant is not part of the declaration: it is chosen per scope
/// (<see cref="IdentityScopeOptions.TenantId"/>).
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaServiceIdentity("billing-reconciliation", id => id
///     .WithRoles("billing-job")
///     .WithPermissions("invoices:reconcile")
///     .WithClaim("department", "finance"));
/// </code>
/// </example>
public sealed class ServiceIdentityBuilder
{
    private readonly List<string> _roles = [];
    private readonly List<string> _permissions = [];
    private readonly List<KeyValuePair<string, string>> _claims = [];

    /// <summary>
    /// Adds roles to the service identity.
    /// </summary>
    /// <param name="roles">The roles.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="roles"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A role is null, empty or whitespace.</exception>
    public ServiceIdentityBuilder WithRoles(params string[] roles)
    {
        AddEntries(_roles, roles, nameof(roles));
        return this;
    }

    /// <summary>
    /// Adds permissions to the service identity.
    /// </summary>
    /// <param name="permissions">The permissions.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="permissions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A permission is null, empty or whitespace.</exception>
    public ServiceIdentityBuilder WithPermissions(params string[] permissions)
    {
        AddEntries(_permissions, permissions, nameof(permissions));
        return this;
    }

    /// <summary>
    /// Adds a claim to the principal of the service identity.
    /// </summary>
    /// <param name="type">The claim type. It must not be a user-id claim type (validated at startup).</param>
    /// <param name="value">The claim value.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="type"/> or <paramref name="value"/> is null, empty or whitespace.</exception>
    public ServiceIdentityBuilder WithClaim(string type, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        _claims.Add(new KeyValuePair<string, string>(type, value));
        return this;
    }

    /// <summary>
    /// Builds the declaration.
    /// </summary>
    internal ServiceIdentityDefinition Build(string name, bool isBuiltIn) =>
        new(name, _roles, _permissions, _claims, isBuiltIn);

    private static void AddEntries(List<string> target, string[] entries, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(entries, parameterName);

        if (Array.Exists(entries, string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Entries must not be null, empty or whitespace.", parameterName);
        }

        target.AddRange(entries);
    }
}
