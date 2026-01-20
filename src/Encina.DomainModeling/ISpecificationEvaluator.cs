namespace Encina.DomainModeling;

/// <summary>
/// Defines the contract for evaluating specifications against a data source.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
/// <remarks>
/// <para>
/// Specification evaluators bridge the gap between domain specifications and
/// data access implementations. They apply the specification's criteria, ordering,
/// paging, and includes to a queryable data source.
/// </para>
/// <para>
/// Each data access provider (EF Core, Dapper, ADO.NET, MongoDB) implements this
/// interface to support specification-based queries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // EF Core implementation usage
/// public class OrderRepository
/// {
///     private readonly ISpecificationEvaluator&lt;Order&gt; _evaluator;
///     private readonly DbContext _context;
///
///     public async Task&lt;IReadOnlyList&lt;Order&gt;&gt; GetOrdersAsync(IQuerySpecification&lt;Order&gt; spec)
///     {
///         var query = _evaluator.GetQuery(_context.Orders.AsQueryable(), spec);
///         return await query.ToListAsync();
///     }
/// }
/// </code>
/// </example>
public interface ISpecificationEvaluator<T> where T : class
{
    /// <summary>
    /// Applies the specification to the input query and returns the modified query.
    /// </summary>
    /// <param name="inputQuery">The source queryable to apply the specification to.</param>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <returns>A queryable with all specification criteria, ordering, and paging applied.</returns>
    /// <remarks>
    /// <para>
    /// This method applies the following in order:
    /// <list type="number">
    ///   <item><description>Filter criteria (WHERE clauses)</description></item>
    ///   <item><description>Includes (eager loading)</description></item>
    ///   <item><description>Ordering (ORDER BY)</description></item>
    ///   <item><description>Pagination (OFFSET/FETCH or keyset)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The returned query is not yet executed; call <c>ToList()</c>, <c>ToListAsync()</c>,
    /// or similar methods to materialize the results.
    /// </para>
    /// </remarks>
    IQueryable<T> GetQuery(IQueryable<T> inputQuery, IQuerySpecification<T> specification);

    /// <summary>
    /// Applies the specification with projection to the input query.
    /// </summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="inputQuery">The source queryable to apply the specification to.</param>
    /// <param name="specification">The specification containing query criteria and selector.</param>
    /// <returns>A queryable of projected results with all specification criteria applied.</returns>
    /// <remarks>
    /// The projection (SELECT) is applied last, after all filtering, ordering, and paging.
    /// This ensures optimal query generation where only required columns are fetched.
    /// </remarks>
    IQueryable<TResult> GetQuery<TResult>(IQueryable<T> inputQuery, IQuerySpecification<T, TResult> specification);
}

/// <summary>
/// Defines the contract for asynchronous specification evaluation with result materialization.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
/// <remarks>
/// This interface extends <see cref="ISpecificationEvaluator{T}"/> with methods that
/// directly return materialized results, useful for providers that don't support
/// <see cref="IQueryable{T}"/> (e.g., Dapper, ADO.NET, MongoDB).
/// </remarks>
public interface IAsyncSpecificationEvaluator<T> : ISpecificationEvaluator<T> where T : class
{
    /// <summary>
    /// Evaluates the specification and returns a list of matching entities.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of entities matching the specification.</returns>
    Task<IReadOnlyList<T>> ToListAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the specification and returns a list of projected results.
    /// </summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="specification">The specification containing query criteria and selector.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of projected results matching the specification.</returns>
    Task<IReadOnlyList<TResult>> ToListAsync<TResult>(
        IQuerySpecification<T, TResult> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the specification and returns the first matching entity, or default if none found.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The first matching entity, or default if none found.</returns>
    Task<T?> FirstOrDefaultAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the specification and returns the single matching entity.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The single matching entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no elements or more than one element match.</exception>
    Task<T> SingleAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the specification and returns the single matching entity, or default if none found.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The single matching entity, or default if none found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when more than one element matches.</exception>
    Task<T?> SingleOrDefaultAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the specification and returns the count of matching entities.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the specification and returns whether any entities match.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if any entities match; otherwise, false.</returns>
    Task<bool> AnyAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default);
}
