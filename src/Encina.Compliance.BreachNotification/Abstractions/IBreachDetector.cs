using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Orchestrates breach detection by evaluating security events against all registered
/// detection rules.
/// </summary>
/// <remarks>
/// <para>
/// The breach detector is the central entry point for security event evaluation.
/// When a <see cref="SecurityEvent"/> is submitted via <see cref="DetectAsync"/>,
/// the detector iterates all registered <see cref="IBreachDetectionRule"/> implementations
/// and aggregates their findings into a list of <see cref="PotentialBreach"/> results.
/// </para>
/// <para>
/// Detection rules can be registered at startup through dependency injection or
/// added at runtime via <see cref="RegisterDetectionRule"/>. This allows applications
/// to adapt their detection strategies dynamically based on evolving threat landscapes.
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must notify the supervisory authority
/// "without undue delay and, where feasible, not later than 72 hours after having
/// become aware of it." The breach detector establishes systematic awareness by
/// continuously evaluating security events, ensuring that breaches are detected
/// as early as possible.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Submit a security event for evaluation
/// var securityEvent = SecurityEvent.Create(
///     eventType: SecurityEventType.UnauthorizedAccess,
///     source: "AuthenticationService",
///     description: "50 failed login attempts from 192.168.1.100",
///     occurredAtUtc: DateTimeOffset.UtcNow);
///
/// var result = await detector.DetectAsync(securityEvent, cancellationToken);
///
/// result.Match(
///     Right: breaches =>
///     {
///         foreach (var breach in breaches)
///             Console.WriteLine($"Detected by {breach.DetectionRuleName}: {breach.Description}");
///     },
///     Left: error => Console.WriteLine($"Detection failed: {error.Message}"));
/// </code>
/// </example>
public interface IBreachDetector
{
    /// <summary>
    /// Evaluates a security event against all registered detection rules.
    /// </summary>
    /// <param name="securityEvent">The security event to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="PotentialBreach"/> instances detected by the
    /// registered rules. Returns an empty list if no rules detected a breach.
    /// Returns an <see cref="EncinaError"/> if the detection process could not be executed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A single security event may trigger multiple detection rules, resulting in
    /// multiple <see cref="PotentialBreach"/> entries. Each entry identifies the rule
    /// that detected it via <see cref="PotentialBreach.DetectionRuleName"/>.
    /// </para>
    /// <para>
    /// If an individual rule fails during evaluation, its error is logged but does
    /// not prevent other rules from executing. The method only returns an error if
    /// the detection process itself cannot be started.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<PotentialBreach>>> DetectAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a detection rule for runtime evaluation.
    /// </summary>
    /// <param name="rule">The detection rule to register.</param>
    /// <remarks>
    /// <para>
    /// Rules registered via this method are added to the set of rules evaluated by
    /// <see cref="DetectAsync"/>. Rules can also be registered through dependency
    /// injection at startup.
    /// </para>
    /// <para>
    /// If a rule with the same <see cref="IBreachDetectionRule.Name"/> is already
    /// registered, the new rule replaces the existing one.
    /// </para>
    /// </remarks>
    void RegisterDetectionRule(IBreachDetectionRule rule);

    /// <summary>
    /// Retrieves the names of all currently registered detection rules.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of rule names, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Useful for diagnostics, health checks, and verifying that expected rules
    /// are loaded at application startup.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<string>>> GetRegisteredRulesAsync(
        CancellationToken cancellationToken = default);
}
