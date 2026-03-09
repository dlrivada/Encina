using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Encina.Security.ABAC.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

using N = Encina.Security.ABAC.Persistence.Xacml.XacmlNamespaces;

namespace Encina.Security.ABAC.Persistence.Xacml;

/// <summary>
/// XACML 3.0 XML implementation of <see cref="IPolicySerializer"/> that converts between
/// Encina ABAC domain models and standards-compliant XACML 3.0 XML documents.
/// </summary>
/// <remarks>
/// <para>
/// This serializer produces and consumes XML documents conforming to the OASIS XACML 3.0
/// Core specification (Working Draft 17). It supports the full policy graph including
/// recursive <see cref="PolicySet"/> nesting, polymorphic <see cref="IExpression"/> trees,
/// variable definitions, obligations, and advice expressions.
/// </para>
/// <para>
/// Encina-specific properties (<c>IsEnabled</c>, <c>Priority</c>) that are not part of the
/// XACML 3.0 standard are serialized as extension attributes in the
/// <c>urn:encina:xacml:extensions:1.0</c> namespace. External XACML tools gracefully ignore
/// these attributes per XSD <c>xs:anyAttribute</c> rules. When deserializing external XACML
/// documents without Encina extensions, defaults are applied (<c>IsEnabled = true</c>,
/// <c>Priority = 0</c>).
/// </para>
/// </remarks>
public sealed class XacmlXmlPolicySerializer : IPolicySerializer
{
    private readonly ILogger<XacmlXmlPolicySerializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="XacmlXmlPolicySerializer"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostics and warnings during serialization.</param>
    public XacmlXmlPolicySerializer(ILogger<XacmlXmlPolicySerializer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════
    //  SERIALIZATION — Encina Domain Model → XACML 3.0 XML
    // ════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string Serialize(PolicySet policySet)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        using var activity = ABACDiagnostics.StartXacmlXmlSerialize("PolicySet");
        var sw = Stopwatch.StartNew();

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            SerializePolicySetElement(policySet, isRoot: true));

        var xml = doc.Declaration + Environment.NewLine + doc.Root!.ToString(SaveOptions.None);
        var xmlSizeBytes = (long)Encoding.UTF8.GetByteCount(xml);

        sw.Stop();

        ABACDiagnostics.XacmlXmlSerializeTotal.Add(1);
        ABACDiagnostics.XacmlXmlSizeBytes.Record(xmlSizeBytes);
        ABACDiagnostics.RecordPapSuccess(activity);
        ABACLogMessages.XacmlXmlSerializationCompleted(_logger, "PolicySet", policySet.Id, xmlSizeBytes, sw.Elapsed.TotalMilliseconds);

