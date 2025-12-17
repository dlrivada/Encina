using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace SimpleMediator.EntityFrameworkCore;

/// <summary>
/// Pipeline behavior that wraps request execution in a database transaction.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior automatically manages transactions for requests that implement
/// <see cref="ITransactionalCommand"/> or are decorated with the <see cref="TransactionAttribute"/>.
/// </para>
/// <para>
/// <b>Transaction Lifecycle</b>:
/// <list type="number">
/// <item><description>Begin transaction before handler execution</description></item>
/// <item><description>Execute handler and pipeline</description></item>
/// <item><description>Commit if result is <c>Right</c>, rollback if <c>Left</c> or exception</description></item>
/// <item><description>Dispose transaction</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Isolation Level</b>: Uses the database's default isolation level unless overridden
/// in the <see cref="TransactionAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Marker interface approach
/// public record CreateOrderCommand(int CustomerId, decimal Amount)
///     : ICommand&lt;Order&gt;, ITransactionalCommand;
///
/// // Attribute approach
/// [Transaction(IsolationLevel = IsolationLevel.ReadCommitted)]
/// public record UpdateInventoryCommand(int ProductId, int Quantity) : ICommand;
///
/// // Both will be wrapped in a transaction automatically
/// var result = await mediator.Send(command);
/// // Transaction committed if Right, rolled back if Left or exception
/// </code>
/// </example>
public sealed class TransactionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TransactionPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    public TransactionPipelineBehavior(
        DbContext dbContext,
        ILogger<TransactionPipelineBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check if request requires transaction
        if (!RequiresTransaction(request))
            return await nextStep();

        // Check if already in transaction (nested transaction scenario)
        if (_dbContext.Database.CurrentTransaction != null)
        {
            _logger.LogDebug(
                "Request {RequestType} is already in a transaction, reusing existing transaction (CorrelationId: {CorrelationId})",
                typeof(TRequest).Name,
                context.CorrelationId);

            return await nextStep();
        }

        // Get isolation level from attribute or use default
        var isolationLevel = GetIsolationLevel(request);

        _logger.LogDebug(
            "Beginning transaction for request {RequestType} with isolation level {IsolationLevel} (CorrelationId: {CorrelationId})",
            typeof(TRequest).Name,
            isolationLevel,
            context.CorrelationId);

        IDbContextTransaction? transaction = null;

        try
        {
            // Begin transaction
            transaction = isolationLevel.HasValue
                ? await _dbContext.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
                : await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Execute pipeline
            var result = await nextStep();

            // Commit or rollback based on result
            await result.Match(
                Right: async _ =>
                {
                    _logger.LogDebug(
                        "Committing transaction for request {RequestType} (CorrelationId: {CorrelationId})",
                        typeof(TRequest).Name,
                        context.CorrelationId);

                    await transaction.CommitAsync(cancellationToken);
                },
                Left: async error =>
                {
                    _logger.LogWarning(
                        "Rolling back transaction for request {RequestType} due to error: {ErrorMessage} (CorrelationId: {CorrelationId})",
                        typeof(TRequest).Name,
                        error.Message,
                        context.CorrelationId);

                    await transaction.RollbackAsync(cancellationToken);
                });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Rolling back transaction for request {RequestType} due to exception (CorrelationId: {CorrelationId})",
                typeof(TRequest).Name,
                context.CorrelationId);

            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);

            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    private static bool RequiresTransaction(TRequest request)
    {
        // Check for marker interface
        if (request is ITransactionalCommand)
            return true;

        // Check for attribute
        var attribute = typeof(TRequest).GetCustomAttributes(typeof(TransactionAttribute), inherit: true)
            .FirstOrDefault();

        return attribute != null;
    }

    private static System.Data.IsolationLevel? GetIsolationLevel(TRequest request)
    {
        var attribute = typeof(TRequest).GetCustomAttributes(typeof(TransactionAttribute), inherit: true)
            .OfType<TransactionAttribute>()
            .FirstOrDefault();

        return attribute?.IsolationLevel;
    }
}
