using System.Security;
using System.Text;

namespace Encina.Security.Sanitization.Sanitizers;

/// <summary>
/// Provides sanitization for XML contexts by escaping XML special entities
/// and removing invalid XML characters.
/// </summary>
/// <remarks>
/// Replaces the five predefined XML entities (<c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>,
/// <c>&quot;</c>, <c>&apos;</c>) and strips characters that are not valid in
/// XML 1.0 content (control characters U+0000–U+0008, U+000B, U+000C, U+000E–U+001F).
/// </remarks>
internal static class XmlSanitizer
{
    /// <summary>
    /// Sanitizes input for safe embedding in XML content.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>The sanitized string with XML entities escaped and invalid characters removed.</returns>
    internal static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // 1. Remove characters that are invalid in XML 1.0
        var cleaned = RemoveInvalidXmlCharacters(input);

        // 2. Escape XML entities using the BCL SecurityElement.Escape
        return SecurityElement.Escape(cleaned) ?? string.Empty;
    }

    /// <summary>
    /// Removes characters that are not valid in XML 1.0 content.
    /// </summary>
    /// <remarks>
    /// Valid XML 1.0 characters: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
    /// </remarks>
    private static string RemoveInvalidXmlCharacters(string input)
    {
        var sb = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (IsValidXmlChar(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static bool IsValidXmlChar(char c) =>
        c == '\t'           // #x9
        || c == '\n'        // #xA
        || c == '\r'        // #xD
        || (c >= 0x20 && c <= 0xD7FF)
        || (c >= 0xE000 && c <= 0xFFFD);
}
