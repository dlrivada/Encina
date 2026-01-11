using System.Text.Json;
using Encina.Messaging.Outbox;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;

namespace Encina.Testing.Messaging;

/// <summary>
/// Test helper for the Outbox pattern using Given/When/Then style.
/// </summary>
/// <remarks>
/// <para>
/// This helper simplifies testing outbox message publication scenarios:
/// </para>
/// <list type="bullet">
/// <item><description><b>Given</b>: Set up existing messages in the outbox</description></item>
/// <item><description><b>When</b>: Execute an action that publishes or processes messages</description></item>
/// <item><description><b>Then</b>: Verify the resulting messages, state, or errors</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var helper = new OutboxTestHelper();
///
/// helper
///     .GivenEmptyOutbox()
///     .WhenMessageAdded(new OrderCreatedEvent { OrderId = orderId })
///     .ThenOutboxContains&lt;OrderCreatedEvent&gt;()
///     .ThenOutboxHasCount(1);
/// </code>
/// </example>
public sealed class OutboxTestHelper : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly FakeOutboxStore _store;
    private readonly FakeTimeProvider _timeProvider;
    private bool _whenExecuted;
    private Exception? _caughtException;
    private readonly List<IOutboxMessage> _givenMessages = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxTestHelper"/> class.
    /// </summary>
    public OutboxTestHelper()
        : this(new FakeOutboxStore(), new FakeTimeProvider())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxTestHelper"/> class with a specific start time.
    /// </summary>
    /// <param name="startTime">The initial time for the test.</param>
    public OutboxTestHelper(DateTimeOffset startTime)
        : this(new FakeOutboxStore(), new FakeTimeProvider(startTime))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxTestHelper"/> class with existing stores.
    /// </summary>
    /// <param name="store">The fake outbox store to use.</param>
    /// <param name="timeProvider">The fake time provider to use.</param>
    public OutboxTestHelper(FakeOutboxStore store, FakeTimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _store = store;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the underlying fake outbox store.
    /// </summary>
    public FakeOutboxStore Store => _store;

    /// <summary>
    /// Gets the fake time provider.
    /// </summary>
    public FakeTimeProvider TimeProvider => _timeProvider;

    #region Given

    /// <summary>
    /// Sets up an empty outbox with no messages.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper GivenEmptyOutbox()
    {
        _store.Clear();
        _givenMessages.Clear();
        _whenExecuted = false;
        _caughtException = null;
        return this;
    }

    /// <summary>
    /// Sets up the outbox with existing messages.
    /// </summary>
    /// <param name="messages">The messages to add to the outbox.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper GivenMessages(params IOutboxMessage[] messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        _store.Clear();
        _givenMessages.Clear();
        _whenExecuted = false;
        _caughtException = null;

        foreach (var message in messages)
        {
            _store.AddAsync(message).GetAwaiter().GetResult();
            _givenMessages.Add(message);
        }

        // Clear only tracking since these are "historical" messages
        _store.ClearTracking();

        return this;
    }

    /// <summary>
    /// Sets up the outbox with a pending message of the specified notification type.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification content.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper GivenPendingMessage<TNotification>(TNotification notification)
        where TNotification : class
    {
        ArgumentNullException.ThrowIfNull(notification);

        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(TNotification).FullName ?? typeof(TNotification).Name,
            Content = JsonSerializer.Serialize(notification, JsonOptions),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ProcessedAtUtc = null,
            RetryCount = 0
        };

        return GivenMessages(message);
    }

    /// <summary>
    /// Sets up the outbox with a processed message.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification content.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper GivenProcessedMessage<TNotification>(TNotification notification)
        where TNotification : class
    {
        ArgumentNullException.ThrowIfNull(notification);

        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(TNotification).FullName ?? typeof(TNotification).Name,
            Content = JsonSerializer.Serialize(notification, JsonOptions),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5),
            ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            RetryCount = 0
        };

        return GivenMessages(message);
    }

    /// <summary>
    /// Sets up the outbox with a failed message that has retried.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification content.</param>
    /// <param name="retryCount">Number of retry attempts.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper GivenFailedMessage<TNotification>(
        TNotification notification,
        int retryCount = 1,
        string errorMessage = "Test error")
        where TNotification : class
    {
        ArgumentNullException.ThrowIfNull(notification);

        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(TNotification).FullName ?? typeof(TNotification).Name,
            Content = JsonSerializer.Serialize(notification, JsonOptions),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            ProcessedAtUtc = null,
            RetryCount = retryCount,
            ErrorMessage = errorMessage,
            NextRetryAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5)
        };

        return GivenMessages(message);
    }

    #endregion

    #region When

    /// <summary>
    /// Adds a message to the outbox.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification content.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper WhenMessageAdded<TNotification>(TNotification notification)
        where TNotification : class
    {
        ArgumentNullException.ThrowIfNull(notification);

        return WhenAsync(async () =>
        {
            var message = new FakeOutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = typeof(TNotification).FullName ?? typeof(TNotification).Name,
                Content = JsonSerializer.Serialize(notification, JsonOptions),
                CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            };

            await _store.AddAsync(message);
        });
    }

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    /// <param name="messageId">The message ID to mark as processed.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper WhenMessageProcessed(Guid messageId)
    {
        return WhenAsync(async () =>
        {
            await _store.MarkAsProcessedAsync(messageId);
        });
    }

    /// <summary>
    /// Marks a message as failed.
    /// </summary>
    /// <param name="messageId">The message ID to mark as failed.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="nextRetryAtUtc">When to retry next.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper WhenMessageFailed(
        Guid messageId,
        string errorMessage = "Test failure",
        DateTime? nextRetryAtUtc = null)
    {
        return WhenAsync(async () =>
        {
            await _store.MarkAsFailedAsync(
                messageId,
                errorMessage,
                nextRetryAtUtc ?? _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5));
        });
    }

    /// <summary>
    /// Executes a custom action on the store.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper When(Action<FakeOutboxStore> action)
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
    /// Executes a custom async action on the store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method synchronously blocks on the async action using <c>GetAwaiter().GetResult()</c>.
    /// This design is intentional to support fluent method chaining in test scenarios.
    /// </para>
    /// <para>
    /// <b>Warning:</b> Do not use this method in contexts with a <see cref="SynchronizationContext"/>
    /// (e.g., UI threads, classic ASP.NET). Doing so may cause deadlocks. This method is designed
    /// for use in unit test frameworks (xUnit, NUnit, MSTest) which typically run without a
    /// <see cref="SynchronizationContext"/>.
    /// </para>
    /// </remarks>
    /// <param name="action">The async action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper WhenAsync(Func<Task> action)
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
    /// <exception cref="InvalidOperationException">Thrown when an exception occurred.</exception>
    public OutboxTestHelper ThenNoException()
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
    /// Asserts that the outbox contains a message of the specified type.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to look for.</typeparam>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper ThenOutboxContains<TNotification>()
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.WasMessageAdded<TNotification>())
        {
            var addedTypes = _store.AddedMessages.Select(m => m.NotificationType).Distinct();
            throw new InvalidOperationException(
                $"Expected outbox to contain message of type '{typeof(TNotification).FullName}' " +
                $"but found: [{string.Join(", ", addedTypes)}]");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the outbox contains a message matching the predicate.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to look for.</typeparam>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper ThenOutboxContains<TNotification>(Func<TNotification, bool> predicate)
        where TNotification : class
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureWhenExecuted();
        ThenNoException();

        var notificationType = typeof(TNotification).FullName;
        var matchingMessages = _store.AddedMessages
            .Where(m => m.NotificationType == notificationType)
            .ToList();

        if (matchingMessages.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected outbox to contain message of type '{typeof(TNotification).Name}' but none were found.");
        }

        foreach (var message in matchingMessages)
        {
            try
            {
                var notification = JsonSerializer.Deserialize<TNotification>(message.Content, JsonOptions);
                if (notification is not null && predicate(notification))
                {
                    return this;
                }
            }
            catch (JsonException)
            {
                // Continue checking other messages
            }
        }

        throw new InvalidOperationException(
            $"Expected outbox to contain message of type '{typeof(TNotification).Name}' matching the predicate but no match was found.");
    }

    /// <summary>
    /// Asserts that the outbox is empty.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper ThenOutboxIsEmpty()
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (_store.AddedMessages.Count > 0)
        {
            var types = _store.AddedMessages.Select(m => m.NotificationType).Distinct();
            throw new InvalidOperationException(
                $"Expected outbox to be empty but found {_store.AddedMessages.Count} message(s): [{string.Join(", ", types)}]");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the outbox has the specified number of messages.
    /// </summary>
    /// <param name="expectedCount">The expected message count.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper ThenOutboxHasCount(int expectedCount)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var actualCount = _store.AddedMessages.Count;
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected outbox to have {expectedCount} message(s) but found {actualCount}.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message was marked as processed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper ThenMessageWasProcessed(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.ProcessedMessageIds.Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to be marked as processed but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message was marked as failed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper ThenMessageWasFailed(Guid messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.FailedMessageIds.Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message {messageId} to be marked as failed but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Gets a message from the outbox by type for further assertions.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <returns>The deserialized notification.</returns>
    public TNotification GetMessage<TNotification>() where TNotification : class
    {
        EnsureWhenExecuted();

        var notificationType = typeof(TNotification).FullName;
        var message = _store.AddedMessages.FirstOrDefault(m => m.NotificationType == notificationType);

        if (message is null)
        {
            throw new InvalidOperationException(
                $"No message of type '{typeof(TNotification).Name}' was found in the outbox.");
        }

        return JsonSerializer.Deserialize<TNotification>(message.Content, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize message content to {typeof(TNotification).Name}");
    }

    /// <summary>
    /// Gets all messages from the outbox by type for further assertions.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <returns>The deserialized notifications.</returns>
    public IReadOnlyList<TNotification> GetMessages<TNotification>() where TNotification : class
    {
        EnsureWhenExecuted();

        var notificationType = typeof(TNotification).FullName;
        return _store.AddedMessages
            .Where(m => m.NotificationType == notificationType)
            .Select(m => JsonSerializer.Deserialize<TNotification>(m.Content, JsonOptions))
            .Where(n => n is not null)
            .Cast<TNotification>()
            .ToList()
            .AsReadOnly();
    }

    #endregion

    #region Time Control

    /// <summary>
    /// Advances time by the specified duration.
    /// </summary>
    /// <param name="duration">The duration to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper AdvanceTimeBy(TimeSpan duration)
    {
        _timeProvider.Advance(duration);
        return this;
    }

    /// <summary>
    /// Advances time by the specified number of minutes.
    /// </summary>
    /// <param name="minutes">The number of minutes to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public OutboxTestHelper AdvanceTimeByMinutes(int minutes)
    {
        _timeProvider.AdvanceMinutes(minutes);
        return this;
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
