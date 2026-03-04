namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Represents a potential breach identified by a detection rule after evaluating a
/// <see cref="SecurityEvent"/>.
/// </summary>
/// <remarks>
/// <para>
/// When an <c>IBreachDetectionRule</c> evaluates a <see cref="SecurityEvent"/> and determines
/// that a potential data breach has occurred, it returns a <see cref="PotentialBreach"/>
/// describing the finding. The <c>IBreachDetector</c> aggregates these findings and
/// triggers the breach handling workflow.
/// </para>
/// <para>
/// A potential breach does not automatically create a <see cref="BreachRecord"/>. The
/// <c>IBreachHandler</c> evaluates the potential breach, assesses severity, and determines
/// whether to create a formal breach record and initiate the 72-hour notification process
/// per GDPR Article 33(1).
/// </para>
/// <para>
/// Per EDPB Guidelines 9/2022, the controller should have internal procedures to assess
/// whether a security incident constitutes a personal data breach requiring notification.
/// The detection rule pipeline implements this assessment systematically.
/// </para>
/// </remarks>
public sealed record PotentialBreach
{
    /// <summary>
    /// Name of the detection rule that identified this potential breach.
    /// </summary>
    /// <remarks>
    /// Corresponds to the <c>IBreachDetectionRule.Name</c> property of the rule
    /// that produced this finding.
    /// </remarks>
    public required string DetectionRuleName { get; init; }

    /// <summary>
    /// Assessed severity of the potential breach.
    /// </summary>
    public required BreachSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable description of why this event is considered a potential breach.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The security event that triggered this detection.
    /// </summary>
    public required SecurityEvent SecurityEvent { get; init; }

    /// <summary>
    /// Timestamp when the potential breach was detected (UTC).
    /// </summary>
    /// <remarks>
    /// This timestamp marks when the detection rule identified the breach, which may be
    /// different from when the security event occurred (<see cref="SecurityEvent.OccurredAtUtc"/>).
    /// </remarks>
    public required DateTimeOffset DetectedAtUtc { get; init; }

    /// <summary>
    /// Recommended actions to take in response to this potential breach.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no specific recommendations are available. Detection rules
    /// may provide tailored recommendations based on the type and severity of the finding.
    /// </remarks>
    public IReadOnlyList<string>? RecommendedActions { get; init; }
}
