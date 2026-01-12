using System.Text.Json;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;

namespace Encina.Testing.Messaging;

/// <summary>
/// Test helper for the Scheduling pattern using Given/When/Then style.
/// </summary>
/// <remarks>
/// <para>
/// This helper simplifies testing scheduled message scenarios:
/// </para>
/// <list type="bullet">
/// <item><description><b>Given</b>: Set up scheduled messages</description></item>
/// <item><description><b>When</b>: Schedule new messages or process due messages</description></item>
/// <item><description><b>Then</b>: Verify scheduling, execution, and CRON behavior</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var helper = new SchedulingTestHelper(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
///
/// helper
///     .GivenNoScheduledMessages()
///     .WhenMessageScheduled(new SendReminderCommand { UserId = userId }, TimeSpan.FromHours(24))
///     .AdvanceTimeByHours(24)
///     .ThenMessageIsDue&lt;SendReminderCommand&gt;();
/// </code>
/// </example>
public sealed class SchedulingTestHelper : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly FakeScheduledMessageStore _store;
    private readonly FakeTimeProvider _timeProvider;
    private bool _whenExecuted;
    private Exception? _caughtException;
    private Guid _lastMessageId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingTestHelper"/> class.
    /// </summary>
    public SchedulingTestHelper()
        : this(new FakeScheduledMessageStore(), new FakeTimeProvider())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingTestHelper"/> class with a specific start time.
    /// </summary>
    /// <param name="startTime">The initial time for the test.</param>
    public SchedulingTestHelper(DateTimeOffset startTime)
        : this(new FakeScheduledMessageStore(), new FakeTimeProvider(startTime))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingTestHelper"/> class with existing stores.
    /// </summary>
    /// <param name="store">The fake scheduled message store to use.</param>
    /// <param name="timeProvider">The fake time provider to use.</param>
    public SchedulingTestHelper(FakeScheduledMessageStore store, FakeTimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _store = store;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the underlying fake scheduled message store.
    /// </summary>
    public FakeScheduledMessageStore Store => _store;

    /// <summary>
    /// Gets the fake time provider.
    /// </summary>
    public FakeTimeProvider TimeProvider => _timeProvider;

    /// <summary>
    /// Gets the ID of the last scheduled message (for chained assertions).
    /// </summary>
    public Guid LastMessageId => _lastMessageId;

    #region Given

    /// <summary>
    /// Sets up an empty store with no scheduled messages.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper GivenNoScheduledMessages()
    {
        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastMessageId = Guid.Empty;
        return this;
    }

    /// <summary>
    /// Sets up a one-time scheduled message.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <param name="scheduledIn">Duration from now to schedule.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper GivenScheduledMessage<TRequest>(TRequest request, TimeSpan scheduledIn)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        var message = new FakeScheduledMessage
        {
            Id = messageId,
            RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            Content = JsonSerializer.Serialize(request, JsonOptions),
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(scheduledIn),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            IsRecurring = false
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a recurring scheduled message with CRON expression.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <param name="cronExpression">The CRON expression for recurrence.</param>
    /// <param name="nextRunIn">Duration from now for next run.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper GivenRecurringMessage<TRequest>(
        TRequest request,
        string cronExpression,
        TimeSpan nextRunIn)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(cronExpression);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        var message = new FakeScheduledMessage
        {
            Id = messageId,
            RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            Content = JsonSerializer.Serialize(request, JsonOptions),
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(nextRunIn),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            IsRecurring = true,
            CronExpression = cronExpression
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a message that is already due.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper GivenDueMessage<TRequest>(TRequest request)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        var message = new FakeScheduledMessage
        {
            Id = messageId,
            RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            Content = JsonSerializer.Serialize(request, JsonOptions),
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1), // Already due
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5),
            IsRecurring = false
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a processed message.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper GivenProcessedMessage<TRequest>(TRequest request)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        var message = new FakeScheduledMessage
        {
            Id = messageId,
            RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            Content = JsonSerializer.Serialize(request, JsonOptions),
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-15),
            ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5),
            IsRecurring = false
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a failed message with retries.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <param name="retryCount">Number of retry attempts.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper GivenFailedMessage<TRequest>(
        TRequest request,
        int retryCount = 1,
        string errorMessage = "Test error")
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        var message = new FakeScheduledMessage
        {
            Id = messageId,
            RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            Content = JsonSerializer.Serialize(request, JsonOptions),
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-15),
            RetryCount = retryCount,
            ErrorMessage = errorMessage,
            NextRetryAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5),
            IsRecurring = false
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a cancelled message.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper GivenCancelledMessage<TRequest>(TRequest request)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        var message = new FakeScheduledMessage
        {
            Id = messageId,
            RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            Content = JsonSerializer.Serialize(request, JsonOptions),
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddHours(1),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5),
            IsCancelled = true,
            IsRecurring = false
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    #endregion

    #region When

    /// <summary>
    /// Schedules a new one-time message.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <param name="delay">Duration from now to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenMessageScheduled<TRequest>(TRequest request, TimeSpan delay)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        return WhenAsync(async () =>
        {
            var message = new FakeScheduledMessage
            {
                Id = messageId,
                RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
                Content = JsonSerializer.Serialize(request, JsonOptions),
                ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(delay),
                CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                IsRecurring = false
            };

            await _store.AddAsync(message);
        });
    }

    /// <summary>
    /// Schedules a new recurring message.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request content.</param>
    /// <param name="cronExpression">The CRON expression for recurrence.</param>
    /// <param name="firstRunIn">Duration from now for first run.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenRecurringMessageScheduled<TRequest>(
        TRequest request,
        string cronExpression,
        TimeSpan firstRunIn)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(cronExpression);

        var messageId = Guid.NewGuid();
        _lastMessageId = messageId;

        return WhenAsync(async () =>
        {
            var message = new FakeScheduledMessage
            {
                Id = messageId,
                RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
                Content = JsonSerializer.Serialize(request, JsonOptions),
                ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(firstRunIn),
                CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                IsRecurring = true,
                CronExpression = cronExpression
            };

            await _store.AddAsync(message);
        });
    }

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    /// <param name="messageId">The message ID to mark as processed.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenMessageProcessed(Guid messageId)
    {
        _lastMessageId = messageId;

        return WhenAsync(async () =>
        {
            await _store.MarkAsProcessedAsync(messageId);
        });
    }

    /// <summary>
    /// Marks the last scheduled message as processed.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenLastMessageProcessed()
    {
        return WhenMessageProcessed(_lastMessageId);
    }

    /// <summary>
    /// Marks a message as failed.
    /// </summary>
    /// <param name="messageId">The message ID to mark as failed.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenMessageFailed(Guid messageId, string errorMessage = "Test failure")
    {
        _lastMessageId = messageId;

        return WhenAsync(async () =>
        {
            await _store.MarkAsFailedAsync(
                messageId,
                errorMessage,
                _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5));
        });
    }

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    /// <param name="messageId">The message ID to cancel.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenMessageCancelled(Guid messageId)
    {
        _lastMessageId = messageId;

        return WhenAsync(async () =>
        {
            await _store.CancelAsync(messageId);
        });
    }

    /// <summary>
    /// Cancels the last scheduled message.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenLastMessageCancelled()
    {
        return WhenMessageCancelled(_lastMessageId);
    }

    /// <summary>
    /// Reschedules a recurring message to its next occurrence.
    /// </summary>
    /// <param name="messageId">The message ID to reschedule.</param>
    /// <param name="nextScheduledAtUtc">The next scheduled time.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenMessageRescheduled(Guid messageId, DateTime nextScheduledAtUtc)
    {
        _lastMessageId = messageId;

        return WhenAsync(async () =>
        {
            await _store.RescheduleRecurringMessageAsync(messageId, nextScheduledAtUtc);
        });
    }

    /// <summary>
    /// Executes a custom action on the store.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper When(Action<FakeScheduledMessageStore> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action(_store);
            _caughtException = null;
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }

        _whenExecuted = true;
        return this;
    }

    /// <summary>
    /// Marks the When phase as executed without performing any action.
    /// </summary>
    /// <remarks>
    /// Use this method when you need to trigger Then assertions after Given setup
    /// without executing any additional action. This is clearer than using
    /// <c>.When(_ => { })</c> with an empty lambda.
    /// </remarks>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenNothing()
    {
        _whenExecuted = true;
        _caughtException = null;
        return this;
    }

    /// <summary>
    /// Executes a custom async action on the store.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper WhenAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action().GetAwaiter().GetResult();
            _caughtException = null;
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }

        _whenExecuted = true;
        return this;
    }

    #endregion

    #region Then

    /// <summary>
    /// Asserts that no exception was thrown during the When phase.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenNoException()
    {
        EnsureWhenExecuted();

        if (_caughtException is not null)
        {
            throw new InvalidOperationException(
                $"Expected no exception but got {_caughtException.GetType().Name}: {_caughtException.Message}",
                _caughtException);
        }

        return this;
    }

    /// <summary>
    /// Asserts that an exception of the specified type was thrown.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>The caught exception for further assertions.</returns>
    public TException ThenThrows<TException>() where TException : Exception
    {
        EnsureWhenExecuted();

        if (_caughtException is null)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but no exception was thrown");
        }

        if (_caughtException is not TException typedException)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but got {_caughtException.GetType().Name}: {_caughtException.Message}");
        }

        return typedException;
    }

    /// <summary>
    /// Asserts that a message of the specified type was scheduled.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageWasScheduled<TRequest>()
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.WasMessageScheduled<TRequest>())
        {
            var scheduledTypes = _store.GetAddedMessages().Select(m => m.RequestType).Distinct();
            throw new InvalidOperationException(
                $"Expected message of type '{typeof(TRequest).FullName}' to be scheduled " +
                $"but found: [{string.Join(", ", scheduledTypes)}]");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message of the specified type is due for execution.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageIsDue<TRequest>()
    {
        EnsureWhenExecuted();
        ThenNoException();

        var requestType = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var dueMessages = _store.GetMessages()
            .Where(m => m.RequestType == requestType &&
                        !m.IsProcessed &&
                        !m.IsCancelled &&
                        m.ScheduledAtUtc <= now)
            .ToList();

        if (dueMessages.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected message of type '{typeof(TRequest).Name}' to be due but none were found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message is not yet due.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageIsNotDue(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var message = _store.GetMessage(messageId);
        if (message is null)
        {
            throw new InvalidOperationException($"Message {messageId} not found.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (message.ScheduledAtUtc <= now)
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to not be due yet but it is due at {message.ScheduledAtUtc}.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message was marked as processed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageWasProcessed(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.GetProcessedMessageIds().Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to be marked as processed but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the last message was marked as processed.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenLastMessageWasProcessed()
    {
        return ThenMessageWasProcessed(_lastMessageId);
    }

    /// <summary>
    /// Asserts that a message was marked as failed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageWasFailed(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.GetFailedMessageIds().Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to be marked as failed but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message was cancelled.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageWasCancelled(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.GetCancelledMessageIds().Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to be cancelled but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message was rescheduled.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageWasRescheduled(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.GetRescheduledMessageIds().Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to be rescheduled but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the store is empty.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenNoScheduledMessages()
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (_store.GetAddedMessages().Count > 0)
        {
            throw new InvalidOperationException(
                $"Expected no scheduled messages but found {_store.GetAddedMessages().Count}.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that there are exactly the specified number of scheduled messages.
    /// </summary>
    /// <param name="expectedCount">The expected count.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenScheduledMessageCount(int expectedCount)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var actualCount = _store.GetAddedMessages().Count;
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCount} scheduled message(s) but found {actualCount}.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message is recurring.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageIsRecurring(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var message = _store.GetMessage(messageId);
        if (message is null)
        {
            throw new InvalidOperationException($"Message {messageId} not found.");
        }

        if (!message.IsRecurring)
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to be recurring but it is not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message has the expected CRON expression.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <param name="expectedCron">The expected CRON expression.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper ThenMessageHasCron(Guid messageId, string expectedCron)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var message = _store.GetMessage(messageId);
        if (message is null)
        {
            throw new InvalidOperationException($"Message {messageId} not found.");
        }

        if (message.CronExpression != expectedCron)
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to have CRON '{expectedCron}' but has '{message.CronExpression}'.");
        }

        return this;
    }

    /// <summary>
    /// Gets a scheduled message for further assertions.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>The deserialized request.</returns>
    public TRequest GetScheduledMessage<TRequest>() where TRequest : class
    {
        EnsureWhenExecuted();

        var requestType = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        var message = _store.GetAddedMessages().FirstOrDefault(m => m.RequestType == requestType);

        if (message is null)
        {
            throw new InvalidOperationException(
                $"No message of type '{typeof(TRequest).Name}' was found in the store.");
        }

        return JsonSerializer.Deserialize<TRequest>(message.Content, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize message content to {typeof(TRequest).Name}");
    }

    /// <summary>
    /// Gets all due messages.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="maxRetries">Maximum retry count before dead-lettering.</param>
    /// <returns>Collection of due messages.</returns>
    public async Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(int batchSize = 100, int maxRetries = 3)
    {
        EnsureWhenExecuted();
        return await _store.GetDueMessagesAsync(batchSize, maxRetries);
    }

    #endregion

    #region Time Control

    /// <summary>
    /// Advances time by the specified duration.
    /// </summary>
    /// <param name="duration">The duration to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper AdvanceTimeBy(TimeSpan duration)
    {
        _timeProvider.Advance(duration);
        return this;
    }

    /// <summary>
    /// Advances time by the specified number of minutes.
    /// </summary>
    /// <param name="minutes">The number of minutes to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper AdvanceTimeByMinutes(int minutes)
    {
        _timeProvider.AdvanceMinutes(minutes);
        return this;
    }

    /// <summary>
    /// Advances time by the specified number of hours.
    /// </summary>
    /// <param name="hours">The number of hours to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper AdvanceTimeByHours(int hours)
    {
        _timeProvider.Advance(TimeSpan.FromHours(hours));
        return this;
    }

    /// <summary>
    /// Advances time by the specified number of days.
    /// </summary>
    /// <param name="days">The number of days to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public SchedulingTestHelper AdvanceTimeByDays(int days)
    {
        _timeProvider.Advance(TimeSpan.FromDays(days));
        return this;
    }

    /// <summary>
    /// Advances time until the specified message is due.
    /// </summary>
    /// <param name="messageId">The message ID to wait for.</param>
    /// <returns>This helper for method chaining.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the message is not found.</exception>
    public SchedulingTestHelper AdvanceTimeUntilDue(Guid messageId)
    {
        var message = _store.GetMessage(messageId)
            ?? throw new KeyNotFoundException($"Scheduled message with ID '{messageId}' was not found.");

        var duration = message.ScheduledAtUtc - _timeProvider.GetUtcNow().UtcDateTime + TimeSpan.FromSeconds(1);
        if (duration > TimeSpan.Zero)
        {
            _timeProvider.Advance(duration);
        }

        return this;
    }

    /// <summary>
    /// Gets the current time.
    /// </summary>
    /// <returns>The current fake time.</returns>
    public DateTimeOffset GetCurrentTime()
    {
        return _timeProvider.GetUtcNow();
    }

    #endregion

    private void EnsureWhenExecuted()
    {
        if (!_whenExecuted)
        {
            throw new InvalidOperationException(
                "When() must be called before Then assertions");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _store.Clear();
    }
}
