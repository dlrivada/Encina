using Bogus;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

namespace Encina.TestInfrastructure.PropertyTests;

/// <summary>
/// Provides Bogus-based data generators for messaging entities.
/// These generators create data that can be mapped to any provider's concrete types.
/// </summary>
/// <remarks>
/// <para>
/// This class generates <see cref="IInboxMessage"/>, <see cref="IOutboxMessage"/>, etc.
/// interface implementations that can be used to populate concrete provider types.
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// // Generate inbox message data
/// var data = MessageDataGenerators.GenerateInboxData(seed);
///
/// // Create provider-specific message
/// var message = new InboxMessage
/// {
///     MessageId = data.MessageId,
///     RequestType = data.RequestType,
///     // ... map other properties
/// };
/// </code>
/// </para>
/// </remarks>
public static class MessageDataGenerators
{
    private static readonly string[] RequestTypes =
    [
        "CreateOrderCommand",
        "UpdateOrderCommand",
        "CancelOrderCommand",
        "ProcessPaymentCommand",
        "RefundPaymentCommand",
        "RegisterCustomerCommand",
        "UpdateInventoryCommand",
        "SendNotificationCommand"
    ];

    private static readonly string[] NotificationTypes =
    [
        "OrderCreated",
        "OrderCompleted",
        "OrderCancelled",
        "PaymentReceived",
        "PaymentFailed",
        "ShipmentDispatched",
        "CustomerRegistered",
        "InventoryUpdated"
    ];

    private static readonly string[] SagaTypes =
    [
        "OrderFulfillmentSaga",
        "PaymentProcessingSaga",
        "CustomerOnboardingSaga",
        "InventoryReservationSaga",
        "ShippingCoordinationSaga"
    ];

    private static readonly string[] SagaStatuses = ["Running", "Completed", "Compensating", "Failed"];

    /// <summary>
    /// Generates inbox message data using a seed for reproducibility.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <returns>Generated inbox message data.</returns>
    public static InboxMessageData GenerateInboxData(int seed)
    {
        var faker = new Faker { Random = new Randomizer(seed) };

        return new InboxMessageData
        {
            MessageId = faker.Random.Guid().ToString(),
            RequestType = faker.PickRandom(RequestTypes),
            ReceivedAtUtc = DateTime.SpecifyKind(faker.Date.Recent(7), DateTimeKind.Utc),
            ProcessedAtUtc = null,
            ExpiresAtUtc = DateTime.SpecifyKind(faker.Date.Soon(30), DateTimeKind.Utc),
            Response = null,
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };
    }

    /// <summary>
    /// Generates processed inbox message data.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <returns>Generated processed inbox message data.</returns>
    public static InboxMessageData GenerateProcessedInboxData(int seed)
    {
        var faker = new Faker { Random = new Randomizer(seed) };
        var receivedAt = DateTime.SpecifyKind(faker.Date.Recent(7), DateTimeKind.Utc);

        return new InboxMessageData
        {
            MessageId = faker.Random.Guid().ToString(),
            RequestType = faker.PickRandom(RequestTypes),
            ReceivedAtUtc = receivedAt,
            ProcessedAtUtc = receivedAt.AddSeconds(faker.Random.Int(1, 60)),
            ExpiresAtUtc = DateTime.SpecifyKind(faker.Date.Soon(30), DateTimeKind.Utc),
            Response = GenerateJsonContent(faker),
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };
    }

