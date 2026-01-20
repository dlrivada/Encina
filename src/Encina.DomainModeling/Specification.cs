using System.Linq.Expressions;

namespace Encina.DomainModeling;

/// <summary>
/// Base class for the Specification Pattern.
/// Encapsulates query logic in reusable, composable, testable objects.
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to.</typeparam>
/// <remarks>
/// The Specification Pattern was coined by Eric Evans and Martin Fowler.
/// It allows encapsulating complex business rules and query predicates into
/// discrete, reusable objects that can be combined using logical operators.
/// </remarks>
/// <example>
/// <code>
/// public class ActiveOrdersSpec : Specification&lt;Order&gt;
/// {
///     public override Expression&lt;Func&lt;Order, bool&gt;&gt; ToExpression()
///         =&gt; order =&gt; order.Status == OrderStatus.Active;
/// }
///
/// // Use with LINQ
/// var orders = dbContext.Orders.Where(spec.ToExpression());
///
/// // Compose specifications
/// var spec = new ActiveOrdersSpec()
///     .And(new HighValueOrdersSpec(1000));
/// </code>
/// </example>
public abstract class Specification<T> : ISpecification<T>
{
    /// <summary>
    /// Converts the specification to an expression tree.
    /// </summary>
    /// <returns>An expression that can be used with LINQ providers.</returns>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Compiles the specification to a delegate for in-memory evaluation.
    /// </summary>
    /// <returns>A compiled function that evaluates the specification.</returns>
    /// <remarks>
    /// Caching the compiled function is recommended for repeated evaluations
    /// as expression compilation has overhead.
    /// </remarks>
    public Func<T, bool> ToFunc() => ToExpression().Compile();

    /// <summary>
    /// Checks if an entity satisfies this specification.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <returns>True if the entity satisfies the specification; otherwise, false.</returns>
    public bool IsSatisfiedBy(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return ToFunc()(entity);
    }

    /// <summary>
    /// Combines this specification with another using logical AND.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new specification representing both conditions.</returns>
    public Specification<T> And(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new AndSpecification<T>(this, other);
    }

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new specification representing either condition.</returns>
    public Specification<T> Or(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OrSpecification<T>(this, other);
    }

    /// <summary>
    /// Negates this specification.
    /// </summary>
    /// <returns>A new specification representing the negation.</returns>
    public Specification<T> Not() => new NotSpecification<T>(this);

    /// <summary>
    /// Implicitly converts a specification to its expression form.
    /// </summary>
    /// <param name="specification">The specification to convert.</param>
    public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
        => specification.ToExpression();
}

/// <summary>
/// Specification that combines two specifications with logical AND.
/// </summary>
internal sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.AndAlso(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter));

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}

/// <summary>
/// Specification that combines two specifications with logical OR.
/// </summary>
internal sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.OrElse(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter));

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}

/// <summary>
/// Specification that negates another specification.
/// </summary>
internal sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T), "x");
        var negated = Expression.Not(Expression.Invoke(expr, parameter));

        return Expression.Lambda<Func<T, bool>>(negated, parameter);
    }
}

/// <summary>
/// Extended specification with query modifiers for ordering, paging, and includes.
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to.</typeparam>
/// <remarks>
/// Use this class when you need more than just filtering.
/// It supports:
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
/// public class RecentOrdersWithItemsSpec : QuerySpecification&lt;Order&gt;
/// {
///     public RecentOrdersWithItemsSpec(int customerId, DateTime? lastCreatedAt = null)
///     {
///         // Multiple criteria combined with AND
///         AddCriteria(o =&gt; o.CustomerId == customerId);
///         AddCriteria(o =&gt; o.Status != OrderStatus.Cancelled);
///
///         // Includes
///         AddInclude(o =&gt; o.Items);
///
///         // Multi-column ordering
///         ApplyOrderByDescending(o =&gt; o.CreatedAtUtc);
///         ApplyThenBy(o =&gt; o.Id); // Deterministic ordering
///
///         // Option 1: Offset-based pagination
///         ApplyPaging(0, 10);
///
///         // Option 2: Keyset pagination (better for large datasets)
///         // ApplyKeysetPagination(o =&gt; o.CreatedAtUtc, lastCreatedAt, 10);
///     }
/// }
/// </code>
/// </example>
public abstract class QuerySpecification<T> : Specification<T>, IQuerySpecification<T>
{
    private readonly List<Expression<Func<T, bool>>> _criteria = [];
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<Expression<Func<T, object>>> _thenByExpressions = [];
    private readonly List<Expression<Func<T, object>>> _thenByDescendingExpressions = [];

