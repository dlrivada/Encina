using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Defines a single breach detection rule that evaluates security events for potential
/// personal data breaches.
/// </summary>
/// <remarks>
/// <para>
/// Detection rules are the core evaluation units of the breach detection engine.
/// Each rule implements domain-specific logic to determine whether a given
/// <see cref="SecurityEvent"/> indicates a potential data breach. Rules are registered
/// with <see cref="IBreachDetector"/> and evaluated sequentially when a security event
/// is submitted for analysis.
/// </para>
/// <para>
/// Examples of detection rules:
/// <list type="bullet">
/// <item><description>Excessive failed authentication attempts from a single IP address.</description></item>
/// <item><description>Bulk data export exceeding a volume threshold.</description></item>
/// <item><description>Access to sensitive data outside business hours.</description></item>
/// <item><description>Unauthorized access to restricted data categories.</description></item>
/// <item><description>Data exfiltration patterns detected by network monitoring.</description></item>
/// </list>
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must become "aware" of a breach to trigger
/// the 72-hour notification obligation. Detection rules establish systematic awareness
/// by continuously evaluating security events against defined criteria, rather than
/// relying on ad-hoc discovery.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class ExcessiveFailedLoginsRule : IBreachDetectionRule
/// {
///     public string Name => "ExcessiveFailedLogins";
///
///     public ValueTask&lt;Either&lt;EncinaError, Option&lt;PotentialBreach&gt;&gt;&gt; EvaluateAsync(
///         SecurityEvent securityEvent,
///         CancellationToken cancellationToken = default)
///     {
///         if (securityEvent.EventType != SecurityEventType.UnauthorizedAccess)
///             return ValueTask.FromResult(Right&lt;EncinaError, Option&lt;PotentialBreach&gt;&gt;(None));
///
///         // Evaluate rule-specific logic...
///         var breach = new PotentialBreach
///         {
///             DetectionRuleName = Name,
///             Severity = BreachSeverity.High,
///             Description = "Excessive failed login attempts detected",
///             SecurityEvent = securityEvent,
///             DetectedAtUtc = DateTimeOffset.UtcNow
///         };
///
///         return ValueTask.FromResult(Right&lt;EncinaError, Option&lt;PotentialBreach&gt;&gt;(breach));
///     }
/// }
/// </code>
/// </example>
public interface IBreachDetectionRule
{
    /// <summary>
    /// Gets the unique name of this detection rule.
    /// </summary>
    /// <remarks>
    /// The name is used for identification in logs, audit trails, and when reporting
    /// which rule detected a potential breach via <see cref="PotentialBreach.DetectionRuleName"/>.
    /// Each registered rule must have a distinct name.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Evaluates a security event to determine if it indicates a potential data breach.
    /// </summary>
    /// <param name="securityEvent">The security event to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Option{PotentialBreach}"/> wrapping the breach if the event matches this
    /// rule's criteria, <c>None</c> if the event does not indicate a breach according to this rule,
    /// or an <see cref="EncinaError"/> if the evaluation could not be performed.
    /// </returns>
    /// <remarks>
    /// Returning <c>None</c> means the event did not match this rule — it does not
    /// indicate that no breach occurred. Other rules may still detect the event as a breach.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default);
}
