using Encina.Tenancy.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Resolves tenant identifier from an HTTP request header.
/// </summary>
/// <remarks>
/// <para>
/// This resolver reads the tenant ID from a configurable HTTP header.
/// The default header name is <c>X-Tenant-ID</c>.
/// </para>
/// <para>
/// The header is chosen by the client, so this resolver is off by default and never authoritative:
/// a verified claim wins, and a header that disagrees with it is denied. The header is honored only
/// when no claim exists and the request peer is trusted
/// (<see cref="HeaderResolverOptions.TrustedProxies"/> or <see cref="HeaderResolverOptions.TrustAnyPeer"/>),
/// which is the gateway scenario where a trusted proxy injects the tenant.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Client request
/// GET /api/orders HTTP/1.1
/// Host: api.example.com
/// X-Tenant-ID: acme-corp
///
/// // Configuration
/// services.AddEncinaTenancyAspNetCore(options =>
/// {
///     options.HeaderResolver.Enabled = true;
///     options.HeaderResolver.HeaderName = "X-Organization-ID";
///     options.HeaderResolver.TrustedProxies.Add(IPNetwork.Parse("10.0.0.0/24")); // the gateway
/// });
/// </code>
/// </example>
public sealed class HeaderTenantResolver : ITenantResolver
{
    /// <summary>
    /// The default header name for tenant identification.
    /// </summary>
    public const string DefaultHeaderName = "X-Tenant-ID";

    /// <summary>
    /// The default priority for this resolver.
    /// </summary>
    public const int DefaultPriority = 100;

    private readonly TenancyAspNetCoreOptions _options;
    private readonly ILogger<HeaderTenantResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderTenantResolver"/> class.
    /// </summary>
    /// <param name="options">The tenancy options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public HeaderTenantResolver(IOptions<TenancyAspNetCoreOptions> options, ILogger<HeaderTenantResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public int Priority => _options.HeaderResolver.Priority;

    /// <inheritdoc />
    /// <remarks>A header is client supplied: it never overrides a verified claim.</remarks>
    public bool IsAuthoritative => false;

    /// <inheritdoc />
    /// <remarks>
    /// The header is read only when the resolver is enabled and the request peer is trusted
    /// (<see cref="HeaderResolverOptions.TrustedProxies"/> or <see cref="HeaderResolverOptions.TrustAnyPeer"/>);
    /// otherwise the resolver returns <c>null</c> and logs at Debug level.
    /// </remarks>
    public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headerOptions = _options.HeaderResolver;

        if (!headerOptions.Enabled)
        {
            return ValueTask.FromResult<string?>(null);
        }

        if (!IsTrustedPeer(context, headerOptions))
        {
            TenancyAspNetCoreLogMessages.HeaderIgnoredUntrustedPeer(_logger, headerOptions.HeaderName);
            return ValueTask.FromResult<string?>(null);
        }

        if (context.Request.Headers.TryGetValue(headerOptions.HeaderName, out var headerValue))
        {
            var tenantId = headerValue.ToString();

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return ValueTask.FromResult<string?>(tenantId);
            }
        }

        return ValueTask.FromResult<string?>(null);
    }

    private static bool IsTrustedPeer(HttpContext context, HeaderResolverOptions headerOptions)
    {
        if (headerOptions.TrustAnyPeer)
        {
            return true;
        }

        var peer = context.Connection.RemoteIpAddress;
        if (peer is null)
        {
            return false;
        }

        if (peer.IsIPv4MappedToIPv6)
        {
            peer = peer.MapToIPv4();
        }

        foreach (var network in headerOptions.TrustedProxies)
        {
            if (network.Contains(peer))
            {
                return true;
            }
        }

        return false;
    }
}
