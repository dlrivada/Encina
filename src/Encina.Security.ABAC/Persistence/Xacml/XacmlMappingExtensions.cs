using System.Globalization;

namespace Encina.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Extension methods that map Encina ABAC enums to their standard XACML 3.0 URN representations
/// and convert CLR values to/from XACML text format.
/// </summary>
/// <remarks>
/// <para>
/// XACML URN mappings follow the OASIS XACML 3.0 specification (Appendix B). Attribute category
/// URNs vary between XACML 1.0 and 3.0 namespaces depending on the category. Combining algorithm
/// URNs include a version-specific prefix and a type segment (<c>rule-combining-algorithm</c> vs
/// <c>policy-combining-algorithm</c>) that distinguishes the two usage contexts.
/// </para>
/// <para>
/// Value formatting and parsing use <see cref="CultureInfo.InvariantCulture"/> for all numeric
/// conversions and ISO 8601 round-trip format (<c>"O"</c>) for date/time values to ensure
/// deterministic, culture-independent XML output.
/// </para>
/// </remarks>
internal static class XacmlMappingExtensions
{
    // ── AttributeCategory URNs ─────────────────────────────────────

    /// <summary>
    /// XACML 1.0 subject category URN: <c>urn:oasis:names:tc:xacml:1.0:subject-category:access-subject</c>.
    /// </summary>
    private const string SubjectCategoryUrn =
        "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject";

    /// <summary>
    /// XACML 3.0 resource category URN: <c>urn:oasis:names:tc:xacml:3.0:attribute-category:resource</c>.
    /// </summary>
    private const string ResourceCategoryUrn =
        "urn:oasis:names:tc:xacml:3.0:attribute-category:resource";

    /// <summary>
    /// XACML 3.0 action category URN: <c>urn:oasis:names:tc:xacml:3.0:attribute-category:action</c>.
    /// </summary>
    private const string ActionCategoryUrn =
        "urn:oasis:names:tc:xacml:3.0:attribute-category:action";

    /// <summary>
    /// XACML 3.0 environment category URN: <c>urn:oasis:names:tc:xacml:3.0:attribute-category:environment</c>.
    /// </summary>
    private const string EnvironmentCategoryUrn =
        "urn:oasis:names:tc:xacml:3.0:attribute-category:environment";

    // ── Combining Algorithm URN Segments ────────────────────────────

    /// <summary>URN prefix for rule-combining algorithms.</summary>
    private const string RuleCombiningType = "rule-combining-algorithm";

    /// <summary>URN prefix for policy-combining algorithms.</summary>
    private const string PolicyCombiningType = "policy-combining-algorithm";

    // ── AttributeCategory Mapping ──────────────────────────────────

