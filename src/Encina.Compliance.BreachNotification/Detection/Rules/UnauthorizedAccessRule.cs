using Encina.Compliance.BreachNotification.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification.Detection.Rules;

/// <summary>
/// Evaluates <see cref="SecurityEventType.UnauthorizedAccess"/> events against a configurable
/// threshold to detect potential unauthorized access breaches.
/// </summary>
/// <remarks>
/// <para>
/// This rule identifies unauthorized access patterns such as brute-force login attempts,
/// credential stuffing, or unauthorized API access. It triggers when an unauthorized
/// access event is reported, as the event itself represents a security concern that
/// warrants breach evaluation.
/// </para>
/// <para>
/// The threshold from <see cref="BreachNotificationOptions.UnauthorizedAccessThreshold"/>
/// can be used by upstream systems to pre-filter events before submission, but this rule
/// treats each submitted unauthorized access event as a potential breach indicator.
/// </para>
/// </remarks>
public sealed class UnauthorizedAccessRule : IBreachDetectionRule
{
    private readonly BreachNotificationOptions _options;
    private readonly ILogger<UnauthorizedAccessRule> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedAccessRule"/> class.
    /// </summary>
    /// <param name="options">Configuration options containing the detection threshold.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public UnauthorizedAccessRule(
        IOptions<BreachNotificationOptions> options,
        ILogger<UnauthorizedAccessRule> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "UnauthorizedAccess";

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);

        if (securityEvent.EventType != SecurityEventType.UnauthorizedAccess)
        {
            return ValueTask.FromResult(
                Right<EncinaError, Option<PotentialBreach>>(None));
        }

        _logger.LogDebug(
            "Evaluating unauthorized access event '{EventId}' from source '{Source}'",
            securityEvent.Id, securityEvent.Source);

        var breach = new PotentialBreach
        {
            DetectionRuleName = Name,
            Severity = BreachSeverity.High,
            Description = $"Unauthorized access detected from source '{securityEvent.Source}': {securityEvent.Description}",
            SecurityEvent = securityEvent,
            DetectedAtUtc = DateTimeOffset.UtcNow,
            RecommendedActions = [
                "Review access logs for affected accounts",
                "Rotate credentials for compromised accounts",
                "Enable multi-factor authentication if not already enabled",
                "Consider temporary IP-based blocking"
            ]
        };

        return ValueTask.FromResult(
            Right<EncinaError, Option<PotentialBreach>>(Some(breach)));
    }
}
