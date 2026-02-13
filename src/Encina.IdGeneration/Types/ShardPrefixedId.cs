using System.Diagnostics.CodeAnalysis;
using LanguageExt;

namespace Encina.IdGeneration;

/// <summary>
/// A strongly-typed string-based ID with an embedded shard prefix, formatted as
/// <c>{shardId}-{sequence}</c> or <c>{shardId}-{ulid}</c>.
/// </summary>
/// <remarks>
/// <para>
/// Shard-prefixed IDs provide human-readable shard routing information directly in
/// the ID string. The shard prefix is separated from the unique portion by a hyphen
/// delimiter, enabling simple string parsing for shard extraction.
/// </para>
/// <para>
/// This format is commonly used in systems where ID readability and easy shard
/// identification are preferred over compact binary representation.
/// </para>
/// <para>
/// The ID is lexicographically sortable within a shard when the sequence portion
/// is time-ordered (e.g., ULID or timestamp-based sequence).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var id = new ShardPrefixedId("shard-01", "01ARZ3NDEKTSV4RRFFQ69G5FAV");
/// string shardId = id.ShardId;   // "shard-01"
/// string sequence = id.Sequence; // "01ARZ3NDEKTSV4RRFFQ69G5FAV"
/// string full = id.ToString();   // "shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV"
///
/// // Parse from string
/// var parsed = ShardPrefixedId.Parse("shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV");
/// </code>
/// </example>
public readonly record struct ShardPrefixedId : IEquatable<ShardPrefixedId>, IComparable<ShardPrefixedId>, IComparable
{
    /// <summary>
    /// The delimiter separating the shard prefix from the sequence portion.
    /// </summary>
    public const char Delimiter = ':';

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardPrefixedId"/> struct.
    /// </summary>
    /// <param name="shardId">The shard identifier prefix.</param>
    /// <param name="sequence">The unique sequence portion of the ID.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="shardId"/> or <paramref name="sequence"/> is null or whitespace,
    /// or when <paramref name="shardId"/> contains the delimiter character.
    /// </exception>
    public ShardPrefixedId(string shardId, string sequence)
    {
        if (string.IsNullOrWhiteSpace(shardId))
        {
            throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(shardId));
        }

        if (shardId.Contains(Delimiter))
        {
            throw new ArgumentException($"Shard ID cannot contain the delimiter character '{Delimiter}'.", nameof(shardId));
        }

        if (string.IsNullOrWhiteSpace(sequence))
        {
            throw new ArgumentException("Sequence cannot be null or whitespace.", nameof(sequence));
        }

        ShardId = shardId;
        Sequence = sequence;
    }

    /// <summary>
    /// Gets the shard identifier prefix.
    /// </summary>
    public string ShardId { get; } = string.Empty;

    /// <summary>
    /// Gets the unique sequence portion of the ID.
    /// </summary>
    public string Sequence { get; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this ID represents the default (empty) value.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(ShardId) && string.IsNullOrEmpty(Sequence);

    /// <summary>
    /// Gets the empty (default) shard-prefixed ID.
    /// </summary>
    public static ShardPrefixedId Empty => default;

    /// <summary>
    /// Implicitly converts a <see cref="ShardPrefixedId"/> to a <see cref="string"/>.
    /// </summary>
    public static implicit operator string(ShardPrefixedId id) => id.ToString();

    /// <summary>
    /// Parses a shard-prefixed ID string.
    /// </summary>
    /// <param name="s">
    /// The string to parse, in the format <c>{shardId}:{sequence}</c>.
    /// </param>
    /// <returns>The parsed <see cref="ShardPrefixedId"/>.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s"/> does not contain exactly one delimiter.
    /// </exception>
    public static ShardPrefixedId Parse(string s)
    {
        if (!TryParse(s, out var result))
        {
            throw new FormatException(
                $"'{s}' is not a valid shard-prefixed ID. Expected format: '{{shardId}}{Delimiter}{{sequence}}'.");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a shard-prefixed ID string.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed ID if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out ShardPrefixedId result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        var delimiterIndex = s.IndexOf(Delimiter);
        if (delimiterIndex <= 0 || delimiterIndex == s.Length - 1)
        {
            result = default;
            return false;
        }

        // Ensure only one delimiter
        if (s.IndexOf(Delimiter, delimiterIndex + 1) >= 0)
        {
            result = default;
            return false;
        }

        var shardId = s[..delimiterIndex];
        var sequence = s[(delimiterIndex + 1)..];

        if (string.IsNullOrWhiteSpace(shardId) || string.IsNullOrWhiteSpace(sequence))
        {
            result = default;
            return false;
        }

        result = new ShardPrefixedId(shardId, sequence);
        return true;
    }

    /// <summary>
    /// Tries to parse a string, returning an <see cref="Either{EncinaError, ShardPrefixedId}"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>Right with the parsed ID; Left with a parse failure error.</returns>
    public static Either<EncinaError, ShardPrefixedId> TryParseEither(string? s)
    {
        if (TryParse(s, out var result))
        {
            return result;
        }

        return IdGenerationErrors.IdParseFailure(s, nameof(ShardPrefixedId));
    }

    /// <inheritdoc />
    public bool Equals(ShardPrefixedId other)
        => string.Equals(ShardId, other.ShardId, StringComparison.Ordinal)
        && string.Equals(Sequence, other.Sequence, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        ShardId.GetHashCode(StringComparison.Ordinal),
        Sequence.GetHashCode(StringComparison.Ordinal));

    /// <inheritdoc />
    public int CompareTo(ShardPrefixedId other)
    {
        var shardComparison = string.Compare(ShardId, other.ShardId, StringComparison.Ordinal);
        return shardComparison != 0
            ? shardComparison
            : string.Compare(Sequence, other.Sequence, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        ShardPrefixedId other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(ShardPrefixedId)}.", nameof(obj))
    };

    /// <summary>
    /// Determines whether the left operand is less than the right operand.
    /// </summary>
    public static bool operator <(ShardPrefixedId left, ShardPrefixedId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left operand is less than or equal to the right operand.
    /// </summary>
    public static bool operator <=(ShardPrefixedId left, ShardPrefixedId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left operand is greater than the right operand.
    /// </summary>
    public static bool operator >(ShardPrefixedId left, ShardPrefixedId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left operand is greater than or equal to the right operand.
    /// </summary>
    public static bool operator >=(ShardPrefixedId left, ShardPrefixedId right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Returns the full shard-prefixed ID string in the format <c>{shardId}:{sequence}</c>.
    /// </summary>
    /// <returns>The full ID string.</returns>
    public override string ToString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        return $"{ShardId}{Delimiter}{Sequence}";
    }
}
