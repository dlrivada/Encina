using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;

namespace Encina.Testing.FsCheck;

/// <summary>
/// Common property-based test properties for Encina types and patterns.
/// </summary>
/// <remarks>
/// <para>
/// This class provides reusable properties that verify common invariants
/// in Encina applications. Use these properties in your tests to ensure
/// your handlers and components follow expected patterns.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// [Property]
/// public Property MyHandler_IsDeterministic()
/// {
///     return EncinaProperties.EitherIsExclusive&lt;int&gt;();
/// }
/// </code>
/// </para>
/// </remarks>
public static class EncinaProperties
{
    #region Either Properties

    /// <summary>
    /// Verifies that an Either is always exclusively Left or Right, never both or neither.
    /// </summary>
    /// <typeparam name="T">The right value type.</typeparam>
    /// <param name="either">The Either to test.</param>
    /// <returns>A property that passes if Either is exclusive.</returns>
    public static Property EitherIsExclusive<T>(Either<EncinaError, T> either)
    {
        return (either.IsLeft ^ either.IsRight)
            .ToProperty()
            .Label($"Either should be exclusively Left ({either.IsLeft}) or Right ({either.IsRight})");
    }

    /// <summary>
    /// Verifies that mapping a Right value preserves the Right state.
    /// </summary>
    /// <typeparam name="T">The original right value type.</typeparam>
    /// <typeparam name="TResult">The mapped right value type.</typeparam>
    /// <param name="either">The Either to test.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A property that passes if Right state is preserved after mapping.</returns>
    public static Property MapPreservesRightState<T, TResult>(
        Either<EncinaError, T> either,
        Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var mapped = either.Map(mapper);
        return (either.IsRight == mapped.IsRight)
            .ToProperty()
            .Label("Map should preserve Right state");
    }

    /// <summary>
    /// Verifies that mapping a Left value preserves the error message.
    /// </summary>
    /// <typeparam name="T">The right value type.</typeparam>
    /// <typeparam name="TResult">The mapped right value type.</typeparam>
    /// <param name="either">The Either to test.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A property that passes if error is preserved when mapping Left.</returns>
    public static Property MapPreservesLeftError<T, TResult>(
        Either<EncinaError, T> either,
        Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (either.IsRight)
        {
            return true.ToProperty()
                .Label("Skipped: Either is Right");
        }

        var originalError = either.Match(
            Left: e => e.Message,
            Right: _ => string.Empty);

        var mapped = either.Map(mapper);
        var mappedError = mapped.Match(
            Left: e => e.Message,
            Right: _ => string.Empty);

        return (originalError == mappedError)
            .ToProperty()
            .Label("Map should preserve error message on Left");
    }

    /// <summary>
    /// Verifies that binding with an always-failing function produces Left.
    /// </summary>
    /// <typeparam name="T">The right value type.</typeparam>
    /// <typeparam name="TResult">The bound result type.</typeparam>
    /// <param name="either">The Either to test.</param>
    /// <param name="error">The error to return.</param>
    /// <returns>A property that passes if binding to failure produces Left.</returns>
    public static Property BindToFailureProducesLeft<T, TResult>(
        Either<EncinaError, T> either,
        EncinaError error)
    {
        var bound = either.Bind<TResult>(_ => error);
        return bound.IsLeft
            .ToProperty()
            .Label("Binding Right to failure should produce Left");
    }

    #endregion

    #region EncinaError Properties

    /// <summary>
    /// Verifies that an EncinaError always has a non-empty message.
    /// </summary>
    /// <param name="error">The error to test.</param>
    /// <returns>A property that passes if the error has a non-empty message.</returns>
    public static Property ErrorHasNonEmptyMessage(EncinaError error)
    {
        return (!string.IsNullOrEmpty(error.Message))
            .ToProperty()
            .Label("EncinaError should always have a non-empty message");
    }

    /// <summary>
    /// Verifies that creating an error from a string preserves the message (or uses default for whitespace-only).
    /// </summary>
    /// <param name="message">The message to use.</param>
    /// <returns>A property that passes if the message is preserved correctly.</returns>
    public static Property ErrorFromStringPreservesMessage(NonEmptyString message)
    {
        var error = EncinaError.New(message.Get);
        // EncinaError replaces whitespace-only messages with a default message
        var expectedMessage = string.IsNullOrWhiteSpace(message.Get)
            ? error.Message // Accept any non-empty default
            : message.Get;

        return (string.IsNullOrWhiteSpace(message.Get)
                ? !string.IsNullOrEmpty(error.Message) // Default should be non-empty
                : error.Message == expectedMessage)
            .ToProperty()
            .Label("Error message should match input string (or use default for whitespace)");
    }

    #endregion

    #region RequestContext Properties

