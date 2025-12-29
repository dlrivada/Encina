using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Base class for fluent domain builders with ROP support.
/// </summary>
/// <typeparam name="T">The type being built.</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for CRTP).</typeparam>
/// <remarks>
/// <para>
/// Domain builders provide a fluent API for creating domain objects
/// with validation at build time. They use Railway Oriented Programming
/// to return Either on failure instead of throwing exceptions.
/// </para>
/// <example>
/// <code>
/// public sealed class OrderBuilder : DomainBuilder&lt;Order, OrderBuilder&gt;
/// {
///     private CustomerId? _customerId;
///     private readonly List&lt;OrderItem&gt; _items = [];
///
///     protected override OrderBuilder This =&gt; this;
///
///     public OrderBuilder ForCustomer(CustomerId customerId)
///     {
///         _customerId = customerId;
///         return this;
///     }
///
///     public OrderBuilder WithItem(ProductId product, int quantity, Money price)
///     {
///         _items.Add(new OrderItem(product, quantity, price));
///         return this;
///     }
///
///     public override Either&lt;DomainBuilderError, Order&gt; Build()
///     {
///         if (_customerId is null)
///             return DomainBuilderError.MissingValue("CustomerId");
///         if (_items.Count == 0)
///             return DomainBuilderError.ValidationFailed("At least one item is required");
///
///         return new Order(_customerId, _items);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class DomainBuilder<T, TBuilder>
    where TBuilder : DomainBuilder<T, TBuilder>
{
    /// <summary>
    /// Returns this builder instance (for fluent chaining).
    /// </summary>
    protected abstract TBuilder This { get; }

    /// <summary>
    /// Builds the domain object.
    /// </summary>
    /// <returns>Either an error or the built object.</returns>
    public abstract Either<DomainBuilderError, T> Build();

    /// <summary>
    /// Builds and throws if validation fails.
    /// </summary>
    /// <returns>The built object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when build fails.</exception>
    public T BuildOrThrow()
    {
        return Build().Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message));
    }

    /// <summary>
    /// Builds and returns None if validation fails.
    /// </summary>
    /// <returns>Some(T) on success, None on failure.</returns>
    public Option<T> TryBuild()
    {
        return Build().Match(
            Right: value => Option<T>.Some(value),
            Left: _ => Option<T>.None);
    }
}

/// <summary>
/// Builder for aggregates with business rule validation.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
/// <typeparam name="TId">The aggregate ID type.</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for CRTP).</typeparam>
/// <remarks>
/// <para>
/// Aggregate builders extend domain builders with business rule support.
/// Rules are accumulated and validated before creating the aggregate.
/// </para>
/// </remarks>
public abstract class AggregateBuilder<TAggregate, TId, TBuilder>
    : DomainBuilder<TAggregate, TBuilder>
    where TAggregate : IAggregateRoot<TId>
    where TId : notnull
    where TBuilder : AggregateBuilder<TAggregate, TId, TBuilder>
{
    private readonly List<IBusinessRule> _rules = [];

    /// <summary>
    /// Adds a business rule to validate during build.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    /// <returns>This builder for fluent chaining.</returns>
    protected TBuilder AddRule(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return This;
    }

    /// <summary>
    /// Adds a business rule conditionally.
    /// </summary>
    /// <param name="condition">Whether to add the rule.</param>
    /// <param name="rule">The rule to add.</param>
    /// <returns>This builder for fluent chaining.</returns>
    protected TBuilder AddRuleWhen(bool condition, IBusinessRule rule)
    {
        if (condition)
        {
            ArgumentNullException.ThrowIfNull(rule);
            _rules.Add(rule);
        }
        return This;
    }

    /// <summary>
    /// Adds a business rule using a factory (lazy evaluation).
    /// </summary>
    /// <param name="condition">Whether to add the rule.</param>
    /// <param name="ruleFactory">Factory to create the rule.</param>
    /// <returns>This builder for fluent chaining.</returns>
    protected TBuilder AddRuleWhen(bool condition, Func<IBusinessRule> ruleFactory)
    {
        if (condition)
        {
            ArgumentNullException.ThrowIfNull(ruleFactory);
            _rules.Add(ruleFactory());
        }
        return This;
    }

    /// <inheritdoc />
    public override Either<DomainBuilderError, TAggregate> Build()
    {
        var ruleCheck = _rules.CheckAll();
        if (ruleCheck.IsLeft)
        {
            var errors = ruleCheck.Match(
                Right: _ => [],
                Left: agg => agg.Errors.Select(e => e.ErrorMessage).ToList());
            return DomainBuilderError.BusinessRulesViolated(errors);
        }

        return CreateAggregate();
    }

    /// <summary>
    /// Creates the aggregate after rules are validated.
    /// </summary>
    /// <returns>Either an error or the created aggregate.</returns>
    protected abstract Either<DomainBuilderError, TAggregate> CreateAggregate();
}

