namespace Encina.Security.PII;

/// <summary>
/// Configuration options for a specific masking operation.
/// </summary>
/// <remarks>
/// <para>
/// Passed to <see cref="Abstractions.IMaskingStrategy.Apply"/> to control the behavior
/// of the masking algorithm. The properties relevant to each operation depend on the
/// <see cref="MaskingMode"/> being used.
/// </para>
/// <para>
/// This is a value type (<c>record struct</c>) to avoid heap allocations in the
/// high-throughput masking pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new MaskingOptions
/// {
///     Mode = MaskingMode.Partial,
///     MaskCharacter = '*',
///     PreserveLength = true,
///     VisibleCharactersStart = 1,
///     VisibleCharactersEnd = 4
/// };
/// </code>
/// </example>
public readonly record struct MaskingOptions
{
    /// <summary>
    /// Gets the masking mode to apply.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="MaskingMode.Partial"/>.
    /// </remarks>
    public MaskingMode Mode { get; init; }

    /// <summary>
    /// Gets the character used to replace masked portions of the value.
    /// </summary>
    /// <remarks>
    /// Default is <c>'*'</c>. Common alternatives include <c>'X'</c> and <c>'#'</c>.
    /// </remarks>
    public char MaskCharacter { get; init; }

    /// <summary>
    /// Gets whether the masked output should preserve the original value's length.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the masked value has the same character count as the original.
    /// When <c>false</c>, the masked value may use a fixed-length mask (e.g., <c>***</c>).
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool PreserveLength { get; init; }

    /// <summary>
    /// Gets the number of characters to leave visible at the start of the value.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="Mode"/> is <see cref="MaskingMode.Partial"/>.
    /// Default is <c>0</c>.
    /// </remarks>
    public int VisibleCharactersStart { get; init; }

    /// <summary>
    /// Gets the number of characters to leave visible at the end of the value.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="Mode"/> is <see cref="MaskingMode.Partial"/>.
    /// Default is <c>0</c>.
    /// </remarks>
    public int VisibleCharactersEnd { get; init; }

    /// <summary>
    /// Gets the replacement text used for <see cref="MaskingMode.Redact"/>.
    /// </summary>
    /// <remarks>
    /// Default is <c>"[REDACTED]"</c>. Customize for localization or compliance requirements.
    /// </remarks>
    public string? RedactedPlaceholder { get; init; }

    /// <summary>
    /// Gets the optional salt used for <see cref="MaskingMode.Hash"/> operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Adding a salt prevents rainbow-table attacks on hashed PII values.
    /// When <c>null</c>, hashing is performed without a salt (not recommended
    /// for low-entropy values like phone numbers).
    /// </para>
    /// </remarks>
    public string? HashSalt { get; init; }

    /// <summary>
    /// Creates a new <see cref="MaskingOptions"/> instance with default values.
    /// </summary>
    public MaskingOptions()
    {
        Mode = MaskingMode.Partial;
        MaskCharacter = '*';
        PreserveLength = true;
        VisibleCharactersStart = 0;
        VisibleCharactersEnd = 0;
        RedactedPlaceholder = "[REDACTED]";
        HashSalt = null;
    }
}
