using System.Reflection;

namespace Encina.Compliance.Consent;

/// <summary>
/// Configuration options for the consent compliance pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="ConsentRequiredPipelineBehavior{TRequest, TResponse}"/>
/// and how consent is validated at runtime.
/// Register via <c>AddEncinaConsent(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaConsent(options =>
/// {
///     options.EnforcementMode = ConsentEnforcementMode.Block;
///     options.DefaultExpirationDays = 365;
///     options.TrackConsentProof = true;
///     options.RequireExplicitConsent = true;
///     options.AllowGranularWithdrawal = true;
///     options.AddHealthCheck = true;
///     options.AutoRegisterFromAttributes = true;
///     options.FailOnUnknownPurpose = false;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
///
///     // Register purposes with fluent API
///     options.DefinePurpose(ConsentPurposes.Marketing, p =>
///     {
///         p.Description = "Email marketing communications";
///         p.RequiresExplicitOptIn = true;
///         p.CanBeWithdrawnAnytime = true;
///     });
/// });
/// </code>
/// </example>
public sealed class ConsentOptions
{
    /// <summary>
    /// Gets or sets the enforcement mode for consent compliance checks.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="ConsentEnforcementMode.Block"/> — requests without valid consent are blocked with an error.</description></item>
    /// <item><description><see cref="ConsentEnforcementMode.Warn"/> — requests without valid consent log a warning but proceed.</description></item>
    /// <item><description><see cref="ConsentEnforcementMode.Disabled"/> — consent validation is completely disabled.</description></item>
    /// </list>
    /// Default is <see cref="ConsentEnforcementMode.Block"/>.
    /// </remarks>
    public ConsentEnforcementMode EnforcementMode { get; set; } = ConsentEnforcementMode.Block;

    /// <summary>
    /// Gets or sets the default expiration period in days for new consent records.
    /// </summary>
    /// <remarks>
    /// When set, new consent records that do not specify an explicit expiration will
    /// default to this period. Set to <c>null</c> to require explicit expiration on
    /// every consent record. Default is 365 days.
    /// </remarks>
    public int? DefaultExpirationDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets whether to store proof of consent (consent form hash/reference).
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the system will record the <see cref="ConsentRecord.ProofOfConsent"/>
    /// field when available. This supports Article 7(1) demonstrability requirements.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool TrackConsentProof { get; set; }

