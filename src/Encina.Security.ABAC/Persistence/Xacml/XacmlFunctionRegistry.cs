using System.Collections.Frozen;

namespace Encina.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Bidirectional registry mapping Encina short function identifiers (from <see cref="XACMLFunctionIds"/>)
/// to their full XACML 3.0 URN representations and back.
/// </summary>
/// <remarks>
/// <para>
/// XACML function URNs include a version segment that varies by specification revision:
/// functions from XACML 1.0 use <c>urn:oasis:names:tc:xacml:1.0:function:{id}</c>,
/// while functions introduced in XACML 3.0 use <c>urn:oasis:names:tc:xacml:3.0:function:{id}</c>.
/// </para>
/// <para>
/// This registry covers all functions declared in <see cref="XACMLFunctionIds"/>. Unknown
/// identifiers are passed through without modification (both directions), enabling
/// forward-compatibility with custom or future standard functions.
/// </para>
/// </remarks>
internal static class XacmlFunctionRegistry
{
    /// <summary>
    /// URN prefix for XACML 1.0 functions.
    /// </summary>
    private const string V1Prefix = "urn:oasis:names:tc:xacml:1.0:function:";

    /// <summary>
    /// URN prefix for XACML 3.0 functions.
    /// </summary>
    private const string V3Prefix = "urn:oasis:names:tc:xacml:3.0:function:";

    /// <summary>
    /// Maps Encina short function IDs to full XACML URNs.
    /// </summary>
    internal static readonly FrozenDictionary<string, string> ShortIdToUrn = BuildShortIdToUrn();

    /// <summary>
    /// Maps full XACML URNs to Encina short function IDs.
    /// </summary>
    internal static readonly FrozenDictionary<string, string> UrnToShortId = BuildUrnToShortId();

    /// <summary>
    /// Converts an Encina short function ID to its full XACML URN.
    /// </summary>
    /// <param name="shortId">The short function identifier (e.g., <c>"string-equal"</c>).</param>
    /// <returns>
    /// The corresponding XACML URN (e.g., <c>"urn:oasis:names:tc:xacml:1.0:function:string-equal"</c>),
    /// or the input unchanged if it is already a URN or not found in the registry.
    /// </returns>
    internal static string ToUrn(string shortId)
    {
        if (ShortIdToUrn.TryGetValue(shortId, out var urn))
        {
            return urn;
        }

        // If it already looks like a URN, pass through.
        if (shortId.StartsWith("urn:", StringComparison.Ordinal))
        {
            return shortId;
        }

        // Unknown short ID — pass through as-is.
        return shortId;
    }

    /// <summary>
    /// Converts a full XACML URN to its Encina short function ID.
    /// </summary>
    /// <param name="urn">The full XACML URN (e.g., <c>"urn:oasis:names:tc:xacml:1.0:function:string-equal"</c>).</param>
    /// <returns>
    /// The corresponding short function ID (e.g., <c>"string-equal"</c>),
    /// or the input unchanged if not found in the registry.
    /// </returns>
    internal static string ToShortId(string urn)
    {
        if (UrnToShortId.TryGetValue(urn, out var shortId))
        {
            return shortId;
        }

        // Unknown URN — pass through as-is.
        return urn;
    }

    /// <summary>
    /// Checks whether the given URN is a known XACML function identifier.
    /// </summary>
    /// <param name="urn">The full XACML URN to check.</param>
    /// <returns><c>true</c> if the URN maps to a known Encina function ID; otherwise <c>false</c>.</returns>
    internal static bool IsKnownUrn(string urn) => UrnToShortId.ContainsKey(urn);

    // ── Registry construction ───────────────────────────────────────

    private static FrozenDictionary<string, string> BuildShortIdToUrn()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        // ── XACML 1.0 Functions ─────────────────────────────────────
        // Equality
        AddV1(map, XACMLFunctionIds.StringEqual);
        AddV1(map, XACMLFunctionIds.BooleanEqual);
        AddV1(map, XACMLFunctionIds.IntegerEqual);
        AddV1(map, XACMLFunctionIds.DoubleEqual);
        AddV1(map, XACMLFunctionIds.DateEqual);
        AddV1(map, XACMLFunctionIds.DateTimeEqual);
        AddV1(map, XACMLFunctionIds.TimeEqual);

        // Comparison — integer
        AddV1(map, XACMLFunctionIds.IntegerGreaterThan);
        AddV1(map, XACMLFunctionIds.IntegerLessThan);
        AddV1(map, XACMLFunctionIds.IntegerGreaterThanOrEqual);
        AddV1(map, XACMLFunctionIds.IntegerLessThanOrEqual);

