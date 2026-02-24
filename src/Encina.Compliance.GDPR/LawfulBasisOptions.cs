using System.Reflection;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Configuration options for the lawful basis validation pipeline behavior.
/// </summary>
/// <remarks>
/// <para>
/// These options control how <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>
/// validates lawful basis declarations and enforces GDPR Article 6(1) requirements.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasis(options => { ... })</c> to configure the pipeline behavior,
/// auto-registration from attributes, and optional health checks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaLawfulBasis(options =>
/// {
///     options.EnforcementMode = LawfulBasisEnforcementMode.Block;
///     options.RequireDeclaredBasis = true;
///     options.ValidateConsentForConsentBasis = true;
///     options.ValidateLIAForLegitimateInterests = true;
///     options.AutoRegisterFromAttributes = true;
///     options.ScanAssemblyContaining&lt;Program&gt;();
///     options.DefaultBasis&lt;GetUserProfileQuery&gt;(LawfulBasis.Contract);
/// });
/// </code>
/// </example>
public sealed class LawfulBasisOptions
{
    /// <summary>
    /// Gets or sets the enforcement mode for lawful basis validation.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="LawfulBasisEnforcementMode.Block"/> — requests without a valid lawful basis are blocked with an error.</description></item>
    /// <item><description><see cref="LawfulBasisEnforcementMode.Warn"/> — requests without a valid lawful basis log a warning but proceed.</description></item>
    /// <item><description><see cref="LawfulBasisEnforcementMode.Disabled"/> — lawful basis validation is completely disabled.</description></item>
    /// </list>
    /// Default is <see cref="LawfulBasisEnforcementMode.Block"/>.
    /// </remarks>
    public LawfulBasisEnforcementMode EnforcementMode { get; set; } = LawfulBasisEnforcementMode.Block;

    /// <summary>
    /// Gets or sets whether all requests decorated with <see cref="ProcessesPersonalDataAttribute"/>
    /// or <see cref="ProcessingActivityAttribute"/> must have a declared lawful basis.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the pipeline behavior requires a <see cref="LawfulBasisAttribute"/> or
    /// <see cref="ProcessingActivityAttribute"/> with a lawful basis on the request type.
    /// When <c>false</c>, only request types with explicit lawful basis attributes are validated.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool RequireDeclaredBasis { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate active consent when the lawful basis is <see cref="LawfulBasis.Consent"/>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c> and an <see cref="IConsentStatusProvider"/> is registered, the behavior
    /// will extract the subject ID and verify active consent for the declared purposes.
    /// When <c>false</c>, consent-based requests are not further validated beyond the basis declaration.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool ValidateConsentForConsentBasis { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate the Legitimate Interest Assessment when the lawful basis
    /// is <see cref="LawfulBasis.LegitimateInterests"/>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the behavior validates the LIA reference via <see cref="ILegitimateInterestAssessment"/>
    /// to ensure it exists and is approved.
    /// When <c>false</c>, legitimate interests requests are not further validated beyond the basis declaration.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool ValidateLIAForLegitimateInterests { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically register lawful basis from <see cref="LawfulBasisAttribute"/>
    /// decorations at startup.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the assemblies in <see cref="AssembliesToScan"/> are scanned for
    /// <see cref="LawfulBasisAttribute"/> decorations and the registrations are created in the
    /// <see cref="ILawfulBasisRegistry"/>. Default is <c>true</c>.
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register a lawful basis health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies access to
    /// <see cref="ILawfulBasisRegistry"/> and <see cref="ILIAStore"/>, counts registrations,
    /// and reports pending LIA reviews. Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets the assemblies to scan for <see cref="LawfulBasisAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your request types decorated with <see cref="LawfulBasisAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the entry assembly will be scanned by default.
    /// </para>
    /// </remarks>
    public HashSet<Assembly> AssembliesToScan { get; } = [];

    /// <summary>
    /// Gets the default lawful bases for request types that are not decorated with attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides a programmatic way to declare lawful bases for request types without using attributes.
    /// These are registered in the <see cref="ILawfulBasisRegistry"/> at startup alongside
    /// attribute-based registrations.
    /// </para>
    /// <para>
    /// Use <see cref="DefaultBasis{TRequest}"/> for type-safe registration.
    /// </para>
    /// </remarks>
    public Dictionary<Type, LawfulBasis> DefaultBases { get; } = [];

    // ================================================================
    // Fluent configuration methods
    // ================================================================

    /// <summary>
    /// Registers a default lawful basis for the specified request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type to register a default basis for.</typeparam>
    /// <param name="basis">The lawful basis to associate with the request type.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to register lawful bases for request types that are not decorated with
    /// <see cref="LawfulBasisAttribute"/>. These registrations are applied at startup via the
    /// <see cref="ILawfulBasisRegistry"/>.
    /// </para>
    /// <para>
    /// If the request type also has a <see cref="LawfulBasisAttribute"/>, the attribute takes priority.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaLawfulBasis(options =>
    /// {
    ///     options.DefaultBasis&lt;GetUserProfileQuery&gt;(LawfulBasis.Contract);
    ///     options.DefaultBasis&lt;AnalyticsCommand&gt;(LawfulBasis.LegitimateInterests);
    /// });
    /// </code>
    /// </example>
    public LawfulBasisOptions DefaultBasis<TRequest>(LawfulBasis basis)
    {
        DefaultBases[typeof(TRequest)] = basis;
        return this;
    }

    /// <summary>
    /// Adds an assembly to the list of assemblies to scan for <see cref="LawfulBasisAttribute"/> decorations.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is <c>null</c>.</exception>
    public LawfulBasisOptions ScanAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        AssembliesToScan.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds the assembly containing the specified type to the list of assemblies to scan.
    /// </summary>
    /// <typeparam name="T">A type whose assembly should be scanned.</typeparam>
    /// <returns>This options instance for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaLawfulBasis(options =>
    /// {
    ///     options.ScanAssemblyContaining&lt;Program&gt;();
    ///     options.ScanAssemblyContaining&lt;MyRequestHandler&gt;();
    /// });
    /// </code>
    /// </example>
    public LawfulBasisOptions ScanAssemblyContaining<T>()
    {
        AssembliesToScan.Add(typeof(T).Assembly);
        return this;
    }
}
