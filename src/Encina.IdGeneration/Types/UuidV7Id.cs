using System.Diagnostics.CodeAnalysis;
using LanguageExt;

namespace Encina.IdGeneration;

/// <summary>
/// A strongly-typed UUID Version 7 (RFC 9562) wrapping a <see cref="Guid"/>.
/// </summary>
/// <remarks>
/// <para>
/// UUIDv7 encodes a Unix timestamp in the most significant 48 bits, followed by
/// random data, resulting in a time-ordered UUID that preserves B-tree index locality.
/// The version nibble (bits 48-51) is set to <c>0111</c> (7), and the variant
/// (bits 64-65) is set to <c>10</c> per RFC 4122/9562.
/// </para>
/// <para>
/// UUIDv7 is the recommended UUID version for new applications requiring time-ordered
/// identifiers with database-friendly index performance. It replaces UUIDv1 (MAC-based)
/// and UUIDv4 (random) for primary key use cases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var id = UuidV7Id.NewUuidV7();
/// Guid raw = id; // implicit conversion
/// UuidV7Id back = Guid.NewGuid(); // implicit conversion (wraps any Guid)
///
/// // Parse from standard GUID format
/// var parsed = UuidV7Id.Parse("019374c8-7b00-7000-8000-000000000001");
/// </code>
/// </example>
public readonly record struct UuidV7Id : IEquatable<UuidV7Id>, IComparable<UuidV7Id>, IComparable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UuidV7Id"/> struct.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value.</param>
    public UuidV7Id(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the underlying <see cref="Guid"/> value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Gets a value indicating whether this ID represents the default (empty Guid) value.
    /// </summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>
    /// Gets the empty (default) UUIDv7 ID.
    /// </summary>
    public static UuidV7Id Empty => default;

    /// <summary>
    /// Creates a new UUIDv7 with the current UTC timestamp and cryptographically random data.
    /// </summary>
    /// <returns>A new <see cref="UuidV7Id"/> conforming to RFC 9562.</returns>
    public static UuidV7Id NewUuidV7()
    {
        return NewUuidV7(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Creates a new UUIDv7 with the specified timestamp and cryptographically random data.
    /// </summary>
    /// <param name="timestamp">The timestamp to encode in the UUID.</param>
    /// <returns>A new <see cref="UuidV7Id"/> conforming to RFC 9562.</returns>
    public static UuidV7Id NewUuidV7(DateTimeOffset timestamp)
    {
        Span<byte> bytes = stackalloc byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

        var ms = timestamp.ToUnixTimeMilliseconds();

        // Encode 48-bit timestamp in big-endian (bytes 0-5)
        bytes[0] = (byte)(ms >> 40);
        bytes[1] = (byte)(ms >> 32);
        bytes[2] = (byte)(ms >> 24);
        bytes[3] = (byte)(ms >> 16);
        bytes[4] = (byte)(ms >> 8);
        bytes[5] = (byte)ms;

        // Set version to 7 (bits 48-51): clear top 4 bits of byte 6, set to 0111
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);

        // Set variant to RFC 4122 (bits 64-65): clear top 2 bits of byte 8, set to 10
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new UuidV7Id(new Guid(bytes, bigEndian: true));
    }

    /// <summary>
    /// Gets the timestamp encoded in this UUIDv7.
    /// </summary>
    /// <returns>The <see cref="DateTimeOffset"/> encoded in the first 48 bits.</returns>
    public DateTimeOffset GetTimestamp()
    {
        Span<byte> bytes = stackalloc byte[16];
        Value.TryWriteBytes(bytes, bigEndian: true, out _);

        long ms = ((long)bytes[0] << 40)
                 | ((long)bytes[1] << 32)
                 | ((long)bytes[2] << 24)
                 | ((long)bytes[3] << 16)
                 | ((long)bytes[4] << 8)
                 | bytes[5];

        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    /// <summary>
    /// Implicitly converts a <see cref="UuidV7Id"/> to a <see cref="Guid"/>.
    /// </summary>
    public static implicit operator Guid(UuidV7Id id) => id.Value;

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="UuidV7Id"/>.
    /// </summary>
    public static implicit operator UuidV7Id(Guid value) => new(value);

    /// <summary>
    /// Parses a string representation of a UUIDv7.
    /// </summary>
    /// <param name="s">The string to parse (standard GUID format).</param>
    /// <returns>The parsed <see cref="UuidV7Id"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid GUID.</exception>
    public static UuidV7Id Parse(string s)
        => new(Guid.Parse(s));

    /// <summary>
    /// Tries to parse a string representation of a UUIDv7.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed ID if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out UuidV7Id result)
    {
        if (Guid.TryParse(s, out var guid))
        {
            result = new UuidV7Id(guid);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Tries to parse a string, returning an <see cref="Either{EncinaError, UuidV7Id}"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>Right with the parsed ID; Left with a parse failure error.</returns>
    public static Either<EncinaError, UuidV7Id> TryParseEither(string? s)
    {
        if (TryParse(s, out var result))
        {
            return result;
        }

        return IdGenerationErrors.IdParseFailure(s, nameof(UuidV7Id));
    }

    /// <inheritdoc />
    public int CompareTo(UuidV7Id other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        UuidV7Id other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(UuidV7Id)}.", nameof(obj))
    };

    /// <summary>
    /// Determines whether the left operand is less than the right operand.
    /// </summary>
    public static bool operator <(UuidV7Id left, UuidV7Id right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left operand is less than or equal to the right operand.
    /// </summary>
    public static bool operator <=(UuidV7Id left, UuidV7Id right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left operand is greater than the right operand.
    /// </summary>
    public static bool operator >(UuidV7Id left, UuidV7Id right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left operand is greater than or equal to the right operand.
    /// </summary>
    public static bool operator >=(UuidV7Id left, UuidV7Id right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
