using System.Text.Json;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;
using LanguageExt;

namespace Encina.Testing;

/// <summary>
/// Shared configuration for EncinaTestContext.
/// </summary>
internal static class EncinaTestContextDefaults
{
    /// <summary>
    /// Gets the JSON serializer options used for saga state deserialization.
    /// </summary>
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}

/// <summary>
/// Test context for fluent assertion chaining after sending a request.
/// </summary>
/// <typeparam name="TResponse">The response type from the request.</typeparam>
/// <remarks>
/// <para>
/// This context wraps the Either result and provides access to the test fixture
/// for verifying side effects like outbox messages and saga state.
/// </para>
/// <para>
/// The context supports chaining via the <see cref="And"/> property and can be
/// implicitly converted to <see cref="Either{EncinaError, TResponse}"/> for
/// direct assertion usage.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = await fixture.SendAsync(new CreateOrderCommand(...));
///
/// context
///     .ShouldSucceed()
///     .And.ShouldSatisfy(r => r.OrderId.ShouldNotBe(Guid.Empty));
/// </code>
/// </example>
public sealed class EncinaTestContext<TResponse>
{
    private const string NoSagasFoundMessage = "No sagas of this type were found.";

    private readonly Either<EncinaError, TResponse> _result;
    private readonly EncinaTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaTestContext{TResponse}"/> class.
    /// </summary>
    /// <param name="result">The Either result from the request.</param>
    /// <param name="fixture">The test fixture for accessing stores.</param>
    internal EncinaTestContext(Either<EncinaError, TResponse> result, EncinaTestFixture fixture)
    {
        _result = result;
        _fixture = fixture;
    }

    /// <summary>
    /// Gets the raw Either result.
    /// </summary>
    public Either<EncinaError, TResponse> Result => _result;

    /// <summary>
    /// Gets this context for fluent chaining.
    /// </summary>
    public EncinaTestContext<TResponse> And => this;

    /// <summary>
    /// Gets whether the result is a success (Right).
    /// </summary>
    public bool IsSuccess => _result.IsRight;

    /// <summary>
    /// Gets whether the result is an error (Left).
    /// </summary>
    public bool IsError => _result.IsLeft;

