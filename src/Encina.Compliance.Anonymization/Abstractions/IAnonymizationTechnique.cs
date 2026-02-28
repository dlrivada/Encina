using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Strategy interface for a specific anonymization technique that can be applied to field values.
/// </summary>
/// <remarks>
/// <para>
/// Each anonymization technique (generalization, suppression, perturbation, data masking,
/// k-anonymity, l-diversity, t-closeness, swapping) is implemented as a separate class
/// implementing this interface. The <see cref="IAnonymizer"/> delegates to the appropriate
/// technique based on the <see cref="FieldAnonymizationRule.Technique"/> specified in the
/// <see cref="AnonymizationProfile"/>.
/// </para>
/// <para>
/// This follows the Strategy pattern, enabling:
/// <list type="bullet">
/// <item>Open/closed principle: new techniques can be added without modifying existing code</item>
/// <item>DI registration via <c>TryAdd</c>: users can replace built-in techniques or add custom ones</item>
/// <item>Testability: each technique can be unit-tested independently</item>
/// </list>
/// </para>
/// <para>
/// Built-in technique implementations include:
/// <list type="bullet">
/// <item><b>Generalization</b>: Reduces precision (e.g., age 34 → age range 30-39)</item>
/// <item><b>Suppression</b>: Removes the value entirely (replaces with null/default)</item>
/// <item><b>Perturbation</b>: Adds random noise to numerical values</item>
/// <item><b>Swapping</b>: Exchanges values between records in a dataset</item>
/// <item><b>DataMasking</b>: Partially masks the value (e.g., <c>john@***</c>)</item>
/// <item><b>KAnonymity</b>: Ensures each record is indistinguishable from k-1 others</item>
/// <item><b>LDiversity</b>: Ensures diversity of sensitive values within equivalence classes</item>
/// <item><b>TCloseness</b>: Ensures distribution closeness between class and dataset</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SuppressionTechnique : IAnonymizationTechnique
/// {
///     public AnonymizationTechnique Technique => AnonymizationTechnique.Suppression;
///
///     public bool CanApply(Type valueType) => true; // Suppression works on any type
///
///     public ValueTask&lt;Either&lt;EncinaError, object?&gt;&gt; ApplyAsync(
///         object? value, Type valueType,
///         IReadOnlyDictionary&lt;string, object&gt;? parameters,
///         CancellationToken cancellationToken)
///     {
///         // Suppression replaces the value with the type's default
///         return ValueTask.FromResult&lt;Either&lt;EncinaError, object?&gt;&gt;(
///             valueType.IsValueType ? Activator.CreateInstance(valueType) : null);
///     }
/// }
/// </code>
/// </example>
public interface IAnonymizationTechnique
{
    /// <summary>
    /// The anonymization technique this implementation handles.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="IAnonymizer"/> to select the correct technique implementation
    /// for each <see cref="FieldAnonymizationRule"/> in the <see cref="AnonymizationProfile"/>.
    /// </remarks>
    AnonymizationTechnique Technique { get; }

    /// <summary>
    /// Applies the anonymization technique to a field value.
    /// </summary>
    /// <param name="value">The original field value to anonymize. May be <c>null</c>.</param>
    /// <param name="valueType">The CLR type of the field value.</param>
    /// <param name="parameters">
    /// Optional technique-specific parameters from the <see cref="FieldAnonymizationRule"/>.
    /// For example, <c>{"Granularity": 10}</c> for generalization or <c>{"Pattern": "***"}</c>
    /// for data masking.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The anonymized value (may be <c>null</c> for suppression), or an <see cref="EncinaError"/>
    /// if the technique could not be applied to the given value type or parameters are invalid.
    /// </returns>
    ValueTask<Either<EncinaError, object?>> ApplyAsync(
        object? value,
        Type valueType,
        IReadOnlyDictionary<string, object>? parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether this technique can be applied to the specified value type.
    /// </summary>
    /// <param name="valueType">The CLR type of the field value to check.</param>
    /// <returns>
    /// <c>true</c> if this technique supports the given type, <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// For example, perturbation only works on numeric types (<c>int</c>, <c>double</c>, etc.),
    /// while suppression works on any type. The <see cref="IAnonymizer"/> uses this method
    /// to validate the profile before applying techniques.
    /// </remarks>
    bool CanApply(Type valueType);
}
