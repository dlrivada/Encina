using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for strongly-typed identifiers.
/// </summary>
public interface IStronglyTypedId
{
    /// <summary>
    /// Gets the underlying value of the identifier as an object.
    /// </summary>
    object ValueAsObject { get; }
}

/// <summary>
/// Marker interface for strongly-typed identifiers with a specific underlying type.
/// </summary>
/// <typeparam name="TValue">The underlying primitive type of the identifier.</typeparam>
public interface IStronglyTypedId<out TValue> : IStronglyTypedId
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying value of the identifier.
    /// </summary>
    TValue Value { get; }
}

/// <summary>
/// Base class for strongly-typed identifiers in Domain-Driven Design.
/// Strongly-typed IDs prevent accidental mixing of IDs from different entity types.
/// </summary>
/// <typeparam name="TValue">The underlying primitive type of the identifier (e.g., Guid, int, string).</typeparam>
/// <remarks>
/// <para>
/// Strongly-typed IDs provide compile-time safety against mixing identifiers from different entities.
/// Instead of using <c>Guid</c> everywhere, you use <c>OrderId</c>, <c>CustomerId</c>, etc.
/// </para>
/// <para>
/// Benefits:
/// <list type="bullet">
///   <item><description>Compile-time safety: Cannot pass an OrderId where a CustomerId is expected.</description></item>
///   <item><description>Self-documenting: Method signatures show the expected ID type.</description></item>
///   <item><description>Encapsulation: ID generation logic can be encapsulated.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderId : StronglyTypedId&lt;Guid&gt;
/// {
///     public OrderId(Guid value) : base(value) { }
///
///     public static OrderId New() => new(Guid.NewGuid());
/// }
///
/// public sealed class CustomerId : StronglyTypedId&lt;Guid&gt;
/// {
///     public CustomerId(Guid value) : base(value) { }
///
///     public static CustomerId New() => new(Guid.NewGuid());
/// }
///
/// // Now the compiler prevents mixing IDs:
/// public void ProcessOrder(OrderId orderId, CustomerId customerId) { }
/// // processOrder(customerId, orderId) won't compile!
/// </code>
/// </example>
public abstract class StronglyTypedId<TValue> : IStronglyTypedId<TValue>, IEquatable<StronglyTypedId<TValue>>, IComparable<StronglyTypedId<TValue>>
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying value of the identifier.
    /// </summary>
    public TValue Value { get; }

    /// <inheritdoc />
    object IStronglyTypedId.ValueAsObject => Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="StronglyTypedId{TValue}"/> class.
    /// </summary>
    /// <param name="value">The underlying value of the identifier.</param>
    protected StronglyTypedId(TValue value)
    {
        Value = value;
    }

    /// <summary>
    /// Determines whether this strongly-typed ID is equal to another.
    /// Two strongly-typed IDs are equal if they have the same type and the same underlying value.
    /// </summary>
    /// <param name="other">The strongly-typed ID to compare with.</param>
    /// <returns><c>true</c> if the IDs are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(StronglyTypedId<TValue>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // IDs of different types are never equal (OrderId != CustomerId even if same Guid)
        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is StronglyTypedId<TValue> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Include type in hash to differentiate between different ID types with same value
        return HashCode.Combine(GetType(), Value);
    }

    /// <summary>
    /// Compares this strongly-typed ID to another.
    /// </summary>
    /// <param name="other">The other strongly-typed ID to compare to.</param>
    /// <returns>A value indicating the relative order of the IDs.</returns>
    public int CompareTo(StronglyTypedId<TValue>? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Comparer<TValue>.Default.Compare(Value, other.Value);
    }

    /// <summary>
    /// Determines whether two strongly-typed IDs are equal.
    /// </summary>
    /// <param name="left">The first ID.</param>
    /// <param name="right">The second ID.</param>
    /// <returns><c>true</c> if the IDs are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(StronglyTypedId<TValue>? left, StronglyTypedId<TValue>? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two strongly-typed IDs are not equal.
    /// </summary>
    /// <param name="left">The first ID.</param>
    /// <param name="right">The second ID.</param>
    /// <returns><c>true</c> if the IDs are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(StronglyTypedId<TValue>? left, StronglyTypedId<TValue>? right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Implicitly converts the strongly-typed ID to its underlying value.
    /// </summary>
    /// <param name="id">The strongly-typed ID to convert.</param>
    public static implicit operator TValue(StronglyTypedId<TValue> id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return id.Value;
    }
}

