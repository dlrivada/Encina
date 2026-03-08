namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix B — Standard data type identifiers used in
/// <see cref="AttributeDesignator.DataType"/>, <see cref="AttributeValue.DataType"/>,
/// and <see cref="IXACMLFunction.ReturnType"/>.
/// </summary>
/// <remarks>
/// These constants map to the XML Schema data types defined in the XACML 3.0
/// specification. They identify the expected type of attribute values and
/// function return types.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming", "CA1720:Identifier contains type name",
    Justification = "Names match the XACML 3.0 / XML Schema standard data type identifiers.")]
public static class XACMLDataTypes
{
    /// <summary>XML Schema string type: <c>http://www.w3.org/2001/XMLSchema#string</c></summary>
    public const string String = "http://www.w3.org/2001/XMLSchema#string";

    /// <summary>XML Schema boolean type: <c>http://www.w3.org/2001/XMLSchema#boolean</c></summary>
    public const string Boolean = "http://www.w3.org/2001/XMLSchema#boolean";

    /// <summary>XML Schema integer type: <c>http://www.w3.org/2001/XMLSchema#integer</c></summary>
    public const string Integer = "http://www.w3.org/2001/XMLSchema#integer";

    /// <summary>XML Schema double type: <c>http://www.w3.org/2001/XMLSchema#double</c></summary>
    public const string Double = "http://www.w3.org/2001/XMLSchema#double";

    /// <summary>XML Schema date type: <c>http://www.w3.org/2001/XMLSchema#date</c></summary>
    public const string Date = "http://www.w3.org/2001/XMLSchema#date";

    /// <summary>XML Schema dateTime type: <c>http://www.w3.org/2001/XMLSchema#dateTime</c></summary>
    public const string DateTime = "http://www.w3.org/2001/XMLSchema#dateTime";

    /// <summary>XML Schema time type: <c>http://www.w3.org/2001/XMLSchema#time</c></summary>
    public const string Time = "http://www.w3.org/2001/XMLSchema#time";

    /// <summary>XML Schema anyURI type: <c>http://www.w3.org/2001/XMLSchema#anyURI</c></summary>
    public const string AnyURI = "http://www.w3.org/2001/XMLSchema#anyURI";

    /// <summary>XML Schema hexBinary type: <c>http://www.w3.org/2001/XMLSchema#hexBinary</c></summary>
    public const string HexBinary = "http://www.w3.org/2001/XMLSchema#hexBinary";

    /// <summary>XML Schema base64Binary type: <c>http://www.w3.org/2001/XMLSchema#base64Binary</c></summary>
    public const string Base64Binary = "http://www.w3.org/2001/XMLSchema#base64Binary";

    /// <summary>XML Schema dayTimeDuration type: <c>http://www.w3.org/2001/XMLSchema#dayTimeDuration</c></summary>
    public const string DayTimeDuration = "http://www.w3.org/2001/XMLSchema#dayTimeDuration";

    /// <summary>XML Schema yearMonthDuration type: <c>http://www.w3.org/2001/XMLSchema#yearMonthDuration</c></summary>
    public const string YearMonthDuration = "http://www.w3.org/2001/XMLSchema#yearMonthDuration";
}
