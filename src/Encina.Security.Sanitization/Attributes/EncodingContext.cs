namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Identifies the output encoding context for a property.
/// </summary>
public enum EncodingContext
{
    /// <summary>Encode for safe rendering in an HTML body.</summary>
    Html,

    /// <summary>Encode for safe embedding in a JavaScript string.</summary>
    JavaScript,

    /// <summary>Encode for safe use in a URL context.</summary>
    Url
}
