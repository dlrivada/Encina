using System.Linq.Expressions;

namespace Encina.DomainModeling;

/// <summary>
/// Defines the contract for the Specification Pattern.
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to.</typeparam>
/// <remarks>
/// The Specification Pattern encapsulates query logic in reusable, composable, testable objects.
/// This interface provides the foundation for building complex queries while maintaining
/// separation of concerns and testability.
/// </remarks>
/// <example>
/// <code>
/// public class ActiveOrdersSpec : Specification&lt;Order&gt;, ISpecification&lt;Order&gt;
/// {
///     public override Expression&lt;Func&lt;Order, bool&gt;&gt; ToExpression()
///         =&gt; order =&gt; order.Status == OrderStatus.Active;
/// }
/// </code>
/// </example>
public interface ISpecification<T>
{
    /// <summary>
    /// Converts the specification to an expression tree.
    /// </summary>
    /// <returns>An expression that can be used with LINQ providers.</returns>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Compiles the specification to a delegate for in-memory evaluation.
    /// </summary>
    /// <returns>A compiled function that evaluates the specification.</returns>
    Func<T, bool> ToFunc();

    /// <summary>
    /// Checks if an entity satisfies this specification.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <returns>True if the entity satisfies the specification; otherwise, false.</returns>
    bool IsSatisfiedBy(T entity);
}

/// <summary>
/// Defines the contract for query specifications with ordering, paging, and includes.
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to.</typeparam>
/// <remarks>
/// Extends <see cref="ISpecification{T}"/> with query modifiers for:
/// <list type="bullet">
///   <item><description>Multiple filter criteria combined with AND logic</description></item>
///   <item><description>Multi-column ordering with ThenBy support</description></item>
///   <item><description>Offset-based pagination (Skip/Take)</description></item>
///   <item><description>Keyset (cursor-based) pagination for better performance</description></item>
///   <item><description>Eager loading (includes)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class RecentOrdersSpec : QuerySpecification&lt;Order&gt;, IQuerySpecification&lt;Order&gt;
/// {
///     public RecentOrdersSpec(int customerId)
///     {
///         AddCriteria(o =&gt; o.CustomerId == customerId);
///         AddCriteria(o =&gt; o.Status != OrderStatus.Cancelled);
///         ApplyOrderByDescending(o =&gt; o.CreatedAtUtc);
///         ApplyThenBy(o =&gt; o.Id);
///         ApplyPaging(0, 10);
///     }
/// }
/// </code>
/// </example>
public interface IQuerySpecification<T> : ISpecification<T>
{
    /// <summary>
    /// Gets all filter criteria expressions to be combined with AND logic.
    /// </summary>
    /// <remarks>
    /// Multiple criteria are combined using logical AND.
    /// An empty collection means no filtering is applied.
    /// </remarks>
    IReadOnlyList<Expression<Func<T, bool>>> Criteria { get; }

    /// <summary>
    /// Gets the list of include expressions for eager loading.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of include strings for string-based eager loading.
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the primary ordering expression (ascending).
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the primary ordering expression (descending).
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the secondary ordering expressions (ascending) for multi-column sorting.
    /// </summary>
    /// <remarks>
    /// These expressions are applied after the primary OrderBy/OrderByDescending.
    /// Use for deterministic ordering when the primary sort key may have duplicates.
    /// </remarks>
    IReadOnlyList<Expression<Func<T, object>>> ThenByExpressions { get; }

    /// <summary>
    /// Gets the secondary ordering expressions (descending) for multi-column sorting.
    /// </summary>
    /// <remarks>
    /// These expressions are applied after ThenByExpressions.
    /// </remarks>
    IReadOnlyList<Expression<Func<T, object>>> ThenByDescendingExpressions { get; }

    /// <summary>
    /// Gets the number of items to take (limit).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the number of items to skip (offset).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets whether offset-based paging is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Gets whether change tracking should be disabled (EF Core).
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Gets whether to use split queries (EF Core).
    /// </summary>
    bool AsSplitQuery { get; }

    /// <summary>
    /// Gets whether keyset (cursor-based) pagination is enabled.
    /// </summary>
    /// <remarks>
    /// Keyset pagination provides better performance than offset-based pagination
    /// for large datasets by using indexed columns instead of OFFSET.
    /// When enabled, use <see cref="KeysetProperty"/> and <see cref="LastKeyValue"/>
    /// to define the cursor position.
    /// </remarks>
    bool KeysetPaginationEnabled { get; }

    /// <summary>
    /// Gets the property expression used for keyset pagination.
    /// </summary>
    /// <remarks>
    /// This should be a unique, indexed property (typically the primary key or a timestamp).
    /// Combined with <see cref="LastKeyValue"/>, it defines the cursor position.
    /// </remarks>
    Expression<Func<T, object>>? KeysetProperty { get; }

    /// <summary>
    /// Gets the last key value for keyset pagination (cursor position).
    /// </summary>
    /// <remarks>
    /// Represents the last value from the previous page.
    /// The query will return rows where the keyset property is greater than this value.
    /// </remarks>
    object? LastKeyValue { get; }
}

/// <summary>
/// Defines the contract for query specifications with projection support.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public interface IQuerySpecification<T, TResult> : IQuerySpecification<T>
{
    /// <summary>
    /// Gets the selector expression for projecting results.
    /// </summary>
    Expression<Func<T, TResult>>? Selector { get; }
}

/// <summary>
/// Defines the contract for specifications that include pagination options.
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to.</typeparam>
/// <remarks>
/// <para>
/// This interface combines the filtering capabilities of <see cref="ISpecification{T}"/>
/// with standardized pagination through <see cref="PaginationOptions"/>.
/// </para>
/// <para>
/// Use this interface when you want to encapsulate both query criteria and pagination
/// parameters in a single specification object.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ActiveOrdersPagedSpec : PagedQuerySpecification&lt;Order&gt;
/// {
///     public ActiveOrdersPagedSpec(int customerId, PaginationOptions pagination)
///         : base(pagination)
///     {
///         AddCriteria(o =&gt; o.CustomerId == customerId);
///         AddCriteria(o =&gt; o.Status == OrderStatus.Active);
///         ApplyOrderByDescending(o =&gt; o.CreatedAtUtc);
///     }
/// }
/// </code>
/// </example>
public interface IPagedSpecification<T> : ISpecification<T>
{
    /// <summary>
    /// Gets the pagination options for this specification.
    /// </summary>
    /// <remarks>
    /// The pagination options determine the page number and page size
    /// for the query results.
    /// </remarks>
    PaginationOptions Pagination { get; }
}

/// <summary>
/// Defines the contract for paged specifications with projection support.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IPagedSpecification{T}"/> with projection capabilities,
/// allowing you to specify both pagination and result transformation in a single specification.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderSummaryPagedSpec : PagedQuerySpecification&lt;Order, OrderSummaryDto&gt;
/// {
///     public OrderSummaryPagedSpec(int customerId, PaginationOptions pagination)
///         : base(pagination)
///     {
///         AddCriteria(o =&gt; o.CustomerId == customerId);
///         ApplyOrderByDescending(o =&gt; o.CreatedAtUtc);
///
///         Selector = o =&gt; new OrderSummaryDto
///         {
///             Id = o.Id,
///             Total = o.Total,
///             Status = o.Status.ToString()
///         };
///     }
/// }
/// </code>
/// </example>
public interface IPagedSpecification<T, TResult> : IPagedSpecification<T>
{
    /// <summary>
    /// Gets the selector expression for projecting results.
    /// </summary>
    Expression<Func<T, TResult>>? Selector { get; }
}
