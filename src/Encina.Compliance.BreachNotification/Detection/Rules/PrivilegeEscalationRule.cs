using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification.Detection.Rules;

/// <summary>
/// Evaluates <see cref="SecurityEventType.PrivilegeEscalation"/> events to detect
/// potential privilege escalation breaches.
/// </summary>
/// <remarks>
/// <para>
/// This rule identifies unauthorized elevation of system privileges: users gaining
/// administrative access without authorization, service accounts accessing resources
/// beyond their scope, or exploitation of vulnerabilities to gain elevated permissions.
/// </para>
/// <para>
/// Privilege escalation is a critical indicator of a potential data breach because
/// elevated privileges grant access to personal data that would otherwise be restricted.
/// Every privilege escalation event is treated as a high-severity breach indicator.
/// </para>
/// </remarks>
public sealed class PrivilegeEscalationRule : IBreachDetectionRule
{
    private readonly ILogger<PrivilegeEscalationRule> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivilegeEscalationRule"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public PrivilegeEscalationRule(ILogger<PrivilegeEscalationRule> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "PrivilegeEscalation";

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);

        if (securityEvent.EventType != SecurityEventType.PrivilegeEscalation)
        {
            return ValueTask.FromResult(
                Right<EncinaError, Option<PotentialBreach>>(None));
        }

        _logger.LogDebug(
            "Evaluating privilege escalation event '{EventId}' from source '{Source}'",
            securityEvent.Id, securityEvent.Source);

        var breach = new PotentialBreach
        {
            DetectionRuleName = Name,
            Severity = BreachSeverity.High,
            Description = $"Privilege escalation detected from source '{securityEvent.Source}': {securityEvent.Description}",
            SecurityEvent = securityEvent,
            DetectedAtUtc = DateTimeOffset.UtcNow,
            RecommendedActions = [
                "Immediately revoke escalated privileges",
                "Audit all actions performed with elevated access",
                "Review access control policies and configurations",
                "Investigate the escalation vector"
            ]
        };

        return ValueTask.FromResult(
            Right<EncinaError, Option<PotentialBreach>>(Some(breach)));
    }
}
