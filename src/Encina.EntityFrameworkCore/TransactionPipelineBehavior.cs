using System.Data;
using Encina.Diagnostics;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

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
/// <item><description>Take a lease on the scope's shared transaction (begin it, or join the one already active)</description></item>
/// <item><description>Execute handler and pipeline</description></item>
/// <item><description>Commit if result is <c>Right</c>, rollback if <c>Left</c> or exception; only the owner of the transaction commits</description></item>
/// <item><description>Dispose the lease</description></item>
/// </list>
/// </para>
/// <para>
/// The transaction is shared with the unit of work and the transactional outbox through
/// <see cref="IDbTransactionAccessor"/>, so they commit or roll back together. A context whose execution
/// strategy retries on failure cannot take part: the request fails with <c>transaction.not_supported</c>.
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
    // Both answers depend only on the request type, so they are computed once per closed generic type.
    private static readonly TransactionAttribute? s_attribute = typeof(TRequest)
        .GetCustomAttributes(typeof(TransactionAttribute), inherit: true)
        .OfType<TransactionAttribute>()
        .FirstOrDefault();

    private static readonly bool s_requiresTransaction =
        s_attribute is not null || typeof(ITransactionalCommand).IsAssignableFrom(typeof(TRequest));

    private static readonly IsolationLevel? s_isolationLevel = s_attribute?.IsolationLevel;

    private readonly IDbTransactionAccessor _transactionAccessor;
    private readonly ILogger<TransactionPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="transactionAccessor">The scope's transaction accessor, shared with the unit of work and the outbox.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transactionAccessor"/> or <paramref name="logger"/> is null.</exception>
    public TransactionPipelineBehavior(
        IDbTransactionAccessor transactionAccessor,
        ILogger<TransactionPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(transactionAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _transactionAccessor = transactionAccessor;
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

        if (!s_requiresTransaction)
        {
            return await nextStep();
        }

        var begun = await _transactionAccessor
            .BeginOrJoinAsync(s_isolationLevel, cancellationToken)
            .ConfigureAwait(false);

        return await begun.Match(
            Right: lease => RunInTransactionAsync(lease, context, nextStep, cancellationToken),
            Left: error => ValueTask.FromResult(Left<EncinaError, TResponse>(error))).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the pipeline inside the leased transaction, then commits or rolls back based on the outcome
    /// (or on an exception). Only the owner of the transaction commits.
    /// </summary>
    private async ValueTask<Either<EncinaError, TResponse>> RunInTransactionAsync(
        IDbTransactionLease lease,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        await using (lease.ConfigureAwait(false))
        {
            if (lease.IsOwner)
            {
                Log.BeginningTransaction(_logger, typeof(TRequest).Name, s_isolationLevel, context.CorrelationId);
            }
            else
            {
                Log.ReusingExistingTransaction(_logger, typeof(TRequest).Name, context.CorrelationId);
            }

            Either<EncinaError, TResponse> result;

            try
            {
                result = await nextStep();
            }
            catch (OperationCanceledException)
            {
                await lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);

                throw;
            }
            catch (Exception ex)
            {
                Log.RollingBackTransactionDueToException(_logger, ex.ForLogging(), typeof(TRequest).Name, context.CorrelationId);

                await lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);

                return EncinaErrors.FromException("transaction.failed", ex);
            }

            return await CommitOrRollbackAsync(lease, result, context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Commits the transaction when the pipeline succeeded (a commit failure replaces the response), or rolls
    /// it back and logs only the error code (never <see cref="EncinaError.Message"/>) when it failed.
    /// </summary>
    private async ValueTask<Either<EncinaError, TResponse>> CommitOrRollbackAsync(
        IDbTransactionLease lease,
        Either<EncinaError, TResponse> result,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        if (result.IsLeft)
        {
            var code = result.Match(Right: _ => string.Empty, Left: error => error.GetEncinaCode());
            Log.RollingBackTransactionDueToError(_logger, typeof(TRequest).Name, code, context.CorrelationId);

            await lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        if (lease.IsOwner)
        {
            Log.CommittingTransaction(_logger, typeof(TRequest).Name, context.CorrelationId);
        }

        var committed = await lease.CommitAsync(cancellationToken).ConfigureAwait(false);

        return committed.Match(
            Right: _ => result,
            Left: error => Left<EncinaError, TResponse>(error));
    }
}