    /// <summary>
    /// Gets all filter criteria expressions to be combined with AND logic.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AddCriteria"/> to add criteria. Multiple criteria are combined using logical AND.
    /// </remarks>
    public IReadOnlyList<Expression<Func<T, bool>>> Criteria => _criteria.AsReadOnly();

    /// <summary>
    /// Gets the list of include expressions for eager loading.
    /// </summary>
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    /// <summary>
    /// Gets the list of include strings for string-based eager loading.
    /// </summary>
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    /// <summary>
    /// Gets the primary ordering expression (ascending).
    /// </summary>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <summary>
    /// Gets the primary ordering expression (descending).
    /// </summary>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <summary>
    /// Gets the secondary ordering expressions (ascending) for multi-column sorting.
    /// </summary>
    /// <remarks>
    /// These expressions are applied after the primary OrderBy/OrderByDescending.
    /// Use for deterministic ordering when the primary sort key may have duplicates.
    /// </remarks>
    public IReadOnlyList<Expression<Func<T, object>>> ThenByExpressions => _thenByExpressions.AsReadOnly();

    /// <summary>
    /// Gets the secondary ordering expressions (descending) for multi-column sorting.
    /// </summary>
    /// <remarks>
    /// These expressions are applied after ThenByExpressions.
    /// </remarks>
    public IReadOnlyList<Expression<Func<T, object>>> ThenByDescendingExpressions => _thenByDescendingExpressions.AsReadOnly();

    /// <summary>
    /// Gets the number of items to take (limit).
    /// </summary>
    public int? Take { get; private set; }

    /// <summary>
    /// Gets the number of items to skip (offset).
    /// </summary>
    public int? Skip { get; private set; }

    /// <summary>
    /// Gets whether offset-based paging is enabled.
    /// </summary>
    public bool IsPagingEnabled => Take.HasValue || Skip.HasValue;

    /// <summary>
    /// Gets or sets whether to disable change tracking (EF Core).
    /// </summary>
    public bool AsNoTracking { get; protected set; } = true;

    /// <summary>
    /// Gets or sets whether to use split queries (EF Core).
    /// </summary>
    public bool AsSplitQuery { get; protected set; }

    /// <summary>
    /// Gets whether keyset (cursor-based) pagination is enabled.
    /// </summary>
    /// <remarks>
    /// Keyset pagination provides better performance than offset-based pagination
    /// for large datasets by using indexed columns instead of OFFSET.
    /// </remarks>
    public bool KeysetPaginationEnabled { get; private set; }

    /// <summary>
    /// Gets the property expression used for keyset pagination.
    /// </summary>
    /// <remarks>
    /// This should be a unique, indexed property (typically the primary key or a timestamp).
    /// </remarks>
    public Expression<Func<T, object>>? KeysetProperty { get; private set; }

    /// <summary>
    /// Gets the last key value for keyset pagination (cursor position).
    /// </summary>
    /// <remarks>
    /// Represents the last value from the previous page.
    /// The query will return rows where the keyset property is greater than this value.
    /// </remarks>
    public object? LastKeyValue { get; private set; }

    /// <inheritdoc />
    public override Expression<Func<T, bool>> ToExpression()
    {
        if (_criteria.Count == 0)
        {
            return x => true;
        }

        if (_criteria.Count == 1)
        {
            return _criteria[0];
        }

        // Combine all criteria with AND logic
        var combined = _criteria[0];
        for (var i = 1; i < _criteria.Count; i++)
        {
            combined = CombineWithAnd(combined, _criteria[i]);
        }

        return combined;
    }

