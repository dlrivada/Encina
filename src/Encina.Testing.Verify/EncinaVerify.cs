using Encina.DomainModeling;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using LanguageExt;

namespace Encina.Testing.Verify;

/// <summary>
/// Static helper methods for Verify snapshot testing with Encina types.
/// </summary>
/// <remarks>
/// <para>
/// This class provides static methods for snapshot testing Encina domain objects.
/// Use these methods in combination with your test framework's Verify integration.
/// </para>
/// <para>
/// <b>Important</b>: Call <see cref="EncinaVerifySettings.Initialize"/> once in your
/// test project (typically in a ModuleInitializer) before using these methods.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your test class (using xUnit + Verify.Xunit)
/// public class OrderHandlerTests
/// {
///     [Fact]
///     public async Task CreateOrder_ReturnsExpectedResponse()
///     {
///         var result = await handler.Handle(new CreateOrder { ... });
///         await Verify(EncinaVerify.PrepareEither(result));
///     }
/// }
/// </code>
/// </example>
public static class EncinaVerify
{
    /// <summary>
    /// Prepares an Either result for snapshot verification.
    /// </summary>
    /// <typeparam name="TLeft">The left (error) type.</typeparam>
    /// <typeparam name="TRight">The right (success) type.</typeparam>
    /// <param name="either">The Either value to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// The resulting object clearly indicates whether the result is Left (error)
    /// or Right (success), and includes the appropriate value.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await handler.Handle(command);
    /// await Verify(EncinaVerify.PrepareEither(result));
    /// </code>
    /// </example>
    public static object PrepareEither<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        return either.Match(
            Right: right => new EitherSnapshot<TLeft, TRight>
            {
                IsRight = true,
                Value = right
            },
            Left: left => (object)new EitherSnapshot<TLeft, TRight>
            {
                IsRight = false,
                Error = left
            });
    }

    /// <summary>
    /// Extracts the success value from an Either result for snapshot verification.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="result">The Either result.</param>
    /// <returns>The success value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result is an error.</exception>
    /// <remarks>
    /// Use this when you expect the result to be successful. If the result is an error,
    /// an exception is thrown to fail the test.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await handler.Handle(new CreateOrder { ... });
    /// await Verify(EncinaVerify.ExtractSuccess(result));
    /// </code>
    /// </example>
    public static TResponse ExtractSuccess<TResponse>(Either<EncinaError, TResponse> result)
    {
        return result.Match(
            Right: response => response,
            Left: error => throw new InvalidOperationException(
                $"Expected success but got error: {error}"));
    }

    /// <summary>
    /// Extracts the error from an Either result for snapshot verification.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="result">The Either result.</param>
    /// <returns>The error value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result is a success.</exception>
    /// <remarks>
    /// Use this when you expect the result to be an error. If the result is successful,
    /// an exception is thrown to fail the test.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await handler.Handle(new CreateOrder { CustomerId = null });
    /// await Verify(EncinaVerify.ExtractError(result));
    /// </code>
    /// </example>
    public static EncinaError ExtractError<TResponse>(Either<EncinaError, TResponse> result)
    {
        return result.Match(
            Right: _ => throw new InvalidOperationException(
                $"Expected error but got success (response type: {typeof(TResponse).Name})."),
            Left: error => error);
    }

    /// <summary>
    /// Prepares an aggregate's uncommitted events for snapshot verification.
    /// </summary>
    /// <param name="aggregate">The aggregate whose uncommitted events to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// This is useful for testing that domain commands raise the expected events.
    /// Events are prepared in order with type information.
    /// </remarks>
    /// <example>
    /// <code>
    /// var order = new OrderAggregate();
    /// order.Create("CUST-001", items);
    ///
    /// await Verify(EncinaVerify.PrepareUncommittedEvents(order));
    /// </code>
    /// </example>
    public static object PrepareUncommittedEvents(IAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var events = aggregate.UncommittedEvents ?? [];
        var filteredEvents = events.Where(e => e is not null).ToList();

        return new UncommittedEventsSnapshot
        {
            AggregateId = aggregate.Id,
            AggregateVersion = aggregate.Version,
            EventCount = filteredEvents.Count,
            Events = filteredEvents
                .Select((e, i) => new EventSnapshot
                {
                    Index = i,
                    EventType = e.GetType().Name,
                    Event = e
                })
                .ToList()
        };
    }

    /// <summary>
    /// Prepares outbox messages for snapshot verification.
    /// </summary>
    /// <param name="messages">The outbox messages to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// The snapshot includes notification type, content, processing status, and retry information.
    /// Timestamps are automatically scrubbed when using <see cref="EncinaVerifySettings.Initialize"/>.
    /// </remarks>
    public static IReadOnlyList<object> PrepareOutboxMessages(IEnumerable<IOutboxMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        return messages.OrderBy(m => m.Id).Select(m => (object)new OutboxMessageSnapshot
        {
            Id = m.Id,
            NotificationType = m.NotificationType,
            Content = m.Content,
            IsProcessed = m.IsProcessed,
            ErrorMessage = m.ErrorMessage,
            RetryCount = m.RetryCount
        }).ToList();
    }

    /// <summary>
    /// Prepares inbox messages for snapshot verification.
    /// </summary>
    /// <param name="messages">The inbox messages to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// The snapshot includes message ID, request type, response, and processing status.
    /// Timestamps are automatically scrubbed when using <see cref="EncinaVerifySettings.Initialize"/>.
    /// </remarks>
    public static IReadOnlyList<object> PrepareInboxMessages(IEnumerable<IInboxMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        return messages.OrderBy(m => m.MessageId).Select(m => (object)new InboxMessageSnapshot
        {
            MessageId = m.MessageId,
            RequestType = m.RequestType,
            Response = m.Response,
            IsProcessed = m.IsProcessed,
            ErrorMessage = m.ErrorMessage,
            RetryCount = m.RetryCount
        }).ToList();
    }

    /// <summary>
    /// Prepares a saga state for snapshot verification.
    /// </summary>
    /// <param name="sagaState">The saga state to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// The snapshot includes saga type, status, current step, and any error information.
    /// Timestamps and IDs are automatically scrubbed when using <see cref="EncinaVerifySettings.Initialize"/>.
    /// </remarks>
    public static object PrepareSagaState(ISagaState sagaState)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        return new SagaStateSnapshot
        {
            SagaId = sagaState.SagaId,
            SagaType = sagaState.SagaType,
            Status = sagaState.Status,
            CurrentStep = sagaState.CurrentStep,
            Data = sagaState.Data,
            ErrorMessage = sagaState.ErrorMessage
        };
    }

    /// <summary>
    /// Prepares scheduled messages for snapshot verification.
    /// </summary>
    /// <param name="messages">The scheduled messages to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// The snapshot includes request type, content, scheduling information, and processing status.
    /// Timestamps are automatically scrubbed when using <see cref="EncinaVerifySettings.Initialize"/>.
    /// </remarks>
    public static IReadOnlyList<object> PrepareScheduledMessages(IEnumerable<IScheduledMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        return messages.OrderBy(m => m.Id).Select(m => (object)new ScheduledMessageSnapshot
        {
            Id = m.Id,
            RequestType = m.RequestType,
            Content = m.Content,
            IsRecurring = m.IsRecurring,
            CronExpression = m.CronExpression,
            IsProcessed = m.IsProcessed,
            ErrorMessage = m.ErrorMessage,
            RetryCount = m.RetryCount
        }).ToList();
    }

    /// <summary>
    /// Prepares dead letter messages for snapshot verification.
    /// </summary>
    /// <param name="messages">The dead letter messages to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// The snapshot includes request type, error information, source pattern, and retry attempts.
    /// Timestamps, IDs, and stack traces are automatically scrubbed when using <see cref="EncinaVerifySettings.Initialize"/>.
    /// </remarks>
    public static IReadOnlyList<object> PrepareDeadLetterMessages(IEnumerable<IDeadLetterMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        return messages.OrderBy(m => m.Id).Select(m => (object)new DeadLetterMessageSnapshot
        {
            Id = m.Id,
            RequestType = m.RequestType,
            RequestContent = m.RequestContent,
            ErrorMessage = m.ErrorMessage,
            ExceptionType = m.ExceptionType,
            SourcePattern = m.SourcePattern,
            TotalRetryAttempts = m.TotalRetryAttempts,
            CorrelationId = m.CorrelationId,
            IsReplayed = m.IsReplayed,
            ReplayResult = m.ReplayResult
        }).ToList();
    }

    /// <summary>
    /// Prepares a handler test result for snapshot verification.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request that was sent.</param>
    /// <param name="result">The result from the handler.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// This creates a comprehensive snapshot that includes both the request and response,
    /// useful for verifying handler behavior with specific inputs.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = new CreateOrderCommand { CustomerId = "CUST-001" };
    /// var result = await handler.Handle(request);
    /// await Verify(EncinaVerify.PrepareHandlerResult(request, result));
    /// </code>
    /// </example>
    public static object PrepareHandlerResult<TRequest, TResponse>(
        TRequest request,
        Either<EncinaError, TResponse> result)
    {
        ArgumentNullException.ThrowIfNull(request);

        return result.Match(
            Right: response => (object)new HandlerResultSnapshot<TRequest, TResponse>
            {
                RequestType = typeof(TRequest).Name,
                Request = request,
                IsSuccess = true,
                Response = response
            },
            Left: error => new HandlerResultSnapshot<TRequest, TResponse>
            {
                RequestType = typeof(TRequest).Name,
                Request = request,
                IsSuccess = false,
                Error = new ErrorSnapshot
                {
                    Message = error.Message,
                    Code = error.GetCode().Match(Some: c => c, None: () => (string?)null)
                }
            });
    }

    /// <summary>
    /// Prepares multiple saga states for snapshot verification.
    /// </summary>
    /// <param name="sagaStates">The saga states to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// The sagas are sorted by saga ID for consistent snapshot output.
    /// </remarks>
    public static IReadOnlyList<object> PrepareSagaStates(IEnumerable<ISagaState> sagaStates)
    {
        ArgumentNullException.ThrowIfNull(sagaStates);

        return sagaStates.OrderBy(s => s.SagaId).Select(PrepareSagaState).ToList();
    }

    /// <summary>
    /// Prepares a validation error result for snapshot verification.
    /// </summary>
    /// <param name="error">The EncinaError containing validation details.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// This is useful for verifying that validation produces expected error messages.
    /// </remarks>
    public static object PrepareValidationError(EncinaError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var code = error.GetCode().Match(Some: c => c, None: () => (string?)null);
        return new ValidationErrorSnapshot
        {
            Message = error.Message,
            Code = code,
            IsValidationError = code?.StartsWith("encina.validation.", StringComparison.OrdinalIgnoreCase) ?? false
        };
    }

    /// <summary>
    /// Prepares a combined test scenario result for snapshot verification.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="result">The handler result.</param>
    /// <param name="outboxMessages">Optional outbox messages to include.</param>
    /// <param name="sagaStates">Optional saga states to include.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// Use this to create comprehensive snapshots that capture the complete state
    /// after a handler execution, including side effects like outbox messages and sagas.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await handler.Handle(command);
    /// await Verify(EncinaVerify.PrepareTestScenario(
    ///     result,
    ///     outboxStore.Messages,
    ///     sagaStore.GetSagas()));
    /// </code>
    /// </example>
    public static object PrepareTestScenario<TResponse>(
        Either<EncinaError, TResponse> result,
        IEnumerable<IOutboxMessage>? outboxMessages = null,
        IEnumerable<ISagaState>? sagaStates = null)
    {
        return new TestScenarioSnapshot
        {
            Result = PrepareEither(result),
            OutboxMessages = outboxMessages != null ? PrepareOutboxMessages(outboxMessages) : null,
            SagaStates = sagaStates != null ? PrepareSagaStates(sagaStates) : null
        };
    }
}

