namespace Encina.Security.Sanitization.Abstractions;

/// <summary>
/// Provides context-aware output encoding to prevent injection attacks.
/// </summary>
/// <remarks>
/// <para>
/// Output encoding transforms data so it is treated as data (not code) in the target context.
/// Unlike sanitization (which removes dangerous content), encoding preserves all content but
/// makes it safe for the specific output context.
/// </para>
/// <para>
/// Each method targets a specific output context (HTML body, HTML attribute, JavaScript, URL, CSS)
/// and applies the appropriate encoding rules as defined by OWASP encoding guidelines.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for use in concurrent pipeline execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Encode user input for display in HTML
/// string safe = encoder.EncodeForHtml("&lt;script&gt;alert('xss')&lt;/script&gt;");
/// // Result: "&amp;lt;script&amp;gt;alert(&#39;xss&#39;)&amp;lt;/script&amp;gt;"
///
/// // Encode for use in a JavaScript string
/// string jsEncoded = encoder.EncodeForJavaScript("'; alert('xss'); //");
/// </code>
/// </example>
public interface IOutputEncoder
{
    /// <summary>
    /// Encodes output for safe rendering in an HTML body context.
    /// </summary>
    /// <param name="input">The string to encode for HTML context.</param>
    /// <returns>The HTML-encoded string.</returns>
    /// <remarks>
    /// Encodes characters that have special meaning in HTML (&amp;, &lt;, &gt;, &quot;, &#39;)
    /// using the <c>System.Text.Encodings.Web.HtmlEncoder</c>.
    /// </remarks>
    string EncodeForHtml(string input);

    /// <summary>
    /// Encodes output for safe use in an HTML attribute value.
    /// </summary>
    /// <param name="input">The string to encode for HTML attribute context.</param>
    /// <returns>The encoded string safe for use in HTML attributes.</returns>
    /// <remarks>
    /// Applies stricter encoding than <see cref="EncodeForHtml"/> to handle attribute-specific
    /// injection vectors. Always quote attribute values in addition to encoding.
    /// </remarks>
    string EncodeForHtmlAttribute(string input);

    /// <summary>
    /// Encodes output for safe embedding in a JavaScript string context.
    /// </summary>
    /// <param name="input">The string to encode for JavaScript context.</param>
    /// <returns>The JavaScript-encoded string.</returns>
    /// <remarks>
    /// Escapes characters that could break out of a JavaScript string literal,
    /// using the <c>System.Text.Encodings.Web.JavaScriptEncoder</c>.
    /// </remarks>
    string EncodeForJavaScript(string input);

    /// <summary>
    /// Encodes output for safe use in a URL context.
    /// </summary>
    /// <param name="input">The string to encode for URL context.</param>
    /// <returns>The URL-encoded string.</returns>
    /// <remarks>
    /// Applies percent-encoding as defined by RFC 3986 using the
    /// <c>System.Text.Encodings.Web.UrlEncoder</c>.
    /// </remarks>
    string EncodeForUrl(string input);

    /// <summary>
    /// Encodes output for safe use in a CSS context.
    /// </summary>
    /// <param name="input">The string to encode for CSS context.</param>
    /// <returns>The CSS-encoded string.</returns>
    /// <remarks>
    /// Escapes characters that could enable CSS injection attacks,
    /// such as expression evaluation or URL manipulation in style values.
    /// </remarks>
    string EncodeForCss(string input);
}
