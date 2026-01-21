using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Resolves tenant identifier from a user claim.
/// </summary>
/// <remarks>
/// <para>
/// This resolver extracts the tenant ID from the authenticated user's claims.
/// The default claim type is <c>tenant_id</c>.
/// </para>
/// <para>
/// This approach is useful when tenant association is part of the user identity,
/// such as when users belong to a specific organization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // JWT token with tenant claim
/// {
///   "sub": "user123",
///   "tenant_id": "acme-corp",
///   "role": "admin"
/// }
///
/// // Configuration
/// services.AddEncinaTenancyAspNetCore(options =>
/// {
///     options.ClaimResolver.ClaimType = "org_id";
/// });
/// </code>
/// </example>
public sealed class ClaimTenantResolver : ITenantResolver
{
    /// <summary>
    /// The default claim type for tenant identification.
    /// </summary>
    public const string DefaultClaimType = "tenant_id";

    /// <summary>
    /// The default priority for this resolver.
    /// </summary>
    public const int DefaultPriority = 110;

    private readonly TenancyAspNetCoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimTenantResolver"/> class.
    /// </summary>
    /// <param name="options">The tenancy options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public ClaimTenantResolver(IOptions<TenancyAspNetCoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public int Priority => _options.ClaimResolver.Priority;

    /// <inheritdoc />
    public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.ClaimResolver.Enabled)
        {
            return ValueTask.FromResult<string?>(null);
        }

        var user = context.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return ValueTask.FromResult<string?>(null);
        }

        var claimType = _options.ClaimResolver.ClaimType;
        var claim = user.FindFirst(claimType);

        if (claim is not null && !string.IsNullOrWhiteSpace(claim.Value))
        {
            return ValueTask.FromResult<string?>(claim.Value);
        }

        return ValueTask.FromResult<string?>(null);
    }
}
