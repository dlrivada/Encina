using System.Diagnostics.CodeAnalysis;

namespace Encina.DomainModeling;

/// <summary>
/// Base class for value objects in Domain-Driven Design.
/// Value objects are immutable and have structural equality - two value objects are equal if all their components are equal.
/// </summary>
/// <remarks>
/// <para>
/// Value objects are one of the tactical patterns in DDD. They represent concepts that are
/// defined by their attributes rather than by identity. Money, Email, Address are common examples.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
///   <item><description>Immutability: Once created, a value object cannot be changed.</description></item>
///   <item><description>Structural equality: Two value objects are equal if all their components are equal.</description></item>
///   <item><description>Self-validation: Value objects validate their invariants in the constructor.</description></item>
///   <item><description>Side-effect free: Methods on value objects should not modify state.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Email : ValueObject
/// {
///     public string Value { get; }
///
///     private Email(string value) => Value = value;
///
///     public static Either&lt;EncinaError, Email&gt; Create(string value)
///     {
///         if (string.IsNullOrWhiteSpace(value))
///             return EncinaErrors.Validation("Email cannot be empty.");
///
///         if (!value.Contains('@'))
///             return EncinaErrors.Validation("Email must contain @.");
///
///         return new Email(value.Trim().ToLowerInvariant());
///     }
///
///     protected override IEnumerable&lt;object?&gt; GetEqualityComponents()
///     {
///         yield return Value;
///     }
/// }
/// </code>
/// </example>
[SuppressMessage("SonarAnalyzer.CSharp", "S4035:Seal class or implement IEqualityComparer",
    Justification = "DDD base class: ValueObject equality is by components, derived types define their components")]
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components that define equality for this value object.
    /// Override this method to specify which properties should be compared for equality.
    /// </summary>
    /// <returns>An enumerable of the components that define equality.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether this value object is equal to another value object.
    /// Two value objects are equal if they have the same type and all their equality components are equal.
    /// </summary>
    /// <param name="other">The value object to compare with.</param>
    /// <returns><c>true</c> if the value objects are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Value objects of different types are never equal
        if (GetType() != other.GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(17, (current, component) =>
                current * 31 + (component?.GetHashCode() ?? 0));
    }

    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    /// <param name="left">The first value object.</param>
    /// <param name="right">The second value object.</param>
    /// <returns><c>true</c> if the value objects are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    /// <param name="left">The first value object.</param>
    /// <param name="right">The second value object.</param>
    /// <returns><c>true</c> if the value objects are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Base class for single-value value objects in Domain-Driven Design.
/// Use this for value objects that wrap a single primitive value (e.g., Email, CustomerId, Money amount).
/// </summary>
/// <typeparam name="TValue">The type of the wrapped value.</typeparam>
/// <remarks>
/// <para>
/// This is a convenience base class for the common case of a value object that wraps a single value.
/// It provides automatic equality based on the wrapped value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Email : SingleValueObject&lt;string&gt;
/// {
///     private Email(string value) : base(value) { }
///
///     public static Either&lt;EncinaError, Email&gt; Create(string value)
///     {
///         if (string.IsNullOrWhiteSpace(value))
///             return EncinaErrors.Validation("Email cannot be empty.");
///
///         if (!value.Contains('@'))
///             return EncinaErrors.Validation("Email must contain @.");
///
///         return new Email(value.Trim().ToLowerInvariant());
///     }
/// }
/// </code>
/// </example>
public abstract class SingleValueObject<TValue> : ValueObject, IComparable<SingleValueObject<TValue>>
    where TValue : notnull
{
    /// <summary>
    /// Gets the wrapped value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleValueObject{TValue}"/> class.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    protected SingleValueObject(TValue value)
    {
        Value = value;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Compares this value object to another value object.
    /// </summary>
    /// <param name="other">The other value object to compare to.</param>
    /// <returns>A value indicating the relative order of the objects.</returns>
    public int CompareTo(SingleValueObject<TValue>? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Comparer<TValue>.Default.Compare(Value, other.Value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Implicitly converts the value object to its underlying value.
    /// </summary>
    /// <param name="valueObject">The value object to convert.</param>
    public static implicit operator TValue(SingleValueObject<TValue> valueObject)
    {
        ArgumentNullException.ThrowIfNull(valueObject);
        return valueObject.Value;
    }
}
