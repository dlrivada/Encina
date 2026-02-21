namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Marks a property for automatic sanitization using a named custom profile.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a string property, the <c>InputSanitizationPipelineBehavior</c>
/// will look up the profile by <see cref="Profile"/> name in the registered
/// <c>SanitizationOptions.Profiles</c> dictionary and apply its rules.
/// </para>
/// <para>
/// If no <see cref="Profile"/> is specified, the default profile from
/// <c>SanitizationOptions.DefaultProfile</c> is used (or <c>StrictText</c> if not configured).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record CreateArticleCommand(
///     string Title,
///     [property: Sanitize(Profile = "BlogPost")] string Content,
///     [property: Sanitize(Profile = "RichText")] string Summary
/// ) : ICommand&lt;ArticleId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SanitizeAttribute : SanitizationAttribute
{
    /// <inheritdoc />
    public override SanitizationType SanitizationType => SanitizationType.Custom;

    /// <summary>
    /// Gets or sets the name of the custom sanitization profile to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The profile must be registered via <c>SanitizationOptions.AddProfile</c> during service
    /// configuration. Profile names are case-insensitive.
    /// </para>
    /// <para>
    /// When <c>null</c>, the default profile from <c>SanitizationOptions.DefaultProfile</c>
    /// is used.
    /// </para>
    /// </remarks>
    public string? Profile { get; set; }
}
