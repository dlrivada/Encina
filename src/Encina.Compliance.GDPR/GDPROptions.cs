using System.Reflection;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Configuration options for the GDPR compliance pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/>
/// and how processing activities are validated at runtime.
/// Register via <c>AddEncinaGDPR(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaGDPR(options =>
/// {
///     options.ControllerName = "Acme Corp";
///     options.ControllerEmail = "privacy@acme.com";
///     options.DataProtectionOfficer = new DataProtectionOfficer("Jane Doe", "dpo@acme.com", "+1-555-0100");
///     options.BlockUnregisteredProcessing = true;
///     options.EnforcementMode = GDPREnforcementMode.Enforce;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
/// });
/// </code>
/// </example>
public sealed class GDPROptions
{
    // --- Controller Information (Article 30(1)(a)) ---

    /// <summary>
    /// Gets or sets the name of the data controller (organization name).
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(a). Identifies the organization responsible for data processing.
    /// </remarks>
    public string? ControllerName { get; set; }

    /// <summary>
    /// Gets or sets the contact email of the data controller.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(a). Used for data subject access requests and supervisory authority contact.
    /// </remarks>
    public string? ControllerEmail { get; set; }

    // --- Data Protection Officer (Articles 37-39) ---

    /// <summary>
    /// Gets or sets the Data Protection Officer information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required by Article 37 for certain organizations (public authorities, large-scale monitoring,
    /// special category data processing). The DPO's contact details must be communicated to the
    /// supervisory authority (Article 37(7)).
    /// </para>
    /// <para>
    /// Set to <c>null</c> if a DPO is not required for your organization.
    /// </para>
    /// </remarks>
    public IDataProtectionOfficer? DataProtectionOfficer { get; set; }

    // --- Enforcement ---

    /// <summary>
    /// Gets or sets whether to block requests whose processing activity is not registered in the RoPA.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, requests decorated with <see cref="ProcessingActivityAttribute"/> or
    /// <see cref="ProcessesPersonalDataAttribute"/> that have no corresponding entry in the
    /// <see cref="IProcessingActivityRegistry"/> will be short-circuited with an error.
    /// When <c>false</c>, a warning is logged but processing continues.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool BlockUnregisteredProcessing { get; set; }

    /// <summary>
    /// Gets or sets the enforcement mode for GDPR compliance checks.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="GDPREnforcementMode.Enforce"/> — non-compliant requests are blocked with an error.</description></item>
    /// <item><description><see cref="GDPREnforcementMode.WarnOnly"/> — non-compliant requests log a warning but proceed.</description></item>
    /// </list>
    /// Default is <see cref="GDPREnforcementMode.Enforce"/>.
    /// </remarks>
    public GDPREnforcementMode EnforcementMode { get; set; } = GDPREnforcementMode.Enforce;

    // --- Auto-Registration ---

    /// <summary>
    /// Gets or sets whether to automatically register processing activities from
    /// <see cref="ProcessingActivityAttribute"/> at startup.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the assemblies in <see cref="AssembliesToScan"/> are scanned for
    /// <see cref="ProcessingActivityAttribute"/> decorations and the activities are registered
    /// in the <see cref="IProcessingActivityRegistry"/>.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    // --- Health Check ---

    /// <summary>
    /// Gets or sets whether to register a GDPR health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the processing activity
    /// registry is populated, controller information is configured, and the compliance validator
    /// is registered. Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    // --- Auto-Registration ---

    /// <summary>
    /// Gets the assemblies to scan for <see cref="ProcessingActivityAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your request types decorated with
    /// <see cref="ProcessingActivityAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the entry assembly will be scanned by default.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];
}
