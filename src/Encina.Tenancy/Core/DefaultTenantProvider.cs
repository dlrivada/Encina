using Encina.AspNetCore;

namespace Encina.Tenancy;

/// <summary>
/// Default implementation of <see cref="ITenantProvider"/> that bridges
/// <see cref="IRequestContextAccessor"/> with <see cref="ITenantStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation:
/// <list type="bullet">
/// <item>Reads tenant ID from <see cref="IRequestContext.TenantId"/> via the accessor</item>
/// <item>Retrieves full tenant metadata from <see cref="ITenantStore"/></item>
/// <item>Handles null cases gracefully (no exceptions for missing tenant context)</item>
/// </list>
/// </para>
/// <para>
/// Register as scoped since it depends on the per-request context.
/// </para>
/// </remarks>
internal sealed class DefaultTenantProvider : ITenantProvider
{
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly ITenantStore _tenantStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTenantProvider"/> class.
    /// </summary>
    /// <param name="requestContextAccessor">The request context accessor.</param>
    /// <param name="tenantStore">The tenant metadata store.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="requestContextAccessor"/> or <paramref name="tenantStore"/> is null.
    /// </exception>
    public DefaultTenantProvider(IRequestContextAccessor requestContextAccessor, ITenantStore tenantStore)
    {
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(tenantStore);

        _requestContextAccessor = requestContextAccessor;
        _tenantStore = tenantStore;
    }

    /// <inheritdoc />
    public string? GetCurrentTenantId()
        => _requestContextAccessor.RequestContext?.TenantId;

    /// <inheritdoc />
    public async ValueTask<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        return await _tenantStore.GetTenantAsync(tenantId, cancellationToken);
    }
}