        // Comparison — double
        AddV1(map, XACMLFunctionIds.DoubleGreaterThan);
        AddV1(map, XACMLFunctionIds.DoubleLessThan);
        AddV1(map, XACMLFunctionIds.DoubleGreaterThanOrEqual);
        AddV1(map, XACMLFunctionIds.DoubleLessThanOrEqual);

        // Comparison — string
        AddV1(map, XACMLFunctionIds.StringGreaterThan);
        AddV1(map, XACMLFunctionIds.StringLessThan);
        AddV1(map, XACMLFunctionIds.StringGreaterThanOrEqual);
        AddV1(map, XACMLFunctionIds.StringLessThanOrEqual);

        // Comparison — date
        AddV1(map, XACMLFunctionIds.DateGreaterThan);
        AddV1(map, XACMLFunctionIds.DateLessThan);
        AddV1(map, XACMLFunctionIds.DateGreaterThanOrEqual);
        AddV1(map, XACMLFunctionIds.DateLessThanOrEqual);

        // Comparison — dateTime
        AddV1(map, XACMLFunctionIds.DateTimeGreaterThan);
        AddV1(map, XACMLFunctionIds.DateTimeLessThan);
        AddV1(map, XACMLFunctionIds.DateTimeGreaterThanOrEqual);
        AddV1(map, XACMLFunctionIds.DateTimeLessThanOrEqual);

        // Comparison — time
        AddV1(map, XACMLFunctionIds.TimeGreaterThan);
        AddV1(map, XACMLFunctionIds.TimeLessThan);
        AddV1(map, XACMLFunctionIds.TimeGreaterThanOrEqual);
        AddV1(map, XACMLFunctionIds.TimeLessThanOrEqual);

        // Arithmetic — integer
        AddV1(map, XACMLFunctionIds.IntegerAdd);
        AddV1(map, XACMLFunctionIds.IntegerSubtract);
        AddV1(map, XACMLFunctionIds.IntegerMultiply);
        AddV1(map, XACMLFunctionIds.IntegerDivide);
        AddV1(map, XACMLFunctionIds.IntegerMod);
        AddV1(map, XACMLFunctionIds.IntegerAbs);

        // Arithmetic — double
        AddV1(map, XACMLFunctionIds.DoubleAdd);
        AddV1(map, XACMLFunctionIds.DoubleSubtract);
        AddV1(map, XACMLFunctionIds.DoubleMultiply);
        AddV1(map, XACMLFunctionIds.DoubleDivide);
        AddV1(map, XACMLFunctionIds.DoubleAbs);
        AddV1(map, XACMLFunctionIds.Round);
        AddV1(map, XACMLFunctionIds.Floor);

        // String (v1.0 subset)
        AddV1(map, XACMLFunctionIds.StringConcatenate);
        AddV1(map, XACMLFunctionIds.StringNormalizeSpace);
        AddV1(map, XACMLFunctionIds.StringNormalizeToLowerCase);
        AddV1(map, XACMLFunctionIds.StringLength);

        // Logical
        AddV1(map, XACMLFunctionIds.And);
        AddV1(map, XACMLFunctionIds.Or);
        AddV1(map, XACMLFunctionIds.Not);
        AddV1(map, XACMLFunctionIds.NOf);

        // Bag — string
        AddV1(map, XACMLFunctionIds.StringOneAndOnly);
        AddV1(map, XACMLFunctionIds.StringBagSize);
        AddV1(map, XACMLFunctionIds.StringIsIn);
        AddV1(map, XACMLFunctionIds.StringBag);

        // Bag — boolean
        AddV1(map, XACMLFunctionIds.BooleanOneAndOnly);
        AddV1(map, XACMLFunctionIds.BooleanBagSize);
        AddV1(map, XACMLFunctionIds.BooleanIsIn);
        AddV1(map, XACMLFunctionIds.BooleanBag);

        // Bag — integer
        AddV1(map, XACMLFunctionIds.IntegerOneAndOnly);
        AddV1(map, XACMLFunctionIds.IntegerBagSize);
        AddV1(map, XACMLFunctionIds.IntegerIsIn);
        AddV1(map, XACMLFunctionIds.IntegerBag);

        // Bag — double
        AddV1(map, XACMLFunctionIds.DoubleOneAndOnly);
        AddV1(map, XACMLFunctionIds.DoubleBagSize);
        AddV1(map, XACMLFunctionIds.DoubleIsIn);
        AddV1(map, XACMLFunctionIds.DoubleBag);

