using Encina.DomainModeling;

namespace Encina.TestInfrastructure.Entities;

/// <summary>
/// Test entity for read/write separation integration tests.
/// Used to verify connection routing based on database intent.
/// </summary>
/// <remarks>
/// This entity is used in integration tests to verify:
/// <list type="bullet">
/// <item><description>Read operations routing to read replicas</description></item>
/// <item><description>Write operations routing to primary</description></item>
/// <item><description>ForceWriteDatabase attribute behavior</description></item>
/// <item><description>Connection factory routing decisions</description></item>
/// </list>
/// </remarks>
public sealed class ReadWriteTestEntity : IEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value for testing updates.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the timestamp for testing time-based queries.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the counter for tracking write operations.
    /// Useful for verifying writes go to primary.
    /// </summary>
    public int WriteCounter { get; set; }

    /// <summary>
    /// Gets or sets the last read replica that served this entity.
    /// Populated by test infrastructure, not stored in database.
    /// </summary>
    public string? LastReadReplica { get; set; }
}

/// <summary>
/// Represents the database intent for routing purposes.
/// </summary>
public enum TestDatabaseIntent
{
    /// <summary>
    /// Read operation - can be routed to any replica.
    /// </summary>
    Read,

    /// <summary>
    /// Write operation - must be routed to primary.
    /// </summary>
    Write,

    /// <summary>
    /// Read operation that must go to primary (e.g., after write).
    /// </summary>
    ReadFromPrimary
}

/// <summary>
/// Test helper for tracking database connection routing decisions.
/// </summary>
public sealed class RoutingDecision
{
    /// <summary>
    /// Gets or sets the operation type that triggered the routing.
    /// </summary>
    public string OperationType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the intended database target.
    /// </summary>
    public TestDatabaseIntent Intent { get; set; }

    /// <summary>
    /// Gets or sets the actual connection string used.
    /// </summary>
    public string ConnectionStringUsed { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether the connection was routed to primary.
    /// </summary>
    public bool RoutedToPrimary { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the routing decision.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
