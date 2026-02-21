namespace Encina.Security.PII.Attributes;

/// <summary>
/// Marks a property or class as containing personally identifiable information (PII)
/// that should be automatically masked.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a property, the <see cref="Abstractions.IPIIMasker"/> will automatically
/// mask the property value using the strategy corresponding to the specified <see cref="Type"/>.
/// </para>
/// <para>
/// When applied to a class, all string properties within that class are treated as PII
/// using the specified <see cref="Type"/> unless individually overridden.
/// </para>
/// <para>
/// The masking behavior can be customized via <see cref="Mode"/>, <see cref="Pattern"/>,
/// and <see cref="Replacement"/> properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record CreateUserCommand(
///     [property: PII(PIIType.Email)] string Email,
///     [property: PII(PIIType.Phone)] string PhoneNumber,
///     [property: PII(PIIType.Name)] string FullName,
///     [property: PII(PIIType.Custom, Pattern = @"\d{3}-\d{2}-\d{4}")] string TaxId
/// ) : ICommand&lt;UserId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PIIAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PIIAttribute"/> class
    /// with the specified PII type.
    /// </summary>
    /// <param name="type">The type of PII this property contains.</param>
    public PIIAttribute(PIIType type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets the type of PII this property contains.
    /// </summary>
    /// <remarks>
    /// Determines which default masking strategy is applied.
    /// Use <see cref="PIIType.Custom"/> with a <see cref="Pattern"/> for non-standard PII types.
    /// </remarks>
    public PIIType Type { get; }

    /// <summary>
    /// Gets or sets a custom regex pattern for identifying the portions to mask.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required when <see cref="Type"/> is <see cref="PIIType.Custom"/>.
    /// Optional for predefined types — when specified, it overrides the default detection pattern.
    /// </para>
    /// <para>
    /// Matched groups within the pattern are replaced with the mask character.
    /// </para>
    /// </remarks>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets a custom replacement string for the masked portions.
    /// </summary>
    /// <remarks>
    /// When specified, this string replaces the masked portions instead of the default
    /// mask character. For example, <c>"[HIDDEN]"</c> or <c>"XXX"</c>.
    /// </remarks>
    public string? Replacement { get; set; }

    /// <summary>
    /// Gets or sets the masking mode to apply.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="MaskingMode.Partial"/>. Override to use
    /// <see cref="MaskingMode.Full"/>, <see cref="MaskingMode.Hash"/>,
    /// <see cref="MaskingMode.Tokenize"/>, or <see cref="MaskingMode.Redact"/>.
    /// </remarks>
    public MaskingMode Mode { get; set; } = MaskingMode.Partial;
}