        // Bag — date
        AddV1(map, XACMLFunctionIds.DateOneAndOnly);
        AddV1(map, XACMLFunctionIds.DateBagSize);
        AddV1(map, XACMLFunctionIds.DateIsIn);
        AddV1(map, XACMLFunctionIds.DateBag);

        // Bag — dateTime
        AddV1(map, XACMLFunctionIds.DateTimeOneAndOnly);
        AddV1(map, XACMLFunctionIds.DateTimeBagSize);
        AddV1(map, XACMLFunctionIds.DateTimeIsIn);
        AddV1(map, XACMLFunctionIds.DateTimeBag);

        // Bag — time
        AddV1(map, XACMLFunctionIds.TimeOneAndOnly);
        AddV1(map, XACMLFunctionIds.TimeBagSize);
        AddV1(map, XACMLFunctionIds.TimeIsIn);
        AddV1(map, XACMLFunctionIds.TimeBag);

        // Bag — anyURI
        AddV1(map, XACMLFunctionIds.AnyURIOneAndOnly);
        AddV1(map, XACMLFunctionIds.AnyURIBagSize);
        AddV1(map, XACMLFunctionIds.AnyURIIsIn);
        AddV1(map, XACMLFunctionIds.AnyURIBag);

        // Set — string
        AddV1(map, XACMLFunctionIds.StringIntersection);
        AddV1(map, XACMLFunctionIds.StringUnion);
        AddV1(map, XACMLFunctionIds.StringSubset);
        AddV1(map, XACMLFunctionIds.StringAtLeastOneMemberOf);
        AddV1(map, XACMLFunctionIds.StringSetEquals);

        // Set — integer
        AddV1(map, XACMLFunctionIds.IntegerIntersection);
        AddV1(map, XACMLFunctionIds.IntegerUnion);
        AddV1(map, XACMLFunctionIds.IntegerSubset);
        AddV1(map, XACMLFunctionIds.IntegerAtLeastOneMemberOf);
        AddV1(map, XACMLFunctionIds.IntegerSetEquals);

        // Set — double
        AddV1(map, XACMLFunctionIds.DoubleIntersection);
        AddV1(map, XACMLFunctionIds.DoubleUnion);
        AddV1(map, XACMLFunctionIds.DoubleSubset);
        AddV1(map, XACMLFunctionIds.DoubleAtLeastOneMemberOf);
        AddV1(map, XACMLFunctionIds.DoubleSetEquals);

        // Regex
        AddV1(map, XACMLFunctionIds.StringRegexpMatch);

        // ── XACML 3.0 Functions ─────────────────────────────────────
        // String (v3.0 subset — introduced in XACML 3.0)
        AddV3(map, XACMLFunctionIds.StringStartsWith);
        AddV3(map, XACMLFunctionIds.StringEndsWith);
        AddV3(map, XACMLFunctionIds.StringContains);
        AddV3(map, XACMLFunctionIds.StringSubstring);

        // Higher-order (redefined in XACML 3.0)
        AddV3(map, XACMLFunctionIds.AnyOfFunc);
        AddV3(map, XACMLFunctionIds.AllOfFunc);
        AddV3(map, XACMLFunctionIds.AnyOfAny);
        AddV3(map, XACMLFunctionIds.AllOfAny);
        AddV3(map, XACMLFunctionIds.AllOfAll);
        AddV3(map, XACMLFunctionIds.Map);

        // Type conversion (introduced in XACML 3.0)
        AddV3(map, XACMLFunctionIds.StringFromInteger);
        AddV3(map, XACMLFunctionIds.IntegerFromString);
        AddV3(map, XACMLFunctionIds.DoubleFromString);
        AddV3(map, XACMLFunctionIds.BooleanFromString);
        AddV3(map, XACMLFunctionIds.StringFromBoolean);
        AddV3(map, XACMLFunctionIds.StringFromDouble);
        AddV3(map, XACMLFunctionIds.StringFromDateTime);

        return map.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, string> BuildUrnToShortId()
    {
        var reverse = new Dictionary<string, string>(ShortIdToUrn.Count, StringComparer.Ordinal);

        foreach (var (shortId, urn) in ShortIdToUrn)
        {
            reverse[urn] = shortId;
        }

        return reverse.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static void AddV1(Dictionary<string, string> map, string shortId) =>
        map[shortId] = V1Prefix + shortId;

    private static void AddV3(Dictionary<string, string> map, string shortId) =>
        map[shortId] = V3Prefix + shortId;
}
