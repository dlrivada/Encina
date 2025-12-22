using LanguageExt;

namespace Encina.EntityFrameworkCore.Sagas;

/// <summary>
/// Base class for implementing sagas (distributed transactions with compensation).
/// </summary>
/// <typeparam name="TSagaData">The type of data accumulated during the saga.</typeparam>
/// <remarks>
/// <para>
/// A Saga orchestrates a sequence of steps (local transactions) and provides compensating
/// actions to rollback changes if a step fails.
/// </para>
/// <para>
/// <b>Saga Lifecycle</b>:
/// <list type="number">
/// <item><description><b>Start</b>: Saga begins with initial data</description></item>
/// <item><description><b>Execute Steps</b>: Each step executes and updates saga data</description></item>
/// <item><description><b>Complete</b>: All steps succeed, saga completes</description></item>
/// <item><description><b>Compensate</b>: If any step fails, compensating actions run in reverse order</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderProcessingSagaData
/// {
///     public Guid OrderId { get; set; }
///     public Guid? ReservationId { get; set; }
///     public Guid? PaymentId { get; set; }
///     public Guid? ShipmentId { get; set; }
/// }
///
/// public class OrderProcessingSaga : Saga&lt;OrderProcessingSagaData&gt;
/// {
///     protected override void ConfigureSteps()
///     {
///         // Step 1: Reserve inventory
///         AddStep(
///             execute: async (data, context, ct) =>
///             {
///                 var result = await _mediator.Send(new ReserveInventoryCommand(data.OrderId), ct);
///                 return result.Match(
///                     Right: reservation => { data.ReservationId = reservation.Id; return Right&lt;MediatorError, OrderProcessingSagaData&gt;(data); },
///                     Left: error => error
///                 );
///             },
///             compensate: async (data, context, ct) =>
///             {
///                 if (data.ReservationId.HasValue)
///                     await _mediator.Send(new CancelReservationCommand(data.ReservationId.Value), ct);
///             }
///         );
///
///         // Step 2: Charge customer
///         AddStep(
///             execute: async (data, context, ct) =>
///             {
///                 var result = await _mediator.Send(new ChargeCustomerCommand(data.OrderId), ct);
///                 return result.Match(
///                     Right: payment => { data.PaymentId = payment.Id; return Right&lt;MediatorError, OrderProcessingSagaData&gt;(data); },
///                     Left: error => error
///                 );
///             },
///             compensate: async (data, context, ct) =>
///             {
///                 if (data.PaymentId.HasValue)
///                     await _mediator.Send(new RefundPaymentCommand(data.PaymentId.Value), ct);
///             }
///         );
///
///         // Step 3: Ship order
///         AddStep(
///             execute: async (data, context, ct) =>
///             {
///                 var result = await _mediator.Send(new ShipOrderCommand(data.OrderId), ct);
///                 return result.Match(
///                     Right: shipment => { data.ShipmentId = shipment.Id; return Right&lt;MediatorError, OrderProcessingSagaData&gt;(data); },
///                     Left: error => error
///                 );
///             },
///             compensate: async (data, context, ct) =>
///             {
///                 if (data.ShipmentId.HasValue)
///                     await _mediator.Send(new CancelShipmentCommand(data.ShipmentId.Value), ct);
///             }
///         );
///     }
/// }
/// </code>
/// </example>
public abstract class Saga<TSagaData>
    where TSagaData : class, new()
{
    private readonly List<SagaStep<TSagaData>> _steps = [];

    /// <summary>
    /// Configures the saga steps.
    /// </summary>
    /// <remarks>
    /// Override this method to define the sequence of steps for the saga.
    /// Steps are executed in the order they are added.
    /// </remarks>
    protected abstract void ConfigureSteps();

    /// <summary>
    /// Adds a step to the saga.
    /// </summary>
    /// <param name="execute">The action to execute for this step.</param>
    /// <param name="compensate">The compensating action if the saga needs to be rolled back.</param>
    protected void AddStep(
        Func<TSagaData, IRequestContext, CancellationToken, ValueTask<Either<MediatorError, TSagaData>>> execute,
        Func<TSagaData, IRequestContext, CancellationToken, Task>? compensate = null)
    {
        _steps.Add(new SagaStep<TSagaData>(execute, compensate));
    }

    /// <summary>
    /// Executes the saga from the current step onwards.
    /// </summary>
    /// <param name="data">The current saga data.</param>
    /// <param name="currentStep">The step index to start from (for resuming).</param>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right</c> with final saga data if all steps succeed,
    /// <c>Left</c> with error if any step fails.
    /// </returns>
    public async ValueTask<Either<MediatorError, TSagaData>> ExecuteAsync(
        TSagaData data,
        int currentStep,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // Ensure steps are configured
        if (_steps.Count == 0)
            ConfigureSteps();

        // Execute remaining steps
        for (var i = currentStep; i < _steps.Count; i++)
        {
            var step = _steps[i];
            var result = await step.Execute(data, context, cancellationToken);

            if (result.IsLeft)
            {
                // Step failed - compensate previous steps
                await CompensateAsync(data, i - 1, context, cancellationToken);
                return result;
            }

            // Update data from successful step
            data = result.Match(
                Right: updatedData => updatedData,
                Left: _ => data); // Won't happen as we checked IsLeft above
        }

        return data;
    }

    /// <summary>
    /// Compensates (rolls back) the saga by executing compensating actions in reverse order.
    /// </summary>
    /// <param name="data">The current saga data.</param>
    /// <param name="fromStep">The step index to compensate from (inclusive, going backwards).</param>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CompensateAsync(
        TSagaData data,
        int fromStep,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // Execute compensations in reverse order
        for (var i = fromStep; i >= 0; i--)
        {
            var step = _steps[i];
            if (step.Compensate != null)
            {
                await step.Compensate(data, context, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Gets the total number of steps in the saga.
    /// </summary>
    public int StepCount
    {
        get
        {
            if (_steps.Count == 0)
                ConfigureSteps();
            return _steps.Count;
        }
    }
}

/// <summary>
/// Represents a single step in a saga.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
internal sealed class SagaStep<TSagaData>
    where TSagaData : class
{
    public Func<TSagaData, IRequestContext, CancellationToken, ValueTask<Either<MediatorError, TSagaData>>> Execute { get; }
    public Func<TSagaData, IRequestContext, CancellationToken, Task>? Compensate { get; }

    public SagaStep(
        Func<TSagaData, IRequestContext, CancellationToken, ValueTask<Either<MediatorError, TSagaData>>> execute,
        Func<TSagaData, IRequestContext, CancellationToken, Task>? compensate)
    {
        Execute = execute;
        Compensate = compensate;
    }
}