    /// <summary>
    /// Verifies that a RequestContext always has a non-empty CorrelationId.
    /// </summary>
    /// <param name="context">The context to test.</param>
    /// <returns>A property that passes if CorrelationId is non-empty.</returns>
    public static Property ContextHasCorrelationId(IRequestContext context)
    {
        return (!string.IsNullOrEmpty(context.CorrelationId))
            .ToProperty()
            .Label("RequestContext should always have a CorrelationId");
    }

    /// <summary>
    /// Verifies that WithMetadata creates a new context without modifying the original.
    /// </summary>
    /// <param name="context">The context to test.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A property that passes if immutability is preserved.</returns>
    public static Property WithMetadataIsImmutable(IRequestContext context, NonEmptyString key, int value)
    {
        // Skip test if key is whitespace-only (RequestContext rejects these)
        if (string.IsNullOrWhiteSpace(key.Get))
        {
            return true.ToProperty()
                .Label("Skipped: Key is whitespace-only");
        }

        var originalMetadataCount = context.Metadata.Count;
        var newContext = context.WithMetadata(key.Get, value);

        return (context.Metadata.Count == originalMetadataCount &&
                 newContext.Metadata.ContainsKey(key.Get))
            .ToProperty()
            .Label("WithMetadata should create new context without modifying original");
    }

    /// <summary>
    /// Verifies that WithUserId creates a new context with the updated value.
    /// </summary>
    /// <param name="context">The context to test.</param>
    /// <param name="userId">The new user ID.</param>
    /// <returns>A property that passes if user ID is updated in new context.</returns>
    public static Property WithUserIdCreatesNewContext(IRequestContext context, NonEmptyString userId)
    {
        var originalUserId = context.UserId;
        var newContext = context.WithUserId(userId.Get);

        return (context.UserId == originalUserId &&
                 newContext.UserId == userId.Get)
            .ToProperty()
            .Label("WithUserId should create new context with updated UserId");
    }

    #endregion

    #region Outbox Properties

    /// <summary>
    /// Verifies that an outbox message marked as processed has a ProcessedAtUtc value.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>A property that passes if processed state is consistent.</returns>
    public static Property OutboxProcessedStateIsConsistent(IOutboxMessage message)
    {
        return (message.IsProcessed == message.ProcessedAtUtc.HasValue)
            .ToProperty()
            .Label("IsProcessed should match ProcessedAtUtc.HasValue");
    }

    /// <summary>
    /// Verifies that dead lettering logic is consistent with retry count.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <param name="maxRetries">The maximum number of retries.</param>
    /// <returns>A property that passes if dead letter state is consistent.</returns>
    public static Property OutboxDeadLetterIsConsistent(IOutboxMessage message, PositiveInt maxRetries)
    {
        var expectedDeadLetter = message.RetryCount >= maxRetries.Get;
        return (message.IsDeadLettered(maxRetries.Get) == expectedDeadLetter)
            .ToProperty()
            .Label("IsDeadLettered should match RetryCount >= MaxRetries");
    }

    /// <summary>
    /// Verifies that outbox messages have required fields populated.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>A property that passes if required fields are present.</returns>
    public static Property OutboxHasRequiredFields(IOutboxMessage message)
    {
        return (message.Id != Guid.Empty &&
                 !string.IsNullOrEmpty(message.NotificationType) &&
                 !string.IsNullOrEmpty(message.Content))
            .ToProperty()
            .Label("Outbox message should have Id, NotificationType, and Content");
    }

    #endregion

    #region Inbox Properties

    /// <summary>
    /// Verifies that an inbox message marked as processed has a ProcessedAtUtc value.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>A property that passes if processed state is consistent.</returns>
    public static Property InboxProcessedStateIsConsistent(IInboxMessage message)
    {
        return (message.IsProcessed == message.ProcessedAtUtc.HasValue)
            .ToProperty()
            .Label("IsProcessed should match ProcessedAtUtc.HasValue");
    }

    /// <summary>
    /// Verifies that inbox messages have required fields populated.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>A property that passes if required fields are present.</returns>
    public static Property InboxHasRequiredFields(IInboxMessage message)
    {
        return (!string.IsNullOrEmpty(message.MessageId) &&
                 !string.IsNullOrEmpty(message.RequestType))
            .ToProperty()
            .Label("Inbox message should have MessageId and RequestType");
    }

    #endregion

    #region Saga Properties

    /// <summary>
    /// Verifies that saga status values are valid.
    /// </summary>
    /// <param name="state">The saga state to test.</param>
    /// <returns>A property that passes if status is valid.</returns>
    public static Property SagaStatusIsValid(ISagaState state)
    {
        var validStatuses = new[] { "Running", "Completed", "Compensating", "Failed" };
        return validStatuses.Contains(state.Status)
            .ToProperty()
            .Label($"Saga status '{state.Status}' should be one of: {string.Join(", ", validStatuses)}");
    }

