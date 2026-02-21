namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Marks a property for automatic HTML stripping, leaving only plain text.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a string property, the <c>InputSanitizationPipelineBehavior</c>
/// will remove all HTML tags and decode HTML entities, leaving only the text content.
/// </para>
/// <para>
/// This is equivalent to using <see cref="SanitizeHtmlAttribute"/> with the
/// <c>StrictText</c> profile, which strips all tags.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record UpdateProfileCommand(
///     Guid UserId,
///     [property: StripHtml] string DisplayName,
///     [property: StripHtml] string Bio
/// ) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class StripHtmlAttribute : SanitizationAttribute
{
    /// <inheritdoc />
    public override SanitizationType SanitizationType => SanitizationType.StripHtml;
}
