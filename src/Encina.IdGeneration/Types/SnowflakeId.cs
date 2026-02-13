using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using LanguageExt;

namespace Encina.IdGeneration;

/// <summary>
/// A strongly-typed 64-bit Snowflake ID wrapping a <see cref="long"/> value.
/// </summary>
/// <remarks>
/// <para>
/// Snowflake IDs encode a timestamp, machine/shard identifier, and sequence counter in a
/// single 64-bit integer. The default bit layout follows the Twitter/X Snowflake convention:
/// 41 bits timestamp + 10 bits machine/shard + 12 bits sequence = 63 bits (sign bit unused).
/// </para>
/// <para>
/// The bit layout is configurable via the generator; this type is a transparent wrapper
/// around the resulting <see cref="long"/> value.
/// </para>
/// <para>
/// IDs are lexicographically sortable by generation time, making them ideal for
/// database primary keys with B-tree index locality.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var id = new SnowflakeId(123456789L);
/// long raw = id; // implicit conversion
/// SnowflakeId back = 123456789L; // implicit conversion
///
/// // Parse from string
/// var parsed = SnowflakeId.Parse("123456789");
/// </code>
/// </example>
public readonly record struct SnowflakeId : IEquatable<SnowflakeId>, IComparable<SnowflakeId>, IComparable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnowflakeId"/> struct.
    /// </summary>
    /// <param name="value">The raw 64-bit Snowflake value.</param>
    public SnowflakeId(long value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the raw 64-bit Snowflake value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Gets a value indicating whether this ID represents the default (zero) value.
    /// </summary>
    public bool IsEmpty => Value == 0;

    /// <summary>
    /// Gets the empty (default) Snowflake ID.
    /// </summary>
    public static SnowflakeId Empty => default;

    /// <summary>
    /// Implicitly converts a <see cref="SnowflakeId"/> to a <see cref="long"/>.
    /// </summary>
    public static implicit operator long(SnowflakeId id) => id.Value;

    /// <summary>
    /// Implicitly converts a <see cref="long"/> to a <see cref="SnowflakeId"/>.
    /// </summary>
    public static implicit operator SnowflakeId(long value) => new(value);

    /// <summary>
    /// Parses a string representation of a Snowflake ID.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed <see cref="SnowflakeId"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid integer.</exception>
    public static SnowflakeId Parse(string s)
        => new(long.Parse(s, CultureInfo.InvariantCulture));

    /// <summary>
    /// Tries to parse a string representation of a Snowflake ID.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed ID if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out SnowflakeId result)
    {
        if (long.TryParse(s, CultureInfo.InvariantCulture, out var value))
        {
            result = new SnowflakeId(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Tries to parse a string, returning an <see cref="Either{EncinaError, SnowflakeId}"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>Right with the parsed ID; Left with a parse failure error.</returns>
    public static Either<EncinaError, SnowflakeId> TryParseEither(string? s)
    {
        if (TryParse(s, out var result))
        {
            return result;
        }

        return IdGenerationErrors.IdParseFailure(s, nameof(SnowflakeId));
    }

    /// <inheritdoc />
    public int CompareTo(SnowflakeId other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        SnowflakeId other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(SnowflakeId)}.", nameof(obj))
    };

    /// <summary>
    /// Determines whether the left operand is less than the right operand.
    /// </summary>
    public static bool operator <(SnowflakeId left, SnowflakeId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left operand is less than or equal to the right operand.
    /// </summary>
    public static bool operator <=(SnowflakeId left, SnowflakeId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left operand is greater than the right operand.
    /// </summary>
    public static bool operator >(SnowflakeId left, SnowflakeId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left operand is greater than or equal to the right operand.
    /// </summary>
    public static bool operator >=(SnowflakeId left, SnowflakeId right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
