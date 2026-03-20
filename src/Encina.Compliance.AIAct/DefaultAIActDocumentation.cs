using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Default implementation of <see cref="IAIActDocumentation"/> that generates basic
/// technical documentation from <see cref="IAISystemRegistry"/> metadata.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a scaffold <see cref="TechnicalDocumentation"/> record
/// populated with the system's registry data. Optional fields (design specifications,
/// accuracy metrics, etc.) are left as <c>null</c> for the user to fill in.
/// </para>
/// <para>
/// For rich template-based documentation generation, see child issue #840
/// ("AI Act Technical Documentation Generation").
/// </para>
/// </remarks>
public sealed class DefaultAIActDocumentation : IAIActDocumentation
{
    private readonly IAISystemRegistry _registry;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="DefaultAIActDocumentation"/>.
    /// </summary>
    /// <param name="registry">The AI system registry for retrieving system metadata.</param>
    /// <param name="timeProvider">Time provider for timestamps.</param>
    public DefaultAIActDocumentation(IAISystemRegistry registry, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _registry = registry;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TechnicalDocumentation>> GenerateDocumentationAsync(
        string systemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemId);

        var result = await _registry.GetSystemAsync(systemId, cancellationToken);
        return result.Map(reg => new TechnicalDocumentation
        {
            SystemId = reg.SystemId,
            Description = reg.Description ?? $"AI system '{reg.Name}' — category: {reg.Category}, risk level: {reg.RiskLevel}.",
            GeneratedAtUtc = _timeProvider.GetUtcNow()
        });
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateDocumentationAsync(
        string systemId,
        TechnicalDocumentation documentation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemId);
        ArgumentNullException.ThrowIfNull(documentation);

        var result = await _registry.GetSystemAsync(systemId, cancellationToken);
        return result.Map(_ => unit);
    }
}
