namespace SimpleMediator.EntityFrameworkCore;

/// <summary>
/// Marker interface to indicate that a command should be executed within a database transaction.
/// </summary>
/// <remarks>
/// <para>
/// Commands implementing this interface will automatically be wrapped in a database transaction
/// by the <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>.
/// </para>
/// <para>
/// The transaction will be committed if the command handler returns a <c>Right</c> result,
/// and rolled back if it returns a <c>Left</c> result or throws an exception.
/// </para>
/// <para>
/// <b>Nested Transactions</b>: If a transaction is already active, the behavior will reuse
/// the existing transaction rather than creating a nested one.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record CreateOrderCommand(int CustomerId, decimal Amount)
///     : ICommand&lt;Order&gt;, ITransactionalCommand;
///
/// public class CreateOrderHandler : ICommandHandler&lt;CreateOrderCommand, Order&gt;
/// {
///     private readonly AppDbContext _dbContext;
///
///     public async ValueTask&lt;Either&lt;MediatorError, Order&gt;&gt; Handle(
///         CreateOrderCommand request,
///         IRequestContext context,
///         CancellationToken cancellationToken)
///     {
///         // This will run in a transaction automatically
///         var order = new Order
///         {
///             CustomerId = request.CustomerId,
///             Amount = request.Amount,
///             CreatedAt = DateTime.UtcNow
///         };
///
///         _dbContext.Orders.Add(order);
///         await _dbContext.SaveChangesAsync(cancellationToken);
///
///         return order;
///         // Transaction committed automatically
///     }
/// }
/// </code>
/// </example>
public interface ITransactionalCommand
{
}
