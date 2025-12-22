using LanguageExt;

namespace Encina.GraphQL;

/// <summary>
/// Interface for bridging GraphQL operations to Encina.
/// </summary>
public interface IGraphQLMediatorBridge
{
    /// <summary>
    /// Sends a query request and returns the response.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the result.</returns>
    ValueTask<Either<MediatorError, TResult>> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IRequest<TResult>;

    /// <summary>
    /// Sends a mutation request and returns the response.
    /// </summary>
    /// <typeparam name="TMutation">The type of the mutation.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="mutation">The mutation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the result.</returns>
    ValueTask<Either<MediatorError, TResult>> MutateAsync<TMutation, TResult>(
        TMutation mutation,
        CancellationToken cancellationToken = default)
        where TMutation : class, IRequest<TResult>;

    /// <summary>
    /// Creates a subscription that yields results as they are published.
    /// </summary>
    /// <typeparam name="TSubscription">The type of the subscription request.</typeparam>
    /// <typeparam name="TResult">The type of the results.</typeparam>
    /// <param name="subscription">The subscription request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of results.</returns>
    IAsyncEnumerable<Either<MediatorError, TResult>> SubscribeAsync<TSubscription, TResult>(
        TSubscription subscription,
        CancellationToken cancellationToken = default)
        where TSubscription : class;
}
