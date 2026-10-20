using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging;

/// <summary>
/// Pipeline behavior that wraps request handlers in database transactions.
/// Commits on success (Right), rolls back on failure (Left) or exceptions.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// <para>
/// This behavior ensures that all database operations within a request are atomic.
/// The transaction is committed if the handler returns a Right (success) result,
/// and rolled back if it returns a Left (error) result or throws an exception. A commit that fails (or is
/// refused because a participant rolled back) replaces the handler's response with its error.
/// </para>
/// <para>
/// The behavior takes a lease on the scope's transaction from <see cref="IDbTransactionAccessor"/>: it begins
/// the transaction (opening the connection if needed) unless one is already active, in which case it joins and
/// only the owner commits. Stores and the unit of work of the same scope enlist in that one transaction.
/// </para>
/// <para>
/// The caller's <see cref="CancellationToken"/> is propagated to connection open and transaction begin.
/// Commit and rollback run with <see cref="CancellationToken.None"/> so that transaction cleanup is not
/// canceled by the caller token, including rollback in cancellation paths.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddScoped(typeof(IPipelineBehavior&lt;,&gt;), typeof(TransactionPipelineBehavior&lt;,&gt;));
///
/// // The behavior will automatically wrap handler execution in a transaction
/// var result = await mediator.Send(new CreateOrderCommand { ... });
/// // Transaction is committed on success, rolled back on failure
/// </code>
/// </example>
public sealed class TransactionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly string RequestName = typeof(TRequest).Name;

    private readonly IDbTransactionAccessor _transactionAccessor;
    private readonly ILogger<TransactionPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="transactionAccessor">
    /// The scope's transaction accessor, through which the behavior begins or joins the transaction that the
    /// stores and the unit of work of the scope share.
    /// </param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transactionAccessor"/> or <paramref name="logger"/> is null.
    /// </exception>
    public TransactionPipelineBehavior(
        IDbTransactionAccessor transactionAccessor,
        ILogger<TransactionPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(transactionAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _transactionAccessor = transactionAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        var begun = await _transactionAccessor.BeginOrJoinAsync(cancellationToken).ConfigureAwait(false);
        if (begun.IsLeft)
        {
            return (EncinaError)begun;
        }

        await using var lease = begun.RequireLease();
        if (lease.IsOwner)
        {
            MessagingLog.TransactionStarted(_logger, RequestName, context.CorrelationId);
        }

        try
        {
            var result = await nextStep().ConfigureAwait(false);

            if (result.IsLeft)
            {
                await RollbackAsync(lease, context).ConfigureAwait(false);
                return result;
            }

            var committed = await CommitAsync(lease, context).ConfigureAwait(false);
            return committed.IsLeft ? (EncinaError)committed : result;
        }
        catch (OperationCanceledException)
        {
            await RollbackAsync(lease, context).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await RollbackAsync(lease, context).ConfigureAwait(false);
            return EncinaErrors.FromException("transaction.failed", ex);
        }
    }

    private async ValueTask<Either<EncinaError, Unit>> CommitAsync(ITransactionLease lease, IRequestContext context)
    {
        // The commit is cleanup: it runs to completion even when the caller canceled.
        var committed = await lease.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        if (committed.IsRight && lease.IsOwner)
        {
            MessagingLog.TransactionCommitted(_logger, RequestName, context.CorrelationId);
        }

        return committed;
    }

    private async ValueTask RollbackAsync(ITransactionLease lease, IRequestContext context)
    {
        await lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        MessagingLog.TransactionRolledBack(_logger, RequestName, context.CorrelationId);
    }
}
