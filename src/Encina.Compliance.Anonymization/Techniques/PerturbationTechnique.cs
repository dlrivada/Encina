using System.Security.Cryptography;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.Techniques;

/// <summary>
/// Anonymization technique that adds controlled random noise to numeric values.
/// </summary>
/// <remarks>
/// <para>
/// Perturbation preserves aggregate statistical properties (mean, distribution)
/// while preventing individual identification. A random noise value within a configurable
/// range is added to or subtracted from the original value.
/// </para>
/// <para>
/// Technique-specific parameters:
/// <list type="bullet">
/// <item>
/// <term><c>"NoiseRange"</c></term>
/// <description>
/// Maximum deviation as a fraction of the original value (0.0-1.0).
/// For example, a noise range of <c>0.1</c> applied to value <c>100</c>
/// produces a value between <c>90</c> and <c>110</c>. Default: <c>0.1</c> (10%).
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Uses <see cref="RandomNumberGenerator"/> for cryptographically secure random noise generation.
/// </para>
/// </remarks>
public sealed class PerturbationTechnique : IAnonymizationTechnique
{
    private const double DefaultNoiseRange = 0.1;

    /// <inheritdoc/>
    public AnonymizationTechnique Technique => AnonymizationTechnique.Perturbation;

    /// <inheritdoc/>
    /// <remarks>
    /// Perturbation only supports numeric types: <c>int</c>, <c>long</c>, <c>double</c>,
    /// <c>decimal</c>, <c>float</c>, <c>short</c>, <c>byte</c>, and their unsigned variants.
    /// </remarks>
    public bool CanApply(Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        var underlying = Nullable.GetUnderlyingType(valueType) ?? valueType;
        return IsNumericType(underlying);
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, object?>> ApplyAsync(
        object? value,
        Type valueType,
        IReadOnlyDictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        if (value is null)
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>(null));
        }

        var underlying = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (!IsNumericType(underlying))
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Left<EncinaError, object?>(
                    AnonymizationErrors.TechniqueNotApplicable(
                        AnonymizationTechnique.Perturbation, "(field)", valueType)));
        }

        try
        {
            var noiseRange = GetNoiseRange(parameters);
            var originalValue = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            var noise = GenerateNoise(noiseRange);
            var perturbedValue = originalValue + (originalValue * noise);

            // Convert back to the original numeric type
            var result = Convert.ChangeType(perturbedValue, underlying, System.Globalization.CultureInfo.InvariantCulture);

            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>(result));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Left<EncinaError, object?>(
                    AnonymizationErrors.AnonymizationFailed("(perturbation)", ex.Message, ex)));
        }
    }

    private static double GetNoiseRange(IReadOnlyDictionary<string, object>? parameters)
    {
        if (parameters is not null && parameters.TryGetValue("NoiseRange", out var noiseRangeObj))
        {
            return Convert.ToDouble(noiseRangeObj, System.Globalization.CultureInfo.InvariantCulture);
        }

        return DefaultNoiseRange;
    }

    /// <summary>
    /// Generates a cryptographically secure random noise factor in the range [-noiseRange, +noiseRange].
    /// </summary>
    private static double GenerateNoise(double noiseRange)
    {
        // Generate a random double in [0, 1) using cryptographic randomness
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var randomValue = (double)(BitConverter.ToUInt64(bytes) >> 11) / (1UL << 53);

        // Map to [-noiseRange, +noiseRange]
        return (randomValue * 2.0 - 1.0) * noiseRange;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) || type == typeof(sbyte)
        || type == typeof(short) || type == typeof(ushort)
        || type == typeof(int) || type == typeof(uint)
        || type == typeof(long) || type == typeof(ulong)
        || type == typeof(float) || type == typeof(double)
        || type == typeof(decimal);
}
