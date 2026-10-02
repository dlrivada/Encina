using Encina.Diagnostics;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore;

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
/// var result = await Encina.Send(command);
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> or <paramref name="logger"/> is null.</exception>
    public TransactionPipelineBehavior(
        DbContext dbContext,
        ILogger<TransactionPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Check if request requires transaction
        if (!RequiresTransaction(request))
        {
            return await nextStep();
        }

        // Check if already in transaction (nested transaction scenario)
        if (_dbContext.Database.CurrentTransaction != null)
        {
            Log.ReusingExistingTransaction(_logger, typeof(TRequest).Name, context.CorrelationId);

            return await nextStep();
        }

        return await ExecuteInNewTransactionAsync(context, nextStep, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Begins a new database transaction, runs the pipeline inside it, and commits or rolls
    /// back based on the outcome (or on an exception).
    /// </summary>
    private async ValueTask<Either<EncinaError, TResponse>> ExecuteInNewTransactionAsync(
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Get isolation level from attribute or use default
        var isolationLevel = GetIsolationLevel();

        Log.BeginningTransaction(_logger, typeof(TRequest).Name, isolationLevel, context.CorrelationId);

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
            await CommitOrRollbackAsync(transaction, result, context, cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (OperationCanceledException)
        {
            await RollbackIfActiveAsync(transaction, cancellationToken).ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            Log.RollingBackTransactionDueToException(_logger, ex.ForLogging(), typeof(TRequest).Name, context.CorrelationId);

            await RollbackIfActiveAsync(transaction, cancellationToken).ConfigureAwait(false);

            return EncinaErrors.FromException("transaction.failed", ex);
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    /// <summary>
    /// Commits the transaction when the pipeline succeeded, or rolls it back and logs only the
    /// error code (never <see cref="EncinaError.Message"/>) when it failed.
    /// </summary>
    private async ValueTask CommitOrRollbackAsync(
        IDbContextTransaction transaction,
        Either<EncinaError, TResponse> result,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        await result.Match(
            Right: async _ =>
            {
                Log.CommittingTransaction(_logger, typeof(TRequest).Name, context.CorrelationId);

                await transaction.CommitAsync(cancellationToken);
            },
            Left: async error =>
            {
                Log.RollingBackTransactionDueToError(_logger, typeof(TRequest).Name, error.GetEncinaCode(), context.CorrelationId);

                await transaction.RollbackAsync(cancellationToken);
            });
    }

    /// <summary>
    /// Rolls back the transaction if one was successfully started before the failure.
    /// </summary>
    private static async ValueTask RollbackIfActiveAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction != null)
        {
            await transaction.RollbackAsync(cancellationToken);
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

    private static System.Data.IsolationLevel? GetIsolationLevel()
    {
        var attribute = typeof(TRequest).GetCustomAttributes(typeof(TransactionAttribute), inherit: true)
            .OfType<TransactionAttribute>()
            .FirstOrDefault();

        return attribute?.IsolationLevel;
    }
}
