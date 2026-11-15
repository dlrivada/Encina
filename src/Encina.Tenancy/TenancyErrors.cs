namespace Encina.Tenancy;

/// <summary>
/// Factory methods and error codes for tenant resolution errors.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>tenancy.{category}</c>. Every message is a fixed string that never
/// carries the tenant identifier: callers record the code, never <see cref="EncinaError.Message"/>
/// (AGENTS.md section 3).
/// </para>
/// <para>
/// Categories follow ADR-062: <see cref="TenantNotFoundCode"/> is <see cref="ErrorCategory.NotFound"/> (rule R1,
/// the request names the tenant), and <see cref="TenantRequiredCode"/> is <see cref="ErrorCategory.InvalidRequest"/>
/// (rule R3, a missing tenant answers 400 as <c>TenantResolutionMiddleware</c> does).
/// </para>
/// </remarks>
public static class TenancyErrors
{
    /// <summary>Error code when a tenant identifier is present but the tenant store does not know it.</summary>
    public const string TenantNotFoundCode = "tenancy.tenant_not_found";

    /// <summary>
    /// Error code when no tenant identifier is available and <see cref="TenancyOptions.RequireTenant"/> is <c>true</c>.
    /// </summary>
    public const string TenantRequiredCode = "tenancy.tenant_required";

    /// <summary>
    /// Creates the error returned when a tenant identifier is present but the tenant store does not know it.
    /// </summary>
    /// <returns>An error with code <see cref="TenantNotFoundCode"/>; its message carries no tenant identifier.</returns>
    /// <example>
    /// <code>
    /// var tenant = await tenantStore.GetTenantAsync(tenantId, cancellationToken);
    /// if (tenant is null)
    /// {
    ///     return TenancyErrors.TenantNotFound();
    /// }
    /// </code>
    /// </example>
    public static EncinaError TenantNotFound() =>
        EncinaErrors.Create(
            TenantNotFoundCode,
            ErrorCategory.NotFound,
            "The specified tenant was not found. Access denied.");

    /// <summary>
    /// Creates the error returned when no tenant identifier is available and a tenant is required.
    /// </summary>
    /// <returns>An error with code <see cref="TenantRequiredCode"/>.</returns>
    public static EncinaError TenantRequired() =>
        EncinaErrors.Create(
            TenantRequiredCode,
            ErrorCategory.InvalidRequest,
            "A tenant is required and none is available. Access denied.");
}
