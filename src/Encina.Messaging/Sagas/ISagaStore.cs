namespace Encina.Messaging.Sagas;

/// <summary>
/// Abstraction for storing and retrieving saga state.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows different persistence implementations for the Saga Pattern:
/// <list type="bullet">
/// <item><description><b>Entity Framework Core</b>: Full ORM with change tracking</description></item>
/// <item><description><b>Dapper</b>: Lightweight micro-ORM with SQL control</description></item>
/// <item><description><b>ADO.NET</b>: Maximum performance, full control</description></item>
/// <item><description><b>Custom</b>: Document stores, event stores, etc.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ISagaStore
{
    /// <summary>
    /// Gets a saga by its unique identifier.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga state if found, otherwise null.</returns>
    Task<ISagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new saga to the store.
    /// </summary>
    /// <param name="sagaState">The saga state to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing saga's state.
    /// </summary>
    /// <param name="sagaState">The saga state to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas that are stuck or need retry.
    /// </summary>
    /// <param name="olderThan">Get sagas not updated since this time.</param>
    /// <param name="batchSize">Maximum number of sagas to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of saga states that may need intervention.</returns>
    Task<IEnumerable<ISagaState>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas that have exceeded their timeout.
    /// </summary>
    /// <param name="batchSize">Maximum number of sagas to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of saga states that have expired.</returns>
    Task<IEnumerable<ISagaState>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
