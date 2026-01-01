namespace Encina.Testing;

/// <summary>
/// Provides fluent assertion chaining support for Either results.
/// Similar to FluentAssertions' AndConstraint pattern, this allows chaining multiple assertions.
/// </summary>
/// <typeparam name="T">The type of the value being asserted.</typeparam>
/// <example>
/// <code>
/// result.ShouldBeSuccess()
///     .And.Value.ShouldBe(expected)
///     .And.ShouldSatisfy(v => v.Name.ShouldNotBeEmpty());
/// </code>
/// </example>
public sealed class AndConstraint<T>
{
    /// <summary>
    /// Gets the value extracted from the Either result.
    /// </summary>
    /// <remarks>
    /// For success assertions, this is the Right value.
    /// For error assertions, this is the Left value (error).
    /// </remarks>
    public T Value { get; }

    /// <summary>
    /// Gets this constraint for fluent chaining.
    /// </summary>
    /// <example>
    /// <code>
    /// result.ShouldBeSuccess()
    ///     .And.ShouldSatisfy(v => v.Id.ShouldBePositive());
    /// </code>
    /// </example>
    public AndConstraint<T> And => this;

    /// <summary>
    /// Initializes a new instance of the <see cref="AndConstraint{T}"/> class.
    /// </summary>
    /// <param name="value">The value to wrap for chained assertions.</param>
    public AndConstraint(T value)
    {
        Value = value;
    }

    /// <summary>
    /// Executes a custom assertion on the value.
    /// </summary>
    /// <param name="assertion">The assertion action to execute on the value.</param>
    /// <returns>This constraint for further chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assertion"/> is null.</exception>
    /// <example>
    /// <code>
    /// result.ShouldBeSuccess()
    ///     .ShouldSatisfy(order =>
    ///     {
    ///         order.Id.ShouldBePositive();
    ///         order.Items.ShouldNotBeEmpty();
    ///     });
    /// </code>
    /// </example>
    public AndConstraint<T> ShouldSatisfy(Action<T> assertion)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        assertion(Value);
        return this;
    }

    /// <summary>
    /// Allows implicit conversion to the underlying value type.
    /// </summary>
    /// <param name="constraint">The constraint to convert.</param>
    /// <returns>The underlying value.</returns>
    /// <example>
    /// <code>
    /// Order order = result.ShouldBeSuccess();
    /// </code>
    /// </example>
    public static implicit operator T(AndConstraint<T> constraint)
    {
        return constraint.Value;
    }
}

/// <summary>
/// Provides async fluent assertion chaining support for Either results.
/// </summary>
/// <typeparam name="T">The type of the value being asserted.</typeparam>
public sealed class AndConstraintAsync<T>
{
    private readonly Task<T> _valueTask;

    /// <summary>
    /// Gets the value extracted from the async Either result.
    /// </summary>
    public Task<T> Value => _valueTask;

    /// <summary>
    /// Gets this constraint for fluent chaining.
    /// </summary>
    public AndConstraintAsync<T> And => this;

    /// <summary>
    /// Initializes a new instance of the <see cref="AndConstraintAsync{T}"/> class.
    /// </summary>
    /// <param name="valueTask">The task containing the value to wrap for chained assertions.</param>
    public AndConstraintAsync(Task<T> valueTask)
    {
        _valueTask = valueTask;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AndConstraintAsync{T}"/> class with a completed value.
    /// </summary>
    /// <param name="value">The value to wrap for chained assertions.</param>
    public AndConstraintAsync(T value)
    {
        _valueTask = Task.FromResult(value);
    }

    /// <summary>
    /// Executes a custom async assertion on the value.
    /// </summary>
    /// <param name="assertion">The async assertion action to execute on the value.</param>
    /// <returns>This constraint for further chaining.</returns>
    public async Task<AndConstraint<T>> ShouldSatisfyAsync(Func<T, Task> assertion)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        var value = await _valueTask;
        await assertion(value);
        return new AndConstraint<T>(value);
    }

    /// <summary>
    /// Executes a custom synchronous assertion on the value.
    /// </summary>
    /// <param name="assertion">The assertion action to execute on the value.</param>
    /// <returns>This constraint for further chaining.</returns>
    public async Task<AndConstraint<T>> ShouldSatisfy(Action<T> assertion)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        var value = await _valueTask;
        assertion(value);
        return new AndConstraint<T>(value);
    }

    /// <summary>
    /// Awaits the value and returns a synchronous <see cref="AndConstraint{T}"/>.
    /// </summary>
    /// <returns>A synchronous constraint wrapping the awaited value.</returns>
    public async Task<AndConstraint<T>> AsSync()
    {
        var value = await _valueTask;
        return new AndConstraint<T>(value);
    }
}
