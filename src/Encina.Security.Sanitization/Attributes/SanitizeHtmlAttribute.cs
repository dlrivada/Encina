namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Marks a property for automatic HTML sanitization using the default sanitization profile.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a string property, the <c>InputSanitizationPipelineBehavior</c>
/// will sanitize the value by removing dangerous HTML elements and attributes
/// while preserving safe content according to the configured default profile.
/// </para>
/// <para>
/// For custom sanitization rules, use <see cref="SanitizeAttribute"/> with a named profile.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record CreatePostCommand(
///     string Title,
///     [property: SanitizeHtml] string Description,
///     [property: SanitizeHtml] string Content
/// ) : ICommand&lt;PostId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SanitizeHtmlAttribute : SanitizationAttribute
{
    /// <inheritdoc />
    public override SanitizationType SanitizationType => SanitizationType.Html;
}
