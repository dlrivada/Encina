using System.Runtime.CompilerServices;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace SimpleMediator.GraphQL;

/// <summary>
/// GraphQL-based bridge to SimpleMediator.
/// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class GraphQLMediatorBridge : IGraphQLMediatorBridge
{
    private readonly IMediator _mediator;
    private readonly ILogger<GraphQLMediatorBridge> _logger;
    private readonly SimpleMediatorGraphQLOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLMediatorBridge"/> class.
    /// </summary>
    /// <param name="mediator">The SimpleMediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public GraphQLMediatorBridge(
        IMediator mediator,
        ILogger<GraphQLMediatorBridge> logger,
        IOptions<SimpleMediatorGraphQLOptions> options)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _mediator = mediator;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, TResult>> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IRequest<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            _logger.LogDebug(
                "Executing GraphQL query of type {QueryType}",
                typeof(TQuery).Name);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ExecutionTimeout);

            var result = await _mediator.Send<TResult>(query, cts.Token).ConfigureAwait(false);

            result.IfRight(_ => _logger.LogDebug(
                "Successfully executed GraphQL query of type {QueryType}",
                typeof(TQuery).Name));

            result.IfLeft(error => _logger.LogWarning(
                "GraphQL query of type {QueryType} failed: {Error}",
                typeof(TQuery).Name,
                error.Message));

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Left<MediatorError, TResult>(
                MediatorErrors.Create(
                    "GRAPHQL_TIMEOUT",
                    $"Query timed out after {_options.ExecutionTimeout.TotalSeconds} seconds."));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to execute GraphQL query of type {QueryType}",
                typeof(TQuery).Name);

            return Left<MediatorError, TResult>(
                MediatorErrors.FromException(
                    "GRAPHQL_QUERY_FAILED",
                    ex,
                    $"Failed to execute query of type {typeof(TQuery).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, TResult>> MutateAsync<TMutation, TResult>(
        TMutation mutation,
        CancellationToken cancellationToken = default)
        where TMutation : class, IRequest<TResult>
    {
        ArgumentNullException.ThrowIfNull(mutation);

        try
        {
            _logger.LogDebug(
                "Executing GraphQL mutation of type {MutationType}",
                typeof(TMutation).Name);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ExecutionTimeout);

            var result = await _mediator.Send<TResult>(mutation, cts.Token).ConfigureAwait(false);

            result.IfRight(_ => _logger.LogDebug(
                "Successfully executed GraphQL mutation of type {MutationType}",
                typeof(TMutation).Name));

            result.IfLeft(error => _logger.LogWarning(
                "GraphQL mutation of type {MutationType} failed: {Error}",
                typeof(TMutation).Name,
                error.Message));

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Left<MediatorError, TResult>(
                MediatorErrors.Create(
                    "GRAPHQL_TIMEOUT",
                    $"Mutation timed out after {_options.ExecutionTimeout.TotalSeconds} seconds."));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to execute GraphQL mutation of type {MutationType}",
                typeof(TMutation).Name);

            return Left<MediatorError, TResult>(
                MediatorErrors.FromException(
                    "GRAPHQL_MUTATION_FAILED",
                    ex,
                    $"Failed to execute mutation of type {typeof(TMutation).Name}."));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<MediatorError, TResult>> SubscribeAsync<TSubscription, TResult>(
        TSubscription subscription,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TSubscription : class
    {
        ArgumentNullException.ThrowIfNull(subscription);

        if (!_options.EnableSubscriptions)
        {
            yield return Left<MediatorError, TResult>(
                MediatorErrors.Create(
                    "GRAPHQL_SUBSCRIPTIONS_DISABLED",
                    "GraphQL subscriptions are disabled."));
            yield break;
        }

        // Subscriptions would typically integrate with a pub/sub system
        // For now, return a not implemented error
        await Task.CompletedTask;

        yield return Left<MediatorError, TResult>(
            MediatorErrors.Create(
                "GRAPHQL_SUBSCRIPTIONS_NOT_IMPLEMENTED",
                "GraphQL subscriptions require integration with a pub/sub system. " +
                "Consider using SimpleMediator.Redis.PubSub or SimpleMediator.InMemory for local subscriptions."));
    }
}
