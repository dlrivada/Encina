using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Encina.Security.Sanitization.Abstractions;

namespace Encina.Security.Sanitization.Encoders;

/// <summary>
/// Default implementation of <see cref="IOutputEncoder"/> using
/// <c>System.Text.Encodings.Web</c> for HTML, JavaScript, and URL encoding,
/// with manual OWASP-compliant CSS encoding.
/// </summary>
/// <remarks>
/// <para>
/// Output encoding transforms data so it is treated as literal data (not code)
/// in the target output context. This is the last line of defense against XSS
/// when rendering user-controlled data in web responses.
/// </para>
/// <para>
/// Thread-safe. All <c>System.Text.Encodings.Web</c> encoders are thread-safe singletons.
/// </para>
/// </remarks>
public sealed class DefaultOutputEncoder : IOutputEncoder
{
    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="HtmlEncoder.Default"/> to encode HTML-special characters
    /// (<c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&quot;</c>, <c>&#39;</c>).
    /// </remarks>
    public string EncodeForHtml(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return HtmlEncoder.Default.Encode(input);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="HtmlEncoder.Default"/> which applies encoding suitable for
    /// both HTML body and attribute contexts. Always quote attribute values in the
    /// rendered HTML for complete protection.
    /// </remarks>
    public string EncodeForHtmlAttribute(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return HtmlEncoder.Default.Encode(input);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="JavaScriptEncoder.Default"/> to escape characters that could
    /// break out of a JavaScript string literal context.
    /// </remarks>
    public string EncodeForJavaScript(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return JavaScriptEncoder.Default.Encode(input);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="UrlEncoder.Default"/> to apply percent-encoding as defined
    /// by RFC 3986.
    /// </remarks>
    public string EncodeForUrl(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return UrlEncoder.Default.Encode(input);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Implements OWASP CSS encoding (Rule #4): all non-alphanumeric characters
    /// are escaped using the CSS <c>\HHHHHH</c> (6-digit hexadecimal) format.
    /// </para>
    /// <para>
    /// Using the full 6-digit format eliminates ambiguity with following characters
    /// and does not require a trailing space terminator.
    /// </para>
    /// </remarks>
    public string EncodeForCss(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
        {
            return string.Empty;
        }

        // Worst case: each char becomes \HHHHHH (7 chars)
        var sb = new StringBuilder(input.Length * 7);

        foreach (var c in input)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else
            {
                // OWASP Rule #4: escape as \HHHHHH (6-digit hex, zero-padded)
                sb.Append('\\');
                sb.Append(((int)c).ToString("X6", CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }
}
