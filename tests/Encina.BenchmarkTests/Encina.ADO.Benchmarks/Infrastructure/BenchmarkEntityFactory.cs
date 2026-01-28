using Encina.ADO.Sqlite.Repository;
using Encina.Messaging.Outbox;
using Encina.Messaging.Inbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

namespace Encina.ADO.Benchmarks.Infrastructure;

/// <summary>
/// Factory for creating benchmark entities for ADO.NET store testing.
/// </summary>
public static class BenchmarkEntityFactory
{
    /// <summary>
    /// Creates a test outbox message for benchmarking.
    /// </summary>
    /// <param name="id">Optional message ID. If not provided, a new GUID is generated.</param>
    /// <returns>A test outbox message.</returns>
    public static BenchmarkOutboxMessage CreateOutboxMessage(Guid? id = null)
    {
        return new BenchmarkOutboxMessage
        {
            Id = id ?? Guid.NewGuid(),
            NotificationType = "Encina.Tests.OrderCreatedEvent",
            Content = """{"OrderId":"12345","CustomerId":"cust-001","TotalAmount":99.99}""",
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null,
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };
    }

    /// <summary>
    /// Creates multiple test outbox messages for benchmarking.
    /// </summary>
    /// <param name="count">The number of messages to create.</param>
    /// <returns>A list of test outbox messages.</returns>
    public static List<BenchmarkOutboxMessage> CreateOutboxMessages(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateOutboxMessage())
            .ToList();
    }

    /// <summary>
    /// Creates a test inbox message for benchmarking.
    /// </summary>
    /// <param name="id">Optional message ID. If not provided, a new GUID is generated.</param>
    /// <returns>A test inbox message.</returns>
    public static BenchmarkInboxMessage CreateInboxMessage(Guid? id = null)
    {
        var messageId = id ?? Guid.NewGuid();
        return new BenchmarkInboxMessage
        {
            MessageId = messageId.ToString(),
            RequestType = "Encina.Tests.ProcessOrderCommand",
            Response = null,
            ErrorMessage = null,
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0,
            NextRetryAtUtc = null
        };
    }

    /// <summary>
    /// Creates multiple test inbox messages for benchmarking.
    /// </summary>
    /// <param name="count">The number of messages to create.</param>
    /// <returns>A list of test inbox messages.</returns>
    public static List<BenchmarkInboxMessage> CreateInboxMessages(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateInboxMessage())
            .ToList();
    }

    /// <summary>
    /// Creates a test saga state for benchmarking.
    /// </summary>
    /// <param name="sagaId">Optional saga ID. If not provided, a new GUID is generated.</param>
    /// <returns>A test saga state.</returns>
    public static BenchmarkSagaState CreateSagaState(Guid? sagaId = null)
    {
        return new BenchmarkSagaState
        {
            SagaId = sagaId ?? Guid.NewGuid(),
            SagaType = "Encina.Tests.OrderFulfillmentSaga",
            Data = """{"OrderId":"12345","Step":"PaymentPending"}""",
            Status = "Running",
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = null,
            ErrorMessage = null,
            TimeoutAtUtc = DateTime.UtcNow.AddHours(1)
        };
    }

    /// <summary>
    /// Creates multiple test saga states for benchmarking.
    /// </summary>
    /// <param name="count">The number of saga states to create.</param>
    /// <returns>A list of test saga states.</returns>
    public static List<BenchmarkSagaState> CreateSagaStates(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateSagaState())
            .ToList();
    }

    /// <summary>
    /// Creates a test scheduled message for benchmarking.
    /// </summary>
    /// <param name="id">Optional message ID. If not provided, a new GUID is generated.</param>
    /// <returns>A test scheduled message.</returns>
    public static BenchmarkScheduledMessage CreateScheduledMessage(Guid? id = null)
    {
        return new BenchmarkScheduledMessage
        {
            Id = id ?? Guid.NewGuid(),
            RequestType = "Encina.Tests.SendReminderCommand",
            Content = """{"UserId":"user-001","ReminderType":"OrderShipped"}""",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(30),
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null,
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null,
            IsRecurring = false,
            CronExpression = null,
            LastExecutedAtUtc = null
        };
    }

    /// <summary>
    /// Creates multiple test scheduled messages for benchmarking.
    /// </summary>
    /// <param name="count">The number of messages to create.</param>
    /// <returns>A list of test scheduled messages.</returns>
    public static List<BenchmarkScheduledMessage> CreateScheduledMessages(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateScheduledMessage())
            .ToList();
    }

    private static readonly string[] Categories = ["Electronics", "Books", "Clothing", "Home", "Sports"];

    /// <summary>
    /// Creates a test repository entity for benchmarking.
    /// </summary>
    /// <param name="id">Optional entity ID. If not provided, a new GUID is generated.</param>
    /// <returns>A test repository entity.</returns>
    public static BenchmarkRepositoryEntity CreateRepositoryEntity(Guid? id = null)
    {
        var random = Random.Shared;
        return new BenchmarkRepositoryEntity
        {
            Id = id ?? Guid.NewGuid(),
            Name = $"Product-{random.Next(1, 10000)}",
            Description = "A test product for benchmark testing",
            Price = Math.Round((decimal)(random.NextDouble() * 1000), 2),
            Quantity = random.Next(1, 100),
            IsActive = random.NextDouble() > 0.2, // 80% active
            Category = Categories[random.Next(Categories.Length)],
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }

    /// <summary>
    /// Creates multiple test repository entities for benchmarking.
    /// </summary>
    /// <param name="count">The number of entities to create.</param>
    /// <returns>A list of test repository entities.</returns>
    public static List<BenchmarkRepositoryEntity> CreateRepositoryEntities(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateRepositoryEntity())
            .ToList();
    }
}

