using Encina.MongoDB.Outbox;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Benchmarks.Infrastructure;

/// <summary>
/// Benchmark entity stored as a BSON document. Mirrors the shape of the ADO/Dapper
/// <c>BenchmarkRepositoryEntity</c> so benchmark numbers remain comparable across providers.
/// </summary>
public sealed class BenchmarkRepositoryEntity
{
    /// <summary>Gets or sets the entity identifier (stored as a BSON string).</summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity name.</summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity description.</summary>
    [BsonElement("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the price.</summary>
    [BsonElement("price")]
    public decimal Price { get; set; }

    /// <summary>Gets or sets the quantity.</summary>
    [BsonElement("quantity")]
    public int Quantity { get; set; }

    /// <summary>Gets or sets a value indicating whether the entity is active.</summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the category.</summary>
    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation timestamp.</summary>
    [BsonElement("createdAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    [BsonElement("updatedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Factory methods for creating benchmark entities and outbox messages.
/// </summary>
public static class BenchmarkEntityFactory
{
    private static readonly string[] Categories = ["Electronics", "Books", "Clothing", "Home", "Sports"];

    /// <summary>
    /// Creates a single <see cref="BenchmarkRepositoryEntity"/> with randomized field values.
    /// </summary>
    /// <param name="id">Optional entity ID. If not provided, a new GUID is generated.</param>
    /// <returns>A freshly populated benchmark entity.</returns>
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
            IsActive = random.NextDouble() > 0.2,
            Category = Categories[random.Next(Categories.Length)],
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }

    /// <summary>
    /// Creates N <see cref="BenchmarkRepositoryEntity"/> instances.
    /// </summary>
    /// <param name="count">The number of entities to create.</param>
    /// <returns>A list with <paramref name="count"/> benchmark entities.</returns>
    public static List<BenchmarkRepositoryEntity> CreateRepositoryEntities(int count)
    {
        return Enumerable.Range(0, count).Select(_ => CreateRepositoryEntity()).ToList();
    }

    /// <summary>
    /// Creates a single <see cref="OutboxMessage"/> ready to append to the outbox collection.
    /// </summary>
    /// <param name="id">Optional message ID. If not provided, a new GUID is generated.</param>
    /// <returns>A benchmark-specific outbox message.</returns>
    public static OutboxMessage CreateOutboxMessage(Guid? id = null)
    {
        return new OutboxMessage
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
}