/// <summary>
/// Error type for domain builder failures.
/// </summary>
/// <param name="Message">Description of the failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="Details">Optional additional details.</param>
public sealed record DomainBuilderError(
    string Message,
    string ErrorCode,
    IReadOnlyList<string>? Details = null)
{
    /// <summary>
    /// Creates an error for a missing required value.
    /// </summary>
    public static DomainBuilderError MissingValue(string propertyName) =>
        new($"Required value '{propertyName}' was not provided", "BUILDER_MISSING_VALUE");

    /// <summary>
    /// Creates an error for a validation failure.
    /// </summary>
    public static DomainBuilderError ValidationFailed(string reason) =>
        new($"Validation failed: {reason}", "BUILDER_VALIDATION_FAILED");

    /// <summary>
    /// Creates an error for business rule violations.
    /// </summary>
    public static DomainBuilderError BusinessRulesViolated(IReadOnlyList<string> violations) =>
        new(
            $"Business rules violated: {string.Join("; ", violations)}",
            "BUILDER_RULES_VIOLATED",
            violations);

    /// <summary>
    /// Creates an error for an invalid state.
    /// </summary>
    public static DomainBuilderError InvalidState(string reason) =>
        new($"Invalid builder state: {reason}", "BUILDER_INVALID_STATE");
}

/// <summary>
/// Fluent extension methods for domain-specific language constructs.
/// </summary>
public static class DomainDslExtensions
{
    /// <summary>
    /// Fluent "is" check using a specification.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="specification">The specification to evaluate.</param>
    /// <returns>True if the entity satisfies the specification.</returns>
    /// <example>
    /// <code>
    /// var isHighValue = order.Is(new HighValueOrderSpec(1000));
    /// </code>
    /// </example>
    public static bool Is<T>(this T entity, Specification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(specification);
        return specification.IsSatisfiedBy(entity);
    }

    /// <summary>
    /// Fluent "satisfies" check using a specification (alias for Is).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="specification">The specification to evaluate.</param>
    /// <returns>True if the entity satisfies the specification.</returns>
    public static bool Satisfies<T>(this T entity, Specification<T> specification)
        => entity.Is(specification);

    /// <summary>
    /// Fluent "violates" check (negated specification).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="specification">The specification to evaluate.</param>
    /// <returns>True if the entity does NOT satisfy the specification.</returns>
    public static bool Violates<T>(this T entity, Specification<T> specification)
        => !entity.Is(specification);

    /// <summary>
    /// Fluent "satisfies" check for business rules.
    /// </summary>
    /// <param name="rule">The business rule to check.</param>
    /// <returns>True if the rule is satisfied.</returns>
    public static bool Passes(this IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule.IsSatisfied();
    }

    /// <summary>
    /// Fluent "violates" check for business rules.
    /// </summary>
    /// <param name="rule">The business rule to check.</param>
    /// <returns>True if the rule is violated.</returns>
    public static bool Fails(this IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return !rule.IsSatisfied();
    }

    /// <summary>
    /// Ensures a condition is valid or returns an error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="condition">The condition that must be true.</param>
    /// <param name="errorMessage">The error message if condition is false.</param>
    /// <returns>Right(value) if condition is true; Left(error) otherwise.</returns>
    public static Either<DomainBuilderError, T> EnsureValid<T>(
        this T value,
        Func<T, bool> condition,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return condition(value)
            ? value
            : DomainBuilderError.ValidationFailed(errorMessage);
    }

    /// <summary>
    /// Ensures a value is not null or returns an error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="propertyName">The property name for the error message.</param>
    /// <returns>Right(value) if not null; Left(error) otherwise.</returns>
    public static Either<DomainBuilderError, T> EnsureNotNull<T>(
        this T? value,
        string propertyName)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return value is not null
            ? value
            : DomainBuilderError.MissingValue(propertyName);
    }

    /// <summary>
    /// Ensures a string is not null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="propertyName">The property name for the error message.</param>
    /// <returns>Right(value) if valid; Left(error) otherwise.</returns>
    public static Either<DomainBuilderError, string> EnsureNotEmpty(
        this string? value,
        string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainBuilderError.MissingValue(propertyName);
    }

    /// <summary>
    /// Ensures a collection is not empty.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="propertyName">The property name for the error message.</param>
    /// <returns>Right(collection) if not empty; Left(error) otherwise.</returns>
    public static Either<DomainBuilderError, IReadOnlyList<T>> EnsureNotEmpty<T>(
        this IReadOnlyList<T>? collection,
        string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (collection is { Count: > 0 })
        {
            return Either<DomainBuilderError, IReadOnlyList<T>>.Right(collection);
        }

        return Either<DomainBuilderError, IReadOnlyList<T>>.Left(
            DomainBuilderError.ValidationFailed($"{propertyName} cannot be empty"));
    }
}