/// <summary>
/// Base class for GUID-based strongly-typed identifiers.
/// Provides convenient factory method for creating new IDs.
/// </summary>
/// <typeparam name="TSelf">The derived type (for CRTP pattern).</typeparam>
/// <example>
/// <code>
/// public sealed class OrderId : GuidStronglyTypedId&lt;OrderId&gt;
/// {
///     public OrderId(Guid value) : base(value) { }
/// }
///
/// // Usage:
/// var orderId = OrderId.New();
/// </code>
/// </example>
public abstract class GuidStronglyTypedId<TSelf> : StronglyTypedId<Guid>
    where TSelf : GuidStronglyTypedId<TSelf>
{
    /// <summary>
    /// Initializes a new instance with the specified GUID value.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    protected GuidStronglyTypedId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new instance with a generated GUID value.
    /// Uses <see cref="Guid.NewGuid"/> to generate the value.
    /// </summary>
    /// <returns>A new strongly-typed ID with a unique GUID value.</returns>
    public static TSelf New()
    {
        return (TSelf)Activator.CreateInstance(typeof(TSelf), Guid.NewGuid())!;
    }

    /// <summary>
    /// Creates a new instance from an existing GUID value.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>A new strongly-typed ID with the specified GUID value.</returns>
    public static TSelf From(Guid value)
    {
        return (TSelf)Activator.CreateInstance(typeof(TSelf), value)!;
    }

    /// <summary>
    /// Attempts to parse a string as a GUID and create a strongly-typed ID.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>Some(TSelf) if parsing succeeded; None otherwise.</returns>
    public static Option<TSelf> TryParse(string value)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return From(guid);
        }

        return Option<TSelf>.None;
    }

    /// <summary>
    /// Gets the empty (all zeros) strongly-typed ID.
    /// Useful for representing "no ID" scenarios without using null.
    /// </summary>
    public static TSelf Empty => From(Guid.Empty);
}

/// <summary>
/// Base class for integer-based strongly-typed identifiers.
/// </summary>
/// <typeparam name="TSelf">The derived type (for CRTP pattern).</typeparam>
public abstract class IntStronglyTypedId<TSelf> : StronglyTypedId<int>
    where TSelf : IntStronglyTypedId<TSelf>
{
    /// <summary>
    /// Initializes a new instance with the specified integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    protected IntStronglyTypedId(int value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new instance from an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new strongly-typed ID with the specified integer value.</returns>
    public static TSelf From(int value)
    {
        return (TSelf)Activator.CreateInstance(typeof(TSelf), value)!;
    }

    /// <summary>
    /// Attempts to parse a string as an integer and create a strongly-typed ID.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>Some(TSelf) if parsing succeeded; None otherwise.</returns>
    public static Option<TSelf> TryParse(string value)
    {
        if (int.TryParse(value, out var intValue))
        {
            return From(intValue);
        }

        return Option<TSelf>.None;
    }
}

/// <summary>
/// Base class for long-based strongly-typed identifiers.
/// </summary>
/// <typeparam name="TSelf">The derived type (for CRTP pattern).</typeparam>
public abstract class LongStronglyTypedId<TSelf> : StronglyTypedId<long>
    where TSelf : LongStronglyTypedId<TSelf>
{
    /// <summary>
    /// Initializes a new instance with the specified long value.
    /// </summary>
    /// <param name="value">The long value.</param>
    protected LongStronglyTypedId(long value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new instance from a long value.
    /// </summary>
    /// <param name="value">The long value.</param>
    /// <returns>A new strongly-typed ID with the specified long value.</returns>
    public static TSelf From(long value)
    {
        return (TSelf)Activator.CreateInstance(typeof(TSelf), value)!;
    }

    /// <summary>
    /// Attempts to parse a string as a long and create a strongly-typed ID.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>Some(TSelf) if parsing succeeded; None otherwise.</returns>
    public static Option<TSelf> TryParse(string value)
    {
        if (long.TryParse(value, out var longValue))
        {
            return From(longValue);
        }

        return Option<TSelf>.None;
    }
}

/// <summary>
/// Base class for string-based strongly-typed identifiers.
/// </summary>
/// <typeparam name="TSelf">The derived type (for CRTP pattern).</typeparam>
public abstract class StringStronglyTypedId<TSelf> : StronglyTypedId<string>
    where TSelf : StringStronglyTypedId<TSelf>
{
    /// <summary>
    /// Initializes a new instance with the specified string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    protected StringStronglyTypedId(string value) : base(value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Creates a new instance from a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new strongly-typed ID with the specified string value.</returns>
    public static TSelf From(string value)
    {
        return (TSelf)Activator.CreateInstance(typeof(TSelf), value)!;
    }
}
