using SimpleMediator.Messaging.Inbox;
using SimpleMediator.Messaging.Outbox;
using SimpleMediator.Messaging.Scheduling;

namespace SimpleMediator.Messaging;

/// <summary>
/// Configuration for messaging patterns in SimpleMediator.
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
/// services.AddSimpleMediatorMessaging(config =>
/// {
///     config.UseTransactions = true;
/// });
///
/// // Distributed system - all patterns
/// services.AddSimpleMediatorMessaging(config =>
/// {
///     config.UseTransactions = true;
///     config.UseOutbox = true;
///     config.UseInbox = true;
///     config.UseSagas = true;
///     config.UseScheduling = true;
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
    /// Gets the configuration options for the Outbox Pattern.
    /// </summary>
    public OutboxOptions OutboxOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Inbox Pattern.
    /// </summary>
    public InboxOptions InboxOptions { get; } = new();

    /// <summary>
    /// Gets the configuration options for the Scheduling Pattern.
    /// </summary>
    public SchedulingOptions SchedulingOptions { get; } = new();

    /// <summary>
    /// Gets a value indicating whether any messaging patterns are enabled.
    /// </summary>
    public bool IsAnyPatternEnabled =>
        UseTransactions || UseOutbox || UseInbox || UseSagas || UseScheduling;
}
