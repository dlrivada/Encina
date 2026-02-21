namespace Encina.Security.PII.Abstractions;

/// <summary>
/// Provides PII masking operations for string values and object graphs.
/// </summary>
/// <remarks>
/// <para>
/// The PII masker is the primary entry point for masking personally identifiable information.
/// It supports masking individual values by <see cref="PIIType"/>, by custom regex pattern,
/// and automatically masking all decorated properties on an object graph.
/// </para>
/// <para>
/// Implementations are expected to be thread-safe and suitable for use as singletons
/// in dependency injection containers.
/// </para>
/// <para>
/// The default implementation uses the registered <see cref="IMaskingStrategy"/> instances
/// for each <see cref="PIIType"/> and caches property metadata for performance.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mask a single value by PII type
/// string masked = masker.Mask("john.doe@example.com", PIIType.Email);
/// // Result: "j***@example.com"
///
/// // Mask using a custom pattern
/// string masked = masker.Mask("ABC-12345", @"\d+");
/// // Result: "ABC-*****"
///
/// // Mask all decorated properties on an object
/// var dto = new UserDto { Email = "john@example.com", Name = "John Doe" };
/// var masked = masker.MaskObject(dto);
/// // Result: UserDto { Email = "j***@example.com", Name = "J*** D**" }
/// </code>
/// </example>
public interface IPIIMasker
{
    /// <summary>
    /// Masks a string value using the strategy associated with the specified <see cref="PIIType"/>.
    /// </summary>
    /// <param name="value">The value to mask. If <c>null</c> or empty, it is returned unchanged.</param>
    /// <param name="type">The type of PII, which determines the masking strategy to apply.</param>
    /// <returns>The masked value.</returns>
    /// <example>
    /// <code>
    /// string masked = masker.Mask("john.doe@example.com", PIIType.Email);
    /// // Result: "j***@example.com"
    /// </code>
    /// </example>
    string Mask(string value, PIIType type);

    /// <summary>
    /// Masks portions of a string value that match the specified regex pattern.
    /// </summary>
    /// <param name="value">The value to mask. If <c>null</c> or empty, it is returned unchanged.</param>
    /// <param name="pattern">
    /// A regular expression pattern identifying the portions to mask.
    /// Matched groups are replaced with the configured mask character.
    /// </param>
    /// <returns>The masked value.</returns>
    /// <example>
    /// <code>
    /// string masked = masker.Mask("License: ABC-12345", @"\d+");
    /// // Result: "License: ABC-*****"
    /// </code>
    /// </example>
    string Mask(string value, string pattern);

    /// <summary>
    /// Creates a copy of the object with all PII-decorated properties masked.
    /// </summary>
    /// <typeparam name="T">The type of object to mask. Must be a reference type.</typeparam>
    /// <param name="obj">The object whose properties should be masked.</param>
    /// <returns>
    /// A new instance of <typeparamref name="T"/> with PII-decorated properties masked.
    /// Properties without PII attributes are copied unchanged.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method inspects all properties decorated with <see cref="Attributes.PIIAttribute"/>,
    /// <see cref="Attributes.SensitiveDataAttribute"/>, or <see cref="Attributes.MaskInLogsAttribute"/>
    /// and applies the appropriate masking strategy.
    /// </para>
    /// <para>
    /// Property metadata is cached after the first invocation for each type to avoid
    /// repeated reflection overhead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var user = new UserDto
    /// {
    ///     Email = "john@example.com",
    ///     Phone = "+1-555-123-4567",
    ///     Name = "John Doe"
    /// };
    /// var masked = masker.MaskObject(user);
    /// // masked.Email = "j***@example.com"
    /// // masked.Phone = "***-***-4567"
    /// // masked.Name = "J*** D**"
    /// </code>
    /// </example>
    T MaskObject<T>(T obj) where T : class;
}
