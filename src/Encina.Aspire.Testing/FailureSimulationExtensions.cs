using Aspire.Hosting;
using Encina.Messaging;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Aspire.Testing;

/// <summary>
/// Extension methods for simulating failures in Encina integration tests.
/// </summary>
/// <remarks>
/// These methods help test resilience and error handling scenarios by artificially
/// inducing failures in the messaging infrastructure.
/// </remarks>
public static class FailureSimulationExtensions
{
    /// <summary>
    /// Simulates a saga timeout by marking the saga as timed out.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="sagaId">The saga ID to time out.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the saga is not found.</exception>
    /// <remarks>
    /// This method directly modifies the saga state to simulate a timeout scenario,
    /// allowing you to test compensation and timeout handling logic.
    /// </remarks>
    public static void SimulateSagaTimeout(this DistributedApplication app, Guid sagaId)
    {
        ArgumentNullException.ThrowIfNull(app);

        var store = app.Services.GetRequiredService<FakeSagaStore>();
        var saga = store.GetSaga(sagaId)
            ?? throw new InvalidOperationException($"Saga with ID '{sagaId}' was not found.");

        saga.Status = SagaStatus.TimedOut;
        saga.TimeoutAtUtc = DateTime.UtcNow;
        saga.LastUpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Simulates a saga failure with the specified error message.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="sagaId">The saga ID to fail.</param>
    /// <param name="errorMessage">The error message to set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the saga is not found.</exception>
    public static void SimulateSagaFailure(
        this DistributedApplication app,
        Guid sagaId,
        string errorMessage = "Simulated failure for testing")
    {
        ArgumentNullException.ThrowIfNull(app);

        var store = app.Services.GetRequiredService<FakeSagaStore>();
        var saga = store.GetSaga(sagaId)
            ?? throw new InvalidOperationException($"Saga with ID '{sagaId}' was not found.");

        saga.Status = SagaStatus.Failed;
        saga.ErrorMessage = errorMessage;
        saga.CompletedAtUtc = DateTime.UtcNow;
        saga.LastUpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Simulates an outbox message failure.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="messageId">
    /// The outbox message ID to fail. Outbox messages use <see cref="Guid"/> identifiers
    /// because they are system-generated for internal message tracking.
    /// </param>
    /// <param name="errorMessage">The error message to set.</param>
    /// <param name="nextRetryAtUtc">Optional next retry time.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> is <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the message is not found.</exception>
    public static void SimulateOutboxMessageFailure(
        this DistributedApplication app,
        Guid messageId,
        string errorMessage = "Simulated failure for testing",
        DateTime? nextRetryAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmpty, nameof(messageId));
        }

        var store = app.Services.GetRequiredService<FakeOutboxStore>();
        var message = store.GetMessage(messageId)
            ?? throw new InvalidOperationException($"Outbox message with ID '{messageId}' was not found.");

        message.ErrorMessage = errorMessage;
        message.RetryCount++;
        message.NextRetryAtUtc = nextRetryAtUtc ?? DateTime.UtcNow.AddMinutes(1);
    }

    /// <summary>
    /// Simulates moving an outbox message to the dead letter queue.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="messageId">The message ID to dead letter.</param>
    /// <param name="maxRetries">The max retries to exceed.</param>
    /// <param name="errorMessage">The error message to set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the message is not found.</exception>
    public static void SimulateOutboxDeadLetter(
        this DistributedApplication app,
        Guid messageId,
        int maxRetries = 3,
        string errorMessage = "Max retries exceeded")
    {
        ArgumentNullException.ThrowIfNull(app);

        var store = app.Services.GetRequiredService<FakeOutboxStore>();
        var message = store.GetMessage(messageId)
            ?? throw new InvalidOperationException($"Outbox message with ID '{messageId}' was not found.");

        message.ErrorMessage = errorMessage;
        message.RetryCount = maxRetries + 1;
        message.NextRetryAtUtc = null;
    }

    /// <summary>
    /// Simulates an inbox message failure.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="messageId">
    /// The inbox message ID to fail. Inbox messages use <see cref="string"/> identifiers
    /// because they are typically provided by external message brokers (e.g., correlation IDs, message IDs from RabbitMQ/Azure Service Bus).
    /// </param>
    /// <param name="errorMessage">The error message to set.</param>
    /// <param name="nextRetryAtUtc">Optional next retry time.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the message is not found.</exception>
    public static void SimulateInboxMessageFailure(
        this DistributedApplication app,
        string messageId,
        string errorMessage = "Simulated failure for testing",
        DateTime? nextRetryAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var store = app.Services.GetRequiredService<FakeInboxStore>();
        var message = store.GetMessage(messageId)
            ?? throw new InvalidOperationException($"Inbox message with ID '{messageId}' was not found.");

        message.ErrorMessage = errorMessage;
        message.RetryCount++;
        message.NextRetryAtUtc = nextRetryAtUtc ?? DateTime.UtcNow.AddMinutes(1);
    }

    /// <summary>
    /// Simulates expiration of an inbox message.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="messageId">The message ID to expire.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the message is not found.</exception>
    public static void SimulateInboxExpiration(
        this DistributedApplication app,
        string messageId)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var store = app.Services.GetRequiredService<FakeInboxStore>();
        var message = store.GetMessage(messageId)
            ?? throw new InvalidOperationException($"Inbox message with ID '{messageId}' was not found.");

        message.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
    }

    /// <summary>
    /// Adds a message directly to the dead letter store.
    /// </summary>
    /// <param name="app">The distributed application.</param>
    /// <param name="requestType">The type of the original request.</param>
    /// <param name="requestContent">The request content.</param>
    /// <param name="sourcePattern">The source pattern (e.g., "Outbox", "Inbox").</param>
    /// <param name="errorMessage">The error that caused dead lettering.</param>
    /// <param name="totalRetryAttempts">The number of retry attempts.</param>
    /// <returns>The ID of the created dead letter message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static async Task<Guid> AddToDeadLetterAsync(
        this DistributedApplication app,
        string requestType,
        string requestContent,
        string sourcePattern = "Simulated",
        string errorMessage = "Simulated dead letter",
        int totalRetryAttempts = 3)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestType);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestContent);

        var store = app.Services.GetRequiredService<FakeDeadLetterStore>();
        var now = DateTime.UtcNow;
        var deadLetterMessage = new FakeDeadLetterMessage
        {
            Id = Guid.NewGuid(),
            RequestType = requestType,
            RequestContent = requestContent,
            SourcePattern = sourcePattern,
            ErrorMessage = errorMessage,
            TotalRetryAttempts = totalRetryAttempts,
            FirstFailedAtUtc = now,
            DeadLetteredAtUtc = now
        };

        await store.AddAsync(deadLetterMessage);
        return deadLetterMessage.Id;
    }
}
