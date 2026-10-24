namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// The outcome of running the <see cref="TenantResolverChain"/> on one request.
/// </summary>
/// <param name="TenantId">
/// The resolved tenant, or <c>null</c> when no resolver produced one or when the request has a mismatch.
/// </param>
/// <param name="AuthoritativeResolver">
/// The type name of the resolver that supplied the verified tenant, or <c>null</c> when no
/// authoritative resolver produced a value.
/// </param>
/// <param name="ConflictingResolver">
/// The type name of the first non-authoritative resolver whose value differs from the verified tenant,
/// or <c>null</c> when there is no mismatch. Never a tenant value.
/// </param>
internal readonly record struct TenantResolution(
    string? TenantId,
    string? AuthoritativeResolver,
    string? ConflictingResolver)
{
    /// <summary>
    /// Gets a value indicating whether a client-supplied tenant disagrees with the verified tenant.
    /// </summary>
    public bool IsMismatch => ConflictingResolver is not null;

    /// <summary>
    /// Gets the outcome for a request where no resolver produced a tenant.
    /// </summary>
    public static TenantResolution None => default;
}
