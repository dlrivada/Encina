namespace Encina.Security.PII.Attributes;

/// <summary>
/// Marks a property or class as containing generic sensitive data that should be masked.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute for data that is sensitive but does not fall into a specific
/// <see cref="PIIType"/> category. Unlike <see cref="PIIAttribute"/>, this attribute
/// does not associate the data with a predefined masking strategy — instead, the
/// <see cref="Mode"/> property controls how the value is masked.
/// </para>
/// <para>
/// Common use cases include API keys, tokens, internal identifiers, passwords,
/// and other confidential data that should not appear in logs or responses.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record ConfigureApiCommand(
///     [property: SensitiveData(MaskingMode.Full)] string ApiKey,
///     [property: SensitiveData(MaskingMode.Redact)] string SecretToken,
///     [property: SensitiveData] string InternalReference
/// ) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveDataAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveDataAttribute"/> class
    /// with the default masking mode (<see cref="MaskingMode.Full"/>).
    /// </summary>
    public SensitiveDataAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveDataAttribute"/> class
    /// with the specified masking mode.
    /// </summary>
    /// <param name="mode">The masking mode to apply.</param>
    public SensitiveDataAttribute(MaskingMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Gets or sets the masking mode to apply.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="MaskingMode.Full"/>. Since sensitive data typically has no
    /// predefined structure, full masking is the safest default.
    /// </remarks>
    public MaskingMode Mode { get; set; } = MaskingMode.Full;
}
