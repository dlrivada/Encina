using Encina.Compliance.BreachNotification.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification.Detection.Rules;

/// <summary>
/// Evaluates <see cref="SecurityEventType.DataExfiltration"/> events to detect potential
/// mass data exfiltration breaches.
/// </summary>
/// <remarks>
/// <para>
/// This rule identifies large-volume data access patterns that may indicate data
/// exfiltration: bulk exports, mass data downloads, or unauthorized data transfers.
/// It triggers on any data exfiltration security event, as the event itself has been
/// classified as a potential exfiltration by the upstream security infrastructure.
/// </para>
/// <para>
/// The threshold from <see cref="BreachNotificationOptions.DataExfiltrationThresholdMB"/>
/// can be referenced by upstream systems for pre-filtering. This rule evaluates all
/// submitted data exfiltration events as potential breach indicators.
/// </para>
/// </remarks>
public sealed class MassDataExfiltrationRule : IBreachDetectionRule
{
    private readonly BreachNotificationOptions _options;
    private readonly ILogger<MassDataExfiltrationRule> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassDataExfiltrationRule"/> class.
    /// </summary>
    /// <param name="options">Configuration options containing the detection threshold.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public MassDataExfiltrationRule(
        IOptions<BreachNotificationOptions> options,
        ILogger<MassDataExfiltrationRule> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "MassDataExfiltration";

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);

        if (securityEvent.EventType != SecurityEventType.DataExfiltration)
        {
            return ValueTask.FromResult(
                Right<EncinaError, Option<PotentialBreach>>(None));
        }

        _logger.LogDebug(
            "Evaluating data exfiltration event '{EventId}' from source '{Source}'",
            securityEvent.Id, securityEvent.Source);

        var breach = new PotentialBreach
        {
            DetectionRuleName = Name,
            Severity = BreachSeverity.Critical,
            Description = $"Mass data exfiltration detected from source '{securityEvent.Source}': {securityEvent.Description}",
            SecurityEvent = securityEvent,
            DetectedAtUtc = DateTimeOffset.UtcNow,
            RecommendedActions = [
                "Immediately revoke access for involved accounts",
                "Assess the scope of data accessed or extracted",
                "Preserve forensic evidence for investigation",
                "Determine categories of personal data affected"
            ]
        };

        return ValueTask.FromResult(
            Right<EncinaError, Option<PotentialBreach>>(Some(breach)));
    }
}
