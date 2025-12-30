using Aspire.Hosting;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Aspire.Testing;

/// <summary>
/// Extension methods for <see cref="DistributedApplication"/> to provide Encina test assertions and helpers.
/// </summary>
public static class DistributedApplicationExtensions
{
    #region Service Access

    /// <summary>
    /// Gets the Encina test context from the distributed application.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <returns>The Encina test context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when WithEncinaTestSupport was not called.</exception>
    public static EncinaTestContext GetEncinaTestContext(this DistributedApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Services.GetRequiredService<EncinaTestContext>();
    }

    /// <summary>
    /// Gets the fake outbox store from the distributed application.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <returns>The fake outbox store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static FakeOutboxStore GetOutboxStore(this DistributedApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Services.GetRequiredService<FakeOutboxStore>();
    }

    /// <summary>
    /// Gets the fake inbox store from the distributed application.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <returns>The fake inbox store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static FakeInboxStore GetInboxStore(this DistributedApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Services.GetRequiredService<FakeInboxStore>();
    }

    /// <summary>
    /// Gets the fake saga store from the distributed application.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <returns>The fake saga store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static FakeSagaStore GetSagaStore(this DistributedApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Services.GetRequiredService<FakeSagaStore>();
    }

    #endregion

    #region Outbox Assertions

    /// <summary>
    /// Asserts that the outbox contains a message of the specified notification type.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to check for.</typeparam>
    /// <param name="app">The distributed application.</param>
    /// <param name="timeout">Optional timeout for the assertion. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the message is not found within the timeout.</exception>
    public static async Task AssertOutboxContainsAsync<TNotification>(
        this DistributedApplication app,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;
        var notificationType = typeof(TNotification).FullName ?? typeof(TNotification).Name;

        await WaitForConditionAsync(
            () => context.OutboxStore.WasMessageAdded(notificationType),
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Outbox message of type '{notificationType}' was not found within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    /// <summary>
    /// Asserts that the outbox contains a message matching the specified predicate.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="predicate">The predicate to match messages against.</param>
    /// <param name="timeout">Optional timeout for the assertion. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when a matching message is not found within the timeout.</exception>
    public static async Task AssertOutboxContainsAsync(
        this DistributedApplication app,
        Func<IOutboxMessage, bool> predicate,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(predicate);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;

        await WaitForConditionAsync(
            () => context.OutboxStore.AddedMessages.Any(predicate),
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Outbox message matching predicate was not found within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    /// <summary>
    /// Gets all pending outbox messages.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <returns>Collection of pending outbox messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static IReadOnlyCollection<FakeOutboxMessage> GetPendingOutboxMessages(this DistributedApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var store = app.GetOutboxStore();
        return store.Messages.Where(m => !m.IsProcessed).ToList().AsReadOnly();
    }

    #endregion

    #region Inbox Assertions

    /// <summary>
    /// Asserts that an inbox message was processed.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="messageId">The message ID to check.</param>
    /// <param name="timeout">Optional timeout for the assertion. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> or <paramref name="messageId"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the message is not processed within the timeout.</exception>
    public static async Task AssertInboxProcessedAsync(
        this DistributedApplication app,
        string messageId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;

        await WaitForConditionAsync(
            () => context.InboxStore.ProcessedMessageIds.Contains(messageId),
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Inbox message '{messageId}' was not processed within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    /// <summary>
    /// Asserts that an inbox message of the specified type was processed.
    /// </summary>
    /// <typeparam name="TMessage">The message type to check for.</typeparam>
    /// <param name="app">The distributed application.</param>
    /// <param name="timeout">Optional timeout for the assertion. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the message is not processed within the timeout.</exception>
    public static async Task AssertInboxProcessedAsync<TMessage>(
        this DistributedApplication app,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;
        var requestType = typeof(TMessage).FullName ?? typeof(TMessage).Name;

        await WaitForConditionAsync(
            () => context.InboxStore.Messages.Any(m =>
                m.RequestType == requestType && m.IsProcessed),
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Inbox message of type '{requestType}' was not processed within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    #endregion

    #region Saga Assertions

    /// <summary>
    /// Asserts that a saga of the specified type has completed.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <param name="app">The distributed application.</param>
    /// <param name="predicate">Optional predicate to filter sagas.</param>
    /// <param name="timeout">Optional timeout for the assertion. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the saga is not completed within the timeout.</exception>
    public static async Task AssertSagaCompletedAsync<TSaga>(
        this DistributedApplication app,
        Func<ISagaState, bool>? predicate = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;
        var sagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name;

        await WaitForConditionAsync(
            () =>
            {
                var sagas = context.SagaStore.Sagas
                    .Where(s => string.Equals(s.SagaType, sagaType, StringComparison.Ordinal));

                if (predicate != null)
                {
                    sagas = sagas.Where(s => predicate(s));
                }

                return sagas.Any(s => s.Status == SagaStatus.Completed);
            },
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Saga of type '{sagaType}' was not completed within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    /// <summary>
    /// Asserts that a saga of the specified type has been compensated.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <param name="app">The distributed application.</param>
    /// <param name="predicate">Optional predicate to filter sagas.</param>
    /// <param name="timeout">Optional timeout for the assertion. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the saga is not compensated within the timeout.</exception>
    public static async Task AssertSagaCompensatedAsync<TSaga>(
        this DistributedApplication app,
        Func<ISagaState, bool>? predicate = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;
        var sagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name;

        await WaitForConditionAsync(
            () =>
            {
                var sagas = context.SagaStore.Sagas
                    .Where(s => string.Equals(s.SagaType, sagaType, StringComparison.Ordinal));

                if (predicate != null)
                {
                    sagas = sagas.Where(s => predicate(s));
                }

                return sagas.Any(s => s.Status == SagaStatus.Compensating || s.Status == SagaStatus.Failed);
            },
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Saga of type '{sagaType}' was not compensated within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    /// <summary>
    /// Gets all running sagas of the specified type.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to filter by.</typeparam>
    /// <param name="app">The distributed application.</param>
    /// <returns>Collection of running sagas.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static IReadOnlyList<FakeSagaState> GetRunningSagas<TSaga>(this DistributedApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var store = app.GetSagaStore();
        var sagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name;

        return store.Sagas
            .Where(s => string.Equals(s.SagaType, sagaType, StringComparison.Ordinal) &&
                        s.Status == SagaStatus.Running)
            .ToList()
            .AsReadOnly();
    }

    #endregion

    #region Dead Letter Assertions

    /// <summary>
    /// Asserts that the dead letter queue contains a message of the specified type.
    /// </summary>
    /// <typeparam name="TMessage">The message type to check for.</typeparam>
    /// <param name="app">The distributed application.</param>
    /// <param name="timeout">Optional timeout for the assertion. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when a matching message is not found within the timeout.</exception>
    public static async Task AssertDeadLetterContainsAsync<TMessage>(
        this DistributedApplication app,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;
        var messageType = typeof(TMessage).FullName ?? typeof(TMessage).Name;

        await WaitForConditionAsync(
            () => context.DeadLetterStore.Messages.Any(m => m.RequestType == messageType),
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Dead letter message of type '{messageType}' was not found within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    /// <summary>
    /// Gets all dead letter messages.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <returns>Collection of dead letter messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static IReadOnlyCollection<FakeDeadLetterMessage> GetDeadLetterMessages(this DistributedApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var store = app.Services.GetRequiredService<FakeDeadLetterStore>();
        return store.Messages;
    }

    #endregion

    #region Wait Helpers

    /// <summary>
    /// Waits for the expected number of outbox messages to be processed.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="expectedMessageCount">The minimum number of messages expected to be processed. Defaults to 1.</param>
    /// <param name="timeout">Optional timeout. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the expected messages are processed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="expectedMessageCount"/> is negative.</exception>
    /// <exception cref="TimeoutException">Thrown when messages are not processed within the timeout.</exception>
    public static async Task WaitForOutboxProcessingAsync(
        this DistributedApplication app,
        int expectedMessageCount = 1,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedMessageCount);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;

        await WaitForConditionAsync(
            () =>
            {
                var processedCount = context.OutboxStore.Messages.Count(m => m.IsProcessed);
                return processedCount >= expectedMessageCount;
            },
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Expected at least {expectedMessageCount} outbox message(s) to be processed within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    /// <summary>
    /// Waits for a specific saga to complete.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="app">The distributed application.</param>
    /// <param name="sagaId">The saga ID to wait for.</param>
    /// <param name="timeout">Optional timeout. Defaults to the configured timeout.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the saga is completed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the saga is not completed within the timeout.</exception>
    public static async Task WaitForSagaCompletionAsync<TSaga>(
        this DistributedApplication app,
        Guid sagaId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var context = app.GetEncinaTestContext();
        var effectiveTimeout = timeout ?? context.Options.DefaultWaitTimeout;

        await WaitForConditionAsync(
            () =>
            {
                var saga = context.SagaStore.GetSaga(sagaId);
                return saga?.Status == SagaStatus.Completed;
            },
            effectiveTimeout,
            context.Options.PollingInterval,
            $"Saga '{sagaId}' was not completed within {effectiveTimeout.TotalSeconds} seconds.",
            cancellationToken);
    }

    #endregion

    #region Private Helpers

    private static async Task WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan pollingInterval,
        string timeoutMessage,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            while (!condition())
            {
                await Task.Delay(pollingInterval, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(timeoutMessage);
        }
    }

    #endregion
}
