namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Marks a property for automatic JavaScript output encoding.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a string property on a response object, the
/// <c>OutputEncodingPipelineBehavior</c> will JavaScript-encode the value
/// before returning it from the pipeline, preventing script injection attacks.
/// </para>
/// <para>
/// JavaScript encoding escapes characters that could break out of a JavaScript
/// string literal context.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record SearchResultDto
/// {
///     [EncodeForJavaScript] public string HighlightSnippet { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncodeForJavaScriptAttribute : EncodingAttribute
{
    /// <inheritdoc />
    public override EncodingContext EncodingContext => EncodingContext.JavaScript;
}