    /// <summary>
    /// Gets or sets whether consent must be explicitly granted before processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the system requires that consent is explicitly granted by the data
    /// subject before any processing occurs. This aligns with GDPR Article 6(1)(a) which
    /// requires "unambiguous" consent and Recital 32 which specifies that consent should
    /// involve a "clear affirmative act."
    /// </para>
    /// <para>
    /// When <c>false</c>, implied consent models may be used (e.g., continued use of service).
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool RequireExplicitConsent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether data subjects can withdraw consent for individual purposes
    /// without affecting other consents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the system supports per-purpose withdrawal, allowing data subjects
    /// to selectively revoke consent. This supports GDPR Article 7(3) which requires that
    /// withdrawal of consent be as easy as giving it.
    /// </para>
    /// <para>
    /// When <c>false</c>, consent withdrawal applies to all purposes at once.
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool AllowGranularWithdrawal { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register a consent health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the consent store
    /// is reachable and the consent validator is registered. Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    // --- Auto-Registration ---

    /// <summary>
    /// Gets or sets whether to automatically scan assemblies for <see cref="RequireConsentAttribute"/>
    /// at startup and validate discovered purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>ConsentAutoRegistrationHostedService</c> scans the assemblies
    /// in <see cref="AssembliesToScan"/> for request types decorated with
    /// <see cref="RequireConsentAttribute"/>. Discovered purposes are validated against
    /// <see cref="PurposeDefinitions"/>.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    /// <summary>
    /// Gets the assemblies to scan for <see cref="RequireConsentAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your request types decorated with
    /// <see cref="RequireConsentAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// auto-registration is skipped.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];

    /// <summary>
    /// Gets the set of known/valid consent purpose identifiers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Purposes discovered via <see cref="RequireConsentAttribute"/> are validated against
    /// this set during auto-registration. Unknown purposes generate warnings or errors
    /// depending on <see cref="FailOnUnknownPurpose"/>.
    /// </para>
    /// <para>
    /// Populate with the consent purposes your application defines. Use constants from
    /// <see cref="ConsentPurposes"/> for standard purposes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.PurposeDefinitions.Add(ConsentPurposes.Marketing);
    /// options.PurposeDefinitions.Add(ConsentPurposes.Analytics);
    /// options.PurposeDefinitions.Add("custom-purpose");
    /// </code>
    /// </example>
    public HashSet<string> PurposeDefinitions { get; } = [];

    /// <summary>
    /// Gets or sets whether to throw an exception when a <see cref="RequireConsentAttribute"/>
    /// references a purpose not in <see cref="PurposeDefinitions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>ConsentAutoRegistrationHostedService</c> throws an
    /// <see cref="InvalidOperationException"/> at startup if any unknown purposes are found,
    /// preventing the application from starting with misconfigured consent requirements.
    /// </para>
    /// <para>
    /// When <c>false</c>, unknown purposes are logged as warnings but do not prevent startup.
    /// </para>
    /// <para>
    /// Default is <c>false</c>.
    /// </para>
    /// </remarks>
    public bool FailOnUnknownPurpose { get; set; }

    /// <summary>
    /// Gets the detailed purpose definitions with metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stores <see cref="PurposeDefinitionEntry"/> instances keyed by purpose identifier.
    /// Use <see cref="DefinePurpose"/> to add entries via the fluent API.
    /// Each entry is automatically added to <see cref="PurposeDefinitions"/> as well.
    /// </para>
    /// </remarks>
    public Dictionary<string, PurposeDefinitionEntry> DetailedPurposeDefinitions { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Defines a consent purpose with detailed metadata using a fluent configuration API.
    /// </summary>
    /// <param name="purpose">The purpose identifier (e.g., <see cref="ConsentPurposes.Marketing"/>).</param>
    /// <param name="configure">An action to configure the purpose definition details.</param>
    /// <returns>This <see cref="ConsentOptions"/> instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the purpose in both <see cref="PurposeDefinitions"/> (for
    /// auto-registration validation) and <see cref="DetailedPurposeDefinitions"/> (for
    /// runtime metadata access).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.DefinePurpose(ConsentPurposes.Marketing, p =>
    /// {
    ///     p.Description = "Email marketing communications";
    ///     p.RequiresExplicitOptIn = true;
    ///     p.CanBeWithdrawnAnytime = true;
    ///     p.DefaultExpirationDays = 180;
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when <paramref name="purpose"/> is null or whitespace.</exception>
    public ConsentOptions DefinePurpose(string purpose, Action<PurposeDefinitionEntry>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var definition = new PurposeDefinitionEntry();
        configure?.Invoke(definition);

        PurposeDefinitions.Add(purpose);
        DetailedPurposeDefinitions[purpose] = definition;

        return this;
    }

    /// <summary>
    /// Detailed metadata for a consent purpose definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by <see cref="ConsentOptions.DefinePurpose"/> to capture rich metadata about
    /// each consent purpose, including whether it requires explicit opt-in (Article 6(1)(a)),
    /// whether it can be withdrawn at any time (Article 7(3)), and default expiration periods.
    /// </para>
    /// </remarks>
    public sealed class PurposeDefinitionEntry
    {
        /// <summary>
        /// Gets or sets the human-readable description of this consent purpose.
        /// </summary>
        /// <remarks>
        /// Describes what the purpose entails and how the data will be used.
        /// Should be written in clear, plain language per Recital 32.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether this purpose requires explicit opt-in from the data subject.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, consent must be given through a clear affirmative act
        /// (GDPR Article 6(1)(a), Recital 32). Pre-ticked checkboxes or silence
        /// do not constitute valid consent. Default is <c>false</c>.
        /// </remarks>
        public bool RequiresExplicitOptIn { get; set; }

        /// <summary>
        /// Gets or sets whether this purpose can be withdrawn at any time.
        /// </summary>
        /// <remarks>
        /// GDPR Article 7(3) requires that withdrawal of consent must be as easy as
        /// giving it. When <c>true</c>, the system allows the data subject to withdraw
        /// consent for this specific purpose at any time without restrictions.
        /// Default is <c>true</c>.
        /// </remarks>
        public bool CanBeWithdrawnAnytime { get; set; } = true;

        /// <summary>
        /// Gets or sets the purpose-specific default expiration period in days.
        /// </summary>
        /// <remarks>
        /// When set, overrides <see cref="ConsentOptions.DefaultExpirationDays"/> for
        /// this specific purpose. Set to <c>null</c> to use the global default.
        /// </remarks>
        public int? DefaultExpirationDays { get; set; }
    }
}
