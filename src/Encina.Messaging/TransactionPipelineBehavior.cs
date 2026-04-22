using System.Data;
using System.Data.Common;
using LanguageExt;

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
/// and rolled back if it returns a Left (error) result or throws an exception.
/// </para>
/// <para>
/// The connection is opened automatically if it's not already open.
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
    private readonly IDbConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="connection">The database connection to use for transactions.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public TransactionPipelineBehavior(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
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

        var dbConnection = _connection as DbConnection;

        if (_connection.State != ConnectionState.Open)
        {
            if (dbConnection is not null)
            {
                await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _connection.Open();
            }
        }

        var transaction = dbConnection is not null
            ? await dbConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : _connection.BeginTransaction();

        try
        {
            var result = await nextStep().ConfigureAwait(false);

            await result.Match(
                Right: _ => CommitAsync(transaction, cancellationToken),
                Left: _ => RollbackAsync(transaction, cancellationToken)).ConfigureAwait(false);

            return result;
        }
        catch (OperationCanceledException)
        {
            await RollbackAsync(transaction, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction, CancellationToken.None).ConfigureAwait(false);
            return EncinaErrors.FromException("transaction.failed", ex);
        }
        finally
        {
            if (transaction is DbTransaction dbTransaction)
            {
                await dbTransaction.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                transaction.Dispose();
            }
        }
    }

    private static async Task CommitAsync(IDbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            transaction.Commit();
        }
    }

    private static async Task RollbackAsync(IDbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            transaction.Rollback();
        }
    }
}
