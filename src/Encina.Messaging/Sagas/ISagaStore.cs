using LanguageExt;

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
/// <para>
/// All methods return <c>Either&lt;EncinaError, T&gt;</c> following the Railway Oriented Programming
/// pattern. Infrastructure failures are captured as <c>Left</c> values instead of throwing exceptions.
/// </para>
/// </remarks>
public interface ISagaStore
{
    /// <summary>
    /// Gets a saga by its unique identifier.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Some(saga)) if found; Right(None) if not found; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new saga to the store.
    /// </summary>
    /// <param name="sagaState">The saga state to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing saga's state.
    /// </summary>
    /// <param name="sagaState">The saga state to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas that are stuck or need retry.
    /// </summary>
    /// <param name="olderThan">Get sagas not updated since this time.</param>
    /// <param name="batchSize">Maximum number of sagas to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(sagas) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, IEnumerable<ISagaState>>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas that have exceeded their timeout.
    /// </summary>
    /// <param name="batchSize">Maximum number of sagas to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(sagas) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, IEnumerable<ISagaState>>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default);
}
