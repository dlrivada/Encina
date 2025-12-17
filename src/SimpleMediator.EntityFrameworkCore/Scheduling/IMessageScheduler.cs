namespace SimpleMediator.EntityFrameworkCore.Scheduling;

/// <summary>
/// Service for scheduling messages for delayed execution.
/// </summary>
/// <remarks>
/// <para>
/// This service allows you to schedule commands or notifications to be executed at a future time.
/// Scheduled messages are persisted in the database and executed by a background processor.
/// </para>
/// <para>
/// <b>Use Cases</b>:
/// <list type="bullet">
/// <item><description>Reminders and notifications</description></item>
/// <item><description>Timeouts for business processes</description></item>
/// <item><description>Delayed retries</description></item>
/// <item><description>Saga timeouts</description></item>
/// <item><description>Recurring tasks (daily reports, cleanup jobs, etc.)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderHandler : ICommandHandler&lt;CreateOrderCommand, Order&gt;
/// {
///     private readonly IMessageScheduler _scheduler;
///
///     public async ValueTask&lt;Either&lt;MediatorError, Order&gt;&gt; Handle(
///         CreateOrderCommand request,
///         IRequestContext context,
///         CancellationToken cancellationToken)
///     {
///         var order = new Order { Id = Guid.NewGuid(), ... };
///
///         // Schedule order cancellation if not paid within 30 minutes
///         await _scheduler.ScheduleAsync(
///             new CancelUnpaidOrderCommand(order.Id),
///             delay: TimeSpan.FromMinutes(30),
///             context,
///             cancellationToken);
///
///         // Schedule reminder email 24 hours before delivery
///         await _scheduler.ScheduleAsync(
///             new SendDeliveryReminderCommand(order.Id),
///             executeAt: order.DeliveryDate.AddHours(-24),
///             context,
///             cancellationToken);
///
///         return order;
///     }
/// }
/// </code>
/// </example>
public interface IMessageScheduler
{
    /// <summary>
    /// Schedules a message for execution at a specific time.
    /// </summary>
    /// <typeparam name="TMessage">The type of message (command or notification).</typeparam>
    /// <param name="message">The message to schedule.</param>
    /// <param name="executeAt">The UTC time when the message should be executed.</param>
    /// <param name="context">The request context (for correlation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the scheduled message.</returns>
    Task<Guid> ScheduleAsync<TMessage>(
        TMessage message,
        DateTime executeAt,
        IRequestContext context,
        CancellationToken cancellationToken = default)
        where TMessage : notnull;

    /// <summary>
    /// Schedules a message for execution after a delay.
    /// </summary>
    /// <typeparam name="TMessage">The type of message (command or notification).</typeparam>
    /// <param name="message">The message to schedule.</param>
    /// <param name="delay">The delay before execution.</param>
    /// <param name="context">The request context (for correlation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the scheduled message.</returns>
    Task<Guid> ScheduleAsync<TMessage>(
        TMessage message,
        TimeSpan delay,
        IRequestContext context,
        CancellationToken cancellationToken = default)
        where TMessage : notnull;

    /// <summary>
    /// Schedules a recurring message.
    /// </summary>
    /// <typeparam name="TMessage">The type of message (command or notification).</typeparam>
    /// <param name="message">The message to schedule.</param>
    /// <param name="cronExpression">Cron expression for recurrence (e.g., "0 0 * * *" for daily at midnight).</param>
    /// <param name="context">The request context (for correlation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the scheduled message.</returns>
    Task<Guid> ScheduleRecurringAsync<TMessage>(
        TMessage message,
        string cronExpression,
        IRequestContext context,
        CancellationToken cancellationToken = default)
        where TMessage : notnull;

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    /// <param name="messageId">The ID of the scheduled message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the message was cancelled; <c>false</c> if already executed or not found.</returns>
    Task<bool> CancelAsync(Guid messageId, CancellationToken cancellationToken = default);
}
