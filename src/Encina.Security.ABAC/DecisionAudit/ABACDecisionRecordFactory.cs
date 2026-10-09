using System.Globalization;

using Encina.Modules;
using Encina.Security.ABAC.Enforcement;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Everything <see cref="ABACDecisionRecordFactory"/> needs to describe one enforced decision.
/// </summary>
internal sealed class ABACDecisionInputs
{
    public required ABACDecisionAuditOptions Audit { get; init; }

    public required ABACEnforcementMode EnforcementMode { get; init; }

    public required Type RequestType { get; init; }

    public required object Request { get; init; }

    public required IRequestContext Context { get; init; }

    /// <summary>The caller read once from the request context; <c>null</c> when unauthenticated.</summary>
    public RequestIdentity? Caller { get; init; }

    /// <summary>The attributes collected for the request; <c>null</c> when none were (unauthenticated caller, early failure).</summary>
    public ABACCollectedAttributes? Attributes { get; init; }

    /// <summary>The verdict of the requirement evaluation; <c>null</c> when none was reached.</summary>
    public ABACRequirementVerdict? Requirement { get; init; }

    public required ABACEnforcementVerdict Verdict { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required DateTimeOffset CompletedAtUtc { get; init; }
}

/// <summary>
/// Builds the <see cref="ABACDecisionRecord"/> of one enforced decision: codes, identifiers and
/// attribute names; attribute values only for the allow-list of the options.
/// </summary>
internal static class ABACDecisionRecordFactory
{
    private const string IpAddressKey = "Encina.Audit.IpAddress";
    private const string UserAgentKey = "Encina.Audit.UserAgent";

    public static ABACDecisionRecord Create(ABACDecisionInputs inputs)
    {
        var requirement = inputs.Requirement;
        var decision = requirement?.Decision;

        return new ABACDecisionRecord
        {
            DecisionId = Guid.CreateVersion7(inputs.StartedAtUtc),
            UserId = inputs.Caller?.UserId,
            IdentityKind = inputs.Caller?.Kind ?? IdentityKind.Anonymous,
            TenantId = inputs.Context.TenantId,
            CorrelationId = inputs.Context.CorrelationId,
            ModuleId = inputs.Context.GetModuleName(),
            IpAddress = MetadataText(inputs.Context, IpAddressKey),
            UserAgent = MetadataText(inputs.Context, UserAgentKey),
            RequestType = inputs.RequestType.Name,
            ResourceId = ResolveResourceId(inputs),
            EnforcedOutcome = inputs.Verdict.Enforced,
            ReasonCode = inputs.Verdict.ReasonCode,
            EnforcementMode = inputs.EnforcementMode,
            Effect = decision?.Effect,
            PolicyId = requirement?.DecidingPolicyId,
            RuleId = requirement?.DecidingRuleId,
            EvaluatedPolicies = requirement?.Trace ?? [],
            TraceTruncated = requirement?.TraceTruncated ?? false,
            ObligationIds = decision is null ? [] : decision.Obligations.Select(obligation => obligation.Id).ToList(),
            AdviceIds = decision is null ? [] : decision.Advice.Select(advice => advice.Id).ToList(),
            AttributeNames = AttributeNamesOf(inputs.Attributes),
            RecordedValues = RecordedValuesOf(inputs.Attributes, inputs.Audit.RecordedAttributeValues),
            StartedAtUtc = inputs.StartedAtUtc,
            CompletedAtUtc = inputs.CompletedAtUtc
        };
    }

    private static string? MetadataText(IRequestContext context, string key) =>
        context.Metadata.TryGetValue(key, out var value) ? value as string : null;

    // Only explicit ids: the request's own declaration first, then the configured resource attribute.
    private static string? ResolveResourceId(ABACDecisionInputs inputs)
    {
        if (inputs.Request is IABACResourceIdentity { ResourceId: { Length: > 0 } declared })
        {
            return declared;
        }

        if (inputs.Attributes is not null
            && inputs.Attributes.Resource.TryGetValue(inputs.Audit.ResourceIdAttributeName, out var attributeValue))
        {
            var text = Convert.ToString(attributeValue, CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(text) ? null : text;
        }

        return null;
    }

    // The names the providers supplied, per category. The built-in subject-id and identity-kind are
    // excluded (decision A5): the identity kind has its own field.
    private static Dictionary<AttributeCategory, IReadOnlyList<string>> AttributeNamesOf(
        ABACCollectedAttributes? attributes)
    {
        var names = new Dictionary<AttributeCategory, IReadOnlyList<string>>();

        if (attributes is null)
        {
            return names;
        }

        AddNames(names, AttributeCategory.Subject, attributes.Subject.Keys
            .Where(name => name is not (ABACSubjectAttributes.SubjectId or ABACSubjectAttributes.IdentityKind)));
        AddNames(names, AttributeCategory.Resource, attributes.Resource.Keys);
        AddNames(names, AttributeCategory.Environment, attributes.Environment.Keys);

        return names;
    }

    private static void AddNames(
        Dictionary<AttributeCategory, IReadOnlyList<string>> names,
        AttributeCategory category,
        IEnumerable<string> keys)
    {
        var sorted = keys.Order(StringComparer.Ordinal).ToList();

        if (sorted.Count > 0)
        {
            names[category] = sorted;
        }
    }

    // Values only for the names the application listed; the first category that has the name wins.
    private static Dictionary<string, string> RecordedValuesOf(
        ABACCollectedAttributes? attributes,
        HashSet<string> allowList)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        if (attributes is null || allowList.Count == 0)
        {
            return values;
        }

        foreach (var name in allowList.Order(StringComparer.Ordinal))
        {
            var found = FindValue(attributes, name);

            if (found is not null)
            {
                values[name] = found;
            }
        }

        return values;
    }

    private static string? FindValue(ABACCollectedAttributes attributes, string name)
    {
        foreach (var category in (IReadOnlyDictionary<string, object>[])[attributes.Subject, attributes.Resource, attributes.Environment])
        {
            if (category.TryGetValue(name, out var value))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }
}
