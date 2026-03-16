using System.Reflection;

using Encina.Compliance.BreachNotification.Model;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Configuration options for the Breach Notification module.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the breach detection pipeline, built-in
/// detection rules, notification preferences, deadline monitoring, and audit trail
/// settings. All options have sensible defaults aligned with GDPR Articles 33 and 34
/// requirements.
/// </para>
/// <para>
/// Register via <c>AddEncinaBreachNotification(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaBreachNotification(options =>
/// {
///     options.EnforcementMode = BreachDetectionEnforcementMode.Block;
///     options.NotificationDeadlineHours = 72;
///     options.EnableDeadlineMonitoring = true;
///     options.AddHealthCheck = true;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
///     options.AddDetectionRule&lt;MyCustomRule&gt;();
/// });
/// </code>
/// </example>
public sealed class BreachNotificationOptions
{
    // --- Enforcement ---

    /// <summary>
    /// Gets or sets the enforcement mode for the breach detection pipeline behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>BreachDetectionPipelineBehavior</c> responds when
    /// a potential breach is detected:
    /// <list type="bullet">
    /// <item><description><see cref="BreachDetectionEnforcementMode.Block"/>: Returns an error, blocking the response.</description></item>
    /// <item><description><see cref="BreachDetectionEnforcementMode.Warn"/>: Logs a warning but allows the response through.</description></item>
    /// <item><description><see cref="BreachDetectionEnforcementMode.Disabled"/>: Skips all breach detection entirely.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default is <see cref="BreachDetectionEnforcementMode.Warn"/> to support gradual adoption.
    /// Production systems should use <see cref="BreachDetectionEnforcementMode.Block"/>.
    /// </para>
    /// </remarks>
    public BreachDetectionEnforcementMode EnforcementMode { get; set; } = BreachDetectionEnforcementMode.Warn;

    // --- Notification ---

    /// <summary>
    /// Gets or sets whether to publish domain notifications for breach lifecycle events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the breach handler publishes notifications at key lifecycle points
    /// (e.g., breach detected, authority notified, subjects notified, breach resolved).
    /// These can be used for audit logging, event sourcing, or integration with external systems.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool PublishNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record an audit trail for all breach notification operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, all breach operations (detection, notification, phased reporting,
    /// resolution, etc.) are recorded in the event stream audit trail
    /// for accountability purposes per GDPR Article 5(2) and Article 33(5).
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool TrackAuditTrail { get; set; } = true;

    // --- Deadline and notification timing ---

    /// <summary>
    /// Gets or sets the notification deadline in hours from breach detection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 33(1), the controller must notify the supervisory authority
    /// "not later than 72 hours after having become aware of it." This property
    /// controls the deadline calculation for breach records.
    /// </para>
    /// <para>
    /// Default is <c>72</c>.
    /// </para>
    /// </remarks>
    public int NotificationDeadlineHours { get; set; } = 72;

    /// <summary>
    /// Gets or sets the alert thresholds (in hours remaining) for deadline warning notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>BreachDeadlineMonitorService</c> publishes <c>DeadlineWarningNotification</c>
    /// events when a breach's remaining time crosses any of these thresholds.
    /// Values should be positive and less than <see cref="NotificationDeadlineHours"/>.
    /// </para>
    /// <para>
    /// Default is <c>[48, 24, 12, 6, 1]</c>.
    /// </para>
    /// </remarks>
    public int[] AlertAtHoursRemaining { get; set; } = [48, 24, 12, 6, 1];

