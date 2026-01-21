using Encina.Messaging.Health;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.MongoDB.Modules;

namespace Encina.MongoDB;

/// <summary>
/// Configuration options for Encina MongoDB integration.
/// </summary>
public sealed class EncinaMongoDbOptions
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    /// <example>mongodb://localhost:27017</example>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = "Encina";

    /// <summary>
    /// Gets or sets the collection names for messaging patterns.
    /// </summary>
    public MongoDbCollectionNames Collections { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to use the Outbox pattern.
    /// </summary>
    public bool UseOutbox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Inbox pattern.
    /// </summary>
    public bool UseInbox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Saga pattern.
    /// </summary>
    public bool UseSagas { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Scheduling pattern.
    /// </summary>
    public bool UseScheduling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create indexes automatically.
    /// </summary>
    public bool CreateIndexes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable module isolation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, MongoDB operations are routed to module-specific databases
    /// based on the current module execution context.
    /// </para>
    /// <para>
    /// Configure module isolation options via <see cref="ModuleIsolationOptions"/>.
    /// </para>
    /// </remarks>
    public bool UseModuleIsolation { get; set; }

    /// <summary>
    /// Gets the module isolation options.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="UseModuleIsolation"/> is <c>true</c>.
    /// </remarks>
    public MongoDbModuleIsolationOptions ModuleIsolationOptions { get; } = new();

    /// <summary>
    /// Gets or sets the saga options.
    /// </summary>
    public SagaOptions SagaOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the scheduling options.
    /// </summary>
    public SchedulingOptions SchedulingOptions { get; set; } = new();

    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();
}

/// <summary>
/// Collection names for MongoDB messaging patterns.
/// </summary>
public sealed class MongoDbCollectionNames
{
    /// <summary>
    /// Gets or sets the collection name for outbox messages.
    /// </summary>
    public string Outbox { get; set; } = "outbox_messages";

    /// <summary>
    /// Gets or sets the collection name for inbox messages.
    /// </summary>
    public string Inbox { get; set; } = "inbox_messages";

    /// <summary>
    /// Gets or sets the collection name for saga states.
    /// </summary>
    public string Sagas { get; set; } = "saga_states";

    /// <summary>
    /// Gets or sets the collection name for scheduled messages.
    /// </summary>
    public string ScheduledMessages { get; set; } = "scheduled_messages";
}
