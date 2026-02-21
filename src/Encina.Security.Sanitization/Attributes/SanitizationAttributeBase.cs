namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Base class for all Encina input sanitization attributes.
/// </summary>
/// <remarks>
/// <para>
/// Sanitization attributes are discovered at runtime by the property metadata cache
/// and used by <c>InputSanitizationPipelineBehavior</c> to determine which properties
/// require sanitization during pipeline execution.
/// </para>
/// <para>
/// Subclasses define specific sanitization strategies:
/// <list type="bullet">
/// <item><description><see cref="SanitizeHtmlAttribute"/> — sanitizes HTML using the default profile</description></item>
/// <item><description><see cref="SanitizeSqlAttribute"/> — sanitizes for SQL context</description></item>
/// <item><description><see cref="SanitizeAttribute"/> — sanitizes using a named custom profile</description></item>
/// <item><description><see cref="StripHtmlAttribute"/> — strips all HTML, leaving plain text</description></item>
/// </list>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public abstract class SanitizationAttribute : Attribute
{
    /// <summary>
    /// Gets the sanitization type that identifies which sanitization strategy to apply.
    /// </summary>
    public abstract SanitizationType SanitizationType { get; }
}
