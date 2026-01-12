using Encina.Testing.Fakes.Stores;
using LanguageExt;
using Shouldly;

namespace Encina.Testing.Modules;

/// <summary>
/// A test context for module test results, providing fluent assertion methods.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This context wraps the result of a module operation and provides chainable
/// assertion methods for verifying success, errors, and messaging patterns.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await fixture.SendAsync(new PlaceOrderCommand(...));
///
/// // Use the And-constraint helper to run assertions on the success value,
/// // then perform assertion(s) on the fixture (outbox, integration events, etc.).
/// result.ShouldSucceedAnd().ShouldSatisfy(r => r.OrderId.ShouldNotBeEmpty());
/// result.OutboxShouldContain&lt;OrderPlacedEvent&gt;();
/// </code>
/// </example>
public sealed class ModuleTestContext<TResponse>
{
    private readonly Either<EncinaError, TResponse> _result;
    private readonly IModuleTestFixtureContext _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleTestContext{TResponse}"/> class.
    /// </summary>
    /// <param name="result">The result to wrap.</param>
    /// <param name="fixture">The parent fixture.</param>
    internal ModuleTestContext(Either<EncinaError, TResponse> result, IModuleTestFixtureContext fixture)
    {
        _result = result;
        _fixture = fixture;
    }

    /// <summary>
    /// Gets the raw Either result.
    /// </summary>
    public Either<EncinaError, TResponse> Result => _result;

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess => _result.IsRight;

    /// <summary>
    /// Gets a value indicating whether the result is an error.
    /// </summary>
    public bool IsError => _result.IsLeft;

    /// <summary>
    /// Gets the success value if the result is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is an error.</exception>
    public TResponse Value =>
        _result.Match(
            Right: v => v,
            Left: e => throw new InvalidOperationException(
                $"Cannot get value from error result: {e.Message}"));

    /// <summary>
    /// Gets the error if the result is an error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
    public EncinaError Error =>
        _result.Match(
            Right: _ => throw new InvalidOperationException("Cannot get error from success result."),
            Left: e => e);

    #region Success Assertions

    /// <summary>
    /// Asserts that the result is a success.
    /// </summary>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> ShouldSucceed()
    {
        _result.IsRight.ShouldBeTrue(
            _result.Match(
                Right: _ => "Expected success",
                Left: e => $"Expected success but got error: {e.Message}"));
        return this;
    }

    /// <summary>
    /// Asserts that the result is a success and satisfies the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to check.</param>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> ShouldSucceedWith(Func<TResponse, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ShouldSucceed();
        predicate(Value).ShouldBeTrue("The success result did not satisfy the predicate.");
        return this;
    }

    /// <summary>
    /// Asserts that the result is a success and returns an <see cref="AndConstraint{T}"/> for further assertions.
    /// </summary>
    /// <returns>An AndConstraint wrapping the success value.</returns>
    public AndConstraint<TResponse> ShouldSucceedAnd()
    {
        ShouldSucceed();
        return new AndConstraint<TResponse>(Value);
    }

    #endregion

    #region Error Assertions

    /// <summary>
    /// Asserts that the result is an error.
    /// </summary>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> ShouldFail()
    {
        _result.IsLeft.ShouldBeTrue("Expected an error but got success.");
        return this;
    }

    /// <summary>
    /// Asserts that the result is an error with a message containing the specified text.
    /// </summary>
    /// <param name="expectedMessagePart">The expected message part.</param>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> ShouldFailWithMessage(string expectedMessagePart)
    {
        ShouldFail();
        Error.Message.ShouldContain(expectedMessagePart);
        return this;
    }

    /// <summary>
    /// Asserts that the result is an error and returns an <see cref="AndConstraint{T}"/> for further assertions.
    /// </summary>
    /// <returns>An AndConstraint wrapping the error.</returns>
    public AndConstraint<EncinaError> ShouldFailAnd()
    {
        ShouldFail();
        return new AndConstraint<EncinaError>(Error);
    }

    /// <summary>
    /// Asserts that the result is a validation error (message contains "validation").
    /// </summary>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> ShouldBeValidationError()
    {
        ShouldFail();
        Error.Message.ToLowerInvariant().ShouldContain("validation");
        return this;
    }

    #endregion

    #region Outbox Assertions

    /// <summary>
    /// Asserts that the outbox contains a message of the specified type.
    /// </summary>
    /// <typeparam name="TMessage">The expected message type.</typeparam>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> OutboxShouldContain<TMessage>()
    {
        var outbox = GetOutboxStore();
        outbox.WasMessageAdded<TMessage>().ShouldBeTrue(
            $"Expected outbox to contain message of type {typeof(TMessage).Name}");
        return this;
    }

    /// <summary>
    /// Asserts that the outbox is empty.
    /// </summary>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> OutboxShouldBeEmpty()
    {
        var outbox = GetOutboxStore();
        outbox.GetMessages().ShouldBeEmpty("Expected outbox to be empty");
        return this;
    }

    private FakeOutboxStore GetOutboxStore()
    {
        var store = _fixture.Outbox;
        if (store is not null)
        {
            return store;
        }

        throw new InvalidOperationException(
            "Outbox store not configured. Call WithMockedOutbox() on the fixture.");
    }

    #endregion

    #region Integration Event Assertions

    /// <summary>
    /// Asserts that an integration event of the specified type was captured.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> IntegrationEventShouldContain<TEvent>() where TEvent : INotification
    {
        var events = GetIntegrationEvents();
        events.Contains<TEvent>().ShouldBeTrue(
            $"Expected integration event of type {typeof(TEvent).Name} to be captured");
        return this;
    }

    /// <summary>
    /// Asserts that an integration event matching the specified predicate was captured.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> IntegrationEventShouldContain<TEvent>(Func<TEvent, bool> predicate)
        where TEvent : INotification
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var events = GetIntegrationEvents();
        events.Contains(predicate).ShouldBeTrue(
            $"No integration event of type {typeof(TEvent).Name} matched the predicate");
        return this;
    }

    /// <summary>
    /// Asserts that no integration events were captured during the test.
    /// </summary>
    /// <returns>This context for chaining.</returns>
    public ModuleTestContext<TResponse> IntegrationEventsShouldBeEmpty()
    {
        var events = GetIntegrationEvents();
        events.ShouldBeEmpty();
        return this;
    }

    private IntegrationEventCollector GetIntegrationEvents()
    {
        return _fixture.IntegrationEvents;
    }

    #endregion

    /// <summary>
    /// Provides access to the AndConstraint for fluent chaining.
    /// </summary>
    public ModuleTestContext<TResponse> And => this;
}
