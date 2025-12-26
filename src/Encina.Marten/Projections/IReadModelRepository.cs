using LanguageExt;

namespace Encina.Marten.Projections;

/// <summary>
/// Repository abstraction for querying and persisting read models.
/// </summary>
/// <typeparam name="TReadModel">The type of read model.</typeparam>
/// <remarks>
/// <para>
/// The read model repository provides a unified interface for read model persistence.
/// It supports different storage backends through provider-specific implementations.
/// </para>
/// <para>
/// <b>Provider Implementations</b>:
/// <list type="bullet">
/// <item><description><b>Marten</b>: Uses Marten's document store for PostgreSQL</description></item>
/// <item><description><b>EF Core</b>: Future - Entity Framework Core implementation</description></item>
/// <item><description><b>Custom</b>: Implement for NoSQL, caching layers, etc.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderQueryHandler : IRequestHandler&lt;GetOrderSummary, OrderSummary&gt;
/// {
///     private readonly IReadModelRepository&lt;OrderSummary&gt; _repository;
///
///     public OrderQueryHandler(IReadModelRepository&lt;OrderSummary&gt; repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;Either&lt;EncinaError, OrderSummary&gt;&gt; Handle(
///         GetOrderSummary query,
///         CancellationToken cancellationToken)
///     {
///         return await _repository.GetByIdAsync(query.OrderId, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IReadModelRepository<TReadModel>
    where TReadModel : class, IReadModel
{
    /// <summary>
    /// Gets a read model by its identifier.
    /// </summary>
    /// <param name="id">The read model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The read model if found; otherwise, a not found error.</returns>
    Task<Either<EncinaError, TReadModel>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple read models by their identifiers.
    /// </summary>
    /// <param name="ids">The read model identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The found read models (missing IDs are not included).</returns>
    Task<Either<EncinaError, IReadOnlyList<TReadModel>>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries read models using a filter predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching read models.</returns>
    Task<Either<EncinaError, IReadOnlyList<TReadModel>>> QueryAsync(
        Func<IQueryable<TReadModel>, IQueryable<TReadModel>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores or updates a read model.
    /// </summary>
    /// <param name="readModel">The read model to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success; otherwise, an error.</returns>
    Task<Either<EncinaError, Unit>> StoreAsync(
        TReadModel readModel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores or updates multiple read models.
    /// </summary>
    /// <param name="readModels">The read models to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success; otherwise, an error.</returns>
    Task<Either<EncinaError, Unit>> StoreManyAsync(
        IEnumerable<TReadModel> readModels,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a read model by its identifier.
    /// </summary>
    /// <param name="id">The read model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success; otherwise, an error.</returns>
    Task<Either<EncinaError, Unit>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all read models of this type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of deleted read models.</returns>
    /// <remarks>
    /// This is typically used during projection rebuilds to clear existing data.
    /// </remarks>
    Task<Either<EncinaError, long>> DeleteAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a read model with the specified identifier exists.
    /// </summary>
    /// <param name="id">The read model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the read model exists; otherwise, <c>false</c>.</returns>
    Task<Either<EncinaError, bool>> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of read models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count.</returns>
    Task<Either<EncinaError, long>> CountAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of read models matching a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of matching read models.</returns>
    Task<Either<EncinaError, long>> CountAsync(
        Func<IQueryable<TReadModel>, IQueryable<TReadModel>> predicate,
        CancellationToken cancellationToken = default);
}
