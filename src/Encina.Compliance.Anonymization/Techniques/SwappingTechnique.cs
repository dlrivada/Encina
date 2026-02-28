using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.Techniques;

/// <summary>
/// Anonymization technique that swaps values between records to break the link between
/// individuals and their specific values.
/// </summary>
/// <remarks>
/// <para>
/// Swapping maintains the overall distribution of values in the dataset while preventing
/// attribution of specific values to specific individuals. Since the same set of values
/// exists in the dataset (just reassigned to different records), aggregate statistics
/// are perfectly preserved.
/// </para>
/// <para>
/// When applied to a single record (outside a dataset context), swapping replaces
/// the value with a type-appropriate default, since there are no other records to swap with.
/// For full swapping behavior, use the <see cref="IAnonymizer"/> which applies techniques
/// at the dataset level when possible.
/// </para>
/// <para>
/// No technique-specific parameters are required. Swapping is inherently applicable
/// to any data type.
/// </para>
/// </remarks>
public sealed class SwappingTechnique : IAnonymizationTechnique
{
    /// <inheritdoc/>
    public AnonymizationTechnique Technique => AnonymizationTechnique.Swapping;

    /// <inheritdoc/>
    /// <remarks>
    /// Swapping works on any type — values are simply reassigned between records.
    /// When applied to a single record, the value is replaced with the type default.
    /// </remarks>
    public bool CanApply(Type valueType) => true;

    /// <inheritdoc/>
    /// <remarks>
    /// When applied to a single record (the typical strategy-level invocation),
    /// swapping replaces the value with the type's default. Full record-level swapping
    /// requires dataset context and is orchestrated by the <see cref="IAnonymizer"/>.
    /// </remarks>
    public ValueTask<Either<EncinaError, object?>> ApplyAsync(
        object? value,
        Type valueType,
        IReadOnlyDictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        try
        {
            // Single-record swapping: replace with default.
            // Full dataset swapping is handled at the anonymizer level.
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
                    AnonymizationErrors.AnonymizationFailed("(swapping)", ex.Message, ex)));
        }
    }
}