/// <summary>
/// Represents a quantity that cannot be negative.
/// </summary>
public readonly struct Quantity : IEquatable<Quantity>, IComparable<Quantity>
{
    /// <summary>
    /// Gets the numeric value of the quantity.
    /// </summary>
    public int Value { get; }

    private Quantity(int value) => Value = value;

    /// <summary>
    /// Creates a new quantity from an integer.
    /// </summary>
    /// <param name="value">The quantity value.</param>
    /// <returns>Either an error or the quantity.</returns>
    public static Either<string, Quantity> Create(int value)
    {
        if (value < 0)
            return "Quantity cannot be negative";
        return new Quantity(value);
    }

    /// <summary>
    /// Creates a quantity without validation (for trusted inputs).
    /// </summary>
    /// <param name="value">The quantity value.</param>
    /// <returns>The quantity.</returns>
    public static Quantity From(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        return new Quantity(value);
    }

    /// <summary>
    /// Gets a zero quantity.
    /// </summary>
    public static Quantity Zero => new(0);

    /// <summary>
    /// Gets a quantity of one.
    /// </summary>
    public static Quantity One => new(1);

    /// <summary>
    /// Adds two quantities.
    /// </summary>
    public Quantity Add(Quantity other) => new(Value + other.Value);

    /// <summary>
    /// Subtracts a quantity (floors at zero).
    /// </summary>
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));

    /// <summary>
    /// Multiplies the quantity by a factor.
    /// </summary>
    public Quantity Multiply(int factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(factor);
        return new Quantity(Value * factor);
    }

    /// <summary>
    /// Checks if this quantity is greater than another.
    /// </summary>
    public bool IsGreaterThan(Quantity other) => Value > other.Value;

    /// <summary>
    /// Checks if this quantity is zero.
    /// </summary>
    public bool IsZero => Value == 0;

    /// <summary>
    /// Checks if this quantity is positive (greater than zero).
    /// </summary>
    public bool IsPositive => Value > 0;

    /// <summary>Addition operator.</summary>
    public static Quantity operator +(Quantity a, Quantity b) => a.Add(b);
    /// <summary>Subtraction operator.</summary>
    public static Quantity operator -(Quantity a, Quantity b) => a.Subtract(b);
    /// <summary>Multiplication operator.</summary>
    public static Quantity operator *(Quantity a, int factor) => a.Multiply(factor);
    /// <summary>Equality operator.</summary>
    public static bool operator ==(Quantity left, Quantity right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Quantity left, Quantity right) => !left.Equals(right);
    /// <summary>Less than operator.</summary>
    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;
    /// <summary>Greater than operator.</summary>
    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;
    /// <summary>Less than or equal operator.</summary>
    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;
    /// <summary>Greater than or equal operator.</summary>
    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public bool Equals(Quantity other) => Value == other.Value;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Quantity other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();
    /// <inheritdoc />
    public int CompareTo(Quantity other) => Value.CompareTo(other.Value);
    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// Represents a percentage value between 0 and 100.
/// </summary>
public readonly struct Percentage : IEquatable<Percentage>, IComparable<Percentage>
{
    /// <summary>
    /// Gets the percentage value (0-100).
    /// </summary>
    public decimal Value { get; }

    private Percentage(decimal value) => Value = value;

    /// <summary>
    /// Creates a new percentage from a decimal value.
    /// </summary>
    /// <param name="value">The percentage value (0-100).</param>
    /// <returns>Either an error or the percentage.</returns>
    public static Either<string, Percentage> Create(decimal value)
    {
        if (value < 0 || value > 100)
            return "Percentage must be between 0 and 100";
        return new Percentage(value);
    }

    /// <summary>
    /// Creates a percentage without validation (for trusted inputs).
    /// </summary>
    /// <param name="value">The percentage value.</param>
    /// <returns>The percentage.</returns>
    public static Percentage From(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
        return new Percentage(value);
    }

    /// <summary>
    /// Gets zero percent.
    /// </summary>
    public static Percentage Zero => new(0);

    /// <summary>
    /// Gets 100 percent.
    /// </summary>
    public static Percentage Full => new(100);

    /// <summary>
    /// Gets 50 percent.
    /// </summary>
    public static Percentage Half => new(50);

    /// <summary>
    /// Applies this percentage to an amount.
    /// </summary>
    /// <param name="amount">The amount to apply the percentage to.</param>
    /// <returns>The calculated value.</returns>
    public decimal ApplyTo(decimal amount) => amount * (Value / 100m);

    /// <summary>
    /// Gets the fractional representation (0-1).
    /// </summary>
    public decimal AsFraction => Value / 100m;

    /// <summary>
    /// Gets the complement (100 - value).
    /// </summary>
    public Percentage Complement => new(100 - Value);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Percentage left, Percentage right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Percentage left, Percentage right) => !left.Equals(right);
    /// <summary>Less than operator.</summary>
    public static bool operator <(Percentage left, Percentage right) => left.CompareTo(right) < 0;
    /// <summary>Greater than operator.</summary>
    public static bool operator >(Percentage left, Percentage right) => left.CompareTo(right) > 0;
    /// <summary>Less than or equal operator.</summary>
    public static bool operator <=(Percentage left, Percentage right) => left.CompareTo(right) <= 0;
    /// <summary>Greater than or equal operator.</summary>
    public static bool operator >=(Percentage left, Percentage right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public bool Equals(Percentage other) => Value == other.Value;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Percentage other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();
    /// <inheritdoc />
    public int CompareTo(Percentage other) => Value.CompareTo(other.Value);
    /// <inheritdoc />
    public override string ToString() => $"{Value}%";
}

