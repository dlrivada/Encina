namespace Encina.Marten;

/// <summary>
/// Configuration options for event metadata tracking.
/// </summary>
/// <remarks>
/// <para>
/// Event metadata enables automatic tracking of causation and correlation IDs
/// for distributed tracing and debugging in event-sourced systems.
/// </para>
/// <para>
/// <b>Correlation ID</b>: Shared across an entire conversation/workflow (e.g., order processing flow).
/// </para>
/// <para>
/// <b>Causation ID</b>: Links to the immediate parent event (forms a linked list of cause-effect).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaMarten(options =>
/// {
///     options.Metadata.CorrelationIdEnabled = true;   // Default: true
///     options.Metadata.CausationIdEnabled = true;     // Default: true
///     options.Metadata.CaptureUserId = true;          // Default: true
///     options.Metadata.CaptureCommitSha = true;       // Optional
///     options.Metadata.CaptureSemanticVersion = true; // Optional
///
///     // Add custom static headers
///     options.Metadata.CustomHeaders["Environment"] = "Production";
/// });
/// </code>
/// </example>
public sealed class EventMetadataOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether correlation ID tracking is enabled.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the correlation ID from <c>IRequestContext</c> is automatically
    /// stored with each event for distributed tracing across workflows.
    /// </remarks>
    public bool CorrelationIdEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether causation ID tracking is enabled.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the causation ID is automatically stored with each event,
    /// forming a linked list of cause-effect relationships.
    /// </remarks>
    public bool CausationIdEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether user ID capture is enabled.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the user ID from <c>IRequestContext</c> is stored as a header
    /// on each event for audit trail purposes.
    /// </remarks>
    public bool CaptureUserId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether tenant ID capture is enabled.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the tenant ID from <c>IRequestContext</c> is stored as a header
    /// on each event for multi-tenant scenarios.
    /// </remarks>
    public bool CaptureTenantId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to capture the Git commit SHA.
    /// Default is false.
    /// </summary>
    /// <remarks>
    /// When enabled, the commit SHA (from configuration or environment) is stored
    /// with each event. This helps correlate events with specific code versions.
    /// </remarks>
    public bool CaptureCommitSha { get; set; }

    /// <summary>
    /// Gets or sets the commit SHA to store with events.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="CaptureCommitSha"/> is true.
    /// Typically set from build configuration or environment variables.
    /// </remarks>
    public string? CommitSha { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to capture the semantic version.
    /// Default is false.
    /// </summary>
    /// <remarks>
    /// When enabled, the application semantic version is stored with each event.
    /// This helps with event schema evolution and debugging.
    /// </remarks>
    public bool CaptureSemanticVersion { get; set; }

    /// <summary>
    /// Gets or sets the semantic version to store with events.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="CaptureSemanticVersion"/> is true.
    /// Typically set from assembly version or configuration.
    /// </remarks>
    public string? SemanticVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to capture the timestamp.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the current UTC timestamp from <c>IRequestContext</c> is stored
    /// as a header. Note that Marten already stores timestamps, so this is for
    /// additional precision or when the request timestamp differs from storage time.
    /// </remarks>
    public bool CaptureTimestamp { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable custom headers storage.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, the <see cref="CustomHeaders"/> dictionary will be stored
    /// with each event. Set to false to disable header storage entirely.
    /// </remarks>
    public bool HeadersEnabled { get; set; } = true;

    /// <summary>
    /// Gets the custom headers to include with all events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to inject static metadata at configuration time.
    /// These headers will be added to every event persisted.
    /// </para>
    /// <para>
    /// Common use cases include:
    /// <list type="bullet">
    /// <item><description>Environment name (Development, Staging, Production)</description></item>
    /// <item><description>Service name for multi-service deployments</description></item>
    /// <item><description>Deployment region or zone</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.Metadata.CustomHeaders["Environment"] = "Production";
    /// options.Metadata.CustomHeaders["ServiceName"] = "OrderService";
    /// options.Metadata.CustomHeaders["Region"] = "eu-west-1";
    /// </code>
    /// </example>
    public Dictionary<string, string> CustomHeaders { get; } = [];

    /// <summary>
    /// Determines whether any metadata feature is enabled.
    /// </summary>
    /// <returns>True if any metadata tracking feature is enabled; otherwise, false.</returns>
    internal bool IsAnyMetadataEnabled()
    {
        return CorrelationIdEnabled
            || CausationIdEnabled
            || CaptureUserId
            || CaptureTenantId
            || CaptureCommitSha
            || CaptureSemanticVersion
            || CaptureTimestamp
            || (HeadersEnabled && CustomHeaders.Count > 0);
    }

    /// <summary>
    /// Determines whether Marten's header storage should be enabled.
    /// </summary>
    /// <returns>True if headers need to be stored; otherwise, false.</returns>
    /// <remarks>
    /// Returns true only if there is actual data to store in headers.
    /// For optional properties like CommitSha and SemanticVersion,
    /// the flag must be enabled AND the value must be configured.
    /// </remarks>
    internal bool RequiresHeaderStorage()
    {
        if (!HeadersEnabled)
        {
            return false;
        }

        // Check if any runtime-provided headers will be captured
        if (CaptureUserId || CaptureTenantId || CaptureTimestamp)
        {
            return true;
        }

        // Check if static metadata is configured
        if (CaptureCommitSha && !string.IsNullOrWhiteSpace(CommitSha))
        {
            return true;
        }

        if (CaptureSemanticVersion && !string.IsNullOrWhiteSpace(SemanticVersion))
        {
            return true;
        }

        // Check for custom headers
        return CustomHeaders.Count > 0;
    }
}
