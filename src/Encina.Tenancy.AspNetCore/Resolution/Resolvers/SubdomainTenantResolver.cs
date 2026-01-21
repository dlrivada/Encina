using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Resolves tenant identifier from the request's subdomain.
/// </summary>
/// <remarks>
/// <para>
/// This resolver extracts the tenant ID from the first subdomain segment
/// of the request's host. For example, <c>acme.example.com</c> would resolve
/// to tenant <c>acme</c>.
/// </para>
/// <para>
/// The base domain must be configured to correctly identify the tenant portion.
/// Subdomains that match the exclude list (e.g., www, api) are ignored.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request URL
/// GET https://acme.example.com/api/orders
///
/// // Configuration
/// services.AddEncinaTenancyAspNetCore(options =>
/// {
///     options.SubdomainResolver.BaseDomain = "example.com";
///     options.SubdomainResolver.ExcludedSubdomains.Add("www");
///     options.SubdomainResolver.ExcludedSubdomains.Add("api");
/// });
/// </code>
/// </example>
public sealed class SubdomainTenantResolver : ITenantResolver
{
    /// <summary>
    /// The default priority for this resolver.
    /// </summary>
    public const int DefaultPriority = 130;

    private readonly TenancyAspNetCoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubdomainTenantResolver"/> class.
    /// </summary>
    /// <param name="options">The tenancy options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public SubdomainTenantResolver(IOptions<TenancyAspNetCoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public int Priority => _options.SubdomainResolver.Priority;

    /// <inheritdoc />
    public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.SubdomainResolver.Enabled)
        {
            return ValueTask.FromResult<string?>(null);
        }

        var host = context.Request.Host.Host;

        if (string.IsNullOrWhiteSpace(host))
        {
            return ValueTask.FromResult<string?>(null);
        }

        var baseDomain = _options.SubdomainResolver.BaseDomain;

        // If no base domain configured, try to extract from host
        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            return ValueTask.FromResult<string?>(null);
        }

        // Check if host ends with the base domain
        if (!host.EndsWith(baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult<string?>(null);
        }

        // Extract subdomain
        var subdomainPart = host[..^baseDomain.Length].TrimEnd('.');

        if (string.IsNullOrWhiteSpace(subdomainPart))
        {
            return ValueTask.FromResult<string?>(null);
        }

        // Get the first subdomain segment (in case of nested subdomains like a.b.example.com)
        var subdomains = subdomainPart.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (subdomains.Length == 0)
        {
            return ValueTask.FromResult<string?>(null);
        }

        var tenantId = subdomains[0];

        // Check if this subdomain should be excluded
        if (_options.SubdomainResolver.ExcludedSubdomains.Contains(tenantId, StringComparer.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult<string?>(null);
        }

        return ValueTask.FromResult<string?>(tenantId);
    }
}
