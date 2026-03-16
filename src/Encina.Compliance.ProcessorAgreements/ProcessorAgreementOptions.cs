using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Configuration options for the Processor Agreements compliance module.
/// </summary>
/// <remarks>
/// <para>
/// Controls the behavior of the <c>ProcessorValidationPipelineBehavior</c>,
/// which enforces GDPR Article 28 requirements at the request pipeline level,
/// plus optional expiration monitoring and health check features.
/// </para>
/// <para>
/// Register via <c>AddEncinaProcessorAgreements(options => { ... })</c>.
/// </para>
/// </remarks>
public sealed class ProcessorAgreementOptions
{
    // --- Enforcement ---

    /// <summary>
    /// Gets or sets the enforcement mode for the processor validation pipeline behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>ProcessorValidationPipelineBehavior</c> responds when a request
    /// targets a processor without a valid Data Processing Agreement:
    /// <list type="bullet">
    /// <item><description><see cref="ProcessorAgreementEnforcementMode.Block"/>: Returns an error, blocking the request.</description></item>
    /// <item><description><see cref="ProcessorAgreementEnforcementMode.Warn"/>: Logs a warning but allows the request through.</description></item>
    /// <item><description><see cref="ProcessorAgreementEnforcementMode.Disabled"/>: Skips all DPA enforcement entirely.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default is <see cref="ProcessorAgreementEnforcementMode.Warn"/> to support gradual adoption.
    /// Production systems should use <see cref="ProcessorAgreementEnforcementMode.Block"/>.
    /// </para>
    /// </remarks>
    public ProcessorAgreementEnforcementMode EnforcementMode { get; set; } = ProcessorAgreementEnforcementMode.Warn;

    /// <summary>
    /// Gets or sets whether requests that lack a valid DPA should be blocked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience alias for setting <see cref="EnforcementMode"/> to
    /// <see cref="ProcessorAgreementEnforcementMode.Block"/>. Setting this to <c>true</c> sets
    /// <c>EnforcementMode = Block</c>; reading it returns <c>true</c> when
    /// <c>EnforcementMode</c> is <see cref="ProcessorAgreementEnforcementMode.Block"/>.
    /// </para>
    /// <para>
    /// Provides a more intuitive configuration experience:
    /// <code>
    /// options.BlockWithoutValidDPA = true;
    /// // Equivalent to: options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
    /// </code>
    /// </para>
    /// </remarks>
    public bool BlockWithoutValidDPA
    {
        get => EnforcementMode == ProcessorAgreementEnforcementMode.Block;
        set
        {
            if (value)
            {
                EnforcementMode = ProcessorAgreementEnforcementMode.Block;
            }
        }
    }

    // --- Sub-processor hierarchy ---

    /// <summary>
    /// Gets or sets the maximum allowed depth for sub-processor chains.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bounds the sub-processor hierarchy depth to prevent unbounded chains
    /// and ensure manageable compliance oversight. Per GDPR Article 28(2),
    /// the controller must authorise sub-processing, and deep chains increase risk.
    /// </para>
    /// <para>
    /// Valid range: 1 to 10 (inclusive). Default is <c>3</c>.
    /// </para>
    /// </remarks>
    public int MaxSubProcessorDepth { get; set; } = 3;

    // --- Expiration monitoring ---

    /// <summary>
    /// Gets or sets whether the DPA expiration monitoring scheduled command is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the <c>CheckDPAExpirationCommand</c> is registered with the
    /// Encina.Scheduling infrastructure for periodic execution. The command queries
    /// the <see cref="Abstractions.IDPAService"/> for agreements approaching or past expiration
    /// and publishes <c>DPAExpiringNotification</c> / <c>DPAExpiredNotification</c> events.
    /// </para>
    /// <para>
    /// Default is <c>false</c>.
    /// </para>
    /// </remarks>
    public bool EnableExpirationMonitoring { get; set; }

    /// <summary>
    /// Gets or sets the interval between expiration monitoring checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="EnableExpirationMonitoring"/> is <c>true</c>.
    /// Must be a positive <see cref="TimeSpan"/>.
    /// </para>
    /// <para>
    /// Default is <c>1 hour</c>.
    /// </para>
    /// </remarks>
    public TimeSpan ExpirationCheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the number of days before DPA expiration to trigger a warning notification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>CheckDPAExpirationHandler</c> uses this value to identify DPAs that are
    /// approaching expiration and publishes <c>DPAExpiringNotification</c> for proactive alerting.
    /// </para>
    /// <para>
    /// Default is <c>30</c> days.
    /// </para>
    /// </remarks>
    public int ExpirationWarningDays { get; set; } = 30;

    // --- Audit trail ---

    /// <summary>
    /// Gets or sets whether to record an audit trail for all processor agreement operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the module records audit entries via the Marten event stream
    /// for enforcement actions (blocked requests, expiration transitions) to support
    /// the accountability principle under GDPR Article 5(2).
    /// </para>
    /// <para>
    /// Audit recording is non-blocking: failures in the audit store do not prevent
    /// the primary operation from completing.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool TrackAuditTrail { get; set; } = true;

    // --- Health check ---

    /// <summary>
    /// Gets or sets whether to register a processor agreement health check.
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }
}
