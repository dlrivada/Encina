namespace Encina.Messaging.Tenancy;

/// <summary>
/// Marks a container in which multi-tenancy is in use, so that messaging services can fail closed
/// when a tenant is required but none is resolved.
/// </summary>
/// <remarks>
/// <c>AddEncinaTenancy</c> registers <see cref="Instance"/>. A service that has this marker registered
/// and finds no tenant denies the operation unless the caller opts out explicitly; with no marker the
/// service behaves as for a single-tenant application. The marker carries no state.
/// </remarks>
public sealed class TenancyInUse
{
    private TenancyInUse()
    {
    }

    /// <summary>
    /// Gets the single marker instance.
    /// </summary>
    public static TenancyInUse Instance { get; } = new();
}
