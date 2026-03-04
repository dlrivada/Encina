using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.BreachNotification.Detection.Rules;

/// <summary>
/// Evaluates <see cref="SecurityEventType.AnomalousQuery"/> events to detect
/// unusual query patterns that may indicate data breach activity.
/// </summary>
/// <remarks>
/// <para>
/// This rule identifies anomalous database or API query patterns: unusual query
/// volumes, queries accessing sensitive data outside normal patterns, or systematic
/// enumeration of personal data records.
/// </para>
/// <para>
/// The threshold from <see cref="BreachNotificationOptions.AnomalousQueryThreshold"/>
/// can be referenced by upstream systems for pre-filtering. This rule evaluates all
/// submitted anomalous query events as potential breach indicators.
/// </para>
/// </remarks>
public sealed class AnomalousQueryPatternRule : IBreachDetectionRule
{
    private readonly BreachNotificationOptions _options;
    private readonly ILogger<AnomalousQueryPatternRule> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnomalousQueryPatternRule"/> class.
    /// </summary>
    /// <param name="options">Configuration options containing the detection threshold.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public AnomalousQueryPatternRule(
        IOptions<BreachNotificationOptions> options,
        ILogger<AnomalousQueryPatternRule> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "AnomalousQueryPattern";

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);

        if (securityEvent.EventType != SecurityEventType.AnomalousQuery)
        {
            return ValueTask.FromResult(
                Right<EncinaError, Option<PotentialBreach>>(None));
        }

        _logger.LogDebug(
            "Evaluating anomalous query event '{EventId}' from source '{Source}'",
            securityEvent.Id, securityEvent.Source);

        var breach = new PotentialBreach
        {
            DetectionRuleName = Name,
            Severity = BreachSeverity.Medium,
            Description = $"Anomalous query pattern detected from source '{securityEvent.Source}': {securityEvent.Description}",
            SecurityEvent = securityEvent,
            DetectedAtUtc = DateTimeOffset.UtcNow,
            RecommendedActions = [
                "Review query logs for the affected data source",
                "Identify the user or service account responsible",
                "Assess whether personal data was accessed or extracted",
                "Consider rate limiting or query restrictions"
            ]
        };

        return ValueTask.FromResult(
            Right<EncinaError, Option<PotentialBreach>>(Some(breach)));
    }
}
