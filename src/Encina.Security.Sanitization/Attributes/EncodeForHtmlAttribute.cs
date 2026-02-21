namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Marks a property for automatic HTML output encoding.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a string property on a response object, the
/// <c>OutputEncodingPipelineBehavior</c> will HTML-encode the value before
/// returning it from the pipeline, preventing XSS attacks.
/// </para>
/// <para>
/// HTML encoding transforms characters like <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c>,
/// <c>&quot;</c>, and <c>&#39;</c> into their HTML entity equivalents.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record UserProfileDto
/// {
///     public Guid Id { get; init; }
///     [EncodeForHtml] public string DisplayName { get; init; }
///     [EncodeForHtml] public string Bio { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncodeForHtmlAttribute : EncodingAttribute
{
    /// <inheritdoc />
    public override EncodingContext EncodingContext => EncodingContext.Html;
}
