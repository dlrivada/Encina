using Encina.Messaging.Auditing;
using Encina.Messaging.ContentRouter;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.DomainEvents;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Messaging.Recoverability;
using Encina.Messaging.RoutingSlip;
using Encina.Messaging.Sagas;
using Encina.Messaging.ScatterGather;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Tenancy;
using Encina.Modules.Isolation;

namespace Encina.Messaging;

/// <summary>
/// Configuration for messaging patterns in Encina.
/// </summary>
/// <remarks>
/// <para>
/// This configuration allows users to opt-in to specific messaging patterns:
/// <list type="bullet">
/// <item><description><b>Transactions</b>: Automatic database transaction management</description></item>
/// <item><description><b>Outbox</b>: Reliable event publishing (at-least-once delivery)</description></item>
/// <item><description><b>Inbox</b>: Idempotent message processing (exactly-once semantics)</description></item>
/// <item><description><b>Sagas</b>: Distributed transactions with compensation</description></item>
/// <item><description><b>RoutingSlips</b>: Dynamic message routing through multiple steps</description></item>
/// <item><description><b>Scheduling</b>: Delayed/recurring message execution</description></item>
/// <item><description><b>Recoverability</b>: Automatic retry with immediate and delayed strategies</description></item>
/// <item><description><b>ContentRouter</b>: Route messages to handlers based on content inspection</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Design Philosophy</b>: All patterns are OPTIONAL. Users only pay for what they use.
/// A simple CRUD app might only use transactions, while a complex distributed system
/// might use all patterns.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple app - only transactions
/// services.AddEncinaMessaging(config =>
/// {
///     config.UseTransactions = true;
/// });
///
/// // Distributed system - all patterns
/// services.AddEncinaMessaging(config =>
/// {
///     config.UseTransactions = true;
///     config.UseOutbox = true;
///     config.UseInbox = true;
///     config.UseSagas = true;
///     config.UseRoutingSlips = true;
///     config.UseScheduling = true;
///     config.UseRecoverability = true;
///     config.UseContentRouter = true;
/// });
/// </code>
/// </example>
public sealed class MessagingConfiguration
{
    /// <summary>
    /// Gets or sets whether to enable automatic transaction management.
    /// </summary>
    /// <remarks>
    /// When enabled, commands marked with <c>[Transaction]</c> or <c>ITransactionalCommand</c>
    /// will automatically be wrapped in database transactions.
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseTransactions { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the Outbox pattern for reliable event publishing.
    /// </summary>
    /// <remarks>
    /// When enabled, notifications are stored in the database and published by a background
    /// processor, ensuring they are never lost even if the system crashes.
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseOutbox { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the Inbox pattern for idempotent message processing.
    /// </summary>
    /// <remarks>
    /// When enabled, commands marked with <c>IIdempotentRequest</c> are tracked to prevent
    /// duplicate processing, ensuring exactly-once semantics.
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseInbox { get; set; }

    /// <summary>
    /// Gets or sets whether to enable Saga orchestration for distributed transactions.
    /// </summary>
    /// <remarks>
    /// When enabled, saga state is persisted and can be resumed/compensated if steps fail.
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseSagas { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the Routing Slip pattern for dynamic message routing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, messages can be routed through multiple processing steps, with
    /// the ability to dynamically add, remove, or modify steps during execution.
    /// </para>
    /// <para>
    /// Unlike Sagas where steps are predefined, Routing Slips allow each step to
    /// modify the remaining itinerary based on the result of its execution.
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseRoutingSlips { get; set; }

    /// <summary>
    /// Gets or sets whether to enable scheduled/delayed message execution.
    /// </summary>
    /// <remarks>
    /// When enabled, messages can be scheduled for future execution (delays, timeouts, recurring tasks).
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseScheduling { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the Recoverability Pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, failed requests are automatically retried using a two-phase strategy:
    /// <list type="number">
    /// <item><description>Immediate retries: Fast, in-memory retries for transient failures</description></item>
    /// <item><description>Delayed retries: Persistent, scheduled retries for extended recovery</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Errors are classified to determine retry behavior. Permanent errors skip retries
    /// and go directly to the Dead Letter Queue (DLQ).
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseRecoverability { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the Dead Letter Queue pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, permanently failed messages are stored in the DLQ for:
    /// <list type="bullet">
    /// <item><description>Inspection and debugging</description></item>
    /// <item><description>Manual or automatic replay</description></item>
    /// <item><description>Alerting and monitoring</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The DLQ integrates with other messaging patterns (Outbox, Inbox, Recoverability, Sagas)
    /// to capture messages that exhaust their retry attempts.
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseDeadLetterQueue { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the Content-Based Router pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, messages can be routed to different handlers based on their
    /// content rather than just their type. This is an Enterprise Integration Pattern (EIP)
    /// that enables dynamic routing based on message properties.
    /// </para>
    /// <para>
    /// Use cases include:
    /// <list type="bullet">
    /// <item><description>Routing high-value orders to a specialized handler</description></item>
    /// <item><description>Routing international orders to a compliance handler</description></item>
    /// <item><description>Routing messages based on priority, category, or other properties</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseContentRouter { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the Scatter-Gather pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, requests can be broadcast to multiple handlers (scatter) and
    /// their responses aggregated using a configurable strategy (gather).
    /// This is an Enterprise Integration Pattern (EIP) for parallel request distribution.
    /// </para>
    /// <para>
    /// Use cases include:
    /// <list type="bullet">
    /// <item><description>Getting price quotes from multiple vendors</description></item>
    /// <item><description>Querying multiple data sources in parallel</description></item>
    /// <item><description>Distributed processing with result aggregation</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseScatterGather { get; set; }

    /// <summary>
    /// Gets or sets whether to enable multi-tenancy integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the messaging infrastructure integrates with Encina.Tenancy
    /// to provide automatic tenant isolation, assignment, and validation.
    /// </para>
    /// <para>
    /// Features include:
    /// <list type="bullet">
    /// <item><description>Automatic tenant ID assignment on entity creation</description></item>
    /// <item><description>Query filters for tenant data isolation</description></item>
    /// <item><description>Tenant validation on save operations</description></item>
    /// <item><description>Database-per-tenant and schema-per-tenant routing</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requires</b>: Encina.Tenancy package to be configured.
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseTenancy { get; set; }

    /// <summary>
    /// Gets or sets whether to enable module isolation for database access.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the messaging infrastructure validates that modules only access
    /// their own database schemas, enforcing modular monolith boundaries at the database level.
    /// </para>
    /// <para>
    /// Features include:
    /// <list type="bullet">
    /// <item><description>SQL schema extraction and validation during command execution</description></item>
    /// <item><description>Per-module schema configuration with shared schema support</description></item>
    /// <item><description>Development-time validation via EF Core interceptors</description></item>
    /// <item><description>Optional production enforcement with database permissions</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Isolation strategies:
    /// <list type="bullet">
    /// <item><description><see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>: Fast development feedback (default)</description></item>
    /// <item><description><see cref="ModuleIsolationStrategy.SchemaWithPermissions"/>: Real DB permissions</description></item>
    /// <item><description><see cref="ModuleIsolationStrategy.ConnectionPerModule"/>: Separate connections</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    public bool UseModuleIsolation { get; set; }

    /// <summary>
    /// Gets or sets whether to enable automatic domain event dispatching after SaveChanges.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, domain events raised by entities (via <see cref="Encina.DomainModeling.Entity{TId}.AddDomainEvent"/>
    /// or <see cref="Encina.DomainModeling.AggregateRoot{TId}.RaiseDomainEvent"/>) are automatically
    /// dispatched through <see cref="IEncina.Publish{TNotification}"/> after SaveChanges completes.
    /// </para>
    /// <para>
    /// <b>Event Flow</b>:
    /// <list type="number">
    /// <item><description>Domain events are raised during aggregate operations</description></item>
    /// <item><description>SaveChanges persists the aggregate state to the database</description></item>
    /// <item><description>After successful persistence, events are dispatched to handlers</description></item>
    /// <item><description>Events are cleared from entities to prevent duplicate dispatch</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requirements</b>: Domain events must implement <see cref="INotification"/> to be dispatched
    /// (configurable via <see cref="DomainEventsOptions"/>).
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseDomainEvents = true;
    ///     config.DomainEventsOptions.StopOnFirstError = true;
    /// });
    /// </code>
    /// </example>
    public bool UseDomainEvents { get; set; }

    /// <summary>
    /// Gets or sets whether to enable read/write database separation (CQRS physical split).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, queries are automatically routed to read replicas while commands
    /// are routed to the primary database. This complements Encina's existing logical CQRS
    /// with physical database separation.
    /// </para>
    /// <para>
    /// Features include:
    /// <list type="bullet">
    /// <item><description>Automatic routing based on IQuery vs ICommand</description></item>
    /// <item><description>Multiple replica selection strategies (RoundRobin, Random, LeastConnections)</description></item>
    /// <item><description>ForceWriteDatabase attribute for read-after-write consistency</description></item>
    /// <item><description>Health checks for primary and all replica connections</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Benefits:
    /// <list type="bullet">
    /// <item><description>Offload reads to replicas for massive query parallelism</description></item>
    /// <item><description>Reduce load on primary database</description></item>
    /// <item><description>Improve read latency with geographically distributed replicas</description></item>
    /// <item><description>Scale reads independently from writes</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseReadWriteSeparation = true;
    ///     config.ReadWriteSeparationOptions.WriteConnectionString = "Server=primary;...";
    ///     config.ReadWriteSeparationOptions.ReadConnectionStrings = new[]
    ///     {
    ///         "Server=replica1;...",
    ///         "Server=replica2;..."
    ///     };
    ///     config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.RoundRobin;
    /// });
    /// </code>
    /// </example>
    public bool UseReadWriteSeparation { get; set; }

    /// <summary>
    /// Gets or sets whether to enable automatic audit field population.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, entities implementing <see cref="Encina.DomainModeling.IAuditableEntity"/>
    /// (or its granular interfaces <see cref="Encina.DomainModeling.ICreatedAtUtc"/>,
    /// <see cref="Encina.DomainModeling.ICreatedBy"/>, <see cref="Encina.DomainModeling.IModifiedAtUtc"/>,
    /// <see cref="Encina.DomainModeling.IModifiedBy"/>) will have their audit fields automatically
    /// populated during save operations.
    /// </para>
    /// <para>
    /// Features include:
    /// <list type="bullet">
    /// <item><description>Automatic <c>CreatedAtUtc</c> and <c>CreatedBy</c> on entity creation</description></item>
    /// <item><description>Automatic <c>ModifiedAtUtc</c> and <c>ModifiedBy</c> on entity modification</description></item>
    /// <item><description>User ID resolution from <see cref="IRequestContext.UserId"/></description></item>
    /// <item><description>Granular control via <see cref="AuditingOptions"/></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseAuditing = true;
    ///     config.AuditingOptions.TrackCreatedBy = true;
    ///     config.AuditingOptions.TrackModifiedBy = true;
    /// });
    /// </code>
    /// </example>
    public bool UseAuditing { get; set; }

    /// <summary>
    /// Gets or sets whether to enable persistent audit log storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, audit log entries are persisted to the database using the
    /// provider-specific <c>IAuditLogStore</c> implementation. This is separate from
    /// <see cref="UseAuditing"/> which only populates audit fields on entities.
    /// </para>
    /// <para>
    /// <b>Note</b>: This requires <see cref="UseAuditing"/> to be enabled and
    /// <see cref="AuditingOptions.LogChangesToStore"/> to be set to <c>true</c>
    /// for the <c>AuditInterceptor</c> to log changes to the store.
    /// </para>
    /// </remarks>
    /// <value>Default: false (opt-in)</value>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseAuditing = true;
    ///     config.UseAuditLogStore = true;
    ///     config.AuditingOptions.LogChangesToStore = true;
    /// });
    /// </code>
    /// </example>
    public bool UseAuditLogStore { get; set; }

    /// <summary>
    /// Gets the configuration options for the Outbox Pattern.
    /// </summary>
    public OutboxOptions OutboxOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Inbox Pattern.
    /// </summary>
    public InboxOptions InboxOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Saga Pattern.
    /// </summary>
    public SagaOptions SagaOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Routing Slip Pattern.
    /// </summary>
    public RoutingSlipOptions RoutingSlipOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Scheduling Pattern.
    /// </summary>
    public SchedulingOptions SchedulingOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Recoverability Pipeline.
    /// </summary>
    public RecoverabilityOptions RecoverabilityOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Dead Letter Queue pattern.
    /// </summary>
    public DeadLetterOptions DeadLetterOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Content-Based Router pattern.
    /// </summary>
    public ContentRouterOptions ContentRouterOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Scatter-Gather pattern.
    /// </summary>
    public ScatterGatherOptions ScatterGatherOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for multi-tenancy integration.
    /// </summary>
    public TenancyMessagingOptions TenancyOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for module isolation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to configure:
    /// <list type="bullet">
    /// <item><description>The isolation strategy (development validation, schema permissions, or separate connections)</description></item>
    /// <item><description>Per-module schema mappings and allowed schemas</description></item>
    /// <item><description>Shared schemas accessible by all modules</description></item>
    /// <item><description>Permission script generation options</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseModuleIsolation = true;
    ///
    ///     // Configure module schemas
    ///     config.ModuleIsolationOptions
    ///         .AddSharedSchema("shared")
    ///         .AddSharedSchema("lookup")
    ///         .ConfigureModule("Orders", schema =>
    ///         {
    ///             schema.SchemaName = "orders";
    ///             schema.DatabaseUser = "orders_user";
    ///         })
    ///         .ConfigureModule("Payments", schema =>
    ///         {
    ///             schema.SchemaName = "payments";
    ///             schema.DatabaseUser = "payments_user";
    ///         });
    /// });
    /// </code>
    /// </example>
    public ModuleIsolationOptions ModuleIsolationOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for read/write database separation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to configure:
    /// <list type="bullet">
    /// <item><description>Primary (write) database connection string</description></item>
    /// <item><description>Read replica connection strings</description></item>
    /// <item><description>Replica selection strategy (RoundRobin, Random, LeastConnections)</description></item>
    /// <item><description>Startup validation of database connections</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseReadWriteSeparation = true;
    ///     config.ReadWriteSeparationOptions.WriteConnectionString = "Server=primary;...";
    ///     config.ReadWriteSeparationOptions.ReadConnectionStrings = new[]
    ///     {
    ///         "Server=replica1;...",
    ///         "Server=replica2;..."
    ///     };
    ///     config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.LeastConnections;
    ///     config.ReadWriteSeparationOptions.ValidateOnStartup = true;
    /// });
    /// </code>
    /// </example>
    public ReadWriteSeparationOptions ReadWriteSeparationOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for domain event dispatching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to configure:
    /// <list type="bullet">
    /// <item><description>Whether to stop on first dispatch error</description></item>
    /// <item><description>Whether events must implement INotification</description></item>
    /// <item><description>Whether to clear events after dispatch</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseDomainEvents = true;
    ///     config.DomainEventsOptions.StopOnFirstError = true;
    ///     config.DomainEventsOptions.RequireINotification = false;
    /// });
    /// </code>
    /// </example>
    public DomainEventsOptions DomainEventsOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for automatic audit field population.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to configure:
    /// <list type="bullet">
    /// <item><description>Whether to track creation timestamps (<c>TrackCreatedAt</c>)</description></item>
    /// <item><description>Whether to track creation user (<c>TrackCreatedBy</c>)</description></item>
    /// <item><description>Whether to track modification timestamps (<c>TrackModifiedAt</c>)</description></item>
    /// <item><description>Whether to track modification user (<c>TrackModifiedBy</c>)</description></item>
    /// <item><description>Whether to log audit changes for debugging (<c>LogAuditChanges</c>)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseAuditing = true;
    ///     config.AuditingOptions.TrackCreatedBy = true;
    ///     config.AuditingOptions.LogAuditChanges = true;
    /// });
    /// </code>
    /// </example>
    public AuditingOptions AuditingOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for provider-specific health checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled (default), the messaging provider automatically registers a health check
    /// for the underlying infrastructure (database, message broker, cache, etc.).
    /// </para>
    /// <para>
    /// This is separate from the pattern-specific health checks (Outbox, Inbox, Saga, Scheduling),
    /// which monitor the state of the messaging patterns themselves.
    /// </para>
    /// </remarks>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();

    /// <summary>
    /// Gets a value indicating whether any messaging patterns are enabled.
    /// </summary>
    public bool IsAnyPatternEnabled =>
        UseTransactions || UseOutbox || UseInbox || UseSagas || UseRoutingSlips || UseScheduling || UseRecoverability || UseDeadLetterQueue || UseContentRouter || UseScatterGather || UseTenancy || UseModuleIsolation || UseReadWriteSeparation || UseDomainEvents || UseAuditing || UseAuditLogStore;
}