    /// <summary>
    /// Verifies that saga states have required fields populated.
    /// </summary>
    /// <param name="state">The saga state to test.</param>
    /// <returns>A property that passes if required fields are present.</returns>
    public static Property SagaHasRequiredFields(ISagaState state)
    {
        return (state.SagaId != Guid.Empty &&
                 !string.IsNullOrEmpty(state.SagaType) &&
                 !string.IsNullOrEmpty(state.Status))
            .ToProperty()
            .Label("Saga state should have SagaId, SagaType, and Status");
    }

    /// <summary>
    /// Verifies that completed sagas have CompletedAtUtc set.
    /// </summary>
    /// <param name="state">The saga state to test.</param>
    /// <returns>A property that passes if completion state is consistent.</returns>
    public static Property SagaCompletedStateIsConsistent(ISagaState state)
    {
        if (state.Status != "Completed")
        {
            return true.ToProperty()
                .Label("Skipped: Saga not completed");
        }

        return state.CompletedAtUtc.HasValue
            .ToProperty()
            .Label("Completed saga should have CompletedAtUtc set");
    }

    /// <summary>
    /// Verifies that CurrentStep is non-negative.
    /// </summary>
    /// <param name="state">The saga state to test.</param>
    /// <returns>A property that passes if CurrentStep is valid.</returns>
    public static Property SagaCurrentStepIsNonNegative(ISagaState state)
    {
        return (state.CurrentStep >= 0)
            .ToProperty()
            .Label("Saga CurrentStep should be non-negative");
    }

    #endregion

    #region Scheduled Message Properties

    /// <summary>
    /// Verifies that scheduled message has valid ScheduledAtUtc.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>A property that passes if ScheduledAtUtc is valid.</returns>
    public static Property ScheduledHasValidTiming(IScheduledMessage message)
    {
        return (message.ScheduledAtUtc != default)
            .ToProperty()
            .Label("Scheduled message should have valid ScheduledAtUtc");
    }

    /// <summary>
    /// Verifies that recurring messages have a cron expression.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>A property that passes if recurring state is consistent.</returns>
    public static Property RecurringHasCronExpression(IScheduledMessage message)
    {
        if (!message.IsRecurring)
        {
            return true.ToProperty()
                .Label("Skipped: Not recurring");
        }

        return (!string.IsNullOrEmpty(message.CronExpression))
            .ToProperty()
            .Label("Recurring message should have CronExpression");
    }

    /// <summary>
    /// Verifies that scheduled messages have required fields populated.
    /// </summary>
    /// <param name="message">The message to test.</param>
    /// <returns>A property that passes if required fields are present.</returns>
    public static Property ScheduledHasRequiredFields(IScheduledMessage message)
    {
        return (message.Id != Guid.Empty &&
                 !string.IsNullOrEmpty(message.RequestType) &&
                 !string.IsNullOrEmpty(message.Content))
            .ToProperty()
            .Label("Scheduled message should have Id, RequestType, and Content");
    }

    #endregion

    #region Handler Properties

    /// <summary>
    /// Creates a property that verifies a handler produces consistent results for the same input.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler function to test.</param>
    /// <param name="request">The request to use.</param>
    /// <returns>A property that passes if the handler is deterministic.</returns>
    /// <remarks>
    /// Note: This tests functional purity. Handlers with side effects may fail this property.
    /// </remarks>
    public static Property HandlerIsDeterministic<TRequest, TResponse>(
        Func<TRequest, Either<EncinaError, TResponse>> handler,
        TRequest request)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result1 = handler(request);
        var result2 = handler(request);

        var bothLeft = result1.IsLeft && result2.IsLeft;
        var bothRight = result1.IsRight && result2.IsRight;

        return (bothLeft || bothRight)
            .ToProperty()
            .Label("Handler should produce consistent Left/Right results for same input");
    }

    /// <summary>
    /// Creates a property that verifies an async handler produces consistent results.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The async handler function to test.</param>
    /// <param name="request">The request to use.</param>
    /// <returns>A property that passes if the handler is deterministic.</returns>
    /// <remarks>
    /// Uses Task.Run to avoid deadlocks when called from a SynchronizationContext.
    /// </remarks>
    public static Property AsyncHandlerIsDeterministic<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<Either<EncinaError, TResponse>>> handler,
        TRequest request)
    {
        ArgumentNullException.ThrowIfNull(handler);

        // Use Task.Run to avoid deadlocks under SynchronizationContext
        var result1 = Task.Run(() => handler(request, CancellationToken.None)).GetAwaiter().GetResult();
        var result2 = Task.Run(() => handler(request, CancellationToken.None)).GetAwaiter().GetResult();

        var bothLeft = result1.IsLeft && result2.IsLeft;
        var bothRight = result1.IsRight && result2.IsRight;

        return (bothLeft || bothRight)
            .ToProperty()
            .Label("Async handler should produce consistent Left/Right results for same input");
    }

    #endregion
}
