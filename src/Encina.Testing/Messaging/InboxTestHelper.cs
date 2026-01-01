using System.Text.Json;
using Encina.Messaging.Inbox;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;

namespace Encina.Testing.Messaging;

/// <summary>
/// Test helper for the Inbox pattern using Given/When/Then style.
/// </summary>
/// <remarks>
/// <para>
/// This helper simplifies testing idempotent message processing scenarios:
/// </para>
/// <list type="bullet">
/// <item><description><b>Given</b>: Set up existing messages in the inbox</description></item>
/// <item><description><b>When</b>: Process or receive a message</description></item>
/// <item><description><b>Then</b>: Verify idempotency, deduplication, and responses</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var helper = new InboxTestHelper();
///
/// // Test idempotency - same message ID should return cached response
/// helper
///     .GivenProcessedMessage("msg-123", new OrderResponse { Success = true })
///     .WhenMessageReceived("msg-123")
///     .ThenMessageWasAlreadyProcessed("msg-123")
///     .ThenCachedResponseIs&lt;OrderResponse&gt;(r => r.Success);
/// </code>
/// </example>
public sealed class InboxTestHelper : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly FakeInboxStore _store;
    private readonly FakeTimeProvider _timeProvider;
    private bool _whenExecuted;
    private Exception? _caughtException;
    private string? _lastCheckedMessageId;
    private IInboxMessage? _lastRetrievedMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxTestHelper"/> class.
    /// </summary>
    public InboxTestHelper()
        : this(new FakeInboxStore(), new FakeTimeProvider())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxTestHelper"/> class with a specific start time.
    /// </summary>
    /// <param name="startTime">The initial time for the test.</param>
    public InboxTestHelper(DateTimeOffset startTime)
        : this(new FakeInboxStore(), new FakeTimeProvider(startTime))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxTestHelper"/> class with existing stores.
    /// </summary>
    /// <param name="store">The fake inbox store to use.</param>
    /// <param name="timeProvider">The fake time provider to use.</param>
    public InboxTestHelper(FakeInboxStore store, FakeTimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _store = store;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the underlying fake inbox store.
    /// </summary>
    public FakeInboxStore Store => _store;

    /// <summary>
    /// Gets the fake time provider.
    /// </summary>
    public FakeTimeProvider TimeProvider => _timeProvider;

    #region Given

    /// <summary>
    /// Sets up an empty inbox with no messages.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper GivenEmptyInbox()
    {
        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastCheckedMessageId = null;
        _lastRetrievedMessage = null;
        return this;
    }

    /// <summary>
    /// Sets up the inbox with an already processed message (for idempotency testing).
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="messageId">The unique message ID (idempotency key).</param>
    /// <param name="cachedResponse">The cached response from the original processing.</param>
    /// <param name="requestType">Optional request type name.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper GivenProcessedMessage<TResponse>(
        string messageId,
        TResponse cachedResponse,
        string? requestType = null)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(cachedResponse);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastCheckedMessageId = null;
        _lastRetrievedMessage = null;

        var message = new FakeInboxMessage
        {
            MessageId = messageId,
            RequestType = requestType ?? typeof(TResponse).FullName ?? "Unknown",
            Response = JsonSerializer.Serialize(cachedResponse, JsonOptions),
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5),
            ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7)
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up the inbox with a pending (unprocessed) message.
    /// </summary>
    /// <param name="messageId">The unique message ID.</param>
    /// <param name="requestType">The request type name.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper GivenPendingMessage(string messageId, string requestType = "TestRequest")
    {
        ArgumentNullException.ThrowIfNull(messageId);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastCheckedMessageId = null;
        _lastRetrievedMessage = null;

        var message = new FakeInboxMessage
        {
            MessageId = messageId,
            RequestType = requestType,
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7)
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up the inbox with a failed message.
    /// </summary>
    /// <param name="messageId">The unique message ID.</param>
    /// <param name="retryCount">Number of retry attempts.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper GivenFailedMessage(
        string messageId,
        int retryCount = 1,
        string errorMessage = "Test error")
    {
        ArgumentNullException.ThrowIfNull(messageId);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastCheckedMessageId = null;
        _lastRetrievedMessage = null;

        var message = new FakeInboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            RetryCount = retryCount,
            ErrorMessage = errorMessage,
            NextRetryAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5)
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up the inbox with an expired message.
    /// </summary>
    /// <param name="messageId">The unique message ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper GivenExpiredMessage(string messageId)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastCheckedMessageId = null;
        _lastRetrievedMessage = null;

        var message = new FakeInboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-10),
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-1) // Already expired
        };

        _store.AddAsync(message).GetAwaiter().GetResult();
        return this;
    }

    #endregion

    #region When

    /// <summary>
    /// Simulates receiving a message and checking for duplicates.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper WhenMessageReceived(string messageId)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        _lastCheckedMessageId = messageId;

        return WhenAsync(async () =>
        {
            _lastRetrievedMessage = await _store.GetMessageAsync(messageId);
        });
    }

    /// <summary>
    /// Registers a new message in the inbox.
    /// </summary>
    /// <param name="messageId">The unique message ID.</param>
    /// <param name="requestType">The request type name.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper WhenMessageRegistered(string messageId, string requestType = "TestRequest")
    {
        ArgumentNullException.ThrowIfNull(messageId);

        _lastCheckedMessageId = messageId;

        return WhenAsync(async () =>
        {
            var message = new FakeInboxMessage
            {
                MessageId = messageId,
                RequestType = requestType,
                ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7)
            };

            await _store.AddAsync(message);
        });
    }

    /// <summary>
    /// Marks a message as processed with a response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="messageId">The message ID to mark as processed.</param>
    /// <param name="response">The response to cache.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper WhenMessageProcessed<TResponse>(string messageId, TResponse response)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(response);

        _lastCheckedMessageId = messageId;

        return WhenAsync(async () =>
        {
            var serializedResponse = JsonSerializer.Serialize(response, JsonOptions);
            await _store.MarkAsProcessedAsync(messageId, serializedResponse);
        });
    }

    /// <summary>
    /// Marks a message as failed.
    /// </summary>
    /// <param name="messageId">The message ID to mark as failed.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper WhenMessageFailed(string messageId, string errorMessage = "Test failure")
    {
        ArgumentNullException.ThrowIfNull(messageId);

        _lastCheckedMessageId = messageId;

        return WhenAsync(async () =>
        {
            await _store.MarkAsFailedAsync(
                messageId,
                errorMessage,
                _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5));
            await _store.IncrementRetryCountAsync(messageId);
        });
    }

    /// <summary>
    /// Executes a custom action on the store.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper When(Action<FakeInboxStore> action)
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
    /// This API is intended for test code only. It intentionally blocks on the async operation
    /// using <c>GetAwaiter().GetResult()</c> to maintain the fluent, synchronous Given/When/Then API
    /// that is convenient for unit tests.
    /// </para>
    /// <para>
    /// <b>Warning:</b> Do not use this method in production code. Blocking on async operations
    /// can cause deadlocks in environments with synchronization contexts (e.g., ASP.NET, UI frameworks).
    /// </para>
    /// </remarks>
    /// <param name="action">The async action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper WhenAsync(Func<Task> action)
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
    public InboxTestHelper ThenNoException()
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
    /// Asserts that a message was already processed (duplicate detected).
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper ThenMessageWasAlreadyProcessed(string messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.IsMessageProcessed(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message '{messageId}' to be already processed but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message is a new message (not a duplicate).
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper ThenMessageIsNew(string messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var message = _store.GetMessage(messageId);
        if (message is not null && message.IsProcessed)
        {
            throw new InvalidOperationException(
                $"Expected message '{messageId}' to be new but it was already processed.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the last retrieved message has a cached response matching the predicate.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="predicate">The predicate to validate the cached response.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper ThenCachedResponseIs<TResponse>(Func<TResponse, bool> predicate)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureWhenExecuted();
        ThenNoException();

        if (_lastRetrievedMessage is null)
        {
            throw new InvalidOperationException(
                "No message was retrieved. Call WhenMessageReceived first.");
        }

        if (string.IsNullOrEmpty(_lastRetrievedMessage.Response))
        {
            throw new InvalidOperationException(
                $"Message '{_lastRetrievedMessage.MessageId}' has no cached response.");
        }

        var response = JsonSerializer.Deserialize<TResponse>(_lastRetrievedMessage.Response, JsonOptions);
        if (response is null)
        {
            throw new InvalidOperationException(
                $"Deserialization returned null for message '{_lastRetrievedMessage.MessageId}'. " +
                $"Raw response: '{_lastRetrievedMessage.Response}'");
        }

        if (!predicate(response))
        {
            throw new InvalidOperationException(
                $"Cached response for message '{_lastRetrievedMessage.MessageId}' did not satisfy the predicate. " +
                $"Response type: {typeof(TResponse).Name}");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the inbox is empty.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper ThenInboxIsEmpty()
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (_store.Messages.Count > 0)
        {
            throw new InvalidOperationException(
                $"Expected inbox to be empty but found {_store.Messages.Count} message(s).");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the inbox has the specified number of messages.
    /// </summary>
    /// <param name="expectedCount">The expected message count.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper ThenInboxHasCount(int expectedCount)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var actualCount = _store.Messages.Count;
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected inbox to have {expectedCount} message(s) but found {actualCount}.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message was marked as processed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper ThenMessageWasProcessed(string messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.ProcessedMessageIds.Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message '{messageId}' to be marked as processed but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a message was marked as failed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper ThenMessageWasFailed(string messageId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.FailedMessageIds.Contains(messageId))
        {
            throw new InvalidOperationException(
                $"Expected message '{messageId}' to be marked as failed but it was not.");
        }

        return this;
    }

    #endregion

    #region Time Control

    /// <summary>
    /// Advances time by the specified duration.
    /// </summary>
    /// <param name="duration">The duration to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper AdvanceTimeBy(TimeSpan duration)
    {
        _timeProvider.Advance(duration);
        return this;
    }

    /// <summary>
    /// Advances time to expire messages.
    /// </summary>
    /// <param name="days">Number of days to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public InboxTestHelper AdvanceTimeByDays(int days)
    {
        _timeProvider.Advance(TimeSpan.FromDays(days));
        return this;
    }

    #endregion

    #region Accessors

    /// <summary>
    /// Gets the cached response from a processed message.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="messageId">The message ID.</param>
    /// <returns>The cached response.</returns>
    public TResponse GetCachedResponse<TResponse>(string messageId) where TResponse : class
    {
        EnsureWhenExecuted();

        var message = _store.GetMessage(messageId);
        if (message is null)
        {
            throw new InvalidOperationException($"Message '{messageId}' not found in inbox.");
        }

        if (string.IsNullOrEmpty(message.Response))
        {
            throw new InvalidOperationException($"Message '{messageId}' has no cached response.");
        }

        return JsonSerializer.Deserialize<TResponse>(message.Response, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize cached response to {typeof(TResponse).Name}");
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
