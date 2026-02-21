namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Base class for all Encina output encoding attributes.
/// </summary>
/// <remarks>
/// <para>
/// Encoding attributes are discovered at runtime by the property metadata cache
/// and used by <c>OutputEncodingPipelineBehavior</c> to determine which properties
/// require encoding on outgoing responses.
/// </para>
/// <para>
/// Unlike sanitization (which modifies content), encoding transforms data so it is
/// treated as data (not code) in the target output context.
/// </para>
/// <para>
/// Subclasses define specific encoding contexts:
/// <list type="bullet">
/// <item><description><see cref="EncodeForHtmlAttribute"/> — encodes for HTML body context</description></item>
/// <item><description><see cref="EncodeForJavaScriptAttribute"/> — encodes for JavaScript string context</description></item>
/// <item><description><see cref="EncodeForUrlAttribute"/> — encodes for URL context</description></item>
/// </list>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public abstract class EncodingAttribute : Attribute
{
    /// <summary>
    /// Gets the encoding context that identifies which encoding strategy to apply.
    /// </summary>
    public abstract EncodingContext EncodingContext { get; }
}
