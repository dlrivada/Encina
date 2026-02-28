using System.Reflection;

using Encina.Compliance.Anonymization.Model;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Configuration options for the Anonymization module.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the anonymization pipeline behavior, technique defaults,
/// key management, and compliance enforcement. All options have sensible defaults aligned with
/// GDPR Articles 4(5), 25, 32, and 89 requirements.
/// </para>
/// <para>
/// Register via <c>AddEncinaAnonymization(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAnonymization(options =>
/// {
///     options.EnforcementMode = AnonymizationEnforcementMode.Warn; // gradual adoption
///     options.TrackAuditTrail = true;
///     options.AddHealthCheck = true;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
/// });
/// </code>
/// </example>
public sealed class AnonymizationOptions
{
    /// <summary>
    /// Gets or sets the enforcement mode for the anonymization pipeline behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>AnonymizationPipelineBehavior</c> responds when a transformation
    /// fails or a decorated field cannot be processed:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="AnonymizationEnforcementMode.Block"/>: Returns an error, preventing the response.</item>
    /// <item><see cref="AnonymizationEnforcementMode.Warn"/>: Logs a warning but returns the untransformed response.</item>
    /// <item><see cref="AnonymizationEnforcementMode.Disabled"/>: Skips anonymization entirely.</item>
    /// </list>
    /// <para>
    /// Default is <see cref="AnonymizationEnforcementMode.Block"/> for production safety.
    /// </para>
    /// </remarks>
    public AnonymizationEnforcementMode EnforcementMode { get; set; } = AnonymizationEnforcementMode.Block;

    /// <summary>
    /// Gets or sets whether to record an audit trail for all anonymization operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, all anonymization, pseudonymization, and tokenization operations
    /// are recorded in the <see cref="IAnonymizationAuditStore"/> for accountability
    /// purposes (GDPR Article 5(2)).
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool TrackAuditTrail { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register an anonymization health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the anonymization infrastructure
    /// is properly configured, required services are registered, and techniques are available.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically scan assemblies for anonymization attributes
    /// (<see cref="AnonymizeAttribute"/>, <see cref="PseudonymizeAttribute"/>,
    /// <see cref="TokenizeAttribute"/>) at startup and build a field map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>AnonymizationAutoRegistrationHostedService</c> scans the assemblies
    /// in <see cref="AssembliesToScan"/> for response types with properties decorated with
    /// anonymization attributes. Discovered types and fields are catalogued and logged
    /// for observability.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    // --- Auto-Registration ---

    /// <summary>
    /// Gets the assemblies to scan for anonymization attribute decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your response types with properties decorated with
    /// <see cref="AnonymizeAttribute"/>, <see cref="PseudonymizeAttribute"/>,
    /// or <see cref="TokenizeAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the entry assembly is used as a fallback.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];
}