    /// <summary>
    /// Adds a filter criteria expression.
    /// </summary>
    /// <param name="criteria">The filter expression to add.</param>
    /// <remarks>
    /// Multiple criteria are combined using logical AND when evaluating the specification.
    /// </remarks>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        _criteria.Add(criteria);
    }

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    /// <param name="includeExpression">The navigation property to include.</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);
        _includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds an include string for string-based eager loading.
    /// </summary>
    /// <param name="includeString">The navigation property path to include.</param>
    protected void AddInclude(string includeString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(includeString);
        _includeStrings.Add(includeString);
    }

    /// <summary>
    /// Applies primary ascending ordering.
    /// </summary>
    /// <param name="orderByExpression">The property to order by.</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByExpression);
        OrderBy = orderByExpression;
        OrderByDescending = null;
    }

    /// <summary>
    /// Applies primary descending ordering.
    /// </summary>
    /// <param name="orderByDescendingExpression">The property to order by.</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByDescendingExpression);
        OrderByDescending = orderByDescendingExpression;
        OrderBy = null;
    }

    /// <summary>
    /// Adds a secondary ascending ordering expression.
    /// </summary>
    /// <param name="thenByExpression">The property for secondary ordering.</param>
    /// <remarks>
    /// Use this to ensure deterministic ordering when the primary sort key may have duplicates.
    /// Commonly used with the entity's ID for stable pagination.
    /// </remarks>
    protected void ApplyThenBy(Expression<Func<T, object>> thenByExpression)
    {
        ArgumentNullException.ThrowIfNull(thenByExpression);
        _thenByExpressions.Add(thenByExpression);
    }

    /// <summary>
    /// Adds a secondary descending ordering expression.
    /// </summary>
    /// <param name="thenByDescendingExpression">The property for secondary descending ordering.</param>
    protected void ApplyThenByDescending(Expression<Func<T, object>> thenByDescendingExpression)
    {
        ArgumentNullException.ThrowIfNull(thenByDescendingExpression);
        _thenByDescendingExpressions.Add(thenByDescendingExpression);
    }

    /// <summary>
    /// Applies offset-based pagination.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <remarks>
    /// For large datasets, consider using <see cref="ApplyKeysetPagination"/> instead
    /// for better performance.
    /// </remarks>
    protected void ApplyPaging(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);
        Skip = skip;
        Take = take;
        KeysetPaginationEnabled = false;
    }

    /// <summary>
    /// Applies keyset (cursor-based) pagination.
    /// </summary>
    /// <param name="keysetProperty">The property expression used as the cursor (must be indexed and unique).</param>
    /// <param name="lastKeyValue">The last key value from the previous page, or null for the first page.</param>
    /// <param name="take">Number of items to take per page.</param>
    /// <remarks>
    /// <para>
    /// Keyset pagination provides consistent performance regardless of page depth,
    /// unlike offset-based pagination which degrades as offset increases.
    /// </para>
    /// <para>
    /// The keyset property should be:
    /// <list type="bullet">
    ///   <item><description>Indexed for performance</description></item>
    ///   <item><description>Unique to avoid skipping/duplicating rows</description></item>
    ///   <item><description>Ordered (the query will filter by <c>KeysetProperty &gt; LastKeyValue</c>)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // First page
    /// ApplyKeysetPagination(o =&gt; o.Id, null, 10);
    ///
    /// // Subsequent pages (using last ID from previous result)
    /// ApplyKeysetPagination(o =&gt; o.Id, lastId, 10);
    /// </code>
    /// </example>
    protected void ApplyKeysetPagination(Expression<Func<T, object>> keysetProperty, object? lastKeyValue, int take)
    {
        ArgumentNullException.ThrowIfNull(keysetProperty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        KeysetPaginationEnabled = true;
        KeysetProperty = keysetProperty;
        LastKeyValue = lastKeyValue;
        Take = take;
        Skip = null;
    }

    /// <summary>
    /// Clears all filter criteria.
    /// </summary>
    protected void ClearCriteria()
    {
        _criteria.Clear();
    }

    /// <summary>
    /// Clears all ordering (primary and secondary).
    /// </summary>
    protected void ClearOrdering()
    {
        OrderBy = null;
        OrderByDescending = null;
        _thenByExpressions.Clear();
        _thenByDescendingExpressions.Clear();
    }

    private static Expression<Func<T, bool>> CombineWithAnd(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.AndAlso(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}

/// <summary>
/// Extended specification with projection support.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
/// <remarks>
/// Use this class when you need to project entities to a different type (DTOs, view models, etc.).
/// Projection happens at the database level for optimal performance.
/// </remarks>
/// <example>
/// <code>
/// public class OrderSummarySpec : QuerySpecification&lt;Order, OrderSummaryDto&gt;
/// {
///     public OrderSummarySpec(int customerId)
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
public abstract class QuerySpecification<T, TResult> : QuerySpecification<T>, IQuerySpecification<T, TResult>
{
    /// <summary>
    /// Gets or sets the selector expression for projecting results.
    /// </summary>
    /// <remarks>
    /// The selector is applied at the database level, selecting only the required columns.
    /// This improves performance compared to loading full entities and mapping in memory.
    /// </remarks>
    public Expression<Func<T, TResult>>? Selector { get; protected set; }
}
