using System.Xml.Linq;

namespace Encina.Security.ABAC.Persistence.Xacml;

/// <summary>
/// XML namespace constants and element/attribute names for XACML 3.0 serialization.
/// </summary>
/// <remarks>
/// <para>
/// Centralizes all <see cref="XNamespace"/> and <see cref="XName"/> constants used by
/// the XACML XML policy serializer to produce and consume standards-compliant
/// XACML 3.0 XML documents.
/// </para>
/// <para>
/// The XACML 3.0 core namespace follows the OASIS Working Draft 17 schema identifier.
/// The Encina extension namespace carries properties not defined by the XACML standard
/// (e.g., <c>IsEnabled</c>, <c>Priority</c>) using <c>xs:anyAttribute</c>-compatible
/// attributes that external XACML tools gracefully ignore.
/// </para>
/// </remarks>
internal static class XacmlNamespaces
{
    // ── Namespace URIs ──────────────────────────────────────────────

    /// <summary>
    /// XACML 3.0 core namespace: <c>urn:oasis:names:tc:xacml:3.0:core:schema:wd-17</c>.
    /// </summary>
    internal static readonly XNamespace XacmlCore =
        XNamespace.Get("urn:oasis:names:tc:xacml:3.0:core:schema:wd-17");

    /// <summary>
    /// Encina extension namespace: <c>urn:encina:xacml:extensions:1.0</c>.
    /// </summary>
    /// <remarks>
    /// Used for Encina-specific attributes (<c>IsEnabled</c>, <c>Priority</c>) that are
    /// not part of the XACML 3.0 standard. External XACML tools ignore this namespace
    /// gracefully per XSD <c>xs:anyAttribute</c> rules.
    /// </remarks>
    internal static readonly XNamespace Encina =
        XNamespace.Get("urn:encina:xacml:extensions:1.0");

    /// <summary>
    /// XML namespace prefix for Encina extensions.
    /// </summary>
    internal const string EncinaPrefix = "encina";

    // ── XACML Element Names ─────────────────────────────────────────

    /// <summary>XACML <c>PolicySet</c> element.</summary>
    internal static readonly XName PolicySetElement = XacmlCore + "PolicySet";

    /// <summary>XACML <c>Policy</c> element.</summary>
    internal static readonly XName PolicyElement = XacmlCore + "Policy";

    /// <summary>XACML <c>Rule</c> element.</summary>
    internal static readonly XName RuleElement = XacmlCore + "Rule";

    /// <summary>XACML <c>Description</c> element.</summary>
    internal static readonly XName DescriptionElement = XacmlCore + "Description";

    /// <summary>XACML <c>Target</c> element.</summary>
    internal static readonly XName TargetElement = XacmlCore + "Target";

    /// <summary>XACML <c>AnyOf</c> element.</summary>
    internal static readonly XName AnyOfElement = XacmlCore + "AnyOf";

    /// <summary>XACML <c>AllOf</c> element.</summary>
    internal static readonly XName AllOfElement = XacmlCore + "AllOf";

    /// <summary>XACML <c>Match</c> element.</summary>
    internal static readonly XName MatchElement = XacmlCore + "Match";

    /// <summary>XACML <c>AttributeValue</c> element.</summary>
    internal static readonly XName AttributeValueElement = XacmlCore + "AttributeValue";

    /// <summary>XACML <c>AttributeDesignator</c> element.</summary>
    internal static readonly XName AttributeDesignatorElement = XacmlCore + "AttributeDesignator";

    /// <summary>XACML <c>Condition</c> element.</summary>
    internal static readonly XName ConditionElement = XacmlCore + "Condition";

    /// <summary>XACML <c>Apply</c> element.</summary>
    internal static readonly XName ApplyElement = XacmlCore + "Apply";

    /// <summary>XACML <c>VariableDefinition</c> element.</summary>
    internal static readonly XName VariableDefinitionElement = XacmlCore + "VariableDefinition";

    /// <summary>XACML <c>VariableReference</c> element.</summary>
    internal static readonly XName VariableReferenceElement = XacmlCore + "VariableReference";

    /// <summary>XACML <c>ObligationExpressions</c> container element.</summary>
    internal static readonly XName ObligationExpressionsElement = XacmlCore + "ObligationExpressions";

