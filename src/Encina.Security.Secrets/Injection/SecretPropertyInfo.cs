using System.Reflection;

namespace Encina.Security.Secrets.Injection;

/// <summary>
/// Captures metadata about a property decorated with <see cref="InjectSecretAttribute"/>,
/// including a compiled setter delegate for high-performance property value updates.
/// </summary>
/// <remarks>
/// <para>
/// Instances are created once per property during discovery and cached in
/// <see cref="SecretPropertyCache"/> to avoid repeated reflection.
/// </para>
/// <para>
/// The <see cref="Setter"/> delegate is compiled from an expression tree at discovery time,
/// eliminating the overhead of <see cref="PropertyInfo.SetValue(object?, object?)"/> during
/// secret injection operations.
/// </para>
/// </remarks>
internal sealed class SecretPropertyInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretPropertyInfo"/> class.
    /// </summary>
    /// <param name="property">The reflected property information.</param>
    /// <param name="attribute">The <see cref="InjectSecretAttribute"/> applied to the property.</param>
    /// <param name="setter">The compiled setter delegate for high-performance property updates.</param>
    internal SecretPropertyInfo(
        PropertyInfo property,
        InjectSecretAttribute attribute,
        Action<object, object?> setter)
    {
        Property = property;
        Attribute = attribute;
        Setter = setter;
    }

    /// <summary>
    /// The reflected property information.
    /// </summary>
    internal PropertyInfo Property { get; }

    /// <summary>
    /// The <see cref="InjectSecretAttribute"/> applied to this property.
    /// </summary>
    internal InjectSecretAttribute Attribute { get; }

    /// <summary>
    /// Compiled setter delegate for setting the property value without reflection overhead.
    /// </summary>
    /// <remarks>
    /// Compiled from an expression tree at discovery time. The first parameter is the
    /// target object instance, and the second is the value to set.
    /// </remarks>
    internal Action<object, object?> Setter { get; }

    /// <summary>
    /// Sets the property value on the target instance using the compiled <see cref="Setter"/> delegate.
    /// </summary>
    /// <param name="instance">The object instance to write to.</param>
    /// <param name="value">The value to set.</param>
    internal void SetValue(object instance, object? value) => Setter(instance, value);
}