    /// <summary>
    /// Generates failed inbox message data.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <param name="retryCount">The number of retries (default: random 1-5).</param>
    /// <returns>Generated failed inbox message data.</returns>
    public static InboxMessageData GenerateFailedInboxData(int seed, int? retryCount = null)
    {
        var faker = new Faker { Random = new Randomizer(seed) };

        return new InboxMessageData
        {
            MessageId = faker.Random.Guid().ToString(),
            RequestType = faker.PickRandom(RequestTypes),
            ReceivedAtUtc = DateTime.SpecifyKind(faker.Date.Recent(7), DateTimeKind.Utc),
            ProcessedAtUtc = null,
            ExpiresAtUtc = DateTime.SpecifyKind(faker.Date.Soon(30), DateTimeKind.Utc),
            Response = null,
            ErrorMessage = faker.Lorem.Sentence(),
            RetryCount = retryCount ?? faker.Random.Int(1, 5),
            NextRetryAtUtc = DateTime.SpecifyKind(faker.Date.Soon(1), DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Generates outbox message data using a seed for reproducibility.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <returns>Generated outbox message data.</returns>
    public static OutboxMessageData GenerateOutboxData(int seed)
    {
        var faker = new Faker { Random = new Randomizer(seed) };

        return new OutboxMessageData
        {
            Id = faker.Random.Guid(),
            NotificationType = faker.PickRandom(NotificationTypes),
            Content = GenerateJsonContent(faker),
            CreatedAtUtc = DateTime.SpecifyKind(faker.Date.Recent(7), DateTimeKind.Utc),
            ProcessedAtUtc = null,
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };
    }

    /// <summary>
    /// Generates processed outbox message data.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <returns>Generated processed outbox message data.</returns>
    public static OutboxMessageData GenerateProcessedOutboxData(int seed)
    {
        var faker = new Faker { Random = new Randomizer(seed) };
        var createdAt = DateTime.SpecifyKind(faker.Date.Recent(7), DateTimeKind.Utc);

        return new OutboxMessageData
        {
            Id = faker.Random.Guid(),
            NotificationType = faker.PickRandom(NotificationTypes),
            Content = GenerateJsonContent(faker),
            CreatedAtUtc = createdAt,
            ProcessedAtUtc = createdAt.AddSeconds(faker.Random.Int(1, 60)),
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };
    }

    /// <summary>
    /// Generates saga state data using a seed for reproducibility.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <returns>Generated saga state data.</returns>
    public static SagaStateData GenerateSagaStateData(int seed)
    {
        var faker = new Faker { Random = new Randomizer(seed) };
        var startedAt = DateTime.SpecifyKind(faker.Date.Recent(7), DateTimeKind.Utc);

        return new SagaStateData
        {
            SagaId = faker.Random.Guid(),
            SagaType = faker.PickRandom(SagaTypes),
            CurrentState = faker.PickRandom(SagaStatuses),
            StateData = GenerateJsonContent(faker),
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = startedAt.AddSeconds(faker.Random.Int(1, 3600)),
            CompletedAtUtc = null,
            Version = faker.Random.Int(1, 10)
        };
    }

    /// <summary>
    /// Generates scheduled message data using a seed for reproducibility.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <returns>Generated scheduled message data.</returns>
    public static ScheduledMessageData GenerateScheduledMessageData(int seed)
    {
        var faker = new Faker { Random = new Randomizer(seed) };

        return new ScheduledMessageData
        {
            Id = faker.Random.Guid(),
            RequestType = faker.PickRandom(RequestTypes),
            Content = GenerateJsonContent(faker),
            ScheduledAtUtc = DateTime.SpecifyKind(faker.Date.Soon(7), DateTimeKind.Utc),
            CreatedAtUtc = DateTime.SpecifyKind(faker.Date.Recent(1), DateTimeKind.Utc),
            ProcessedAtUtc = null,
            ErrorMessage = null,
            RetryCount = 0
        };
    }

    private static string GenerateJsonContent(Faker faker, int propertyCount = 3)
    {
        var properties = new List<string>(propertyCount);
        for (int i = 0; i < propertyCount; i++)
        {
            var key = faker.Lorem.Word();
            var value = faker.Lorem.Sentence().Replace("\"", "\\\"");
            properties.Add($"\"{key}\":\"{value}\"");
        }

        return "{" + string.Join(",", properties) + "}";
    }
}

/// <summary>
/// Data transfer object for inbox message properties.
/// </summary>
public sealed record InboxMessageData
{
    /// <summary>Gets or sets the message ID.</summary>
    public required string MessageId { get; init; }

    /// <summary>Gets or sets the request type.</summary>
    public required string RequestType { get; init; }

    /// <summary>Gets or sets when the message was received (UTC).</summary>
    public required DateTime ReceivedAtUtc { get; init; }

    /// <summary>Gets or sets when the message was processed (UTC).</summary>
    public DateTime? ProcessedAtUtc { get; init; }

    /// <summary>Gets or sets when the message expires (UTC).</summary>
    public required DateTime ExpiresAtUtc { get; init; }

    /// <summary>Gets or sets the response payload.</summary>
    public string? Response { get; init; }

    /// <summary>Gets or sets the error message if failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets or sets the retry count.</summary>
    public int RetryCount { get; init; }

    /// <summary>Gets or sets the next retry time (UTC).</summary>
    public DateTime? NextRetryAtUtc { get; init; }
}

/// <summary>
/// Data transfer object for outbox message properties.
/// </summary>
public sealed record OutboxMessageData
{
    /// <summary>Gets or sets the message ID.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets or sets the notification type.</summary>
    public required string NotificationType { get; init; }

    /// <summary>Gets or sets the content payload.</summary>
    public required string Content { get; init; }

    /// <summary>Gets or sets when the message was created (UTC).</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets or sets when the message was processed (UTC).</summary>
    public DateTime? ProcessedAtUtc { get; init; }

    /// <summary>Gets or sets the error message if failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets or sets the retry count.</summary>
    public int RetryCount { get; init; }

    /// <summary>Gets or sets the next retry time (UTC).</summary>
    public DateTime? NextRetryAtUtc { get; init; }
}

/// <summary>
/// Data transfer object for saga state properties.
/// </summary>
public sealed record SagaStateData
{
    /// <summary>Gets or sets the saga ID.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Gets or sets the saga type name.</summary>
    public required string SagaType { get; init; }

    /// <summary>Gets or sets the current state.</summary>
    public required string CurrentState { get; init; }

    /// <summary>Gets or sets the state data payload.</summary>
    public required string StateData { get; init; }

    /// <summary>Gets or sets when the saga started (UTC).</summary>
    public required DateTime StartedAtUtc { get; init; }

    /// <summary>Gets or sets when the saga was last updated (UTC).</summary>
    public required DateTime LastUpdatedAtUtc { get; init; }

    /// <summary>Gets or sets when the saga completed (UTC).</summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>Gets or sets the version for optimistic concurrency.</summary>
    public int Version { get; init; }
}

/// <summary>
/// Data transfer object for scheduled message properties.
/// </summary>
public sealed record ScheduledMessageData
{
    /// <summary>Gets or sets the message ID.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets or sets the request type.</summary>
    public required string RequestType { get; init; }

    /// <summary>Gets or sets the content payload.</summary>
    public required string Content { get; init; }

    /// <summary>Gets or sets when the message should be processed (UTC).</summary>
    public required DateTime ScheduledAtUtc { get; init; }

    /// <summary>Gets or sets when the message was created (UTC).</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets or sets when the message was processed (UTC).</summary>
    public DateTime? ProcessedAtUtc { get; init; }

    /// <summary>Gets or sets the error message if failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets or sets the retry count.</summary>
    public int RetryCount { get; init; }
}
