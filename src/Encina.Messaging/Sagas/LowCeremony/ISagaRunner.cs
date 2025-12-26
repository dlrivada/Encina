using LanguageExt;

namespace Encina.Messaging.Sagas.LowCeremony;

/// <summary>
/// Executes saga definitions with full lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// The saga runner handles the complete saga lifecycle:
/// <list type="number">
/// <item><description>Starts the saga and persists initial state</description></item>
/// <item><description>Executes steps sequentially</description></item>
/// <item><description>On success, marks saga as completed</description></item>
/// <item><description>On failure, runs compensation in reverse order</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ISagaRunner
{
    /// <summary>
    /// Executes a saga definition with the provided initial data.
    /// </summary>
    /// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
    /// <param name="definition">The saga definition to execute.</param>
    /// <param name="initialData">The initial data to start the saga with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right</c> with the saga result if successful,
    /// <c>Left</c> with an error if the saga failed (after compensation).
    /// </returns>
    ValueTask<Either<EncinaError, SagaResult<TData>>> RunAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        TData initialData,
        CancellationToken cancellationToken = default)
        where TData : class, new();

    /// <summary>
    /// Executes a saga definition with default initial data.
    /// </summary>
    /// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
    /// <param name="definition">The saga definition to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right</c> with the saga result if successful,
    /// <c>Left</c> with an error if the saga failed (after compensation).
    /// </returns>
    ValueTask<Either<EncinaError, SagaResult<TData>>> RunAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        CancellationToken cancellationToken = default)
        where TData : class, new();
}

/// <summary>
/// Result of executing a saga.
/// </summary>
/// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
/// <param name="SagaId">The unique saga identifier.</param>
/// <param name="Data">The final saga data after all steps completed.</param>
/// <param name="StepsExecuted">The number of steps that were executed.</param>
public sealed record SagaResult<TData>(
    Guid SagaId,
    TData Data,
    int StepsExecuted)
    where TData : class;