/// <summary>
/// Represents a date range with start and end dates.
/// </summary>
public readonly struct DateRange : IEquatable<DateRange>
{
    /// <summary>
    /// Gets the start date.
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets the end date.
    /// </summary>
    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a new date range.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>Either an error or the date range.</returns>
    public static Either<string, DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
            return "End date cannot be before start date";
        return new DateRange(start, end);
    }

    /// <summary>
    /// Creates a date range without validation (for trusted inputs).
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>The date range.</returns>
    public static DateRange From(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException("End date cannot be before start date");
        return new DateRange(start, end);
    }

    /// <summary>
    /// Creates a single-day range.
    /// </summary>
    public static DateRange SingleDay(DateOnly date) => new(date, date);

    /// <summary>
    /// Creates a range of days from start.
    /// </summary>
    public static DateRange Days(DateOnly start, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        return new DateRange(start, start.AddDays(count - 1));
    }

    /// <summary>
    /// Gets the number of days in the range (inclusive).
    /// </summary>
    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    /// <summary>
    /// Checks if a date falls within this range.
    /// </summary>
    public bool Contains(DateOnly date) => date >= Start && date <= End;

    /// <summary>
    /// Checks if this range overlaps with another.
    /// </summary>
    public bool Overlaps(DateRange other) =>
        Start <= other.End && End >= other.Start;

    /// <summary>
    /// Checks if this range fully contains another.
    /// </summary>
    public bool FullyContains(DateRange other) =>
        Start <= other.Start && End >= other.End;

    /// <summary>
    /// Gets the intersection with another range.
    /// </summary>
    public Option<DateRange> Intersect(DateRange other)
    {
        if (!Overlaps(other))
            return Option<DateRange>.None;

        var start = Start > other.Start ? Start : other.Start;
        var end = End < other.End ? End : other.End;
        return Option<DateRange>.Some(new DateRange(start, end));
    }

    /// <summary>
    /// Extends the range by a number of days.
    /// </summary>
    public DateRange ExtendBy(int days) => new(Start, End.AddDays(days));

    /// <summary>Equality operator.</summary>
    public static bool operator ==(DateRange left, DateRange right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(DateRange left, DateRange right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(DateRange other) => Start == other.Start && End == other.End;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DateRange other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Start, End);
    /// <inheritdoc />
    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}

/// <summary>
/// Represents a time range with start and end times.
/// </summary>
public readonly struct TimeRange : IEquatable<TimeRange>
{
    /// <summary>
    /// Gets the start time.
    /// </summary>
    public TimeOnly Start { get; }

    /// <summary>
    /// Gets the end time.
    /// </summary>
    public TimeOnly End { get; }

    private TimeRange(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a new time range.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="end">The end time.</param>
    /// <returns>Either an error or the time range.</returns>
    public static Either<string, TimeRange> Create(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            return "End time must be after start time";
        return new TimeRange(start, end);
    }

    /// <summary>
    /// Creates a time range without validation (for trusted inputs).
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="end">The end time.</param>
    /// <returns>The time range.</returns>
    public static TimeRange From(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            throw new ArgumentException("End time must be after start time");
        return new TimeRange(start, end);
    }

    /// <summary>
    /// Gets the duration of the time range.
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Checks if a time falls within this range.
    /// </summary>
    public bool Contains(TimeOnly time) => time >= Start && time <= End;

    /// <summary>
    /// Checks if this range overlaps with another.
    /// </summary>
    public bool Overlaps(TimeRange other) =>
        Start < other.End && End > other.Start;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(TimeRange left, TimeRange right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(TimeRange left, TimeRange right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(TimeRange other) => Start == other.Start && End == other.End;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TimeRange other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Start, End);
    /// <inheritdoc />
    public override string ToString() => $"{Start:HH:mm} - {End:HH:mm}";
}
