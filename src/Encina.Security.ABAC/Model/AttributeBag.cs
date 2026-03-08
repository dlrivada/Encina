namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 attribute bag — a multi-valued collection of <see cref="AttributeValue"/>
/// instances for a single attribute.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.3.2 — In XACML, every attribute is inherently multi-valued (a "bag").
/// A subject may have multiple roles, a resource may belong to multiple categories,
/// etc. The bag is the fundamental unit of attribute storage and is used by bag functions
/// (e.g., <c>*-one-and-only</c>, <c>*-bag-size</c>, <c>*-is-in</c>) and set functions
/// (e.g., <c>*-intersection</c>, <c>*-union</c>, <c>*-subset</c>).
/// </para>
/// <para>
/// This is a sealed class (not a record) because it wraps an <see cref="IReadOnlyList{T}"/>
/// and provides bag-specific operations beyond simple value equality.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Single-valued attribute bag
/// var singleBag = AttributeBag.Of(new AttributeValue { DataType = "string", Value = "Finance" });
///
/// // Multi-valued attribute bag (user has multiple roles)
/// var multiBag = AttributeBag.Of(
///     new AttributeValue { DataType = "string", Value = "Admin" },
///     new AttributeValue { DataType = "string", Value = "Manager" }
/// );
///
/// // Empty bag (attribute not found)
/// var emptyBag = AttributeBag.Empty;
/// </code>
/// </example>
public sealed class AttributeBag
{
    private readonly IReadOnlyList<AttributeValue> _values;

    private AttributeBag(IReadOnlyList<AttributeValue> values)
    {
        _values = values;
    }

    /// <summary>
    /// Gets an empty attribute bag representing an absent or unresolved attribute.
    /// </summary>
    public static AttributeBag Empty { get; } = new([]);

    /// <summary>
    /// Gets the attribute values in this bag.
    /// </summary>
    public IReadOnlyList<AttributeValue> Values => _values;

    /// <summary>
    /// Gets the number of values in this bag.
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    /// Gets a value indicating whether this bag is empty (contains no values).
    /// </summary>
    public bool IsEmpty => _values.Count == 0;

    /// <summary>
    /// Creates a new attribute bag containing the specified values.
    /// </summary>
    /// <param name="values">The attribute values to include in the bag.</param>
    /// <returns>A new <see cref="AttributeBag"/> containing the specified values.</returns>
    public static AttributeBag Of(params AttributeValue[] values) =>
        values.Length == 0 ? Empty : new AttributeBag(values);

    /// <summary>
    /// Creates a new attribute bag from a read-only list of values.
    /// </summary>
    /// <param name="values">The attribute values to include in the bag.</param>
    /// <returns>A new <see cref="AttributeBag"/> containing the specified values.</returns>
    public static AttributeBag FromValues(IReadOnlyList<AttributeValue> values) =>
        values.Count == 0 ? Empty : new AttributeBag(values);

    /// <summary>
    /// Returns the single value in the bag, or throws if the bag does not contain exactly one value.
    /// </summary>
    /// <returns>The single <see cref="AttributeValue"/> in the bag.</returns>
    /// <exception cref="InvalidOperationException">The bag does not contain exactly one value.</exception>
    /// <remarks>Implements the XACML 3.0 <c>*-one-and-only</c> bag function semantics.</remarks>
    public AttributeValue SingleValue()
    {
        if (_values.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one value in the attribute bag, but found {_values.Count}.");
        }

        return _values[0];
    }
}
