using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

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
/// <item><description><b>Scheduling</b>: Delayed/recurring message execution</description></item>
/// <item><description><b>Recoverability</b>: Automatic retry with immediate and delayed strategies</description></item>
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
///     config.UseScheduling = true;
///     config.UseRecoverability = true;
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
    /// Gets the configuration options for the Scheduling Pattern.
    /// </summary>
    public SchedulingOptions SchedulingOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Recoverability Pipeline.
    /// </summary>
    public RecoverabilityOptions RecoverabilityOptions { get; } = new();

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
        UseTransactions || UseOutbox || UseInbox || UseSagas || UseScheduling || UseRecoverability;
}
