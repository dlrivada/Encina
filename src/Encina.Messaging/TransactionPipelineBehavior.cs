using System.Data;
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

        // Open connection if needed
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        // Begin transaction
        using var transaction = _connection.BeginTransaction();

        try
        {
            var result = await nextStep();

            // Commit on success, rollback on error
            result.Match(
                Right: _ => transaction.Commit(),
                Left: _ => transaction.Rollback());

            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
