using System.Collections.Concurrent;

using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification.Detection;

/// <summary>
/// Default implementation of <see cref="IBreachDetector"/> that iterates all registered
/// detection rules and aggregates their findings.
/// </summary>
/// <remarks>
/// <para>
/// When a <see cref="SecurityEvent"/> is submitted via <see cref="DetectAsync"/>,
/// the detector evaluates it against ALL registered <see cref="IBreachDetectionRule"/>
/// implementations. A single security event can match multiple rules, resulting in
/// multiple <see cref="PotentialBreach"/> findings.
/// </para>
/// <para>
/// Rules are registered either via dependency injection (constructor parameter) or
/// at runtime via <see cref="RegisterDetectionRule"/>. Runtime registrations are
/// thread-safe via <see cref="ConcurrentBag{T}"/>.
/// </para>
/// <para>
/// If an individual rule fails during evaluation, its error is logged but does not
/// prevent other rules from executing. The method only returns an error if the
/// detection process itself cannot be started.
/// </para>
/// </remarks>
public sealed class DefaultBreachDetector : IBreachDetector
{
    private readonly ConcurrentDictionary<string, IBreachDetectionRule> _rules = new(StringComparer.Ordinal);
    private readonly ILogger<DefaultBreachDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBreachDetector"/> class.
    /// </summary>
    /// <param name="rules">The initial set of detection rules registered via DI.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultBreachDetector(
        IEnumerable<IBreachDetectionRule> rules,
        ILogger<DefaultBreachDetector> logger)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        foreach (var rule in rules)
        {
            _rules[rule.Name] = rule;
        }

        _logger.LogDebug("DefaultBreachDetector initialized with {RuleCount} rules", _rules.Count);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PotentialBreach>>> DetectAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);

        var detectedBreaches = new List<PotentialBreach>();

        foreach (var rule in _rules.Values)
        {
            try
            {
                _logger.LogDebug(
                    "Evaluating rule '{RuleName}' for security event '{EventId}' of type {EventType}",
                    rule.Name, securityEvent.Id, securityEvent.EventType);

                var result = await rule.EvaluateAsync(securityEvent, cancellationToken);

                result.Match(
                    Right: optionalBreach =>
                    {
                        optionalBreach.IfSome(breach =>
                        {
                            detectedBreaches.Add(breach);
                            _logger.LogInformation(
                                "Rule '{RuleName}' detected potential breach for event '{EventId}': {Description}",
                                rule.Name, securityEvent.Id, breach.Description);
                        });
                    },
                    Left: error =>
                    {
                        _logger.LogWarning(
                            "Rule '{RuleName}' evaluation failed for event '{EventId}': {ErrorMessage}",
                            rule.Name, securityEvent.Id, error.Message);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Rule '{RuleName}' threw an exception evaluating event '{EventId}'",
                    rule.Name, securityEvent.Id);
            }
        }

        _logger.LogDebug(
            "Detection complete for event '{EventId}': {BreachCount} potential breaches detected",
            securityEvent.Id, detectedBreaches.Count);

        return Right<EncinaError, IReadOnlyList<PotentialBreach>>(
            detectedBreaches.AsReadOnly());
    }

    /// <inheritdoc />
    public void RegisterDetectionRule(IBreachDetectionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        _rules[rule.Name] = rule;

        _logger.LogInformation("Registered detection rule '{RuleName}'", rule.Name);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<string>>> GetRegisteredRulesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> ruleNames = _rules.Keys.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<string>>>(
            Right<EncinaError, IReadOnlyList<string>>(ruleNames));
    }
}
