namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Configuration options for ASP.NET Core tenant resolution.
/// </summary>
/// <remarks>
/// <para>
/// Configure these options when calling <c>AddEncinaTenancyAspNetCore()</c>:
/// </para>
/// <code>
/// services.AddEncinaTenancyAspNetCore(options =>
/// {
///     options.HeaderResolver.HeaderName = "X-Organization-ID";
///     options.ClaimResolver.Enabled = true;
///     options.SubdomainResolver.BaseDomain = "example.com";
/// });
/// </code>
/// </remarks>
public sealed class TenancyAspNetCoreOptions
{
    /// <summary>
    /// Gets the header resolver options.
    /// </summary>
    public HeaderResolverOptions HeaderResolver { get; } = new();

    /// <summary>
    /// Gets the claim resolver options.
    /// </summary>
    public ClaimResolverOptions ClaimResolver { get; } = new();

    /// <summary>
    /// Gets the route resolver options.
    /// </summary>
    public RouteResolverOptions RouteResolver { get; } = new();

    /// <summary>
    /// Gets the subdomain resolver options.
    /// </summary>
    public SubdomainResolverOptions SubdomainResolver { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to return a 400 Bad Request
    /// response when the tenant cannot be resolved and <see cref="TenancyOptions.RequireTenant"/>
    /// is <c>true</c>.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool Return400WhenTenantRequired { get; set; } = true;
}

/// <summary>
/// Configuration options for the header-based tenant resolver.
/// </summary>
public sealed class HeaderResolverOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether this resolver is enabled.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP header name to read the tenant ID from.
    /// </summary>
    /// <value>The default is <c>X-Tenant-ID</c>.</value>
    public string HeaderName { get; set; } = HeaderTenantResolver.DefaultHeaderName;

    /// <summary>
    /// Gets or sets the priority of this resolver.
    /// </summary>
    /// <value>The default is 100.</value>
    public int Priority { get; set; } = HeaderTenantResolver.DefaultPriority;
}

/// <summary>
/// Configuration options for the claim-based tenant resolver.
/// </summary>
public sealed class ClaimResolverOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether this resolver is enabled.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the claim type to read the tenant ID from.
    /// </summary>
    /// <value>The default is <c>tenant_id</c>.</value>
    public string ClaimType { get; set; } = ClaimTenantResolver.DefaultClaimType;

    /// <summary>
    /// Gets or sets the priority of this resolver.
    /// </summary>
    /// <value>The default is 110.</value>
    public int Priority { get; set; } = ClaimTenantResolver.DefaultPriority;
}

/// <summary>
/// Configuration options for the route-based tenant resolver.
/// </summary>
public sealed class RouteResolverOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether this resolver is enabled.
    /// </summary>
    /// <value>The default is <c>false</c>.</value>
    /// <remarks>
    /// This resolver is disabled by default because it requires specific
    /// route templates to be configured.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the route parameter name to read the tenant ID from.
    /// </summary>
    /// <value>The default is <c>tenantId</c>.</value>
    public string ParameterName { get; set; } = RouteTenantResolver.DefaultParameterName;

    /// <summary>
    /// Gets or sets the priority of this resolver.
    /// </summary>
    /// <value>The default is 120.</value>
    public int Priority { get; set; } = RouteTenantResolver.DefaultPriority;
}

/// <summary>
/// Configuration options for the subdomain-based tenant resolver.
/// </summary>
public sealed class SubdomainResolverOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether this resolver is enabled.
    /// </summary>
    /// <value>The default is <c>false</c>.</value>
    /// <remarks>
    /// This resolver is disabled by default because it requires
    /// <see cref="BaseDomain"/> to be configured.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the base domain used to identify tenant subdomains.
    /// </summary>
    /// <value>The default is <c>null</c>.</value>
    /// <remarks>
    /// <para>
    /// For example, if the base domain is <c>example.com</c>, then
    /// <c>acme.example.com</c> would resolve to tenant <c>acme</c>.
    /// </para>
    /// <para>
    /// This setting is required when the subdomain resolver is enabled.
    /// </para>
    /// </remarks>
    public string? BaseDomain { get; set; }

    /// <summary>
    /// Gets the list of subdomains to exclude from tenant resolution.
    /// </summary>
    /// <value>
    /// A list containing common subdomains like <c>www</c> and <c>api</c>
    /// by default.
    /// </value>
    public List<string> ExcludedSubdomains { get; } = ["www", "api", "admin", "static", "cdn", "assets"];

    /// <summary>
    /// Gets or sets the priority of this resolver.
    /// </summary>
    /// <value>The default is 130.</value>
    public int Priority { get; set; } = SubdomainTenantResolver.DefaultPriority;
}
