using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.Techniques;

/// <summary>
/// Anonymization technique that removes values entirely by replacing them with <c>null</c> or the type's default.
/// </summary>
/// <remarks>
/// <para>
/// Suppression provides the strongest privacy protection at the cost of data utility —
/// the field value is completely eliminated. This is suitable for direct identifiers
/// (name, email, phone number) that have no analytical value in the target dataset.
/// </para>
/// <para>
/// For value types (e.g., <c>int</c>, <c>DateTime</c>), the default value is used
/// (e.g., <c>0</c>, <c>default(DateTime)</c>). For reference types, <c>null</c> is returned.
/// </para>
/// <para>
/// No technique-specific parameters are required.
/// </para>
/// </remarks>
public sealed class SuppressionTechnique : IAnonymizationTechnique
{
    /// <inheritdoc/>
    public AnonymizationTechnique Technique => AnonymizationTechnique.Suppression;

    /// <inheritdoc/>
    /// <remarks>
    /// Suppression works on any type — both value types and reference types can be suppressed.
    /// </remarks>
    public bool CanApply(Type valueType) => true;

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, object?>> ApplyAsync(
        object? value,
        Type valueType,
        IReadOnlyDictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        try
        {
            var result = valueType.IsValueType
                ? Activator.CreateInstance(valueType)
                : null;

            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>(result));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Left<EncinaError, object?>(
                    AnonymizationErrors.AnonymizationFailed("(suppression)", ex.Message, ex)));
        }
    }
}
