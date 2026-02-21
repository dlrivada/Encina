namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Identifies the sanitization strategy to apply to a property.
/// </summary>
public enum SanitizationType
{
    /// <summary>Sanitize HTML using the default or specified profile.</summary>
    Html,

    /// <summary>Sanitize for safe use in SQL contexts.</summary>
    Sql,

    /// <summary>Sanitize for safe use in shell/command-line contexts.</summary>
    Shell,

    /// <summary>Sanitize using a named custom profile.</summary>
    Custom,

    /// <summary>Strip all HTML, leaving only plain text.</summary>
    StripHtml
}