#region Snapshot Types

/// <summary>
/// Internal snapshot representation for Either values.
/// </summary>
internal sealed class EitherSnapshot<TLeft, TRight>
{
    public bool IsRight { get; set; }
    public TRight? Value { get; set; }
    public TLeft? Error { get; set; }
}

/// <summary>
/// Internal snapshot representation for uncommitted events.
/// </summary>
internal sealed class UncommittedEventsSnapshot
{
    public Guid AggregateId { get; set; }
    public int AggregateVersion { get; set; }
    public int EventCount { get; set; }
    public IReadOnlyList<EventSnapshot> Events { get; set; } = [];
}

/// <summary>
/// Internal snapshot representation for a single event.
/// </summary>
internal sealed class EventSnapshot
{
    public int Index { get; set; }
    public string EventType { get; set; } = string.Empty;
    public object? Event { get; set; }
}

/// <summary>
/// Internal snapshot representation for outbox messages.
/// </summary>
internal sealed class OutboxMessageSnapshot
{
    public Guid Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// Internal snapshot representation for inbox messages.
/// </summary>
internal sealed class InboxMessageSnapshot
{
    public string MessageId { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string? Response { get; set; }
    public bool IsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// Internal snapshot representation for saga state.
/// </summary>
internal sealed class SagaStateSnapshot
{
    public Guid SagaId { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public string Data { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Internal snapshot representation for scheduled messages.
/// </summary>
internal sealed class ScheduledMessageSnapshot
{
    public Guid Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string? CronExpression { get; set; }
    public bool IsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// Internal snapshot representation for dead letter messages.
/// </summary>
internal sealed class DeadLetterMessageSnapshot
{
    public Guid Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string RequestContent { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string SourcePattern { get; set; } = string.Empty;
    public int TotalRetryAttempts { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsReplayed { get; set; }
    public string? ReplayResult { get; set; }
}

/// <summary>
/// Internal snapshot representation for handler results.
/// </summary>
internal sealed class HandlerResultSnapshot<TRequest, TResponse>
{
    public string RequestType { get; set; } = string.Empty;
    public TRequest? Request { get; set; }
    public bool IsSuccess { get; set; }
    public TResponse? Response { get; set; }
    public ErrorSnapshot? Error { get; set; }
}

/// <summary>
/// Internal snapshot representation for errors.
/// </summary>
internal sealed class ErrorSnapshot
{
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}

/// <summary>
/// Internal snapshot representation for validation errors.
/// </summary>
internal sealed class ValidationErrorSnapshot
{
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsValidationError { get; set; }
}

/// <summary>
/// Internal snapshot representation for complete test scenarios.
/// </summary>
internal sealed class TestScenarioSnapshot
{
    public object? Result { get; set; }
    public IReadOnlyList<object>? OutboxMessages { get; set; }
    public IReadOnlyList<object>? SagaStates { get; set; }
}

#endregion
