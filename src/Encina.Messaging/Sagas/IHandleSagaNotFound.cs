namespace Encina.Messaging.Sagas;

/// <summary>
/// Handles scenarios where a saga is not found during message correlation.
/// </summary>
/// <typeparam name="TMessage">The type of message that failed to correlate.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to define custom handling when a message cannot
/// be correlated to an existing saga. Common scenarios include:
/// </para>
/// <list type="bullet">
///   <item><description>Starting a new saga for late-arriving events</description></item>
///   <item><description>Moving orphaned messages to a dead letter queue</description></item>
///   <item><description>Logging and alerting for investigation</description></item>
///   <item><description>Ignoring known-safe scenarios</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class OrderSagaNotFoundHandler : IHandleSagaNotFound&lt;PaymentCompleted&gt;
/// {
///     private readonly ILogger&lt;OrderSagaNotFoundHandler&gt; _logger;
///
///     public OrderSagaNotFoundHandler(ILogger&lt;OrderSagaNotFoundHandler&gt; logger)
///     {
///         _logger = logger;
///     }
///
///     public async Task HandleAsync(PaymentCompleted message, SagaNotFoundContext context, CancellationToken ct)
///     {
///         _logger.LogWarning("Saga not found for OrderId: {OrderId}", message.OrderId);
///
///         // Choose a handling strategy:
///         // context.Ignore();
///         // await context.MoveToDeadLetterAsync("Saga correlation failed", ct);
///     }
/// }
/// </code>
/// </example>
public interface IHandleSagaNotFound<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// Handles a message that failed to correlate to an existing saga.
    /// </summary>
    /// <param name="message">The message that could not be correlated.</param>
    /// <param name="context">
    /// Context providing information about the failed correlation and
    /// actions that can be taken.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(TMessage message, SagaNotFoundContext context, CancellationToken cancellationToken);
}
