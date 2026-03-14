using Encina.Compliance.PrivacyByDesign.Model;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Notification published when a request field deviates from its declared privacy-respecting
/// default value.
/// </summary>
/// <remarks>
/// <para>
/// Published when the pipeline behavior detects that a field decorated with
/// <see cref="PrivacyDefaultAttribute"/> has a value that differs from its declared default.
/// This indicates an explicit opt-in to more permissive data processing.
/// </para>
/// <para>
/// Per GDPR Article 25(2), personal data should not be made accessible by default without
/// the individual's intervention. This notification provides an audit point for tracking
/// when defaults are overridden.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;PrivacyDefaultOverridden&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Log consent-related changes for audit purposes.</description></item>
/// <item><description>Track which users are opting into less private settings.</description></item>
/// <item><description>Trigger consent verification workflows.</description></item>
/// </list>
/// </remarks>
/// <param name="RequestTypeName">The fully-qualified type name of the request.</param>
/// <param name="OverriddenFields">The fields whose values deviate from their declared privacy defaults.</param>
/// <param name="TenantId">The tenant identifier, or <see langword="null"/> when tenancy is not used.</param>
/// <param name="ModuleId">The module identifier, or <see langword="null"/> when module isolation is not used.</param>
public sealed record PrivacyDefaultOverridden(
    string RequestTypeName,
    IReadOnlyList<DefaultPrivacyFieldInfo> OverriddenFields,
    string? TenantId,
    string? ModuleId) : INotification;
