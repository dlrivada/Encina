using Encina.DomainModeling;
using MongoDB.Driver;

namespace Encina.MongoDB.Repository;

/// <summary>
/// MongoDB specification evaluator that translates specifications to MongoDB queries.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This evaluator uses <see cref="SpecificationFilterBuilder{TEntity}"/> to translate
/// specification expressions to MongoDB <see cref="FilterDefinition{T}"/> and
/// <see cref="SortDefinition{T}"/>.
/// </para>
/// <para>
/// Unlike EF Core's <c>ISpecificationEvaluator</c>, MongoDB does not use <see cref="IQueryable{T}"/>.
/// Instead, this evaluator returns <see cref="IFindFluent{TDocument, TProjection}"/> for maximum
/// flexibility with MongoDB's query API.
/// </para>
/// <para>
/// Supported features:
/// <list type="bullet">
/// <item><description>Multiple criteria combined with AND logic</description></item>
/// <item><description>Multi-column ordering (OrderBy/ThenBy)</description></item>
/// <item><description>Offset-based pagination (Skip/Take via Limit)</description></item>
/// <item><description>Keyset (cursor-based) pagination for large datasets</description></item>
/// </list>
/// </para>
/// <para>
/// Note: EF Core-specific features like AsNoTracking, AsSplitQuery, and Includes
/// are not applicable to MongoDB and are ignored.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var evaluator = new SpecificationEvaluatorMongoDB&lt;Order&gt;(collection);
/// var spec = new ActiveOrdersSpec();
///
/// // Using IFindFluent for full query control
/// var orders = await evaluator.GetFindFluent(spec).ToListAsync();
///
/// // Using simplified list method
/// var orders = await evaluator.ToListAsync(spec);
/// </code>
/// </example>
public sealed class SpecificationEvaluatorMongoDB<T>
    where T : class
{
    private readonly IMongoCollection<T> _collection;
    private readonly SpecificationFilterBuilder<T> _filterBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationEvaluatorMongoDB{T}"/> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection to query.</param>
    /// <exception cref="ArgumentNullException">Thrown when collection is null.</exception>
    public SpecificationEvaluatorMongoDB(IMongoCollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        _collection = collection;
        _filterBuilder = new SpecificationFilterBuilder<T>();
    }

    /// <summary>
    /// Creates an <see cref="IFindFluent{TDocument, TDocument}"/> with all specification
    /// constraints applied.
    /// </summary>
    /// <param name="specification">The query specification to apply.</param>
    /// <returns>An <see cref="IFindFluent{TDocument, TDocument}"/> ready for execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    /// <remarks>
    /// <para>
    /// This method returns an <see cref="IFindFluent{TDocument, TDocument}"/> that can be
    /// further customized before execution. Use this when you need additional query options
    /// not covered by the specification.
    /// </para>
    /// <para>
    /// The returned fluent interface has filter, sort, skip, and limit applied based on
    /// the specification configuration.
    /// </para>
    /// </remarks>
    public IFindFluent<T, T> GetFindFluent(IQuerySpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var filter = _filterBuilder.BuildFilterInternal(specification);
        var findFluent = _collection.Find(filter);

        // Apply sorting
        var sort = _filterBuilder.BuildSortDefinition(specification);
        if (sort is not null)
        {
            findFluent = findFluent.Sort(sort);
        }

        // Apply pagination
        if (!specification.KeysetPaginationEnabled && specification.IsPagingEnabled)
        {
            // Offset-based pagination
            if (specification.Skip.HasValue)
            {
                findFluent = findFluent.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                findFluent = findFluent.Limit(specification.Take.Value);
            }
        }
        else if (specification.KeysetPaginationEnabled && specification.Take.HasValue)
        {
            // For keyset pagination, only apply Limit (Skip is not used)
            findFluent = findFluent.Limit(specification.Take.Value);
        }

        return findFluent;
    }

    /// <summary>
    /// Evaluates the specification and returns a list of matching entities.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of entities matching the specification.</returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    public async Task<IReadOnlyList<T>> ToListAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await GetFindFluent(specification)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Evaluates the specification and returns the first matching entity, or default if none found.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The first matching entity, or default if none found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    public async Task<T?> FirstOrDefaultAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await GetFindFluent(specification)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Evaluates the specification and returns the single matching entity, or default if none found.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The single matching entity, or default if none found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when more than one element matches.</exception>
    public async Task<T?> SingleOrDefaultAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        // Fetch up to 2 to verify single result
        var results = await GetFindFluent(specification)
            .Limit(2)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results.Count switch
        {
            0 => default,
            1 => results[0],
            _ => throw new InvalidOperationException("Sequence contains more than one element.")
        };
    }

    /// <summary>
    /// Evaluates the specification and returns the count of matching entities.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count of matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    public async Task<int> CountAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var filter = _filterBuilder.BuildFilterInternal(specification);
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return (int)count;
    }

    /// <summary>
    /// Evaluates the specification and returns whether any entities match.
    /// </summary>
    /// <param name="specification">The specification containing query criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if any entities match; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    public async Task<bool> AnyAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var filter = _filterBuilder.BuildFilterInternal(specification);
        var count = await _collection.CountDocumentsAsync(
            filter,
            new CountOptions { Limit = 1 },
            cancellationToken)
            .ConfigureAwait(false);

        return count > 0;
    }
}
