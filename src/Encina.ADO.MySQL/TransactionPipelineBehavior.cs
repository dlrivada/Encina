using System.Data;
using LanguageExt;

namespace Encina.ADO.MySQL;

/// <summary>
/// Pipeline behavior that wraps request handlers in database transactions.
/// Commits on success (Right), rolls back on failure (Left) or exceptions.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class TransactionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDbConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    public TransactionPipelineBehavior(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Open connection if needed
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        // Begin transaction
        using var transaction = _connection.BeginTransaction();

        try
        {
            var result = await next();

            // Commit on success, rollback on error
            await result.Match(
                Right: async _ =>
                {
                    transaction.Commit();
                    await Task.CompletedTask;
                },
                Left: async _ =>
                {
                    transaction.Rollback();
                    await Task.CompletedTask;
                });

            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
