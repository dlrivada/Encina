namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Represents a security event that may indicate a personal data breach.
/// </summary>
/// <remarks>
/// <para>
/// Security events are the input for the breach detection engine. Applications
/// create <see cref="SecurityEvent"/> instances from their security infrastructure
/// (e.g., authentication logs, access control systems, IDS/IPS alerts) and submit
/// them for evaluation against registered <c>IBreachDetectionRule</c> implementations.
/// </para>
/// <para>
/// The <c>BreachDetectionPipelineBehavior&lt;TRequest, TResponse&gt;</c> can also
/// generate security events automatically for requests marked with the
/// <c>[BreachMonitored]</c> attribute.
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must become "aware" of a breach to trigger
/// the 72-hour notification obligation. Security event collection and breach detection
/// rules help establish this awareness systematically rather than relying on ad-hoc
/// discovery.
/// </para>
/// </remarks>
public sealed record SecurityEvent
{
    /// <summary>
    /// Unique identifier for this security event.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Type of security event.
    /// </summary>
    public required SecurityEventType EventType { get; init; }

    /// <summary>
    /// Source system or component that generated this event.
    /// </summary>
    /// <remarks>
    /// Examples: "AuthenticationService", "DatabaseAuditLog", "FileAccessMonitor",
    /// "NetworkFirewall", "ApplicationLayer".
    /// </remarks>
    public required string Source { get; init; }

    /// <summary>
    /// Human-readable description of the security event.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Timestamp when the security event occurred (UTC).
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Identifier of the user associated with this event.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the event is not attributable to a specific user
    /// (e.g., system-level intrusion, automated attack).
    /// </remarks>
    public string? UserId { get; init; }

    /// <summary>
    /// IP address associated with this event.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the event originates from an internal system or
    /// the IP address is not available.
    /// </remarks>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Type of the entity affected by this event.
    /// </summary>
    /// <remarks>
    /// Examples: "User", "Order", "PaymentRecord", "HealthRecord".
    /// <c>null</c> when the event does not target a specific entity type.
    /// </remarks>
    public string? AffectedEntityType { get; init; }

    /// <summary>
    /// Identifier of the entity affected by this event.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the event does not target a specific entity instance.
    /// </remarks>
    public string? AffectedEntityId { get; init; }

    /// <summary>
    /// Additional metadata associated with this security event.
    /// </summary>
    /// <remarks>
    /// Allows attaching domain-specific information for custom detection rules.
    /// Examples: query count thresholds, accessed table names, authentication method used.
    /// <c>null</c> when no additional metadata is available.
    /// </remarks>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    /// <summary>
    /// Creates a new security event with a generated unique identifier.
    /// </summary>
    /// <param name="eventType">Type of security event.</param>
    /// <param name="source">Source system or component.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="occurredAtUtc">Timestamp when the event occurred.</param>
    /// <returns>A new <see cref="SecurityEvent"/> with a generated GUID identifier.</returns>
    public static SecurityEvent Create(
        SecurityEventType eventType,
        string source,
        string description,
        DateTimeOffset occurredAtUtc) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            Source = source,
            Description = description,
            OccurredAtUtc = occurredAtUtc
        };
}
