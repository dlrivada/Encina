using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.ProcessorAgreements.Services;

/// <summary>
/// Resolves the tenant a Processor Agreements operation runs under, shared by the processor and
/// DPA services so that both apply the same isolation rules (#3517, #3679).
/// </summary>
/// <remarks>
/// <para>
/// The tenant comes from the ambient <see cref="IRequestContextAccessor.RequestContext"/>.
/// <c>null</c> and the empty string are the same "no tenant" value, normalized to <c>null</c>.
/// Callers filter every query with an explicit predicate on the read model's <c>TenantId</c>
/// against the resolved value, so with no tenant the query reads the null-tenant scope: it is
/// never an unfiltered query over every tenant.
/// </para>
/// <para>
/// <see cref="ProcessorAgreementOptions.RequireTenantContext"/> decides whether a missing tenant
/// fails closed: its default (<c>null</c>) fails closed when <c>Encina.Tenancy</c> is registered;
/// an explicit <c>false</c> in a multi-tenant application is an opt-out logged at construction
/// (SPEC-002 DEC-006).
/// </para>
/// </remarks>
internal sealed class ProcessorAgreementTenantScope
{
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly ILogger _logger;
    private readonly bool _isTenantContextRequired;

    /// <summary>
    /// Initializes a new instance of <see cref="ProcessorAgreementTenantScope"/> and logs the
    /// explicit opt-out when a multi-tenant application disabled enforcement.
    /// </summary>
    /// <param name="requestContextAccessor">The accessor of the ambient request context.</param>
    /// <param name="options">The module options.</param>
    /// <param name="isMultiTenantApplication">Whether <c>Encina.Tenancy</c> is registered.</param>
    /// <param name="logger">The logger of the owning service.</param>
    public ProcessorAgreementTenantScope(
        IRequestContextAccessor requestContextAccessor,
        ProcessorAgreementOptions options,
        bool isMultiTenantApplication,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _requestContextAccessor = requestContextAccessor;
        _logger = logger;
        _isTenantContextRequired = options.RequireTenantContext ?? isMultiTenantApplication;

        if (options.RequireTenantContext == false && isMultiTenantApplication)
        {
            logger.ProcessorAgreementTenantEnforcementOptedOut();
        }
    }

    /// <summary>
    /// Resolves the ambient tenant of a query or a by-id mutation.
    /// </summary>
    /// <param name="operation">The operation name, used in the error and the log.</param>
    /// <param name="tenantId">The resolved tenant, or <c>null</c> for the null-tenant scope.</param>
    /// <param name="error">The <see cref="ProcessorAgreementErrors.TenantRequiredCode"/> error when refused.</param>
    /// <returns><c>true</c> when the operation may proceed.</returns>
    public bool TryResolve(string operation, out string? tenantId, out EncinaError? error)
    {
        tenantId = Normalize(_requestContextAccessor.RequestContext?.TenantId);

        if (tenantId is null && _isTenantContextRequired)
        {
            return Refuse(operation, out tenantId, out error);
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Resolves the tenant a new record is written under: the explicit tenant when it agrees with
    /// the ambient one, otherwise the ambient one. An explicit tenant that contradicts the ambient
    /// tenant is refused; with no ambient tenant an explicit tenant is accepted.
    /// </summary>
    /// <param name="operation">The operation name, used in the error and the log.</param>
    /// <param name="explicitTenantId">The tenant passed by the caller, or <c>null</c>.</param>
    /// <param name="tenantId">The resolved tenant, or <c>null</c> for the null-tenant scope.</param>
    /// <param name="error">The <see cref="ProcessorAgreementErrors.TenantRequiredCode"/> error when refused.</param>
    /// <returns><c>true</c> when the operation may proceed.</returns>
    public bool TryResolveWrite(
        string operation, string? explicitTenantId, out string? tenantId, out EncinaError? error)
    {
        var ambientTenantId = Normalize(_requestContextAccessor.RequestContext?.TenantId);
        var requestedTenantId = Normalize(explicitTenantId);

        if (requestedTenantId is not null && ambientTenantId is not null
            && !Matches(requestedTenantId, ambientTenantId))
        {
            return Refuse(operation, out tenantId, out error);
        }

        tenantId = requestedTenantId ?? ambientTenantId;

        if (tenantId is null && _isTenantContextRequired)
        {
            return Refuse(operation, out tenantId, out error);
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Compares two tenant ids, treating <c>null</c> and the empty string as the same value.
    /// </summary>
    /// <param name="left">The first tenant id.</param>
    /// <param name="right">The second tenant id.</param>
    /// <returns><c>true</c> when both name the same tenant.</returns>
    public static bool Matches(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);

    /// <summary>
    /// Builds a cache key scoped by tenant: <c>pa:tenant:{tenant}:{suffix}</c>, with the literal
    /// <c>"-"</c> when no tenant is present, so one tenant's entry is never served to another.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the cached entry.</param>
    /// <param name="suffix">The rest of the key, such as <c>processor:{id}</c>.</param>
    /// <returns>The tenant-scoped cache key.</returns>
    public static string CacheKey(string? tenantId, string suffix) =>
        $"pa:tenant:{Normalize(tenantId) ?? "-"}:{suffix}";

    private static string? Normalize(string? tenantId) =>
        string.IsNullOrEmpty(tenantId) ? null : tenantId;

    private bool Refuse(string operation, out string? tenantId, out EncinaError? error)
    {
        _logger.ProcessorAgreementTenantContextMissing(operation);
        tenantId = null;
        error = ProcessorAgreementErrors.TenantRequired(operation);
        return false;
    }
}
