using System.Reflection;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Configuration options for the Data Subject Rights module.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the DSR pipeline behaviors, request processing,
/// and compliance enforcement. All options have sensible defaults aligned with GDPR
/// Articles 15-22 requirements.
/// </para>
/// <para>
/// Register via <c>AddEncinaDataSubjectRights(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaDataSubjectRights(options =>
/// {
///     options.RestrictionEnforcementMode = DSREnforcementMode.Warn; // gradual adoption
///     options.DefaultDeadlineDays = 30;
///     options.AddHealthCheck = true;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
/// });
/// </code>
/// </example>
public sealed class DataSubjectRightsOptions
{
    /// <summary>
    /// Gets or sets the enforcement mode for processing restriction checks (Article 18).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>ProcessingRestrictionPipelineBehavior</c> responds when a request
    /// targets a data subject with an active processing restriction:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="DSREnforcementMode.Block"/>: Returns an error, preventing processing.</item>
    /// <item><see cref="DSREnforcementMode.Warn"/>: Logs a warning but allows processing.</item>
    /// <item><see cref="DSREnforcementMode.Disabled"/>: Skips restriction checks entirely.</item>
    /// </list>
    /// <para>
    /// Default is <see cref="DSREnforcementMode.Block"/> for production safety.
    /// </para>
    /// </remarks>
    public DSREnforcementMode RestrictionEnforcementMode { get; set; } = DSREnforcementMode.Block;

    /// <summary>
    /// Gets or sets whether to register a DSR health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the DSR request store
    /// is reachable, required services are registered, and no overdue requests exist.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically scan assemblies for <see cref="PersonalDataAttribute"/>
    /// at startup and build a personal data map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>DSRAutoRegistrationHostedService</c> scans the assemblies
    /// in <see cref="AssembliesToScan"/> for entity types with properties decorated with
    /// <see cref="PersonalDataAttribute"/>. Discovered types and fields are catalogued
    /// for use by <see cref="IPersonalDataLocator"/> implementations.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to publish domain notifications for DSR lifecycle events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the DSR handler publishes notifications at key lifecycle points
    /// (e.g., request received, request completed, erasure executed). These can be used
    /// for audit logging, event sourcing, or integration with external systems.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool PublishNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets the default deadline in days for completing a DSR request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 12(3), controllers must respond to data subject requests
    /// "without undue delay and in any event within one month." This translates to
    /// approximately 30 calendar days.
    /// </para>
    /// <para>
    /// Default is <c>30</c> days.
    /// </para>
    /// </remarks>
    public int DefaultDeadlineDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum extension in days that can be granted for complex requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 12(3), the initial deadline may be extended by "a further two months
    /// where necessary, taking into account the complexity and number of the requests."
    /// The data subject must be informed of the extension within the original deadline.
    /// </para>
    /// <para>
    /// Default is <c>60</c> days (2 additional months).
    /// </para>
    /// </remarks>
    public int MaxExtensionDays { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to record an audit trail for all DSR operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, all DSR operations (requests received, identity verified, data accessed,
    /// erasure executed, etc.) are recorded in the <see cref="IDSRAuditStore"/> for
    /// accountability purposes (Article 5(2)).
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool TrackAuditTrail { get; set; } = true;

    // --- Auto-Registration ---

    /// <summary>
    /// Gets the assemblies to scan for <see cref="PersonalDataAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your entity types with properties decorated with
    /// <see cref="PersonalDataAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the entry assembly is used as a fallback.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];

    // --- Category Defaults ---

    /// <summary>
    /// Gets the set of personal data categories that are erasable by default during
    /// right-to-erasure (Article 17) requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a category is in this set, all <see cref="PersonalDataAttribute"/> fields of
    /// that category are eligible for erasure unless the field explicitly sets
    /// <see cref="PersonalDataAttribute.Erasable"/> to <c>false</c> or has
    /// <see cref="PersonalDataAttribute.LegalRetention"/> set to <c>true</c>.
    /// </para>
    /// <para>
    /// If empty, all categories are considered erasable (the per-field
    /// <see cref="PersonalDataAttribute.Erasable"/> flag is the final arbiter).
    /// </para>
    /// </remarks>
    public HashSet<PersonalDataCategory> DefaultErasableCategories { get; } = [];

    /// <summary>
    /// Gets the set of personal data categories that are included in portability
    /// exports (Article 20) by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a category is in this set, all <see cref="PersonalDataAttribute"/> fields of
    /// that category are included in data portability exports unless the field explicitly
    /// sets <see cref="PersonalDataAttribute.Portable"/> to <c>false</c>.
    /// </para>
    /// <para>
    /// If empty, all categories are considered portable (the per-field
    /// <see cref="PersonalDataAttribute.Portable"/> flag is the final arbiter).
    /// </para>
    /// </remarks>
    public HashSet<PersonalDataCategory> DefaultPortableCategories { get; } = [];
}