    /// <summary>
    /// Gets or sets the supervisory authority contact information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 33, notifications must be sent to the supervisory authority.
    /// This property stores the authority's contact details (e.g., email address,
    /// API endpoint, or reference identifier).
    /// </para>
    /// <para>
    /// Default is <c>null</c> (must be configured for production use).
    /// </para>
    /// </remarks>
    public string? SupervisoryAuthority { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically notify the authority when a high-severity breach is detected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the breach handler automatically triggers authority notification
    /// for breaches with severity at or above <see cref="BreachSeverity.High"/>.
    /// When <c>false</c>, authority notification must be triggered manually.
    /// </para>
    /// <para>
    /// Default is <c>false</c>.
    /// </para>
    /// </remarks>
    public bool AutoNotifyOnHighSeverity { get; set; }

    /// <summary>
    /// Gets or sets whether phased reporting is enabled per GDPR Article 33(4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per Article 33(4), "where, and in so far as, it is not possible to provide the
    /// information at the same time, the information may be provided in phases without
    /// undue further delay." When enabled, the breach handler accepts phased reports.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool PhasedReportingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum breach severity that triggers data subject notification per Article 34.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per Article 34(1), the controller must communicate the breach to data subjects
    /// when it "is likely to result in a high risk to the rights and freedoms of
    /// natural persons." This threshold determines the severity level at which
    /// subject notification is recommended.
    /// </para>
    /// <para>
    /// Default is <see cref="BreachSeverity.High"/>.
    /// </para>
    /// </remarks>
    public BreachSeverity SubjectNotificationSeverityThreshold { get; set; } = BreachSeverity.High;

    // --- Deadline monitoring ---

    /// <summary>
    /// Gets or sets whether the background deadline monitoring service is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>BreachDeadlineMonitorService</c> background service runs
    /// monitoring cycles at the interval specified by <see cref="DeadlineCheckInterval"/>.
    /// When <c>false</c>, deadline monitoring must be managed manually.
    /// </para>
    /// <para>
    /// Default is <c>false</c>. Enable for production systems that need proactive
    /// deadline tracking.
    /// </para>
    /// </remarks>
    public bool EnableDeadlineMonitoring { get; set; }

    /// <summary>
    /// Gets or sets the interval between deadline monitoring checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how frequently the <c>BreachDeadlineMonitorService</c> checks for
    /// breaches approaching their notification deadline. Only applies when
    /// <see cref="EnableDeadlineMonitoring"/> is <c>true</c>.
    /// </para>
    /// <para>
    /// Default is <c>15 minutes</c>. Shorter intervals increase alerting responsiveness
    /// but may add database load.
    /// </para>
    /// </remarks>
    public TimeSpan DeadlineCheckInterval { get; set; } = TimeSpan.FromMinutes(15);

    // --- Health check ---

    /// <summary>
    /// Gets or sets whether to register a breach notification health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the breach notification stores
    /// are reachable, the detector is operational, and reports on any overdue breaches.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    // --- Built-in detection rule thresholds ---

    /// <summary>
    /// Gets or sets the threshold for the <c>UnauthorizedAccessRule</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a <see cref="SecurityEventType.UnauthorizedAccess"/> event is evaluated,
    /// the rule triggers a potential breach if the event description or metadata
    /// indicates this threshold has been exceeded (e.g., number of failed login attempts).
    /// </para>
    /// <para>
    /// Default is <c>5</c>.
    /// </para>
    /// </remarks>
    public int UnauthorizedAccessThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the data volume threshold (in megabytes) for the <c>MassDataExfiltrationRule</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a <see cref="SecurityEventType.DataExfiltration"/> event is evaluated,
    /// the rule triggers a potential breach if the volume of data accessed exceeds
    /// this threshold.
    /// </para>
    /// <para>
    /// Default is <c>100</c> MB.
    /// </para>
    /// </remarks>
    public int DataExfiltrationThresholdMB { get; set; } = 100;

    /// <summary>
    /// Gets or sets the query volume threshold for the <c>AnomalousQueryPatternRule</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a <see cref="SecurityEventType.AnomalousQuery"/> event is evaluated,
    /// the rule triggers a potential breach if the number of queries exceeds this threshold.
    /// </para>
    /// <para>
    /// Default is <c>1000</c>.
    /// </para>
    /// </remarks>
    public int AnomalousQueryThreshold { get; set; } = 1000;

    // --- Auto-registration ---

    /// <summary>
    /// Gets the assemblies to scan for <see cref="BreachMonitoredAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Add assemblies that contain your request types decorated with
    /// <see cref="BreachMonitoredAttribute"/> so they can be discovered
    /// for pipeline behavior registration.
    /// </para>
    /// <para>
    /// If empty, the entry assembly is used as a fallback.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];

    // --- Detection rule registration ---

    /// <summary>
    /// Gets the detection rule types configured via the fluent API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Types added via <see cref="AddDetectionRule{TRule}"/> are registered in the
    /// DI container as <see cref="IBreachDetectionRule"/> implementations at startup.
    /// </para>
    /// </remarks>
    internal List<Type> DetectionRuleTypes { get; } = [];

    /// <summary>
    /// Registers a custom <see cref="IBreachDetectionRule"/> implementation for DI registration.
    /// </summary>
    /// <typeparam name="TRule">
    /// The detection rule type. Must implement <see cref="IBreachDetectionRule"/>.
    /// </typeparam>
    /// <returns>This options instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Rules registered here are added to the DI container as
    /// <see cref="IBreachDetectionRule"/> implementations, alongside the built-in rules.
    /// They will be injected into the <c>DefaultBreachDetector</c> and evaluated during
    /// breach detection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaBreachNotification(options =>
    /// {
    ///     options.AddDetectionRule&lt;GeoLocationAnomalyRule&gt;();
    ///     options.AddDetectionRule&lt;OffHoursAccessRule&gt;();
    /// });
    /// </code>
    /// </example>
    public BreachNotificationOptions AddDetectionRule<TRule>()
        where TRule : class, IBreachDetectionRule
    {
        var ruleType = typeof(TRule);

        if (!DetectionRuleTypes.Contains(ruleType))
        {
            DetectionRuleTypes.Add(ruleType);
        }

        return this;
    }
}
