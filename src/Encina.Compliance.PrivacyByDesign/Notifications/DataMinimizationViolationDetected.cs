using Encina.Compliance.PrivacyByDesign.Model;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Notification published when the data minimization pipeline behavior detects one or more
/// Privacy by Design violations in a request.
/// </summary>
/// <remarks>
/// <para>
/// Published after <c>DataMinimizationPipelineBehavior</c> analyzes a request and finds
/// violations of data minimization, purpose limitation, or default privacy requirements.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DataMinimizationViolationDetected&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Alert compliance teams about privacy violations.</description></item>
/// <item><description>Record violations in an audit trail.</description></item>
/// <item><description>Update compliance dashboards and reporting systems.</description></item>
/// <item><description>Trigger automated remediation workflows.</description></item>
/// </list>
/// <para>
/// This notification is published in both <see cref="PrivacyByDesignEnforcementMode.Block"/>
/// and <see cref="PrivacyByDesignEnforcementMode.Warn"/> modes, allowing monitoring even
/// when violations do not block processing.
/// </para>
/// </remarks>
/// <param name="RequestTypeName">The fully-qualified type name of the request that was validated.</param>
/// <param name="Violations">The list of violations detected during validation.</param>
/// <param name="EnforcementMode">The enforcement mode active when the violations were detected.</param>
/// <param name="MinimizationScore">
/// The data minimization score (0.0–1.0) at the time of validation.
/// A lower score indicates more unnecessary fields with values.
/// </param>
/// <param name="TenantId">The tenant identifier, or <see langword="null"/> when tenancy is not used.</param>
/// <param name="ModuleId">The module identifier, or <see langword="null"/> when module isolation is not used.</param>
public sealed record DataMinimizationViolationDetected(
    string RequestTypeName,
    IReadOnlyList<PrivacyViolation> Violations,
    PrivacyByDesignEnforcementMode EnforcementMode,
    double MinimizationScore,
    string? TenantId,
    string? ModuleId) : INotification;
