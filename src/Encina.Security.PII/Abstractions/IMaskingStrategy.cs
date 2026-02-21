namespace Encina.Security.PII.Abstractions;

/// <summary>
/// Defines a strategy for masking PII values.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="PIIType"/> has a default masking strategy, but custom strategies
/// can be registered via <see cref="PIIOptions.AddStrategy{TStrategy}(PIIType)"/>
/// to override the default behavior.
/// </para>
/// <para>
/// Implementations must be stateless and thread-safe, as a single instance is shared
/// across all masking operations for a given <see cref="PIIType"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomPhoneMaskingStrategy : IMaskingStrategy
/// {
///     public string Apply(string value, MaskingOptions options)
///     {
///         // Keep country code, mask the rest except last 2 digits
///         if (value.Length &lt; 4) return new string(options.MaskCharacter, value.Length);
///         return value[..3] + new string(options.MaskCharacter, value.Length - 5) + value[^2..];
///     }
/// }
///
/// // Register the custom strategy
/// services.AddEncinaPII(options =>
/// {
///     options.AddStrategy&lt;CustomPhoneMaskingStrategy&gt;(PIIType.Phone);
/// });
/// </code>
/// </example>
public interface IMaskingStrategy
{
    /// <summary>
    /// Applies the masking strategy to the specified value.
    /// </summary>
    /// <param name="value">
    /// The original PII value to mask. Guaranteed to be non-null and non-empty
    /// by the calling <see cref="IPIIMasker"/>.
    /// </param>
    /// <param name="options">
    /// The masking options controlling the behavior of this operation,
    /// including the mask character, visible character counts, and mode.
    /// </param>
    /// <returns>The masked value. Must never return <c>null</c>.</returns>
    string Apply(string value, MaskingOptions options);
}
