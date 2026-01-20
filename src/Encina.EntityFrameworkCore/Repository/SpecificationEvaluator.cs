using System.Linq.Expressions;
using Encina.DomainModeling;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Repository;

/// <summary>
/// Utility class for applying specifications to <see cref="IQueryable{T}"/> for EF Core queries.
/// </summary>
/// <remarks>
/// <para>
/// This evaluator translates <see cref="Specification{T}"/> instances into EF Core
/// queryable expressions, enabling full SQL translation of specification predicates.
/// </para>
/// <para>
/// For <see cref="QuerySpecification{T}"/>, additional features are applied:
/// <list type="bullet">
/// <item><description>Multiple criteria combined with AND logic</description></item>
/// <item><description>Includes for eager loading navigation properties</description></item>
/// <item><description>Multi-column ordering (OrderBy/ThenBy)</description></item>
/// <item><description>Offset-based pagination (Skip/Take)</description></item>
/// <item><description>Keyset (cursor-based) pagination for large datasets</description></item>
/// <item><description>AsNoTracking for read-only queries</description></item>
/// <item><description>AsSplitQuery for multiple result sets</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Apply a specification to a DbSet
/// var query = SpecificationEvaluator.GetQuery(
///     dbContext.Orders.AsQueryable(),
///     new ActiveOrdersSpec());
///
/// // The query is now ready for SQL translation
/// var orders = await query.ToListAsync();
/// </code>
/// </example>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Applies a specification to a queryable, returning a new queryable with
    /// all specification constraints applied.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="inputQuery">The input queryable to apply the specification to.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>A queryable with the specification applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputQuery"/> or <paramref name="specification"/> is null.
    /// </exception>
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> inputQuery,
        Specification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        var query = inputQuery;

        // Apply the filter predicate
        query = query.Where(specification.ToExpression());

        // If it's a QuerySpecification, apply additional features
        if (specification is QuerySpecification<T> querySpec)
        {
            query = ApplyQuerySpecificationFeatures(query, querySpec);
        }

        return query;
    }

    /// <summary>
    /// Applies a specification with projection to a queryable, returning a new queryable
    /// with all specification constraints and projection applied.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="inputQuery">The input queryable to apply the specification to.</param>
    /// <param name="specification">The specification with projection to apply.</param>
    /// <returns>A queryable with the specification and projection applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputQuery"/> or <paramref name="specification"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specification does not have a selector defined.
    /// </exception>
    public static IQueryable<TResult> GetQuery<T, TResult>(
        IQueryable<T> inputQuery,
        QuerySpecification<T, TResult> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        if (specification.Selector is null)
        {
            throw new InvalidOperationException(
                $"The specification {specification.GetType().Name} must have a Selector defined for projection.");
        }

        var query = inputQuery;

        // Apply the filter predicate
        query = query.Where(specification.ToExpression());

        // Apply QuerySpecification features (includes, ordering, paging, tracking)
        query = ApplyQuerySpecificationFeatures(query, specification);

        // Apply projection
        return query.Select(specification.Selector);
    }

    /// <summary>
    /// Internal helper for interface-based specification evaluation.
    /// Used by <see cref="SpecificationEvaluatorEF{T}"/>.
    /// </summary>
    internal static IQueryable<T> GetQueryInternal<T>(
        IQueryable<T> inputQuery,
        IQuerySpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        var query = inputQuery;

        // Apply the filter predicate
        query = query.Where(specification.ToExpression());

        // Apply additional features
        query = ApplyQuerySpecificationFeatures(query, specification);

        return query;
    }

    /// <summary>
    /// Internal helper for interface-based specification with projection.
    /// Used by <see cref="SpecificationEvaluatorEF{T}"/>.
    /// </summary>
    internal static IQueryable<TResult> GetQueryInternal<T, TResult>(
        IQueryable<T> inputQuery,
        IQuerySpecification<T, TResult> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        if (specification.Selector is null)
        {
            throw new InvalidOperationException(
                $"The specification {specification.GetType().Name} must have a Selector defined for projection.");
        }

        var query = inputQuery;

        // Apply the filter predicate
        query = query.Where(specification.ToExpression());

        // Apply QuerySpecification features (includes, ordering, paging, tracking)
        query = ApplyQuerySpecificationFeatures(query, specification);

        // Apply projection
        return query.Select(specification.Selector);
    }

    private static IQueryable<T> ApplyQuerySpecificationFeatures<T>(
        IQueryable<T> query,
        QuerySpecification<T> querySpec)
        where T : class
    {
        return ApplyQuerySpecificationFeatures(query, (IQuerySpecification<T>)querySpec);
    }

    private static IQueryable<T> ApplyQuerySpecificationFeatures<T>(
        IQueryable<T> query,
        IQuerySpecification<T> querySpec)
        where T : class
    {
        // Apply AsNoTracking if specified
        if (querySpec.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        // Apply AsSplitQuery if specified
        if (querySpec.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        // Apply includes
        foreach (var include in querySpec.Includes)
        {
            query = query.Include(include);
        }

        // Apply string-based includes
        foreach (var includeString in querySpec.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        // Apply keyset pagination filter (before ordering)
        if (querySpec.KeysetPaginationEnabled && querySpec.KeysetProperty is not null && querySpec.LastKeyValue is not null)
        {
            query = ApplyKeysetFilter(query, querySpec.KeysetProperty, querySpec.LastKeyValue);
        }

        // Apply ordering with ThenBy support
        query = ApplyOrdering(query, querySpec);

        // Apply pagination (offset-based only if keyset is not enabled)
        if (!querySpec.KeysetPaginationEnabled && querySpec.IsPagingEnabled)
        {
            if (querySpec.Skip.HasValue)
            {
                query = query.Skip(querySpec.Skip.Value);
            }

            if (querySpec.Take.HasValue)
            {
                query = query.Take(querySpec.Take.Value);
            }
        }
        else if (querySpec.KeysetPaginationEnabled && querySpec.Take.HasValue)
        {
            // For keyset pagination, only apply Take (Skip is not used)
            query = query.Take(querySpec.Take.Value);
        }

        return query;
    }

    private static IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, IQuerySpecification<T> querySpec)
        where T : class
    {
        IOrderedQueryable<T>? orderedQuery = null;

        // Apply primary ordering
        if (querySpec.OrderBy is not null)
        {
            orderedQuery = query.OrderBy(querySpec.OrderBy);
        }
        else if (querySpec.OrderByDescending is not null)
        {
            orderedQuery = query.OrderByDescending(querySpec.OrderByDescending);
        }

        // If no primary ordering, return the original query
        if (orderedQuery is null)
        {
            return query;
        }

        // Apply ThenBy expressions
        foreach (var thenBy in querySpec.ThenByExpressions)
        {
            orderedQuery = orderedQuery.ThenBy(thenBy);
        }

        // Apply ThenByDescending expressions
        foreach (var thenByDesc in querySpec.ThenByDescendingExpressions)
        {
            orderedQuery = orderedQuery.ThenByDescending(thenByDesc);
        }

        return orderedQuery;
    }

    private static IQueryable<T> ApplyKeysetFilter<T>(
        IQueryable<T> query,
        Expression<Func<T, object>> keysetProperty,
        object lastKeyValue)
        where T : class
    {
        // Build: x => x.KeysetProperty > lastKeyValue
        var parameter = keysetProperty.Parameters[0];
        var memberExpression = GetMemberExpression(keysetProperty.Body);

        if (memberExpression is null)
        {
            throw new InvalidOperationException(
                "Keyset property expression must be a simple member access expression.");
        }

        var memberType = GetMemberType(memberExpression);
        var convertedValue = ConvertValue(lastKeyValue, memberType);

        var greaterThan = Expression.GreaterThan(
            memberExpression,
            Expression.Constant(convertedValue, memberType));

        var lambda = Expression.Lambda<Func<T, bool>>(greaterThan, parameter);

        return query.Where(lambda);
    }

    private static MemberExpression? GetMemberExpression(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member,
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression member } => member,
            _ => null
        };
    }

    private static Type GetMemberType(MemberExpression memberExpression)
    {
        return memberExpression.Type;
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (value.GetType() == targetType)
        {
            return value;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Instance-based specification evaluator for EF Core that implements <see cref="ISpecificationEvaluator{T}"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <remarks>
/// Use this class when you need dependency injection or want to work with the
/// <see cref="ISpecificationEvaluator{T}"/> interface.
/// For static usage, use <see cref="SpecificationEvaluator"/> instead.
/// </remarks>
public sealed class SpecificationEvaluatorEF<T> : ISpecificationEvaluator<T>
    where T : class
{
    /// <inheritdoc />
    public IQueryable<T> GetQuery(IQueryable<T> inputQuery, IQuerySpecification<T> specification)
    {
        return SpecificationEvaluator.GetQueryInternal(inputQuery, specification);
    }

    /// <inheritdoc />
    public IQueryable<TResult> GetQuery<TResult>(IQueryable<T> inputQuery, IQuerySpecification<T, TResult> specification)
    {
        return SpecificationEvaluator.GetQueryInternal(inputQuery, specification);
    }
}
