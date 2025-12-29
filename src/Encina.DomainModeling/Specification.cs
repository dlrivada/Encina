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
public abstract class Specification<T>
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
/// It supports eager loading (includes), ordering, and pagination.
/// </remarks>
/// <example>
/// <code>
/// public class RecentOrdersWithItemsSpec : QuerySpecification&lt;Order&gt;
/// {
///     public RecentOrdersWithItemsSpec(int customerId)
///     {
///         Criteria = o =&gt; o.CustomerId == customerId;
///         AddInclude(o =&gt; o.Items);
///         ApplyOrderByDescending(o =&gt; o.CreatedAtUtc);
///         ApplyPaging(0, 10);
///     }
/// }
/// </code>
/// </example>
public abstract class QuerySpecification<T> : Specification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];

    /// <summary>
    /// Gets the filter criteria expression.
    /// </summary>
    protected Expression<Func<T, bool>>? Criteria { get; set; }

    /// <summary>
    /// Gets the list of include expressions for eager loading.
    /// </summary>
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    /// <summary>
    /// Gets the list of include strings for string-based eager loading.
    /// </summary>
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    /// <summary>
    /// Gets the ordering expression (ascending).
    /// </summary>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <summary>
    /// Gets the ordering expression (descending).
    /// </summary>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <summary>
    /// Gets the number of items to take.
    /// </summary>
    public int? Take { get; private set; }

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    public int? Skip { get; private set; }

    /// <summary>
    /// Gets whether paging is enabled.
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

    /// <inheritdoc />
    public override Expression<Func<T, bool>> ToExpression()
        => Criteria ?? (x => true);

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
    /// Applies ascending ordering.
    /// </summary>
    /// <param name="orderByExpression">The property to order by.</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByExpression);
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Applies descending ordering.
    /// </summary>
    /// <param name="orderByDescendingExpression">The property to order by.</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByDescendingExpression);
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Applies pagination.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    protected void ApplyPaging(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);
        Skip = skip;
        Take = take;
    }
}

/// <summary>
/// Extended specification with projection support.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public abstract class QuerySpecification<T, TResult> : QuerySpecification<T>
{
    /// <summary>
    /// Gets the selector expression for projecting results.
    /// </summary>
    public Expression<Func<T, TResult>>? Selector { get; protected set; }
}
