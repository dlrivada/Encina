using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Resolves tenant identifier from route parameters.
/// </summary>
/// <remarks>
/// <para>
/// This resolver extracts the tenant ID from a route parameter.
/// The default parameter name is <c>tenantId</c>.
/// </para>
/// <para>
/// This approach is useful for RESTful APIs where the tenant is part
/// of the URL structure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Route template
/// app.MapGet("/tenants/{tenantId}/orders", GetOrders);
///
/// // Request URL
/// GET /tenants/acme-corp/orders
///
/// // Configuration
/// services.AddEncinaTenancyAspNetCore(options =>
/// {
///     options.RouteResolver.ParameterName = "org";
/// });
/// </code>
/// </example>
public sealed class RouteTenantResolver : ITenantResolver
{
    /// <summary>
    /// The default route parameter name for tenant identification.
    /// </summary>
    public const string DefaultParameterName = "tenantId";

    /// <summary>
    /// The default priority for this resolver.
    /// </summary>
    public const int DefaultPriority = 120;

    private readonly TenancyAspNetCoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteTenantResolver"/> class.
    /// </summary>
    /// <param name="options">The tenancy options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public RouteTenantResolver(IOptions<TenancyAspNetCoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public int Priority => _options.RouteResolver.Priority;

    /// <inheritdoc />
    public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.RouteResolver.Enabled)
        {
            return ValueTask.FromResult<string?>(null);
        }

        var parameterName = _options.RouteResolver.ParameterName;

        if (context.Request.RouteValues.TryGetValue(parameterName, out var routeValue))
        {
            var tenantId = routeValue?.ToString();

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return ValueTask.FromResult<string?>(tenantId);
            }
        }

        return ValueTask.FromResult<string?>(null);
    }
}