    /// <summary>XACML <c>ObligationExpression</c> element.</summary>
    internal static readonly XName ObligationExpressionElement = XacmlCore + "ObligationExpression";

    /// <summary>XACML <c>AdviceExpressions</c> container element.</summary>
    internal static readonly XName AdviceExpressionsElement = XacmlCore + "AdviceExpressions";

    /// <summary>XACML <c>AdviceExpression</c> element.</summary>
    internal static readonly XName AdviceExpressionElement = XacmlCore + "AdviceExpression";

    /// <summary>XACML <c>AttributeAssignmentExpression</c> element.</summary>
    internal static readonly XName AttributeAssignmentExpressionElement = XacmlCore + "AttributeAssignmentExpression";

    // ── XACML Attribute Names (unqualified per XSD) ─────────────────

    /// <summary>XACML <c>PolicySetId</c> attribute on <c>PolicySet</c>.</summary>
    internal const string PolicySetIdAttr = "PolicySetId";

    /// <summary>XACML <c>PolicyCombiningAlgId</c> attribute on <c>PolicySet</c>.</summary>
    internal const string PolicyCombiningAlgIdAttr = "PolicyCombiningAlgId";

    /// <summary>XACML <c>PolicyId</c> attribute on <c>Policy</c>.</summary>
    internal const string PolicyIdAttr = "PolicyId";

    /// <summary>XACML <c>Version</c> attribute on <c>Policy</c> and <c>PolicySet</c>.</summary>
    internal const string VersionAttr = "Version";

    /// <summary>XACML <c>RuleCombiningAlgId</c> attribute on <c>Policy</c>.</summary>
    internal const string RuleCombiningAlgIdAttr = "RuleCombiningAlgId";

    /// <summary>XACML <c>RuleId</c> attribute on <c>Rule</c>.</summary>
    internal const string RuleIdAttr = "RuleId";

    /// <summary>XACML <c>Effect</c> attribute on <c>Rule</c>.</summary>
    internal const string EffectAttr = "Effect";

    /// <summary>XACML <c>MatchId</c> attribute on <c>Match</c>.</summary>
    internal const string MatchIdAttr = "MatchId";

    /// <summary>XACML <c>FunctionId</c> attribute on <c>Apply</c>.</summary>
    internal const string FunctionIdAttr = "FunctionId";

    /// <summary>XACML <c>DataType</c> attribute on <c>AttributeValue</c> and <c>AttributeDesignator</c>.</summary>
    internal const string DataTypeAttr = "DataType";

    /// <summary>XACML <c>Category</c> attribute on <c>AttributeDesignator</c>.</summary>
    internal const string CategoryAttr = "Category";

    /// <summary>XACML <c>AttributeId</c> attribute on <c>AttributeDesignator</c> and <c>AttributeAssignmentExpression</c>.</summary>
    internal const string AttributeIdAttr = "AttributeId";

    /// <summary>XACML <c>MustBePresent</c> attribute on <c>AttributeDesignator</c>.</summary>
    internal const string MustBePresentAttr = "MustBePresent";

    /// <summary>XACML <c>VariableId</c> attribute on <c>VariableDefinition</c> and <c>VariableReference</c>.</summary>
    internal const string VariableIdAttr = "VariableId";

    /// <summary>XACML <c>ObligationId</c> attribute on <c>ObligationExpression</c>.</summary>
    internal const string ObligationIdAttr = "ObligationId";

    /// <summary>XACML <c>FulfillOn</c> attribute on <c>ObligationExpression</c>.</summary>
    internal const string FulfillOnAttr = "FulfillOn";

    /// <summary>XACML <c>AdviceId</c> attribute on <c>AdviceExpression</c>.</summary>
    internal const string AdviceIdAttr = "AdviceId";

    /// <summary>XACML <c>AppliesTo</c> attribute on <c>AdviceExpression</c>.</summary>
    internal const string AppliesToAttr = "AppliesTo";

    // ── Encina Extension Attribute Names ────────────────────────────

    /// <summary>Encina <c>IsEnabled</c> extension attribute on <c>PolicySet</c> and <c>Policy</c>.</summary>
    internal static readonly XName IsEnabledAttr = Encina + "IsEnabled";

    /// <summary>Encina <c>Priority</c> extension attribute on <c>PolicySet</c> and <c>Policy</c>.</summary>
    internal static readonly XName PriorityAttr = Encina + "Priority";
}