    /// <summary>
    /// Asserts that the result is a success (Right).
    /// </summary>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is an error.
    /// </exception>
    public EncinaTestContext<TResponse> ShouldSucceed()
    {
        if (_result.IsLeft)
        {
            var error = _result.Match(
                Right: _ => default!,
                Left: e => e);
            throw new InvalidOperationException(
                $"Expected success but got error: {error}");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the result is a success and applies a verification action.
    /// </summary>
    /// <param name="verify">Action to verify the response value.</param>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is an error.
    /// </exception>
    public EncinaTestContext<TResponse> ShouldSucceedWith(Action<TResponse> verify)
    {
        ArgumentNullException.ThrowIfNull(verify);
        ShouldSucceed();

        var response = _result.Match(
            Right: r => r,
            Left: _ => default!);

        verify(response);
        return this;
    }

    /// <summary>
    /// Asserts that the result satisfies a condition.
    /// </summary>
    /// <param name="verify">Action to verify the response value.</param>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is an error.
    /// </exception>
    public EncinaTestContext<TResponse> ShouldSatisfy(Action<TResponse> verify)
    {
        return ShouldSucceedWith(verify);
    }

    /// <summary>
    /// Asserts that the result is an error (Left).
    /// </summary>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is a success.
    /// </exception>
    public EncinaTestContext<TResponse> ShouldFail()
    {
        if (_result.IsRight)
        {
            throw new InvalidOperationException(
                $"Expected error but got success (response type: {typeof(TResponse).Name}).");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the result is an error and applies a verification action.
    /// </summary>
    /// <param name="verify">Action to verify the error value.</param>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is a success.
    /// </exception>
    public EncinaTestContext<TResponse> ShouldFailWith(Action<EncinaError> verify)
    {
        ArgumentNullException.ThrowIfNull(verify);
        ShouldFail();

        var error = _result.Match(
            Right: _ => default!,
            Left: e => e);

        verify(error);
        return this;
    }

    /// <summary>
    /// Gets the success value or throws if the result is an error.
    /// </summary>
    /// <returns>The success value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is an error.
    /// </exception>
    public TResponse GetSuccessValue()
    {
        ShouldSucceed();
        return _result.Match(
            Right: r => r,
            Left: _ => default!);
    }

    /// <summary>
    /// Gets the error value or throws if the result is a success.
    /// </summary>
    /// <returns>The error value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is a success.
    /// </exception>
    public EncinaError GetErrorValue()
    {
        ShouldFail();
        return _result.Match(
            Right: _ => default!,
            Left: e => e);
    }

    /// <summary>
    /// Asserts that a notification of the specified type was added to the outbox.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to check.</typeparam>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the notification was not found in the outbox or outbox is not configured.
    /// </exception>
    public EncinaTestContext<TResponse> OutboxShouldContain<TNotification>()
    {
        var outbox = GetOutbox();

        if (!outbox.WasMessageAdded<TNotification>())
        {
            throw new InvalidOperationException(
                $"Expected outbox to contain message of type '{typeof(TNotification).Name}' but it was not found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a notification matching the predicate was added to the outbox.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to check.</typeparam>
    /// <param name="predicate">Predicate to match the notification.</param>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching notification was found in the outbox.
    /// </exception>
    public EncinaTestContext<TResponse> OutboxShouldContain<TNotification>(Func<TNotification, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var outbox = GetOutbox();

        var notificationType = typeof(TNotification).FullName;
        var matchingMessages = outbox.GetAddedMessages()
            .Where(m => m.NotificationType == notificationType)
            .ToList();

        if (matchingMessages.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected outbox to contain message of type '{typeof(TNotification).Name}' but it was not found.");
        }

        // Deserialize each message and apply the predicate
        foreach (var message in matchingMessages)
        {
            try
            {
                var deserializedNotification = JsonSerializer.Deserialize<TNotification>(
                    message.Content,
                    EncinaTestContextDefaults.JsonOptions);

                if (deserializedNotification is not null && predicate(deserializedNotification))
                {
                    return this;
                }
            }
            catch (JsonException)
            {
                // Skip non-deserializable messages; if no message satisfies the predicate, we'll throw below
                continue;
            }
        }

        // No message satisfied the predicate
        throw new InvalidOperationException(
            $"Expected outbox to contain message of type '{typeof(TNotification).Name}' matching the predicate but no match was found.");
    }

    /// <summary>
    /// Asserts that a saga of the specified type was started.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no saga of the specified type was started.
    /// </exception>
    public EncinaTestContext<TResponse> SagaShouldBeStarted<TSaga>()
    {
        var sagaStore = GetSagaStore();
        var sagaType = typeof(TSaga).FullName;

        var startedSagas = sagaStore.GetAddedSagas()
            .Where(s => s.SagaType == sagaType)
            .ToList();

        if (startedSagas.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected saga of type '{typeof(TSaga).Name}' to be started but none was found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the outbox is empty.
    /// </summary>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the outbox contains messages.
    /// </exception>
    public EncinaTestContext<TResponse> OutboxShouldBeEmpty()
    {
        var outbox = GetOutbox();

        if (outbox.GetAddedMessages().Count > 0)
        {
            throw new InvalidOperationException(
                $"Expected outbox to be empty but it contains {outbox.GetAddedMessages().Count} message(s).");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the outbox contains exactly the specified number of messages.
    /// </summary>
    /// <param name="expectedCount">The expected number of messages.</param>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the outbox message count doesn't match.
    /// </exception>
    public EncinaTestContext<TResponse> OutboxShouldContainExactly(int expectedCount)
    {
        var outbox = GetOutbox();
        var actualCount = outbox.GetAddedMessages().Count;

        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected outbox to contain exactly {expectedCount} message(s) but found {actualCount}.");
        }

        return this;
    }

    private FakeOutboxStore GetOutbox()
    {
        try
        {
            return _fixture.Outbox;
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Cannot verify outbox messages. Call WithMockedOutbox() when setting up the fixture.");
        }
    }

    private FakeSagaStore GetSagaStore()
    {
        try
        {
            return _fixture.SagaStore;
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Cannot verify saga state. Call WithMockedSaga() when setting up the fixture.");
        }
    }

    private FakeTimeProvider GetTimeProvider()
    {
        try
        {
            return _fixture.TimeProvider;
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Cannot use time-travel testing. Call WithFakeTimeProvider() when setting up the fixture.");
        }
    }

    /// <summary>
    /// Advances time by the specified duration for time-travel testing.
    /// </summary>
    /// <param name="duration">The duration to advance time.</param>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture was not configured with <c>WithFakeTimeProvider()</c>.
    /// </exception>
    /// <remarks>
    /// Use this to test time-dependent behavior such as saga timeouts, scheduled message
    /// execution, cache expiration, and retry delays.
    /// </remarks>
    /// <example>
    /// <code>
    /// var context = await fixture.SendAsync(new StartOrderCommand(...));
    /// context
    ///     .ShouldSucceed()
    ///     .ThenAdvanceTimeBy(TimeSpan.FromHours(2))
    ///     .And.SagaShouldHaveTimedOut&lt;OrderSaga&gt;();
    /// </code>
    /// </example>
    public EncinaTestContext<TResponse> ThenAdvanceTimeBy(TimeSpan duration)
    {
        GetTimeProvider().Advance(duration);
        return this;
    }

    /// <summary>
    /// Advances time by the specified number of minutes.
    /// </summary>
    /// <param name="minutes">The number of minutes to advance.</param>
    /// <returns>This context for method chaining.</returns>
    public EncinaTestContext<TResponse> ThenAdvanceTimeByMinutes(int minutes)
    {
        GetTimeProvider().AdvanceMinutes(minutes);
        return this;
    }

    /// <summary>
    /// Advances time by the specified number of hours.
    /// </summary>
    /// <param name="hours">The number of hours to advance.</param>
    /// <returns>This context for method chaining.</returns>
    public EncinaTestContext<TResponse> ThenAdvanceTimeByHours(int hours)
    {
        GetTimeProvider().Advance(TimeSpan.FromHours(hours));
        return this;
    }

    /// <summary>
    /// Advances time by the specified number of days.
    /// </summary>
    /// <param name="days">The number of days to advance.</param>
    /// <returns>This context for method chaining.</returns>
    public EncinaTestContext<TResponse> ThenAdvanceTimeByDays(int days)
    {
        GetTimeProvider().Advance(TimeSpan.FromDays(days));
        return this;
    }

    /// <summary>
    /// Sets the fake time to a specific value.
    /// </summary>
    /// <param name="time">The time to set.</param>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when attempting to move time backwards.</exception>
    public EncinaTestContext<TResponse> ThenSetTimeTo(DateTimeOffset time)
    {
        GetTimeProvider().SetUtcNow(time);
        return this;
    }

    /// <summary>
    /// Asserts that a saga of the specified type has timed out.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no saga of the specified type has timed out.
    /// </exception>
    public EncinaTestContext<TResponse> SagaShouldHaveTimedOut<TSaga>()
    {
        var sagaStore = GetSagaStore();
        var sagaType = typeof(TSaga).FullName;

        var timedOutSagas = sagaStore.GetAddedSagas()
            .Where(s => s.SagaType == sagaType && s.Status == SagaStatus.TimedOut)
            .ToList();

        if (timedOutSagas.Count == 0)
        {
            var existingSagas = sagaStore.GetAddedSagas()
                .Where(s => s.SagaType == sagaType)
                .Select(s => s.Status)
                .ToList();

            var statusInfo = existingSagas.Count > 0
                ? $"Found sagas with status: {string.Join(", ", existingSagas)}"
                : NoSagasFoundMessage;

            throw new InvalidOperationException(
                $"Expected saga of type '{typeof(TSaga).Name}' to have timed out but it didn't. {statusInfo}");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a saga of the specified type has completed successfully.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no saga of the specified type has completed.
    /// </exception>
    public EncinaTestContext<TResponse> SagaShouldHaveCompleted<TSaga>()
    {
        var sagaStore = GetSagaStore();
        var sagaType = typeof(TSaga).FullName;

        var completedSagas = sagaStore.GetAddedSagas()
            .Where(s => s.SagaType == sagaType && s.Status == SagaStatus.Completed)
            .ToList();

        if (completedSagas.Count == 0)
        {
            var existingSagas = sagaStore.GetAddedSagas()
                .Where(s => s.SagaType == sagaType)
                .Select(s => s.Status)
                .ToList();

            var statusInfo = existingSagas.Count > 0
                ? $"Found sagas with status: {string.Join(", ", existingSagas)}"
                : NoSagasFoundMessage;

            throw new InvalidOperationException(
                $"Expected saga of type '{typeof(TSaga).Name}' to have completed but it didn't. {statusInfo}");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a saga of the specified type is compensating.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no saga of the specified type is compensating.
    /// </exception>
    public EncinaTestContext<TResponse> SagaShouldBeCompensating<TSaga>()
    {
        var sagaStore = GetSagaStore();
        var sagaType = typeof(TSaga).FullName;

        var compensatingSagas = sagaStore.GetAddedSagas()
            .Where(s => s.SagaType == sagaType && s.Status == SagaStatus.Compensating)
            .ToList();

        if (compensatingSagas.Count == 0)
        {
            var existingSagas = sagaStore.GetAddedSagas()
                .Where(s => s.SagaType == sagaType)
                .Select(s => s.Status)
                .ToList();

            var statusInfo = existingSagas.Count > 0
                ? $"Found sagas with status: {string.Join(", ", existingSagas)}"
                : NoSagasFoundMessage;

            throw new InvalidOperationException(
                $"Expected saga of type '{typeof(TSaga).Name}' to be compensating but it wasn't. {statusInfo}");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a saga of the specified type has failed.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <returns>This context for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no saga of the specified type has failed.
    /// </exception>
    public EncinaTestContext<TResponse> SagaShouldHaveFailed<TSaga>()
    {
        var sagaStore = GetSagaStore();
        var sagaType = typeof(TSaga).FullName;

        var failedSagas = sagaStore.GetAddedSagas()
            .Where(s => s.SagaType == sagaType && s.Status == SagaStatus.Failed)
            .ToList();

        if (failedSagas.Count == 0)
        {
            var existingSagas = sagaStore.GetAddedSagas()
                .Where(s => s.SagaType == sagaType)
                .Select(s => s.Status)
                .ToList();

            var statusInfo = existingSagas.Count > 0
                ? $"Found sagas with status: {string.Join(", ", existingSagas)}"
                : NoSagasFoundMessage;

            throw new InvalidOperationException(
                $"Expected saga of type '{typeof(TSaga).Name}' to have failed but it didn't. {statusInfo}");
        }

        return this;
    }

    /// <summary>
    /// Implicitly converts the test context to the underlying Either result.
    /// </summary>
    /// <param name="context">The test context.</param>
    public static implicit operator Either<EncinaError, TResponse>(EncinaTestContext<TResponse> context)
    {
        return context._result;
    }
}
