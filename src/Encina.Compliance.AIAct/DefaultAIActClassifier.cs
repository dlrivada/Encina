using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Model;

using LanguageExt;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Default implementation of <see cref="IAIActClassifier"/> that classifies AI systems
/// based on their registry metadata.
/// </summary>
/// <remarks>
/// <para>
/// Classification uses the system's <see cref="AISystemCategory"/> and registered
/// <see cref="ProhibitedPractice"/> list to determine the risk level per Article 6.
/// Systems with prohibited practices are always classified as <see cref="AIRiskLevel.Prohibited"/>.
/// </para>
/// </remarks>
public sealed class DefaultAIActClassifier : IAIActClassifier
{
    private readonly IAISystemRegistry _registry;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="DefaultAIActClassifier"/>.
    /// </summary>
    /// <param name="registry">The AI system registry to look up system metadata.</param>
    /// <param name="timeProvider">Time provider for timestamps.</param>
    public DefaultAIActClassifier(IAISystemRegistry registry, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _registry = registry;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, AIRiskLevel>> ClassifySystemAsync(
        string systemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemId);

        var result = await _registry.GetSystemAsync(systemId, cancellationToken);
        return result.Map(reg => reg.ProhibitedPractices.Count > 0
            ? AIRiskLevel.Prohibited
            : reg.RiskLevel);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsProhibitedAsync(
        string systemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemId);

        var result = await _registry.GetSystemAsync(systemId, cancellationToken);
        return result.Map(reg =>
            reg.RiskLevel == AIRiskLevel.Prohibited || reg.ProhibitedPractices.Count > 0);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, AIActComplianceResult>> EvaluateComplianceAsync(
        string systemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemId);

        var result = await _registry.GetSystemAsync(systemId, cancellationToken);
        return result.Map(reg =>
        {
            var isProhibited = reg.RiskLevel == AIRiskLevel.Prohibited || reg.ProhibitedPractices.Count > 0;
            var effectiveRiskLevel = isProhibited ? AIRiskLevel.Prohibited : reg.RiskLevel;
            var requiresOversight = effectiveRiskLevel == AIRiskLevel.HighRisk;
            var requiresTransparency = effectiveRiskLevel is AIRiskLevel.HighRisk or AIRiskLevel.LimitedRisk;

            var violations = new List<string>();

            if (isProhibited)
            {
                foreach (var practice in reg.ProhibitedPractices)
                {
                    violations.Add($"Art. 5: Prohibited practice detected — {practice}");
                }

                if (reg.ProhibitedPractices.Count == 0)
                {
                    violations.Add("Art. 5: System is classified as prohibited.");
                }
            }

            return new AIActComplianceResult
            {
                SystemId = systemId,
                RiskLevel = effectiveRiskLevel,
                IsProhibited = isProhibited,
                RequiresHumanOversight = requiresOversight,
                RequiresTransparency = requiresTransparency,
                Violations = violations.AsReadOnly(),
                EvaluatedAtUtc = _timeProvider.GetUtcNow()
            };
        });
    }
}
