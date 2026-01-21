using Microsoft.AspNetCore.Http;
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
/// This is the most common approach for API clients where the tenant
/// context is explicitly specified in the request.
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
///     options.HeaderResolver.HeaderName = "X-Organization-ID";
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

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderTenantResolver"/> class.
    /// </summary>
    /// <param name="options">The tenancy options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public HeaderTenantResolver(IOptions<TenancyAspNetCoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public int Priority => _options.HeaderResolver.Priority;

    /// <inheritdoc />
    public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.HeaderResolver.Enabled)
        {
            return ValueTask.FromResult<string?>(null);
        }

        var headerName = _options.HeaderResolver.HeaderName;

        if (context.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            var tenantId = headerValue.ToString();

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return ValueTask.FromResult<string?>(tenantId);
            }
        }

        return ValueTask.FromResult<string?>(null);
    }
}
