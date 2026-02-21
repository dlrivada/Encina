namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Marks a property for automatic URL output encoding.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a string property on a response object, the
/// <c>OutputEncodingPipelineBehavior</c> will URL-encode (percent-encode)
/// the value before returning it from the pipeline.
/// </para>
/// <para>
/// URL encoding applies percent-encoding as defined by RFC 3986, transforming
/// unsafe characters into their <c>%XX</c> equivalents.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record RedirectResponseDto
/// {
///     [EncodeForUrl] public string ReturnUrl { get; init; }
///     [EncodeForUrl] public string CallbackParameter { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncodeForUrlAttribute : EncodingAttribute
{
    /// <inheritdoc />
    public override EncodingContext EncodingContext => EncodingContext.Url;
}
