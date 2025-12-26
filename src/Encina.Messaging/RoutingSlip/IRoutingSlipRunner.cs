using LanguageExt;

namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Executes routing slip definitions.
/// </summary>
public interface IRoutingSlipRunner
{
    /// <summary>
    /// Runs a routing slip with default initial data.
    /// </summary>
    /// <typeparam name="TData">The type of data being routed.</typeparam>
    /// <param name="definition">The routing slip definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the routing slip execution.</returns>
    ValueTask<Either<EncinaError, RoutingSlipResult<TData>>> RunAsync<TData>(
        BuiltRoutingSlipDefinition<TData> definition,
        CancellationToken cancellationToken = default)
        where TData : class, new();

    /// <summary>
    /// Runs a routing slip with the specified initial data.
    /// </summary>
    /// <typeparam name="TData">The type of data being routed.</typeparam>
    /// <param name="definition">The routing slip definition.</param>
    /// <param name="initialData">The initial data to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the routing slip execution.</returns>
    ValueTask<Either<EncinaError, RoutingSlipResult<TData>>> RunAsync<TData>(
        BuiltRoutingSlipDefinition<TData> definition,
        TData initialData,
        CancellationToken cancellationToken = default)
        where TData : class, new();
}
