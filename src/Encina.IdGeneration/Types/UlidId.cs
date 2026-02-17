using System.Diagnostics.CodeAnalysis;
using LanguageExt;

namespace Encina.IdGeneration;

/// <summary>
/// A strongly-typed 128-bit ULID (Universally Unique Lexicographically Sortable Identifier)
/// wrapping a <see cref="Guid"/> internally.
/// </summary>
/// <remarks>
/// <para>
/// ULIDs encode 48 bits of timestamp and 80 bits of randomness in 128 bits. They are
/// lexicographically sortable when represented in their canonical Crockford Base32
/// encoding (26 characters, e.g., <c>01ARZ3NDEKTSV4RRFFQ69G5FAV</c>).
/// </para>
/// <para>
/// Internally stored as a <see cref="Guid"/> for database compatibility (128-bit),
/// but the string representation uses Crockford Base32 encoding per the ULID specification.
/// </para>
/// <para>
/// See <see href="https://github.com/ulid/spec"/> for the ULID specification.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var id = UlidId.NewUlid();
/// string crockford = id.ToString(); // "01ARZ3NDEKTSV4RRFFQ69G5FAV"
/// Guid raw = id; // implicit conversion to Guid
///
/// // Parse from Crockford Base32
/// var parsed = UlidId.Parse("01ARZ3NDEKTSV4RRFFQ69G5FAV");
/// </code>
/// </example>
public readonly record struct UlidId : IEquatable<UlidId>, IComparable<UlidId>, IComparable
{
    /// <summary>
    /// The length of a ULID string in Crockford Base32 encoding.
    /// </summary>
    public const int StringLength = 26;

    private static readonly char[] CrockfordBase32Chars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".ToCharArray();

    private static readonly byte[] CrockfordBase32Decode = CreateDecodingTable();

    /// <summary>
    /// Initializes a new instance of the <see cref="UlidId"/> struct from raw bytes.
    /// </summary>
    /// <param name="bytes">The 16 bytes representing the ULID (big-endian: 6 bytes timestamp + 10 bytes random).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="bytes"/> is not exactly 16 bytes.</exception>
    public UlidId(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16)
        {
            throw new ArgumentException("ULID must be exactly 16 bytes.", nameof(bytes));
        }

        Bytes = bytes.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UlidId"/> struct from a <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The GUID representation of the ULID.</param>
    public UlidId(Guid value)
    {
        Span<byte> span = stackalloc byte[16];
        value.TryWriteBytes(span);
        Bytes = span.ToArray();
    }

    /// <summary>
    /// Gets the raw 16-byte representation of this ULID.
    /// </summary>
    internal byte[] Bytes { get; } = new byte[16];

    /// <summary>
    /// Gets a value indicating whether this ID represents the default (all zeros) value.
    /// </summary>
    public bool IsEmpty => Bytes.AsSpan().SequenceEqual(stackalloc byte[16]);

    /// <summary>
    /// Gets the empty (default) ULID.
    /// </summary>
    public static UlidId Empty => default;

    /// <summary>
    /// Creates a new ULID with the current UTC timestamp and cryptographically random bytes.
    /// </summary>
    /// <returns>A new <see cref="UlidId"/>.</returns>
    public static UlidId NewUlid()
    {
        return NewUlid(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Creates a new ULID with the specified timestamp and cryptographically random bytes.
    /// </summary>
    /// <param name="timestamp">The timestamp to encode in the ULID.</param>
    /// <returns>A new <see cref="UlidId"/>.</returns>
    public static UlidId NewUlid(DateTimeOffset timestamp)
    {
        Span<byte> bytes = stackalloc byte[16];
        var ms = timestamp.ToUnixTimeMilliseconds();

        // Encode 48-bit timestamp (big-endian, first 6 bytes)
        bytes[0] = (byte)(ms >> 40);
        bytes[1] = (byte)(ms >> 32);
        bytes[2] = (byte)(ms >> 24);
        bytes[3] = (byte)(ms >> 16);
        bytes[4] = (byte)(ms >> 8);
        bytes[5] = (byte)ms;

        // Fill remaining 10 bytes with cryptographic randomness
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes[6..]);

        return new UlidId(bytes);
    }

    /// <summary>
    /// Gets the timestamp encoded in this ULID.
    /// </summary>
    /// <returns>The <see cref="DateTimeOffset"/> encoded in the first 48 bits.</returns>
    public DateTimeOffset GetTimestamp()
    {
        long ms = ((long)Bytes[0] << 40)
                 | ((long)Bytes[1] << 32)
                 | ((long)Bytes[2] << 24)
                 | ((long)Bytes[3] << 16)
                 | ((long)Bytes[4] << 8)
                 | Bytes[5];

        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    /// <summary>
    /// Converts this ULID to a <see cref="Guid"/>.
    /// </summary>
    /// <returns>A <see cref="Guid"/> representation of this ULID.</returns>
    public Guid ToGuid() => new(Bytes);

    /// <summary>
    /// Implicitly converts a <see cref="UlidId"/> to a <see cref="Guid"/>.
    /// </summary>
    public static implicit operator Guid(UlidId id) => id.ToGuid();

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="UlidId"/>.
    /// </summary>
    public static implicit operator UlidId(Guid value) => new(value);

    /// <summary>
    /// Parses a Crockford Base32 encoded ULID string.
    /// </summary>
    /// <param name="s">The 26-character Crockford Base32 string.</param>
    /// <returns>The parsed <see cref="UlidId"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid ULID string.</exception>
    public static UlidId Parse(string s)
    {
        if (!TryParse(s, out var result))
        {
            throw new FormatException($"'{s}' is not a valid Crockford Base32 ULID string.");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a Crockford Base32 encoded ULID string.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed ULID if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out UlidId result)
    {
        if (s is null || s.Length != StringLength)
        {
            result = default;
            return false;
        }

        Span<byte> bytes = stackalloc byte[16];

        // Decode 26 Crockford Base32 characters into 16 bytes (128 bits)
        // Each character represents 5 bits: 26 * 5 = 130 bits (2 bits padding)
        var upperInput = s.AsSpan();
        Span<byte> values = stackalloc byte[26];

        for (var i = 0; i < 26; i++)
        {
            var c = char.ToUpperInvariant(upperInput[i]);
            if (c > 127 || CrockfordBase32Decode[c] == 0xFF)
            {
                result = default;
                return false;
            }

            values[i] = CrockfordBase32Decode[c];
        }

        // Decode timestamp (first 10 chars = 50 bits, use 48)
        bytes[0] = (byte)((values[0] << 5) | values[1]);
        bytes[1] = (byte)((values[2] << 3) | (values[3] >> 2));
        bytes[2] = (byte)((values[3] << 6) | (values[4] << 1) | (values[5] >> 4));
        bytes[3] = (byte)((values[5] << 4) | (values[6] >> 1));
        bytes[4] = (byte)((values[6] << 7) | (values[7] << 2) | (values[8] >> 3));
        bytes[5] = (byte)((values[8] << 5) | values[9]);

        // Decode randomness (next 16 chars = 80 bits)
        bytes[6] = (byte)((values[10] << 3) | (values[11] >> 2));
        bytes[7] = (byte)((values[11] << 6) | (values[12] << 1) | (values[13] >> 4));
        bytes[8] = (byte)((values[13] << 4) | (values[14] >> 1));
        bytes[9] = (byte)((values[14] << 7) | (values[15] << 2) | (values[16] >> 3));
        bytes[10] = (byte)((values[16] << 5) | values[17]);
        bytes[11] = (byte)((values[18] << 3) | (values[19] >> 2));
        bytes[12] = (byte)((values[19] << 6) | (values[20] << 1) | (values[21] >> 4));
        bytes[13] = (byte)((values[21] << 4) | (values[22] >> 1));
        bytes[14] = (byte)((values[22] << 7) | (values[23] << 2) | (values[24] >> 3));
        bytes[15] = (byte)((values[24] << 5) | values[25]);

        result = new UlidId(bytes);
        return true;
    }

    /// <summary>
    /// Tries to parse a string, returning an <see cref="Either{EncinaError, UlidId}"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>Right with the parsed ID; Left with a parse failure error.</returns>
    public static Either<EncinaError, UlidId> TryParseEither(string? s)
    {
        if (TryParse(s, out var result))
        {
            return result;
        }

        return IdGenerationErrors.IdParseFailure(s, nameof(UlidId));
    }

    /// <inheritdoc />
    public bool Equals(UlidId other) => Bytes.AsSpan().SequenceEqual(other.Bytes);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Use first 8 bytes as a long for efficient hashing
        return HashCode.Combine(
            BitConverter.ToInt64(Bytes, 0),
            BitConverter.ToInt64(Bytes, 8));
    }

    /// <inheritdoc />
    public int CompareTo(UlidId other) => Bytes.AsSpan().SequenceCompareTo(other.Bytes);

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        UlidId other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(UlidId)}.", nameof(obj))
    };

    /// <summary>
    /// Determines whether the left operand is less than the right operand.
    /// </summary>
    public static bool operator <(UlidId left, UlidId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left operand is less than or equal to the right operand.
    /// </summary>
    public static bool operator <=(UlidId left, UlidId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left operand is greater than the right operand.
    /// </summary>
    public static bool operator >(UlidId left, UlidId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left operand is greater than or equal to the right operand.
    /// </summary>
    public static bool operator >=(UlidId left, UlidId right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Returns the Crockford Base32 encoded string representation of this ULID.
    /// </summary>
    /// <returns>A 26-character Crockford Base32 string.</returns>
    public override string ToString()
    {
        Span<char> chars = stackalloc char[26];

        // Encode timestamp (6 bytes = 48 bits → 10 chars)
        chars[0] = CrockfordBase32Chars[(Bytes[0] & 224) >> 5];
        chars[1] = CrockfordBase32Chars[Bytes[0] & 31];
        chars[2] = CrockfordBase32Chars[(Bytes[1] & 248) >> 3];
        chars[3] = CrockfordBase32Chars[((Bytes[1] & 7) << 2) | ((Bytes[2] & 192) >> 6)];
        chars[4] = CrockfordBase32Chars[(Bytes[2] & 62) >> 1];
        chars[5] = CrockfordBase32Chars[((Bytes[2] & 1) << 4) | ((Bytes[3] & 240) >> 4)];
        chars[6] = CrockfordBase32Chars[((Bytes[3] & 15) << 1) | ((Bytes[4] & 128) >> 7)];
        chars[7] = CrockfordBase32Chars[(Bytes[4] & 124) >> 2];
        chars[8] = CrockfordBase32Chars[((Bytes[4] & 3) << 3) | ((Bytes[5] & 224) >> 5)];
        chars[9] = CrockfordBase32Chars[Bytes[5] & 31];

        // Encode randomness (10 bytes = 80 bits → 16 chars)
        chars[10] = CrockfordBase32Chars[(Bytes[6] & 248) >> 3];
        chars[11] = CrockfordBase32Chars[((Bytes[6] & 7) << 2) | ((Bytes[7] & 192) >> 6)];
        chars[12] = CrockfordBase32Chars[(Bytes[7] & 62) >> 1];
        chars[13] = CrockfordBase32Chars[((Bytes[7] & 1) << 4) | ((Bytes[8] & 240) >> 4)];
        chars[14] = CrockfordBase32Chars[((Bytes[8] & 15) << 1) | ((Bytes[9] & 128) >> 7)];
        chars[15] = CrockfordBase32Chars[(Bytes[9] & 124) >> 2];
        chars[16] = CrockfordBase32Chars[((Bytes[9] & 3) << 3) | ((Bytes[10] & 224) >> 5)];
        chars[17] = CrockfordBase32Chars[Bytes[10] & 31];
        chars[18] = CrockfordBase32Chars[(Bytes[11] & 248) >> 3];
        chars[19] = CrockfordBase32Chars[((Bytes[11] & 7) << 2) | ((Bytes[12] & 192) >> 6)];
        chars[20] = CrockfordBase32Chars[(Bytes[12] & 62) >> 1];
        chars[21] = CrockfordBase32Chars[((Bytes[12] & 1) << 4) | ((Bytes[13] & 240) >> 4)];
        chars[22] = CrockfordBase32Chars[((Bytes[13] & 15) << 1) | ((Bytes[14] & 128) >> 7)];
        chars[23] = CrockfordBase32Chars[(Bytes[14] & 124) >> 2];
        chars[24] = CrockfordBase32Chars[((Bytes[14] & 3) << 3) | ((Bytes[15] & 224) >> 5)];
        chars[25] = CrockfordBase32Chars[Bytes[15] & 31];

        return new string(chars);
    }

    private static byte[] CreateDecodingTable()
    {
        var table = new byte[128];
        Array.Fill(table, (byte)0xFF);

        for (byte i = 0; i < CrockfordBase32Chars.Length; i++)
        {
            table[CrockfordBase32Chars[i]] = i;

            // Crockford Base32 case-insensitive
            var lower = char.ToLowerInvariant(CrockfordBase32Chars[i]);
            if (lower < 128)
            {
                table[lower] = i;
            }
        }

        // Crockford Base32 aliases: O → 0, I/L → 1
        table['O'] = 0;
        table['o'] = 0;
        table['I'] = 1;
        table['i'] = 1;
        table['L'] = 1;
        table['l'] = 1;

        return table;
    }
}
