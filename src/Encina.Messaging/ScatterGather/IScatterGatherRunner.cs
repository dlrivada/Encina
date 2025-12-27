using LanguageExt;

namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Executes scatter-gather operations.
/// </summary>
/// <remarks>
/// <para>
/// The scatter-gather pattern broadcasts a request to multiple handlers (scatter)
/// and then aggregates the responses using a gather handler.
/// </para>
/// <para>
/// This pattern is useful for:
/// <list type="bullet">
/// <item>Getting quotes from multiple vendors</item>
/// <item>Querying multiple data sources</item>
/// <item>Distributed processing with aggregation</item>
/// </list>
/// </para>
/// </remarks>
public interface IScatterGatherRunner
{
    /// <summary>
    /// Executes a scatter-gather operation with the given request.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to scatter.</typeparam>
    /// <typeparam name="TResponse">The type of response from handlers.</typeparam>
    /// <param name="definition">The scatter-gather definition.</param>
    /// <param name="request">The request to send to scatter handlers.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the scatter-gather result.</returns>
    ValueTask<Either<EncinaError, ScatterGatherResult<TResponse>>> ExecuteAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class;
}