/// <summary>
/// Benchmark-specific outbox message implementation.
/// </summary>
public sealed class BenchmarkOutboxMessage : IOutboxMessage
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string NotificationType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
}

/// <summary>
/// Benchmark-specific inbox message implementation.
/// </summary>
public sealed class BenchmarkInboxMessage : IInboxMessage
{
    /// <inheritdoc />
    public string MessageId { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Response { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public DateTime ReceivedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime ExpiresAtUtc { get; set; }

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc />
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAtUtc;
}

/// <summary>
/// Benchmark-specific saga state implementation.
/// </summary>
public sealed class BenchmarkSagaState : ISagaState
{
    /// <inheritdoc />
    public Guid SagaId { get; set; }

    /// <inheritdoc />
    public string SagaType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Data { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Status { get; set; } = string.Empty;

    /// <inheritdoc />
    public int CurrentStep { get; set; }

    /// <inheritdoc />
    public DateTime StartedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? CompletedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? TimeoutAtUtc { get; set; }
}

/// <summary>
/// Benchmark-specific scheduled message implementation.
/// </summary>
public sealed class BenchmarkScheduledMessage : IScheduledMessage
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime ScheduledAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsRecurring { get; set; }

    /// <inheritdoc />
    public string? CronExpression { get; set; }

    /// <inheritdoc />
    public DateTime? LastExecutedAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <inheritdoc />
    public bool IsDue() => DateTime.UtcNow >= ScheduledAtUtc && !IsProcessed;

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
}

/// <summary>
/// Benchmark entity for repository testing.
/// </summary>
public sealed class BenchmarkRepositoryEntity
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets whether the entity is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Entity mapping for BenchmarkRepositoryEntity.
/// </summary>
public sealed class BenchmarkRepositoryEntityMapping : IEntityMapping<BenchmarkRepositoryEntity, Guid>
{
    /// <inheritdoc />
    public string TableName => "BenchmarkEntities";

    /// <inheritdoc />
    public string IdColumnName => "Id";

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["Name"] = "Name",
        ["Description"] = "Description",
        ["Price"] = "Price",
        ["Quantity"] = "Quantity",
        ["IsActive"] = "IsActive",
        ["Category"] = "Category",
        ["CreatedAtUtc"] = "CreatedAtUtc",
        ["UpdatedAtUtc"] = "UpdatedAtUtc"
    };

    /// <inheritdoc />
    public IReadOnlySet<string> InsertExcludedProperties { get; } = new HashSet<string>();

    /// <inheritdoc />
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new HashSet<string> { "Id", "CreatedAtUtc" };

    /// <inheritdoc />
    public Guid GetId(BenchmarkRepositoryEntity entity) => entity.Id;
}
