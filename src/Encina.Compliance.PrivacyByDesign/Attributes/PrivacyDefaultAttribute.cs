namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Declares the privacy-respecting default value for a property.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a property of a request type that is decorated with
/// <see cref="EnforceDataMinimizationAttribute"/>, the pipeline behavior validates that
/// the field's value matches the declared default. Deviations are reported as
/// <see cref="Model.PrivacyViolationType.DefaultPrivacy"/> violations.
/// </para>
/// <para>
/// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
/// organisational measures for ensuring that, by default, personal data are not made
/// accessible without the individual's intervention to an indefinite number of natural persons."
/// </para>
/// <para>
/// This attribute enables declarative enforcement of privacy-respecting defaults. For example,
/// a "share with third parties" flag should default to <see langword="false"/>, and a
/// "data retention days" field should default to the minimum required period.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EnforceDataMinimization(Purpose = "User Preferences")]
/// public sealed record UpdatePreferencesCommand(
///     string UserId,
///     [property: PrivacyDefault(false)]
///     bool ShareWithThirdParties,
///     [property: PrivacyDefault(false)]
///     bool EnableProfiling,
///     [property: PrivacyDefault(30)]
///     int DataRetentionDays) : ICommand&lt;Unit&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PrivacyDefaultAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrivacyDefaultAttribute"/> class
    /// with the specified privacy-respecting default value.
    /// </summary>
    /// <param name="defaultValue">
    /// The privacy-respecting default value for this property. Pass <see langword="null"/>
    /// when <see langword="null"/> is the most privacy-respecting default (e.g., optional
    /// personal data fields). Primitive values are boxed automatically.
    /// </param>
    public PrivacyDefaultAttribute(object? defaultValue)
    {
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Gets the declared privacy-respecting default value.
    /// </summary>
    /// <remarks>
    /// The pipeline behavior compares the actual property value against this default
    /// using <see cref="object.Equals(object?, object?)"/>. When the values differ,
    /// a <see cref="Model.PrivacyViolationType.DefaultPrivacy"/> violation is reported,
    /// indicating that the user or system has explicitly opted into more permissive processing.
    /// </remarks>
    public object? DefaultValue { get; }
}
