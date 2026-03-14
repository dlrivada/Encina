namespace Encina.Compliance.CrossBorderTransfer;

/// <summary>
/// Configuration options for cross-border data transfer compliance enforcement.
/// </summary>
/// <remarks>
/// <para>
/// These options control how the <see cref="Pipeline.TransferBlockingPipelineBehavior{TRequest, TResponse}"/>
/// enforces GDPR Chapter V compliance for international data transfers.
/// </para>
/// <para>
/// Configure via <c>AddEncinaCrossBorderTransfer(options =&gt; { ... })</c> during service registration.
/// </para>
/// </remarks>
public sealed class CrossBorderTransferOptions
{
    /// <summary>
    /// Gets or sets the enforcement mode for cross-border transfer validation.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="CrossBorderTransferEnforcementMode.Block"/>, which blocks
    /// non-compliant transfers. Use <see cref="CrossBorderTransferEnforcementMode.Warn"/>
    /// during migration or <see cref="CrossBorderTransferEnforcementMode.Disabled"/> in
    /// development environments.
    /// </remarks>
    public CrossBorderTransferEnforcementMode EnforcementMode { get; set; } = CrossBorderTransferEnforcementMode.Block;

    /// <summary>
    /// Gets or sets the default ISO 3166-1 alpha-2 country code for the data source (exporter).
    /// </summary>
    /// <remarks>
    /// Used when the source country is not specified in the
    /// <see cref="Attributes.RequiresCrossBorderTransferAttribute"/> or request properties.
    /// Typically set to the country where the primary data center is located.
    /// </remarks>
    /// <example>"DE" (Germany), "FR" (France), "NL" (Netherlands)</example>
    public string DefaultSourceCountryCode { get; set; } = "DE";

    /// <summary>
    /// Gets or sets the risk threshold for TIA assessments.
    /// </summary>
    /// <remarks>
    /// Risk scores above this threshold require supplementary measures or block the transfer.
    /// Must be between 0.0 and 1.0 inclusive. Defaults to 0.6.
    /// </remarks>
    public double TIARiskThreshold { get; set; } = 0.6;

    /// <summary>
    /// Gets or sets the default TIA expiration in days.
    /// </summary>
    /// <remarks>
    /// <c>null</c> means TIAs do not auto-expire. Defaults to 365 days (1 year).
    /// </remarks>
    public int? DefaultTIAExpirationDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets the default SCC agreement expiration in days.
    /// </summary>
    /// <remarks>
    /// <c>null</c> means SCC agreements do not auto-expire. Defaults to <c>null</c>.
    /// </remarks>
    public int? DefaultSCCExpirationDays { get; set; }

    /// <summary>
    /// Gets or sets the default transfer authorization expiration in days.
    /// </summary>
    /// <remarks>
    /// <c>null</c> means transfers do not auto-expire. Defaults to 365 days (1 year).
    /// </remarks>
    public int? DefaultTransferExpirationDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets whether to automatically detect cross-border transfers.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the pipeline behavior will analyze all requests for potential
    /// cross-border transfers, not just those decorated with
    /// <see cref="Attributes.RequiresCrossBorderTransferAttribute"/>. Defaults to <c>false</c>.
    /// </remarks>
    public bool AutoDetectTransfers { get; set; }

    /// <summary>
    /// Gets or sets whether caching is enabled for transfer validation results.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Disable for testing or when immediate consistency is required.
    /// </remarks>
    public bool CacheEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache time-to-live in minutes.
    /// </summary>
    /// <remarks>
    /// Defaults to 5 minutes. Must be greater than 0.
    /// </remarks>
    public int CacheTTLMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to register a health check for cross-border transfer compliance.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>false</c>. When enabled, a health check is registered that verifies
    /// connectivity to the underlying stores.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether a TIA is required for transfers to non-adequate countries.
    /// </summary>
    /// <remarks>
    /// Per Schrems II, a TIA is generally required for transfers based on SCCs or BCRs
    /// to countries without an adequacy decision. Defaults to <c>true</c>.
    /// </remarks>
    public bool RequireTIAForNonAdequate { get; set; } = true;

    /// <summary>
    /// Gets or sets whether SCCs are required for transfers to non-adequate countries.
    /// </summary>
    /// <remarks>
    /// Per GDPR Art. 46(2)(c), SCCs provide appropriate safeguards for transfers
    /// without an adequacy decision. Defaults to <c>true</c>.
    /// </remarks>
    public bool RequireSCCForNonAdequate { get; set; } = true;

    // --- Expiration monitoring ---

    /// <summary>
    /// Gets or sets whether to enable the background expiration monitoring service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <see cref="Notifications.TransferExpirationMonitor"/> background
    /// service runs monitoring cycles at the interval specified by <see cref="ExpirationCheckInterval"/>.
    /// When <c>false</c>, expiration monitoring must be managed manually.
    /// </para>
    /// <para>
    /// Defaults to <c>false</c>. Enable for proactive expiration tracking.
    /// </para>
    /// </remarks>
    public bool EnableExpirationMonitoring { get; set; }

    /// <summary>
    /// Gets or sets the interval between expiration monitoring checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how frequently the <see cref="Notifications.TransferExpirationMonitor"/>
    /// checks for transfers, TIAs, and SCC agreements approaching their expiration date.
    /// Only applies when <see cref="EnableExpirationMonitoring"/> is <c>true</c>.
    /// </para>
    /// <para>
    /// Default is <c>1 hour</c>. Shorter intervals increase alerting responsiveness
    /// but may add database load.
    /// </para>
    /// </remarks>
    public TimeSpan ExpirationCheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the number of days before expiration to start warning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transfers, TIAs, and SCC agreements expiring within this many days will trigger
    /// "expiring" notifications. Defaults to 30 days.
    /// </para>
    /// </remarks>
    public int AlertBeforeExpirationDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to publish expiration notifications via <see cref="IEncina"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the expiration monitor publishes <c>INotification</c> events that
    /// handlers can subscribe to for alerting, dashboard updates, or automated workflows.
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool PublishExpirationNotifications { get; set; } = true;
}
