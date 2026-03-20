using System.Collections.Concurrent;
using System.Reflection;

using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Default in-memory implementation of <see cref="IHumanOversightEnforcer"/> that tracks
/// human oversight decisions using a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation determines oversight requirements by checking the
/// <see cref="RequireHumanOversightAttribute"/> on request types. Decisions are stored
/// in memory and are lost on application restart.
/// </para>
/// <para>
/// For production systems requiring persistent decision records across all 13 database
/// providers, see child issue #839 ("AI Act Human Oversight &amp; Decision Records").
/// </para>
/// </remarks>
public sealed class DefaultHumanOversightEnforcer : IHumanOversightEnforcer
{
    private readonly ConcurrentDictionary<Guid, HumanDecisionRecord> _decisions = new();
    private static readonly ConcurrentDictionary<Type, bool> AttributeCache = new();

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> RequiresHumanReviewAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest);
        var requiresReview = AttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttribute<RequireHumanOversightAttribute>() is not null);

        return ValueTask.FromResult(Right<EncinaError, bool>(requiresReview));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordHumanDecisionAsync(
        HumanDecisionRecord decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);

        _decisions[decision.DecisionId] = decision;
        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> HasHumanApprovalAsync(
        Guid decisionId,
        CancellationToken cancellationToken = default)
    {
        var exists = _decisions.ContainsKey(decisionId);
        return ValueTask.FromResult(Right<EncinaError, bool>(exists));
    }
}
