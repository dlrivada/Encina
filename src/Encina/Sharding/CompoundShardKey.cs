namespace Encina.Sharding;

/// <summary>
/// Represents a compound shard key composed of one or more ordered components.
/// </summary>
/// <remarks>
/// <para>
/// Compound shard keys enable multi-field routing strategies such as
/// range-on-first-key + hash-on-second-key (e.g., <c>{region, customerId}</c>).
/// Components are ordered: the first component is the primary routing dimension,
/// subsequent components provide secondary routing within the primary partition.
/// </para>
/// <para>
/// For single-component keys, use the implicit conversion from <see cref="string"/>
/// for convenience.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Multi-component key
/// var key = new CompoundShardKey("us-east", "customer-123");
///
/// // Single-component key via implicit conversion
/// CompoundShardKey simple = "customer-123";
///
/// // Serialized form uses pipe delimiter
/// key.ToString(); // "us-east|customer-123"
/// </code>
/// </example>
public sealed class CompoundShardKey : IEquatable<CompoundShardKey>
{
    private const char Delimiter = '|';

    /// <summary>
    /// Initializes a new instance of <see cref="CompoundShardKey"/> with the specified components.
    /// </summary>
    /// <param name="components">
    /// The ordered shard key components. Must contain at least one non-null element.
    /// The first component is the primary routing key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="components"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="components"/> is <see langword="null"/>.
    /// </exception>
    public CompoundShardKey(params string[] components)
    {
        ArgumentNullException.ThrowIfNull(components);

        if (components.Length == 0)
        {
            throw new ArgumentException("Compound shard key must have at least one component.", nameof(components));
        }

        Components = components.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CompoundShardKey"/> with the specified component list.
    /// </summary>
    /// <param name="components">
    /// The ordered shard key components. Must contain at least one non-null element.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="components"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="components"/> is <see langword="null"/>.
    /// </exception>
    public CompoundShardKey(IReadOnlyList<string> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        if (components.Count == 0)
        {
            throw new ArgumentException("Compound shard key must have at least one component.", nameof(components));
        }

        Components = components.ToArray();
    }

    /// <summary>
    /// Gets the ordered list of shard key components.
    /// </summary>
    /// <remarks>
    /// The first component (<see cref="PrimaryComponent"/>) is the primary routing dimension.
    /// Subsequent components provide secondary routing within the primary partition.
    /// </remarks>
    public IReadOnlyList<string> Components { get; }

    /// <summary>
    /// Gets the primary (first) component of the compound key.
    /// </summary>
    /// <remarks>
    /// This is the component used for top-level routing decisions
    /// (e.g., range partitioning by region).
    /// </remarks>
    public string PrimaryComponent => Components[0];

    /// <summary>
    /// Gets a value indicating whether this key has secondary components beyond the primary.
    /// </summary>
    public bool HasSecondaryComponents => Components.Count > 1;

    /// <summary>
    /// Gets the number of components in this compound key.
    /// </summary>
    public int ComponentCount => Components.Count;

    /// <summary>
    /// Implicitly converts a single string value to a single-component <see cref="CompoundShardKey"/>.
    /// </summary>
    /// <param name="singleComponent">The single shard key value.</param>
    public static implicit operator CompoundShardKey(string singleComponent)
        => new(singleComponent);

    /// <summary>
    /// Returns a pipe-delimited string representation of the compound key components.
    /// </summary>
    /// <returns>
    /// A string with components joined by the pipe character (<c>|</c>).
    /// For example, <c>"us-east|customer-123"</c>.
    /// </returns>
    public override string ToString()
        => string.Join(Delimiter, Components);

    /// <inheritdoc />
    public bool Equals(CompoundShardKey? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || Components.Count != other.Components.Count)
        {
            return false;
        }

        for (var index = 0; index < Components.Count; index++)
        {
            if (!string.Equals(Components[index], other.Components[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is CompoundShardKey other && Equals(other);

    /// <summary>
    /// Compares two compound shard keys for value equality.
    /// </summary>
    public static bool operator ==(CompoundShardKey? left, CompoundShardKey? right)
        => Equals(left, right);

    /// <summary>
    /// Compares two compound shard keys for value inequality.
    /// </summary>
    public static bool operator !=(CompoundShardKey? left, CompoundShardKey? right)
        => !Equals(left, right);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var component in Components)
        {
            hash.Add(component, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
