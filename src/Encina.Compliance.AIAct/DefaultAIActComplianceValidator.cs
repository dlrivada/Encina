using System.Reflection;

using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Default implementation of <see cref="IAIActComplianceValidator"/> that orchestrates
/// compliance checks across the classifier, human oversight enforcer, and system registry.
/// </summary>
/// <remarks>
/// <para>
/// This is the main entry point invoked by the <c>AIActCompliancePipelineBehavior</c>.
/// The validation flow for each request is:
/// </para>
/// <list type="number">
/// <item>Resolve the system ID from the parameter or request attributes</item>
/// <item>Classify the system's risk level (Art. 6, Annex III)</item>
/// <item>Check for prohibited practices (Art. 5)</item>
/// <item>Verify human oversight requirements (Art. 14)</item>
/// <item>Assess transparency obligations (Art. 13, Art. 50)</item>
/// </list>
/// <para>
/// When no AI system is associated with the request (no attribute and no system ID),
/// the validator returns a minimal-risk compliance result with no violations.
/// </para>
/// </remarks>
public sealed class DefaultAIActComplianceValidator : IAIActComplianceValidator
{
    private readonly IAIActClassifier _classifier;
    private readonly IAISystemRegistry _registry;
    private readonly IHumanOversightEnforcer _oversightEnforcer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="DefaultAIActComplianceValidator"/>.
    /// </summary>
    /// <param name="classifier">The AI Act classifier for risk evaluation.</param>
    /// <param name="registry">The AI system registry for metadata lookup.</param>
    /// <param name="oversightEnforcer">The human oversight enforcer for review checks.</param>
    /// <param name="timeProvider">Time provider for timestamps.</param>
    public DefaultAIActComplianceValidator(
        IAIActClassifier classifier,
        IAISystemRegistry registry,
        IHumanOversightEnforcer oversightEnforcer,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(classifier);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(oversightEnforcer);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _classifier = classifier;
        _registry = registry;
        _oversightEnforcer = oversightEnforcer;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, AIActComplianceResult>> ValidateAsync<TRequest>(
        TRequest request,
        string? systemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolvedSystemId = ResolveSystemId<TRequest>(systemId);

        if (resolvedSystemId is null || !_registry.IsRegistered(resolvedSystemId))
        {
            return new AIActComplianceResult
            {
                SystemId = resolvedSystemId ?? typeof(TRequest).FullName ?? typeof(TRequest).Name,
                RiskLevel = AIRiskLevel.MinimalRisk,
                IsProhibited = false,
                RequiresHumanOversight = false,
                RequiresTransparency = false,
                EvaluatedAtUtc = _timeProvider.GetUtcNow()
            };
        }

        var complianceResult = await _classifier.EvaluateComplianceAsync(resolvedSystemId, cancellationToken);

        return await complianceResult.MatchAsync(
            RightAsync: async compliance =>
            {
                var violations = compliance.Violations.ToList();

                var oversightResult = await _oversightEnforcer.RequiresHumanReviewAsync(request, cancellationToken);
                var requiresOversight = compliance.RequiresHumanOversight ||
                    oversightResult.Match(Right: r => r, Left: _ => false);

                var requestType = typeof(TRequest);
                var transparencyAttr = requestType.GetCustomAttribute<AITransparencyAttribute>();
                var requiresTransparency = compliance.RequiresTransparency || transparencyAttr is not null;

                return Right<EncinaError, AIActComplianceResult>(compliance with
                {
                    RequiresHumanOversight = requiresOversight,
                    RequiresTransparency = requiresTransparency,
                    Violations = violations.AsReadOnly()
                });
            },
            Left: error => error);
    }

    private static string? ResolveSystemId<TRequest>(string? explicitSystemId)
    {
        if (explicitSystemId is not null)
        {
            return explicitSystemId;
        }

        var requestType = typeof(TRequest);

        var highRiskAttr = requestType.GetCustomAttribute<HighRiskAIAttribute>();
        if (highRiskAttr?.SystemId is not null)
        {
            return highRiskAttr.SystemId;
        }

        var oversightAttr = requestType.GetCustomAttribute<RequireHumanOversightAttribute>();
        if (oversightAttr?.SystemId is not null)
        {
            return oversightAttr.SystemId;
        }

        if (highRiskAttr is not null)
        {
            return requestType.FullName ?? requestType.Name;
        }

        return null;
    }
}
