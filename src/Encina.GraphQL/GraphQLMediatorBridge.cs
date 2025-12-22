using System.Runtime.CompilerServices;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.GraphQL;

/// <summary>
/// GraphQL-based bridge to Encina.
/// </summary>
public sealed class GraphQLEncinaBridge : IGraphQLEncinaBridge
{
    private readonly IEncina _Encina;
    private readonly ILogger<GraphQLEncinaBridge> _logger;
    private readonly EncinaGraphQLOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLEncinaBridge"/> class.
    /// </summary>
    /// <param name="Encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public GraphQLEncinaBridge(
        IEncina Encina,
        ILogger<GraphQLEncinaBridge> logger,
        IOptions<EncinaGraphQLOptions> options)
    {
        ArgumentNullException.ThrowIfNull(Encina);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _Encina = Encina;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResult>> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IRequest<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            Log.ExecutingQuery(_logger, typeof(TQuery).Name);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ExecutionTimeout);

            var result = await _Encina.Send<TResult>(query, cts.Token).ConfigureAwait(false);

            result.IfRight(_ => Log.SuccessfullyExecutedQuery(_logger, typeof(TQuery).Name));

            result.IfLeft(error => Log.QueryFailed(_logger, typeof(TQuery).Name, error.Message));

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, TResult>(
                EncinaErrors.Create(
                    "GRAPHQL_TIMEOUT",
                    $"Query timed out after {_options.ExecutionTimeout.TotalSeconds} seconds."));
        }
        catch (Exception ex)
        {
            Log.FailedToExecuteQuery(_logger, ex, typeof(TQuery).Name);

            return Left<EncinaError, TResult>(
                EncinaErrors.FromException(
                    "GRAPHQL_QUERY_FAILED",
                    ex,
                    $"Failed to execute query of type {typeof(TQuery).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResult>> MutateAsync<TMutation, TResult>(
        TMutation mutation,
        CancellationToken cancellationToken = default)
        where TMutation : class, IRequest<TResult>
    {
        ArgumentNullException.ThrowIfNull(mutation);

        try
        {
            Log.ExecutingMutation(_logger, typeof(TMutation).Name);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ExecutionTimeout);

            var result = await _Encina.Send<TResult>(mutation, cts.Token).ConfigureAwait(false);

            result.IfRight(_ => Log.SuccessfullyExecutedMutation(_logger, typeof(TMutation).Name));

            result.IfLeft(error => Log.MutationFailed(_logger, typeof(TMutation).Name, error.Message));

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, TResult>(
                EncinaErrors.Create(
                    "GRAPHQL_TIMEOUT",
                    $"Mutation timed out after {_options.ExecutionTimeout.TotalSeconds} seconds."));
        }
        catch (Exception ex)
        {
            Log.FailedToExecuteMutation(_logger, ex, typeof(TMutation).Name);

            return Left<EncinaError, TResult>(
                EncinaErrors.FromException(
                    "GRAPHQL_MUTATION_FAILED",
                    ex,
                    $"Failed to execute mutation of type {typeof(TMutation).Name}."));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, TResult>> SubscribeAsync<TSubscription, TResult>(
        TSubscription subscription,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TSubscription : class
    {
        ArgumentNullException.ThrowIfNull(subscription);

        if (!_options.EnableSubscriptions)
        {
            yield return Left<EncinaError, TResult>(
                EncinaErrors.Create(
                    "GRAPHQL_SUBSCRIPTIONS_DISABLED",
                    "GraphQL subscriptions are disabled."));
            yield break;
        }

        // Subscriptions would typically integrate with a pub/sub system
        // For now, return a not implemented error
        await Task.CompletedTask;

        yield return Left<EncinaError, TResult>(
            EncinaErrors.Create(
                "GRAPHQL_SUBSCRIPTIONS_NOT_IMPLEMENTED",
                "GraphQL subscriptions require integration with a pub/sub system. " +
                "Consider using Encina.Redis.PubSub or Encina.InMemory for local subscriptions."));
    }
}