        return xml;
    }

    /// <inheritdoc />
    public string Serialize(Policy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        using var activity = ABACDiagnostics.StartXacmlXmlSerialize("Policy");
        var sw = Stopwatch.StartNew();

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            SerializePolicyElement(policy, isRoot: true));

        var xml = doc.Declaration + Environment.NewLine + doc.Root!.ToString(SaveOptions.None);
        var xmlSizeBytes = (long)Encoding.UTF8.GetByteCount(xml);

        sw.Stop();

        ABACDiagnostics.XacmlXmlSerializeTotal.Add(1);
        ABACDiagnostics.XacmlXmlSizeBytes.Record(xmlSizeBytes);
        ABACDiagnostics.RecordPapSuccess(activity);
        ABACLogMessages.XacmlXmlSerializationCompleted(_logger, "Policy", policy.Id, xmlSizeBytes, sw.Elapsed.TotalMilliseconds);

        return xml;
    }

    // ── PolicySet ──────────────────────────────────────────────────

    private static XElement SerializePolicySetElement(PolicySet policySet, bool isRoot = false)
    {
        var element = new XElement(N.PolicySetElement,
            new XAttribute(N.PolicySetIdAttr, policySet.Id),
            new XAttribute(N.VersionAttr, policySet.Version ?? "1.0"),
            new XAttribute(N.PolicyCombiningAlgIdAttr,
                policySet.Algorithm.ToXacmlUrn(isRuleCombining: false)));

        if (isRoot)
        {
            element.Add(new XAttribute(XNamespace.Xmlns + N.EncinaPrefix, N.Encina));
        }

        // Encina extension attributes
        element.Add(new XAttribute(N.IsEnabledAttr, policySet.IsEnabled));
        element.Add(new XAttribute(N.PriorityAttr, policySet.Priority));

        if (policySet.Description is not null)
        {
            element.Add(new XElement(N.DescriptionElement, policySet.Description));
        }

        if (policySet.Target is not null)
        {
            element.Add(SerializeTargetElement(policySet.Target));
        }

        // Nested PolicySets
        foreach (var nestedPolicySet in policySet.PolicySets)
        {
            element.Add(SerializePolicySetElement(nestedPolicySet));
        }

        // Child Policies
        foreach (var policy in policySet.Policies)
        {
            element.Add(SerializePolicyElement(policy));
        }

        // Obligations
        if (policySet.Obligations.Count > 0)
        {
            element.Add(SerializeObligationExpressionsElement(policySet.Obligations));
        }

        // Advice
        if (policySet.Advice.Count > 0)
        {
            element.Add(SerializeAdviceExpressionsElement(policySet.Advice));
        }

        return element;
    }

    // ── Policy ─────────────────────────────────────────────────────

    private static XElement SerializePolicyElement(Policy policy, bool isRoot = false)
    {
        var element = new XElement(N.PolicyElement,
            new XAttribute(N.PolicyIdAttr, policy.Id),
            new XAttribute(N.VersionAttr, policy.Version ?? "1.0"),
            new XAttribute(N.RuleCombiningAlgIdAttr,
                policy.Algorithm.ToXacmlUrn(isRuleCombining: true)));

        if (isRoot)
        {
            element.Add(new XAttribute(XNamespace.Xmlns + N.EncinaPrefix, N.Encina));
        }

        // Encina extension attributes
        element.Add(new XAttribute(N.IsEnabledAttr, policy.IsEnabled));
        element.Add(new XAttribute(N.PriorityAttr, policy.Priority));

        if (policy.Description is not null)
        {
            element.Add(new XElement(N.DescriptionElement, policy.Description));
        }

        if (policy.Target is not null)
        {
            element.Add(SerializeTargetElement(policy.Target));
        }

        // Variable definitions (before rules, per XACML schema ordering)
        foreach (var varDef in policy.VariableDefinitions)
        {
            element.Add(SerializeVariableDefinitionElement(varDef));
        }

        // Rules
        foreach (var rule in policy.Rules)
        {
            element.Add(SerializeRuleElement(rule));
        }

        // Obligations
        if (policy.Obligations.Count > 0)
        {
            element.Add(SerializeObligationExpressionsElement(policy.Obligations));
        }

        // Advice
        if (policy.Advice.Count > 0)
        {
            element.Add(SerializeAdviceExpressionsElement(policy.Advice));
        }

        return element;
    }

    // ── Rule ───────────────────────────────────────────────────────

    private static XElement SerializeRuleElement(Rule rule)
    {
        var element = new XElement(N.RuleElement,
            new XAttribute(N.RuleIdAttr, rule.Id),
            new XAttribute(N.EffectAttr, rule.Effect.ToXacmlString()));

        if (rule.Description is not null)
        {
            element.Add(new XElement(N.DescriptionElement, rule.Description));
        }

        if (rule.Target is not null)
        {
            element.Add(SerializeTargetElement(rule.Target));
        }

        if (rule.Condition is not null)
        {
            element.Add(SerializeConditionElement(rule.Condition));
        }

        if (rule.Obligations.Count > 0)
        {
            element.Add(SerializeObligationExpressionsElement(rule.Obligations));
        }

        if (rule.Advice.Count > 0)
        {
            element.Add(SerializeAdviceExpressionsElement(rule.Advice));
        }

        return element;
    }

    // ── Target / AnyOf / AllOf / Match ─────────────────────────────

    private static XElement SerializeTargetElement(Target target)
    {
        var element = new XElement(N.TargetElement);

        foreach (var anyOf in target.AnyOfElements)
        {
            element.Add(SerializeAnyOfElement(anyOf));
        }

        return element;
    }

    private static XElement SerializeAnyOfElement(AnyOf anyOf)
    {
        var element = new XElement(N.AnyOfElement);

        foreach (var allOf in anyOf.AllOfElements)
        {
            element.Add(SerializeAllOfElement(allOf));
        }

        return element;
    }

    private static XElement SerializeAllOfElement(AllOf allOf)
    {
        var element = new XElement(N.AllOfElement);

        foreach (var match in allOf.Matches)
        {
            element.Add(SerializeMatchElement(match));
        }

        return element;
    }

    private static XElement SerializeMatchElement(Match match)
    {
        return new XElement(N.MatchElement,
            new XAttribute(N.MatchIdAttr, XacmlFunctionRegistry.ToUrn(match.FunctionId)),
            SerializeAttributeValueElement(match.AttributeValue),
            SerializeAttributeDesignatorElement(match.AttributeDesignator));
    }

    // ── Condition ──────────────────────────────────────────────────

    private static XElement SerializeConditionElement(Apply condition)
    {
        return new XElement(N.ConditionElement,
            SerializeApplyElement(condition));
    }

    // ── Expressions ────────────────────────────────────────────────

    private static XElement SerializeExpressionElement(IExpression expression)
    {
        return expression switch
        {
            Apply apply => SerializeApplyElement(apply),
            AttributeDesignator designator => SerializeAttributeDesignatorElement(designator),
            AttributeValue value => SerializeAttributeValueElement(value),
            VariableReference varRef => SerializeVariableReferenceElement(varRef),
            _ => throw new InvalidOperationException(
                $"Unknown IExpression type: {expression.GetType().FullName}")
        };
    }

    private static XElement SerializeApplyElement(Apply apply)
    {
        var element = new XElement(N.ApplyElement,
            new XAttribute(N.FunctionIdAttr, XacmlFunctionRegistry.ToUrn(apply.FunctionId)));

        foreach (var arg in apply.Arguments)
        {
            element.Add(SerializeExpressionElement(arg));
        }

        return element;
    }

    private static XElement SerializeAttributeDesignatorElement(AttributeDesignator designator)
    {
        return new XElement(N.AttributeDesignatorElement,
            new XAttribute(N.CategoryAttr, designator.Category.ToXacmlUrn()),
            new XAttribute(N.AttributeIdAttr, designator.AttributeId),
            new XAttribute(N.DataTypeAttr, designator.DataType),
            new XAttribute(N.MustBePresentAttr, designator.MustBePresent));
    }

    private static XElement SerializeAttributeValueElement(AttributeValue value)
    {
        var dataType = !string.IsNullOrEmpty(value.DataType)
            ? value.DataType
            : XacmlDataTypeMap.InferDataType(value.Value);

        var element = new XElement(N.AttributeValueElement,
            new XAttribute(N.DataTypeAttr, dataType));

        var text = XacmlMappingExtensions.FormatXacmlValue(value.Value, dataType);
        if (!string.IsNullOrEmpty(text))
        {
            element.Value = text;
        }

        return element;
    }

    private static XElement SerializeVariableReferenceElement(VariableReference varRef)
    {
        return new XElement(N.VariableReferenceElement,
            new XAttribute(N.VariableIdAttr, varRef.VariableId));
    }

    private static XElement SerializeVariableDefinitionElement(VariableDefinition varDef)
    {
        var element = new XElement(N.VariableDefinitionElement,
            new XAttribute(N.VariableIdAttr, varDef.VariableId));

        element.Add(SerializeExpressionElement(varDef.Expression));

        return element;
    }

    // ── Obligations ────────────────────────────────────────────────

    private static XElement SerializeObligationExpressionsElement(IReadOnlyList<Obligation> obligations)
    {
        var container = new XElement(N.ObligationExpressionsElement);

        foreach (var obligation in obligations)
        {
            var obElement = new XElement(N.ObligationExpressionElement,
                new XAttribute(N.ObligationIdAttr, obligation.Id),
                new XAttribute(N.FulfillOnAttr, obligation.FulfillOn.ToXacmlString()));

            foreach (var assignment in obligation.AttributeAssignments)
            {
                obElement.Add(SerializeAttributeAssignmentElement(assignment));
            }

            container.Add(obElement);
        }

        return container;
    }

    // ── Advice ─────────────────────────────────────────────────────

    private static XElement SerializeAdviceExpressionsElement(IReadOnlyList<AdviceExpression> adviceList)
    {
        var container = new XElement(N.AdviceExpressionsElement);

        foreach (var advice in adviceList)
        {
            var advElement = new XElement(N.AdviceExpressionElement,
                new XAttribute(N.AdviceIdAttr, advice.Id),
                new XAttribute(N.AppliesToAttr, advice.AppliesTo.ToXacmlString()));

            foreach (var assignment in advice.AttributeAssignments)
            {
                advElement.Add(SerializeAttributeAssignmentElement(assignment));
            }

            container.Add(advElement);
        }

        return container;
    }

    // ── AttributeAssignment ────────────────────────────────────────

    private static XElement SerializeAttributeAssignmentElement(AttributeAssignment assignment)
    {
        var element = new XElement(N.AttributeAssignmentExpressionElement,
            new XAttribute(N.AttributeIdAttr, assignment.AttributeId));

        if (assignment.Category is not null)
        {
            element.Add(new XAttribute(N.CategoryAttr, assignment.Category.Value.ToXacmlUrn()));
        }

        element.Add(SerializeExpressionElement(assignment.Value));

        return element;
    }

    // ════════════════════════════════════════════════════════════════
    //  DESERIALIZATION — XACML 3.0 XML → Encina Domain Model
    // ════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public Either<EncinaError, PolicySet> DeserializePolicySet(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
            ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "PolicySet", "Input data is null or empty.");
            return Left(ABACErrors.DeserializationFailed("PolicySet", "Input data is null or empty."));
        }

        using var activity = ABACDiagnostics.StartXacmlXmlDeserialize("PolicySet");
        var sw = Stopwatch.StartNew();

        try
        {
            var doc = XDocument.Parse(data);
            var root = doc.Root;
            if (root is null || root.Name != N.PolicySetElement)
            {
                var errorMsg =
                    $"Expected root element '{N.PolicySetElement.LocalName}' in XACML namespace, " +
                    $"but found '{root?.Name.LocalName ?? "(empty)"}'. Ensure the XML is a valid XACML 3.0 PolicySet document.";
                ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
                ABACDiagnostics.RecordPapFailure(activity, errorMsg);
                ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "PolicySet", errorMsg);
                return Left(ABACErrors.DeserializationFailed("PolicySet", errorMsg));
            }

            var policySet = ParsePolicySetElement(root);

            sw.Stop();

            ABACDiagnostics.XacmlXmlDeserializeTotal.Add(1);
            ABACDiagnostics.RecordPapSuccess(activity);
            ABACLogMessages.XacmlXmlDeserializationCompleted(_logger, "PolicySet", policySet.Id, sw.Elapsed.TotalMilliseconds);

            return Right(policySet);
        }
        catch (XmlException ex)
        {
            ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
            ABACDiagnostics.RecordPapFailure(activity, ex.Message);
            ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "PolicySet", $"XML parse error: {ex.Message}");
            return Left(ABACErrors.DeserializationFailed("PolicySet", $"XML parse error: {ex.Message}"));
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
            ABACDiagnostics.RecordPapFailure(activity, ex.Message);
            ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "PolicySet", ex.Message);
            return Left(ABACErrors.DeserializationFailed("PolicySet", ex.Message));
        }
    }

    /// <inheritdoc />
    public Either<EncinaError, Policy> DeserializePolicy(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
            ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "Policy", "Input data is null or empty.");
            return Left(ABACErrors.DeserializationFailed("Policy", "Input data is null or empty."));
        }

        using var activity = ABACDiagnostics.StartXacmlXmlDeserialize("Policy");
        var sw = Stopwatch.StartNew();

        try
        {
            var doc = XDocument.Parse(data);
            var root = doc.Root;
            if (root is null || root.Name != N.PolicyElement)
            {
                var errorMsg =
                    $"Expected root element '{N.PolicyElement.LocalName}' in XACML namespace, " +
                    $"but found '{root?.Name.LocalName ?? "(empty)"}'. Ensure the XML is a valid XACML 3.0 Policy document.";
                ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
                ABACDiagnostics.RecordPapFailure(activity, errorMsg);
                ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "Policy", errorMsg);
                return Left(ABACErrors.DeserializationFailed("Policy", errorMsg));
            }

            var policy = ParsePolicyElement(root);

            sw.Stop();

            ABACDiagnostics.XacmlXmlDeserializeTotal.Add(1);
            ABACDiagnostics.RecordPapSuccess(activity);
            ABACLogMessages.XacmlXmlDeserializationCompleted(_logger, "Policy", policy.Id, sw.Elapsed.TotalMilliseconds);

            return Right(policy);
        }
        catch (XmlException ex)
        {
            ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
            ABACDiagnostics.RecordPapFailure(activity, ex.Message);
            ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "Policy", $"XML parse error: {ex.Message}");
            return Left(ABACErrors.DeserializationFailed("Policy", $"XML parse error: {ex.Message}"));
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            ABACDiagnostics.XacmlXmlErrorTotal.Add(1);
            ABACDiagnostics.RecordPapFailure(activity, ex.Message);
            ABACLogMessages.XacmlXmlDeserializationFailed(_logger, "Policy", ex.Message);
            return Left(ABACErrors.DeserializationFailed("Policy", ex.Message));
        }
    }

    // ── PolicySet Parsing ──────────────────────────────────────────

    private PolicySet ParsePolicySetElement(XElement element)
    {
        var id = RequireAttribute(element, N.PolicySetIdAttr);
        var version = (string?)element.Attribute(N.VersionAttr);
        var description = (string?)element.Element(N.DescriptionElement);

        var algorithmUrn = RequireAttribute(element, N.PolicyCombiningAlgIdAttr);
        var algorithm = XacmlMappingExtensions.ToCombiningAlgorithmId(algorithmUrn);

        var target = ParseTargetElement(element.Element(N.TargetElement));

        // Encina extensions (defaults: IsEnabled=true, Priority=0)
        var hasEncinaExtensions = element.Attribute(N.IsEnabledAttr) is not null;
        var isEnabled = ParseBoolAttribute(element, N.IsEnabledAttr, defaultValue: true);
        var priority = ParseIntAttribute(element, N.PriorityAttr, defaultValue: 0);

        if (!hasEncinaExtensions)
        {
            ABACLogMessages.XacmlXmlExtensionsMissing(_logger, "PolicySet", id);
        }

        // Nested PolicySets
        var policySets = element.Elements(N.PolicySetElement)
            .Select(ParsePolicySetElement)
            .ToList();

        // Child Policies
        var policies = element.Elements(N.PolicyElement)
            .Select(ParsePolicyElement)
            .ToList();

        // Obligations
        var obligations = ParseObligationExpressions(element.Element(N.ObligationExpressionsElement));

        // Advice
        var advice = ParseAdviceExpressions(element.Element(N.AdviceExpressionsElement));

        return new PolicySet
        {
            Id = id,
            Version = version,
            Description = description,
            Target = target,
            Algorithm = algorithm,
            Policies = policies,
            PolicySets = policySets,
            Obligations = obligations,
            Advice = advice,
            IsEnabled = isEnabled,
            Priority = priority
        };
    }

    // ── Policy Parsing ─────────────────────────────────────────────

    private Policy ParsePolicyElement(XElement element)
    {
        var id = RequireAttribute(element, N.PolicyIdAttr);
        var version = (string?)element.Attribute(N.VersionAttr);
        var description = (string?)element.Element(N.DescriptionElement);

        var algorithmUrn = RequireAttribute(element, N.RuleCombiningAlgIdAttr);
        var algorithm = XacmlMappingExtensions.ToCombiningAlgorithmId(algorithmUrn);

        var target = ParseTargetElement(element.Element(N.TargetElement));

        // Encina extensions (defaults: IsEnabled=true, Priority=0)
        var hasEncinaExtensions = element.Attribute(N.IsEnabledAttr) is not null;
        var isEnabled = ParseBoolAttribute(element, N.IsEnabledAttr, defaultValue: true);
        var priority = ParseIntAttribute(element, N.PriorityAttr, defaultValue: 0);

        if (!hasEncinaExtensions)
        {
            ABACLogMessages.XacmlXmlExtensionsMissing(_logger, "Policy", id);
        }

        // Variable definitions
        var variableDefinitions = element.Elements(N.VariableDefinitionElement)
            .Select(ParseVariableDefinitionElement)
            .ToList();

        // Rules
        var rules = element.Elements(N.RuleElement)
            .Select(ParseRuleElement)
            .ToList();

        // Obligations
        var obligations = ParseObligationExpressions(element.Element(N.ObligationExpressionsElement));

        // Advice
        var advice = ParseAdviceExpressions(element.Element(N.AdviceExpressionsElement));

        return new Policy
        {
            Id = id,
            Version = version,
            Description = description,
            Target = target,
            Algorithm = algorithm,
            Rules = rules,
            VariableDefinitions = variableDefinitions,
            Obligations = obligations,
            Advice = advice,
            IsEnabled = isEnabled,
            Priority = priority
        };
    }

    // ── Rule Parsing ───────────────────────────────────────────────

    private Rule ParseRuleElement(XElement element)
    {
        var id = RequireAttribute(element, N.RuleIdAttr);
        var effectStr = RequireAttribute(element, N.EffectAttr);
        var effect = XacmlMappingExtensions.ToEffect(effectStr);

        var description = (string?)element.Element(N.DescriptionElement);
        var target = ParseTargetElement(element.Element(N.TargetElement));
        var condition = ParseConditionElement(element.Element(N.ConditionElement));

        var obligations = ParseObligationExpressions(element.Element(N.ObligationExpressionsElement));
        var advice = ParseAdviceExpressions(element.Element(N.AdviceExpressionsElement));

        return new Rule
        {
            Id = id,
            Effect = effect,
            Description = description,
            Target = target,
            Condition = condition,
            Obligations = obligations,
            Advice = advice
        };
    }

    // ── Target / AnyOf / AllOf / Match Parsing ─────────────────────

    private static Target? ParseTargetElement(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var anyOfElements = element.Elements(N.AnyOfElement)
            .Select(ParseAnyOfElement)
            .ToList();

        return new Target { AnyOfElements = anyOfElements };
    }

    private static AnyOf ParseAnyOfElement(XElement element)
    {
        var allOfElements = element.Elements(N.AllOfElement)
            .Select(ParseAllOfElement)
            .ToList();

        return new AnyOf { AllOfElements = allOfElements };
    }

    private static AllOf ParseAllOfElement(XElement element)
    {
        var matches = element.Elements(N.MatchElement)
            .Select(ParseMatchElement)
            .ToList();

        return new AllOf { Matches = matches };
    }

    private static Match ParseMatchElement(XElement element)
    {
        var matchIdUrn = RequireAttribute(element, N.MatchIdAttr);
        var functionId = XacmlFunctionRegistry.ToShortId(matchIdUrn);

        var attrValueEl = element.Element(N.AttributeValueElement)
            ?? throw new InvalidOperationException(
                $"Match element is missing required child element '{N.AttributeValueElement.LocalName}'.");

        var attrDesignatorEl = element.Element(N.AttributeDesignatorElement)
            ?? throw new InvalidOperationException(
                $"Match element is missing required child element '{N.AttributeDesignatorElement.LocalName}'.");

        return new Match
        {
            FunctionId = functionId,
            AttributeValue = ParseAttributeValueElement(attrValueEl),
            AttributeDesignator = ParseAttributeDesignatorElement(attrDesignatorEl)
        };
    }

    // ── Condition Parsing ──────────────────────────────────────────

    private Apply? ParseConditionElement(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        // XACML 3.0: <Condition> wraps a single <Apply> element
        var applyEl = element.Element(N.ApplyElement);
        if (applyEl is null)
        {
            ABACLogMessages.XacmlXmlUnknownElement(_logger, "Condition (no Apply child)");
            return null;
        }

        return ParseApplyElement(applyEl);
    }

    // ── Expression Parsing ─────────────────────────────────────────

    private IExpression ParseExpressionElement(XElement element)
    {
        if (element.Name == N.ApplyElement)
        {
            return ParseApplyElement(element);
        }

        if (element.Name == N.AttributeDesignatorElement)
        {
            return ParseAttributeDesignatorElement(element);
        }

        if (element.Name == N.AttributeValueElement)
        {
            return ParseAttributeValueElement(element);
        }

        if (element.Name == N.VariableReferenceElement)
        {
            return ParseVariableReferenceElement(element);
        }

        ABACLogMessages.XacmlXmlUnknownElement(_logger, element.Name.LocalName);

        return new AttributeValue
        {
            DataType = XACMLDataTypes.String,
            Value = element.Value
        };
    }

    private Apply ParseApplyElement(XElement element)
    {
        var functionIdUrn = RequireAttribute(element, N.FunctionIdAttr);
        var functionId = XacmlFunctionRegistry.ToShortId(functionIdUrn);

        var arguments = element.Elements()
            .Select(ParseExpressionElement)
            .ToList();

        return new Apply
        {
            FunctionId = functionId,
            Arguments = arguments
        };
    }

    private static AttributeDesignator ParseAttributeDesignatorElement(XElement element)
    {
        var categoryUrn = RequireAttribute(element, N.CategoryAttr);
        var category = XacmlMappingExtensions.ToAttributeCategory(categoryUrn);

        var attributeId = RequireAttribute(element, N.AttributeIdAttr);
        var dataType = RequireAttribute(element, N.DataTypeAttr);

        var mustBePresent = ParseBoolAttribute(element, N.MustBePresentAttr, defaultValue: false);

        return new AttributeDesignator
        {
            Category = category,
            AttributeId = attributeId,
            DataType = dataType,
            MustBePresent = mustBePresent
        };
    }

    private static AttributeValue ParseAttributeValueElement(XElement element)
    {
        var dataType = (string?)element.Attribute(N.DataTypeAttr) ?? XACMLDataTypes.String;
        var text = element.Value;

        var value = XacmlMappingExtensions.ParseXacmlValue(text, dataType);

        return new AttributeValue
        {
            DataType = dataType,
            Value = value
        };
    }

    private static VariableReference ParseVariableReferenceElement(XElement element)
    {
        var variableId = (string?)element.Attribute(N.VariableIdAttr)
            ?? throw new InvalidOperationException(
                $"VariableReference element is missing required attribute '{N.VariableIdAttr}'.");

        return new VariableReference { VariableId = variableId };
    }

    private VariableDefinition ParseVariableDefinitionElement(XElement element)
    {
        var variableId = (string?)element.Attribute(N.VariableIdAttr)
            ?? throw new InvalidOperationException(
                $"VariableDefinition element is missing required attribute '{N.VariableIdAttr}'.");

        // The VariableDefinition contains exactly one expression child
        var expressionEl = element.Elements().FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"VariableDefinition '{variableId}' has no child expression element.");

        return new VariableDefinition
        {
            VariableId = variableId,
            Expression = ParseExpressionElement(expressionEl)
        };
    }

    // ── Obligation Parsing ─────────────────────────────────────────

    private List<Obligation> ParseObligationExpressions(XElement? container)
    {
        if (container is null)
        {
            return [];
        }

        return container.Elements(N.ObligationExpressionElement)
            .Select(ParseObligationExpression)
            .ToList();
    }

    private Obligation ParseObligationExpression(XElement element)
    {
        var id = RequireAttribute(element, N.ObligationIdAttr);
        var fulfillOnStr = RequireAttribute(element, N.FulfillOnAttr);
        var fulfillOn = XacmlMappingExtensions.ToFulfillOn(fulfillOnStr);

        var assignments = element.Elements(N.AttributeAssignmentExpressionElement)
            .Select(ParseAttributeAssignmentElement)
            .ToList();

        return new Obligation
        {
            Id = id,
            FulfillOn = fulfillOn,
            AttributeAssignments = assignments
        };
    }

    // ── Advice Parsing ─────────────────────────────────────────────

    private List<AdviceExpression> ParseAdviceExpressions(XElement? container)
    {
        if (container is null)
        {
            return [];
        }

        return container.Elements(N.AdviceExpressionElement)
            .Select(ParseAdviceExpression)
            .ToList();
    }

    private AdviceExpression ParseAdviceExpression(XElement element)
    {
        var id = RequireAttribute(element, N.AdviceIdAttr);
        var appliesToStr = RequireAttribute(element, N.AppliesToAttr);
        var appliesTo = XacmlMappingExtensions.ToFulfillOn(appliesToStr);

        var assignments = element.Elements(N.AttributeAssignmentExpressionElement)
            .Select(ParseAttributeAssignmentElement)
            .ToList();

        return new AdviceExpression
        {
            Id = id,
            AppliesTo = appliesTo,
            AttributeAssignments = assignments
        };
    }

    // ── AttributeAssignment Parsing ────────────────────────────────

    private AttributeAssignment ParseAttributeAssignmentElement(XElement element)
    {
        var attributeId = RequireAttribute(element, N.AttributeIdAttr);

        // Category is optional on AttributeAssignmentExpression
        AttributeCategory? category = null;
        var categoryStr = (string?)element.Attribute(N.CategoryAttr);
        if (categoryStr is not null)
        {
            category = XacmlMappingExtensions.ToAttributeCategory(categoryStr);
        }

        // The AttributeAssignmentExpression contains exactly one expression child
        var expressionEl = element.Elements().FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"AttributeAssignmentExpression '{attributeId}' has no child expression element.");

        return new AttributeAssignment
        {
            AttributeId = attributeId,
            Category = category,
            Value = ParseExpressionElement(expressionEl)
        };
    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Reads a required attribute from an element, throwing if missing.
    /// </summary>
    private static string RequireAttribute(XElement element, string attributeName)
    {
        return (string?)element.Attribute(attributeName)
            ?? throw new InvalidOperationException(
                $"Element '{element.Name.LocalName}' is missing required attribute '{attributeName}'.");
    }

    /// <summary>
    /// Parses a boolean attribute with a default value when the attribute is absent.
    /// </summary>
    private static bool ParseBoolAttribute(XElement element, XName attributeName, bool defaultValue)
    {
        var attr = (string?)element.Attribute(attributeName);
        if (attr is null)
        {
            return defaultValue;
        }

        return attr.Equals("true", StringComparison.OrdinalIgnoreCase)
            || attr.Equals("True", StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses an integer attribute with a default value when the attribute is absent.
    /// </summary>
    private static int ParseIntAttribute(XElement element, XName attributeName, int defaultValue)
    {
        var attr = (string?)element.Attribute(attributeName);
        if (attr is null)
        {
            return defaultValue;
        }

        return int.TryParse(attr, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }
}
