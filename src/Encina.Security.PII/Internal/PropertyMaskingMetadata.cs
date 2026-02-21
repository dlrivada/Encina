using System.Reflection;

namespace Encina.Security.PII.Internal;

/// <summary>
/// Captures metadata about a property that requires PII masking,
/// including a compiled setter delegate for high-performance property value updates.
/// </summary>
/// <remarks>
/// Instances are created once per property during discovery and cached in
/// <see cref="PIIPropertyScanner"/> to avoid repeated reflection.
/// </remarks>
internal readonly record struct PropertyMaskingMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyMaskingMetadata"/> struct.
    /// </summary>
    internal PropertyMaskingMetadata(
        PropertyInfo property,
        PIIType type,
        MaskingMode mode,
        string? pattern,
        string? replacement,
        bool logOnly,
        Action<object, object?> setter)
    {
        Property = property;
        Type = type;
        Mode = mode;
        Pattern = pattern;
        Replacement = replacement;
        LogOnly = logOnly;
        Setter = setter;
    }

    /// <summary>
    /// The reflected property information.
    /// </summary>
    internal PropertyInfo Property { get; }

    /// <summary>
    /// The type of PII this property contains.
    /// </summary>
    internal PIIType Type { get; }

    /// <summary>
    /// The masking mode to apply.
    /// </summary>
    internal MaskingMode Mode { get; }

    /// <summary>
    /// Optional custom regex pattern for masking.
    /// </summary>
    internal string? Pattern { get; }

    /// <summary>
    /// Optional custom replacement string.
    /// </summary>
    internal string? Replacement { get; }

    /// <summary>
    /// Whether this property should only be masked in logging contexts.
    /// </summary>
    internal bool LogOnly { get; }

    /// <summary>
    /// Compiled setter delegate for setting the property value without reflection overhead.
    /// </summary>
    internal Action<object, object?> Setter { get; }

    /// <summary>
    /// Gets the property value from the target instance.
    /// </summary>
    internal object? GetValue(object instance) => Property.GetValue(instance);

    /// <summary>
    /// Sets the property value on the target instance using the compiled setter.
    /// </summary>
    internal void SetValue(object instance, object? value) => Setter(instance, value);
}
