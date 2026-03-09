using System.Collections.Frozen;
using System.Globalization;

namespace Encina.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Maps CLR types to XACML data type URIs and provides DataType-keyed parser functions
/// for converting XACML text content back to CLR values.
/// </summary>
/// <remarks>
/// <para>
/// This class is used during serialization when an <c>AttributeValue</c> has no explicit
/// <c>DataType</c> set — the CLR type of the value is inspected to infer the correct
/// XACML data type URI. During deserialization, the <c>DataType</c> URI from the XML
/// attribute selects the appropriate parser function.
/// </para>
/// <para>
/// All dictionaries are built as <see cref="FrozenDictionary{TKey,TValue}"/> for optimal
/// read performance since they are immutable after initialization.
/// </para>
/// </remarks>
internal static class XacmlDataTypeMap
{
    /// <summary>
    /// Maps CLR <see cref="Type"/> objects to their corresponding XACML data type URIs.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="InferDataType"/> to determine the XACML data type when the
    /// <c>DataType</c> property is not explicitly set on an <c>AttributeValue</c>.
    /// </remarks>
    private static readonly FrozenDictionary<Type, string> ClrTypeToDataType = BuildClrTypeMap();

    /// <summary>
    /// Maps XACML data type URIs to parser functions that convert XACML text content to CLR values.
    /// </summary>
    /// <remarks>
    /// Each parser function accepts a non-null, non-empty string and returns the corresponding
    /// CLR object. The caller is responsible for null/empty checks before invoking a parser.
    /// </remarks>
    internal static readonly FrozenDictionary<string, Func<string, object?>> Parsers = BuildParsers();

    /// <summary>
    /// Infers the XACML data type URI from a CLR value's runtime type.
    /// </summary>
    /// <param name="value">The CLR value to inspect. May be <c>null</c>.</param>
    /// <returns>
    /// The inferred XACML data type URI (e.g., <c>"http://www.w3.org/2001/XMLSchema#string"</c>),
    /// or <see cref="XACMLDataTypes.String"/> as the default fallback for <c>null</c> values
    /// or unrecognized types.
    /// </returns>
    /// <remarks>
    /// <para>CLR type to XACML data type mapping:</para>
    /// <list type="table">
    /// <listheader><term>CLR Type</term><description>XACML DataType</description></listheader>
    /// <item><term><see cref="string"/></term><description><see cref="XACMLDataTypes.String"/></description></item>
    /// <item><term><see cref="bool"/></term><description><see cref="XACMLDataTypes.Boolean"/></description></item>
    /// <item><term><see cref="int"/>, <see cref="long"/>, <see cref="short"/>, <see cref="byte"/></term><description><see cref="XACMLDataTypes.Integer"/></description></item>
    /// <item><term><see cref="double"/>, <see cref="float"/>, <see cref="decimal"/></term><description><see cref="XACMLDataTypes.Double"/></description></item>
    /// <item><term><see cref="DateTime"/></term><description><see cref="XACMLDataTypes.DateTime"/></description></item>
    /// <item><term><see cref="DateTimeOffset"/></term><description><see cref="XACMLDataTypes.DateTime"/></description></item>
    /// <item><term><see cref="DateOnly"/></term><description><see cref="XACMLDataTypes.Date"/></description></item>
    /// <item><term><see cref="TimeOnly"/></term><description><see cref="XACMLDataTypes.Time"/></description></item>
    /// <item><term><see cref="Uri"/></term><description><see cref="XACMLDataTypes.AnyURI"/></description></item>
    /// <item><term><see cref="byte"/>[]</term><description><see cref="XACMLDataTypes.Base64Binary"/></description></item>
    /// </list>
    /// </remarks>
    internal static string InferDataType(object? value)
    {
        if (value is null)
        {
            return XACMLDataTypes.String;
        }

        var type = value.GetType();

        if (ClrTypeToDataType.TryGetValue(type, out var dataType))
        {
            return dataType;
        }

        // Handle Uri subclasses and byte arrays which may not match exactly
        if (value is Uri)
        {
            return XACMLDataTypes.AnyURI;
        }

        if (value is byte[])
        {
            return XACMLDataTypes.Base64Binary;
        }

        // Default fallback for unknown types
        return XACMLDataTypes.String;
    }

    /// <summary>
    /// Checks whether the given XACML data type URI has a known parser.
    /// </summary>
    /// <param name="dataType">The XACML data type URI to check.</param>
    /// <returns><c>true</c> if a parser exists for the data type; otherwise <c>false</c>.</returns>
    internal static bool IsKnownDataType(string dataType) => Parsers.ContainsKey(dataType);

    // ── Map Construction ───────────────────────────────────────────

    private static FrozenDictionary<Type, string> BuildClrTypeMap()
    {
        var map = new Dictionary<Type, string>
        {
            // String
            [typeof(string)] = XACMLDataTypes.String,

            // Boolean
            [typeof(bool)] = XACMLDataTypes.Boolean,

            // Integer types → XACML integer
            [typeof(int)] = XACMLDataTypes.Integer,
            [typeof(long)] = XACMLDataTypes.Integer,
            [typeof(short)] = XACMLDataTypes.Integer,
            [typeof(byte)] = XACMLDataTypes.Integer,

            // Floating-point types → XACML double
            [typeof(double)] = XACMLDataTypes.Double,
            [typeof(float)] = XACMLDataTypes.Double,
            [typeof(decimal)] = XACMLDataTypes.Double,

            // Date/Time types
            [typeof(DateTime)] = XACMLDataTypes.DateTime,
            [typeof(DateTimeOffset)] = XACMLDataTypes.DateTime,
            [typeof(DateOnly)] = XACMLDataTypes.Date,
            [typeof(TimeOnly)] = XACMLDataTypes.Time,

            // URI
            [typeof(Uri)] = XACMLDataTypes.AnyURI,

            // Binary
            [typeof(byte[])] = XACMLDataTypes.Base64Binary
        };

        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<string, Func<string, object?>> BuildParsers()
    {
        var map = new Dictionary<string, Func<string, object?>>(StringComparer.Ordinal)
        {
            [XACMLDataTypes.String] = static text => text,

            [XACMLDataTypes.Boolean] = static text => text is "true" or "1",

            [XACMLDataTypes.Integer] = static text =>
                long.Parse(text, CultureInfo.InvariantCulture),

            [XACMLDataTypes.Double] = static text =>
                double.Parse(text, CultureInfo.InvariantCulture),

            [XACMLDataTypes.DateTime] = static text =>
                DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),

            [XACMLDataTypes.Date] = static text =>
                DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None),

            [XACMLDataTypes.Time] = static text =>
                TimeOnly.Parse(text, CultureInfo.InvariantCulture),

            [XACMLDataTypes.AnyURI] = static text =>
                new Uri(text, UriKind.RelativeOrAbsolute),

            [XACMLDataTypes.HexBinary] = static text =>
                Convert.FromHexString(text),

            [XACMLDataTypes.Base64Binary] = static text =>
                Convert.FromBase64String(text),

            [XACMLDataTypes.DayTimeDuration] = static text => text,

            [XACMLDataTypes.YearMonthDuration] = static text => text
        };

        return map.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