    /// <summary>
    /// Converts an <see cref="AttributeCategory"/> enum value to its full XACML URN.
    /// </summary>
    /// <param name="category">The attribute category to convert.</param>
    /// <returns>The XACML URN string for the given category.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="category"/> is not a defined enum value.</exception>
    internal static string ToXacmlUrn(this AttributeCategory category) => category switch
    {
        AttributeCategory.Subject => SubjectCategoryUrn,
        AttributeCategory.Resource => ResourceCategoryUrn,
        AttributeCategory.Action => ActionCategoryUrn,
        AttributeCategory.Environment => EnvironmentCategoryUrn,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, $"Unknown AttributeCategory: {category}")
    };

    /// <summary>
    /// Converts a full XACML category URN string back to an <see cref="AttributeCategory"/> enum value.
    /// </summary>
    /// <param name="urn">The XACML URN string.</param>
    /// <returns>The corresponding <see cref="AttributeCategory"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="urn"/> does not match any known category URN.</exception>
    internal static AttributeCategory ToAttributeCategory(string urn) => urn switch
    {
        SubjectCategoryUrn => AttributeCategory.Subject,
        ResourceCategoryUrn => AttributeCategory.Resource,
        ActionCategoryUrn => AttributeCategory.Action,
        EnvironmentCategoryUrn => AttributeCategory.Environment,
        _ => throw new ArgumentException($"Unknown XACML attribute category URN: {urn}", nameof(urn))
    };

    // ── CombiningAlgorithmId Mapping ───────────────────────────────

    /// <summary>
    /// Converts a <see cref="CombiningAlgorithmId"/> enum value to its full XACML URN.
    /// </summary>
    /// <param name="algorithm">The combining algorithm to convert.</param>
    /// <param name="isRuleCombining">
    /// <c>true</c> to produce a <c>rule-combining-algorithm</c> URN (for use on <c>Policy</c> elements);
    /// <c>false</c> to produce a <c>policy-combining-algorithm</c> URN (for use on <c>PolicySet</c> elements).
    /// </param>
    /// <returns>The full XACML URN for the combining algorithm.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="algorithm"/> is not a defined enum value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// XACML 3.0 combining algorithm URNs follow the pattern:
    /// <c>urn:oasis:names:tc:xacml:{version}:{type}:{id}</c>
    /// where <c>{version}</c> is <c>1.0</c> or <c>3.0</c> depending on the algorithm,
    /// and <c>{type}</c> is either <c>rule-combining-algorithm</c> or <c>policy-combining-algorithm</c>.
    /// </para>
    /// </remarks>
    internal static string ToXacmlUrn(this CombiningAlgorithmId algorithm, bool isRuleCombining)
    {
        var type = isRuleCombining ? RuleCombiningType : PolicyCombiningType;

        return algorithm switch
        {
            // XACML 3.0 algorithms
            CombiningAlgorithmId.DenyOverrides =>
                $"urn:oasis:names:tc:xacml:3.0:{type}:deny-overrides",
            CombiningAlgorithmId.PermitOverrides =>
                $"urn:oasis:names:tc:xacml:3.0:{type}:permit-overrides",
            CombiningAlgorithmId.DenyUnlessPermit =>
                $"urn:oasis:names:tc:xacml:3.0:{type}:deny-unless-permit",
            CombiningAlgorithmId.PermitUnlessDeny =>
                $"urn:oasis:names:tc:xacml:3.0:{type}:permit-unless-deny",
            CombiningAlgorithmId.OrderedDenyOverrides =>
                $"urn:oasis:names:tc:xacml:3.0:{type}:ordered-deny-overrides",
            CombiningAlgorithmId.OrderedPermitOverrides =>
                $"urn:oasis:names:tc:xacml:3.0:{type}:ordered-permit-overrides",

            // XACML 1.0 algorithms
            CombiningAlgorithmId.FirstApplicable =>
                $"urn:oasis:names:tc:xacml:1.0:{type}:first-applicable",
            CombiningAlgorithmId.OnlyOneApplicable =>
                $"urn:oasis:names:tc:xacml:1.0:{type}:only-one-applicable",

            _ => throw new ArgumentOutOfRangeException(
                nameof(algorithm), algorithm, $"Unknown CombiningAlgorithmId: {algorithm}")
        };
    }

    /// <summary>
    /// Converts a full XACML combining algorithm URN back to a <see cref="CombiningAlgorithmId"/> enum value.
    /// </summary>
    /// <param name="urn">The full XACML URN string.</param>
    /// <returns>The corresponding <see cref="CombiningAlgorithmId"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="urn"/> does not match any known combining algorithm URN.</exception>
    /// <remarks>
    /// This method accepts both <c>rule-combining-algorithm</c> and <c>policy-combining-algorithm</c>
    /// variants of the URN — the type segment is ignored, and only the algorithm suffix is used for matching.
    /// </remarks>
    internal static CombiningAlgorithmId ToCombiningAlgorithmId(string urn)
    {
        // Extract the algorithm suffix after the last ':'
        // e.g., "urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides" → "deny-overrides"
        var lastColon = urn.LastIndexOf(':');
        if (lastColon < 0 || lastColon == urn.Length - 1)
        {
            throw new ArgumentException($"Invalid combining algorithm URN format: {urn}", nameof(urn));
        }

        var suffix = urn[(lastColon + 1)..];

        return suffix switch
        {
            "deny-overrides" => CombiningAlgorithmId.DenyOverrides,
            "permit-overrides" => CombiningAlgorithmId.PermitOverrides,
            "first-applicable" => CombiningAlgorithmId.FirstApplicable,
            "only-one-applicable" => CombiningAlgorithmId.OnlyOneApplicable,
            "deny-unless-permit" => CombiningAlgorithmId.DenyUnlessPermit,
            "permit-unless-deny" => CombiningAlgorithmId.PermitUnlessDeny,
            "ordered-deny-overrides" => CombiningAlgorithmId.OrderedDenyOverrides,
            "ordered-permit-overrides" => CombiningAlgorithmId.OrderedPermitOverrides,
            _ => throw new ArgumentException(
                $"Unknown XACML combining algorithm URN: {urn}", nameof(urn))
        };
    }

    // ── Effect Mapping ─────────────────────────────────────────────

    /// <summary>
    /// Converts an <see cref="Effect"/> enum value to its XACML string representation.
    /// </summary>
    /// <param name="effect">The effect value to convert.</param>
    /// <returns>The XACML string (<c>"Permit"</c> or <c>"Deny"</c>).</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="effect"/> is not <see cref="Effect.Permit"/> or <see cref="Effect.Deny"/>.
    /// Only <c>Permit</c> and <c>Deny</c> are valid in XACML Rule <c>Effect</c> attributes;
    /// <c>NotApplicable</c> and <c>Indeterminate</c> are evaluation results, not rule effects.
    /// </exception>
    internal static string ToXacmlString(this Effect effect) => effect switch
    {
        Effect.Permit => "Permit",
        Effect.Deny => "Deny",
        _ => throw new ArgumentOutOfRangeException(
            nameof(effect), effect,
            $"Only Permit and Deny are valid XACML Rule effects. Got: {effect}")
    };

    /// <summary>
    /// Converts an XACML effect string to an <see cref="Effect"/> enum value.
    /// </summary>
    /// <param name="value">The XACML string (<c>"Permit"</c> or <c>"Deny"</c>).</param>
    /// <returns>The corresponding <see cref="Effect"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a recognized effect string.</exception>
    internal static Effect ToEffect(string value) => value switch
    {
        "Permit" => Effect.Permit,
        "Deny" => Effect.Deny,
        _ => throw new ArgumentException($"Unknown XACML Effect value: {value}", nameof(value))
    };

    // ── FulfillOn Mapping ──────────────────────────────────────────

    /// <summary>
    /// Converts a <see cref="FulfillOn"/> enum value to its XACML string representation.
    /// </summary>
    /// <param name="fulfillOn">The fulfillOn value to convert.</param>
    /// <returns>The XACML string (<c>"Permit"</c> or <c>"Deny"</c>).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="fulfillOn"/> is not a defined enum value.</exception>
    internal static string ToXacmlString(this FulfillOn fulfillOn) => fulfillOn switch
    {
        FulfillOn.Permit => "Permit",
        FulfillOn.Deny => "Deny",
        _ => throw new ArgumentOutOfRangeException(
            nameof(fulfillOn), fulfillOn, $"Unknown FulfillOn value: {fulfillOn}")
    };

    /// <summary>
    /// Converts an XACML FulfillOn/AppliesTo string to a <see cref="FulfillOn"/> enum value.
    /// </summary>
    /// <param name="value">The XACML string (<c>"Permit"</c> or <c>"Deny"</c>).</param>
    /// <returns>The corresponding <see cref="FulfillOn"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a recognized value.</exception>
    internal static FulfillOn ToFulfillOn(string value) => value switch
    {
        "Permit" => FulfillOn.Permit,
        "Deny" => FulfillOn.Deny,
        _ => throw new ArgumentException($"Unknown XACML FulfillOn value: {value}", nameof(value))
    };

    // ── Value Formatting & Parsing ─────────────────────────────────

    /// <summary>
    /// Formats a CLR value as XACML text content for an <c>AttributeValue</c> element.
    /// </summary>
    /// <param name="value">The CLR value to format. May be <c>null</c>.</param>
    /// <param name="dataType">
    /// The XACML data type URI (from <see cref="XACMLDataTypes"/>) that determines the formatting rules.
    /// </param>
    /// <returns>
    /// The formatted string representation suitable for XACML XML text content,
    /// or an empty string if <paramref name="value"/> is <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>Formatting rules by data type:</para>
    /// <list type="bullet">
    /// <item><description><see cref="XACMLDataTypes.String"/> — <c>ToString()</c></description></item>
    /// <item><description><see cref="XACMLDataTypes.Boolean"/> — XSD boolean: <c>"true"</c>/<c>"false"</c> (lowercase)</description></item>
    /// <item><description><see cref="XACMLDataTypes.Integer"/> — Invariant culture numeric string</description></item>
    /// <item><description><see cref="XACMLDataTypes.Double"/> — Invariant culture numeric string (round-trip <c>"R"</c> format)</description></item>
    /// <item><description><see cref="XACMLDataTypes.DateTime"/> — ISO 8601 round-trip format (<c>"O"</c>)</description></item>
    /// <item><description><see cref="XACMLDataTypes.Date"/> — ISO 8601 date format (<c>"yyyy-MM-dd"</c>)</description></item>
    /// <item><description><see cref="XACMLDataTypes.Time"/> — ISO 8601 time format (<c>"HH:mm:ss.FFFFFFFK"</c>)</description></item>
    /// <item><description><see cref="XACMLDataTypes.AnyURI"/> — <c>ToString()</c> on the URI</description></item>
    /// <item><description>Others — <c>ToString()</c> with <see cref="CultureInfo.InvariantCulture"/></description></item>
    /// </list>
    /// </remarks>
    internal static string FormatXacmlValue(object? value, string dataType)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return dataType switch
        {
            XACMLDataTypes.Boolean when value is bool b =>
                b ? "true" : "false",

            XACMLDataTypes.Integer when value is IFormattable f =>
                f.ToString(null, CultureInfo.InvariantCulture),

            XACMLDataTypes.Double when value is IFormattable f =>
                f.ToString("R", CultureInfo.InvariantCulture),

            XACMLDataTypes.DateTime when value is DateTime dt =>
                dt.ToString("O", CultureInfo.InvariantCulture),

            XACMLDataTypes.DateTime when value is DateTimeOffset dto =>
                dto.ToString("O", CultureInfo.InvariantCulture),

            XACMLDataTypes.Date when value is DateTime dt =>
                dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),

            XACMLDataTypes.Date when value is DateTimeOffset dto =>
                dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),

            XACMLDataTypes.Time when value is DateTime dt =>
                dt.ToString("HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture),

            XACMLDataTypes.Time when value is DateTimeOffset dto =>
                dto.ToString("HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture),

            XACMLDataTypes.Time when value is TimeOnly t =>
                t.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),

            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    /// <summary>
    /// Parses XACML text content from an <c>AttributeValue</c> element back to a CLR value.
    /// </summary>
    /// <param name="text">The text content of the XACML <c>AttributeValue</c> element. May be <c>null</c> or empty.</param>
    /// <param name="dataType">
    /// The XACML data type URI (from <see cref="XACMLDataTypes"/>) that determines the parsing rules.
    /// </param>
    /// <returns>
    /// The parsed CLR value, or <c>null</c> if <paramref name="text"/> is <c>null</c> or empty.
    /// </returns>
    /// <remarks>
    /// <para>Parsing rules by data type:</para>
    /// <list type="bullet">
    /// <item><description><see cref="XACMLDataTypes.String"/> — returned as-is</description></item>
    /// <item><description><see cref="XACMLDataTypes.Boolean"/> — parsed as XSD boolean (<c>"true"</c>/<c>"1"</c> → <c>true</c>)</description></item>
    /// <item><description><see cref="XACMLDataTypes.Integer"/> — parsed as <see cref="long"/></description></item>
    /// <item><description><see cref="XACMLDataTypes.Double"/> — parsed as <see cref="double"/> with invariant culture</description></item>
    /// <item><description><see cref="XACMLDataTypes.DateTime"/> — parsed as <see cref="DateTimeOffset"/> (ISO 8601)</description></item>
    /// <item><description><see cref="XACMLDataTypes.Date"/> — parsed as <see cref="DateTime"/> (date only)</description></item>
    /// <item><description><see cref="XACMLDataTypes.Time"/> — parsed as <see cref="TimeOnly"/></description></item>
    /// <item><description><see cref="XACMLDataTypes.AnyURI"/> — parsed as <see cref="Uri"/></description></item>
    /// <item><description>Others — returned as <see cref="string"/></description></item>
    /// </list>
    /// </remarks>
    internal static object? ParseXacmlValue(string? text, string dataType)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return dataType switch
        {
            XACMLDataTypes.String => text,

            XACMLDataTypes.Boolean => text is "true" or "1",

            XACMLDataTypes.Integer =>
                long.Parse(text, CultureInfo.InvariantCulture),

            XACMLDataTypes.Double =>
                double.Parse(text, CultureInfo.InvariantCulture),

            XACMLDataTypes.DateTime =>
                DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),

            XACMLDataTypes.Date =>
                DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None),

            XACMLDataTypes.Time =>
                TimeOnly.Parse(text, CultureInfo.InvariantCulture),

            XACMLDataTypes.AnyURI =>
                new Uri(text, UriKind.RelativeOrAbsolute),

            XACMLDataTypes.HexBinary =>
                Convert.FromHexString(text),

            XACMLDataTypes.Base64Binary =>
                Convert.FromBase64String(text),

            // Unknown data types — return raw text for forward compatibility
            _ => text
        };
    }
}
