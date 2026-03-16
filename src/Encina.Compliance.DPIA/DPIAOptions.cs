using System.Reflection;

using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Configuration options for the DPIA (Data Protection Impact Assessment) module.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the DPIA pipeline behavior, assessment engine,
/// expiration monitoring, and audit trail settings. All options have sensible defaults
/// aligned with GDPR Article 35 requirements.
/// </para>
/// <para>
/// Register via <c>AddEncinaDPIA(options => { ... })</c>.
/// </para>
/// </remarks>
public sealed class DPIAOptions
{
    // --- Enforcement ---

    /// <summary>
    /// Gets or sets the enforcement mode for the DPIA pipeline behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>DPIAPipelineBehavior</c> responds when a DPIA assessment
    /// is required but not available or expired:
    /// <list type="bullet">
    /// <item><description><see cref="DPIAEnforcementMode.Block"/>: Returns an error, blocking the request.</description></item>
    /// <item><description><see cref="DPIAEnforcementMode.Warn"/>: Logs a warning but allows the request through.</description></item>
    /// <item><description><see cref="DPIAEnforcementMode.Disabled"/>: Skips all DPIA enforcement entirely.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default is <see cref="DPIAEnforcementMode.Warn"/> to support gradual adoption.
    /// Production systems should use <see cref="DPIAEnforcementMode.Block"/>.
    /// </para>
    /// </remarks>
    public DPIAEnforcementMode EnforcementMode { get; set; } = DPIAEnforcementMode.Warn;

    /// <summary>
    /// Gets or sets whether requests that lack a DPIA assessment should be blocked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience alias for setting <see cref="EnforcementMode"/> to
    /// <see cref="DPIAEnforcementMode.Block"/>. Setting this to <c>true</c> sets
    /// <c>EnforcementMode = Block</c>; reading it returns <c>true</c> when
    /// <c>EnforcementMode</c> is <see cref="DPIAEnforcementMode.Block"/>.
    /// </para>
    /// <para>
    /// Provides a more intuitive configuration experience:
    /// <code>
    /// options.BlockWithoutDPIA = true;
    /// // Equivalent to: options.EnforcementMode = DPIAEnforcementMode.Block;
    /// </code>
    /// </para>
    /// </remarks>
    public bool BlockWithoutDPIA
    {
        get => EnforcementMode == DPIAEnforcementMode.Block;
        set
        {
            if (value)
            {
                EnforcementMode = DPIAEnforcementMode.Block;
            }
        }
    }

    // --- Assessment ---

    /// <summary>
    /// Gets or sets the default review period for approved DPIA assessments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 35(11), the controller must review assessments periodically.
    /// This value determines how long an approved assessment remains valid before
    /// requiring re-evaluation.
    /// </para>
    /// <para>
    /// Default is <c>365 days</c> (1 year).
    /// </para>
    /// </remarks>
    public TimeSpan DefaultReviewPeriod { get; set; } = TimeSpan.FromDays(365);

    /// <summary>
    /// Gets or sets the DPO (Data Protection Officer) email address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 35(2), the controller must seek the DPO's advice when
    /// carrying out a DPIA. This email is used when initiating DPO consultations.
    /// </para>
    /// <para>
    /// Default is <see langword="null"/> (must be configured for DPO consultation).
    /// </para>
    /// </remarks>
    public string? DPOEmail { get; set; }

    /// <summary>
    /// Gets or sets the DPO (Data Protection Officer) name.
    /// </summary>
    public string? DPOName { get; set; }

    // --- Notifications ---

    /// <summary>
    /// Gets or sets whether to publish domain notifications for DPIA lifecycle events.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c>.
    /// </remarks>
    public bool PublishNotifications { get; set; } = true;

    // --- Expiration monitoring ---

    /// <summary>
    /// Gets or sets whether the background expiration monitoring service is enabled.
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableExpirationMonitoring { get; set; }

    /// <summary>
    /// Gets or sets the interval between expiration monitoring checks.
    /// </summary>
    /// <remarks>
    /// Default is <c>1 hour</c>.
    /// </remarks>
    public TimeSpan ExpirationCheckInterval { get; set; } = TimeSpan.FromHours(1);

    // --- Auto-registration ---

    /// <summary>
    /// Gets or sets whether to automatically scan assemblies for <see cref="RequiresDPIAAttribute"/>
    /// decorations and create draft assessments at startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, a hosted service scans <see cref="AssembliesToScan"/> at application startup,
    /// discovers request types decorated with <c>[RequiresDPIA]</c>, and creates draft assessments
    /// via the <see cref="Abstractions.IDPIAService"/> for any types that do not already have an assessment.
    /// </para>
    /// <para>
    /// Default is <c>false</c>.
    /// </para>
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; }

    /// <summary>
    /// Gets or sets whether to enable heuristic auto-detection of high-risk processing types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled (in conjunction with <see cref="AutoRegisterFromAttributes"/>), the auto-registration
    /// service also applies heuristic analysis to discover request types that might require a DPIA
    /// even if they are not decorated with <see cref="RequiresDPIAAttribute"/>. Detection is based
    /// on naming patterns and property types that indicate high-risk processing (e.g., biometric data,
    /// health data, automated decision-making).
    /// </para>
    /// <para>
    /// This supplements attribute-based detection and follows the EDPB WP 248 rev.01 guidance
    /// that organizations should proactively identify high-risk processing operations.
    /// </para>
    /// <para>
    /// Default is <c>false</c>.
    /// </para>
    /// </remarks>
    public bool AutoDetectHighRisk { get; set; }

    /// <summary>
    /// Gets the list of assemblies to scan for <see cref="RequiresDPIAAttribute"/> decorations
    /// during auto-registration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If empty when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>, the entry assembly
    /// is used as a fallback.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];

    // --- Health check ---

    /// <summary>
    /// Gets or sets whether to register a DPIA health check.
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }
}
