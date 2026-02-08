using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Encina.DomainModeling;
using Encina.DomainModeling.Pagination;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Repository;

/// <summary>
/// Converts <see cref="Specification{TEntity}"/> to MongoDB <see cref="FilterDefinition{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This builder translates specification expressions to MongoDB filter definitions using
/// the <see cref="Builders{T}.Filter"/> API for type-safe construction.
/// </para>
/// <para>
/// Supported operations:
/// - Equality and inequality comparisons (==, !=, &lt;, &gt;, &lt;=, &gt;=)
/// - Null checks (== null, != null)
/// - Boolean AND/OR combinations
/// - NOT operations
/// - String operations: Contains, StartsWith, EndsWith
/// </para>
/// <para>
/// For <see cref="IQuerySpecification{T}"/>:
/// - Multiple criteria combined with AND logic
/// - Sorting with ThenBy chaining via <see cref="BuildSortDefinition"/>
/// - Keyset (cursor-based) pagination via BuildKeysetFilter methods
/// - AsNoTracking and AsSplitQuery properties are ignored (not applicable to MongoDB)
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var builder = new SpecificationFilterBuilder&lt;Order&gt;();
/// var specification = new OrderByCustomerSpec(customerId);
/// var filter = builder.BuildFilter(specification);
///
/// var orders = await collection.Find(filter).ToListAsync();
/// </code>
/// </example>
public sealed class SpecificationFilterBuilder<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Builds a MongoDB filter from a specification.
    /// </summary>
    /// <param name="specification">The specification to convert.</param>
    /// <returns>A MongoDB filter definition.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specification contains unsupported expression patterns.
    /// </exception>
    public FilterDefinition<TEntity> BuildFilter(Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var expression = specification.ToExpression();
        return TranslateExpression(expression.Body, expression.Parameters[0]);
    }

    /// <summary>
    /// Builds a MongoDB filter from a specification, or returns an empty filter if null.
    /// </summary>
    /// <param name="specification">The specification to convert, or null for no filter.</param>
    /// <returns>A MongoDB filter definition, or an empty filter if specification is null.</returns>
    public FilterDefinition<TEntity> BuildFilterOrEmpty(Specification<TEntity>? specification)
    {
        if (specification is null)
        {
            return Builders<TEntity>.Filter.Empty;
        }

        return BuildFilter(specification);
    }

    /// <summary>
    /// Builds a MongoDB filter from a query specification.
    /// </summary>
    /// <param name="specification">The query specification to convert.</param>
    /// <returns>A MongoDB filter definition with all criteria combined using AND logic.</returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specification contains unsupported expression patterns.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method handles <see cref="IQuerySpecification{T}"/> which may have multiple criteria.
    /// All criteria are combined using AND logic.
    /// </para>
    /// <para>
    /// Note: AsNoTracking and AsSplitQuery properties are ignored as they are not applicable to MongoDB.
    /// Use <see cref="BuildSortDefinition"/> for sorting and pagination options.
    /// </para>
    /// </remarks>
    public FilterDefinition<TEntity> BuildFilter(QuerySpecification<TEntity> specification)
    {
        return BuildFilterInternal((IQuerySpecification<TEntity>)specification);
    }

    /// <summary>
    /// Internal method for building filter from interface.
    /// </summary>
    internal FilterDefinition<TEntity> BuildFilterInternal(IQuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var filters = new List<FilterDefinition<TEntity>>();

        // Add the main expression filter
        var expression = specification.ToExpression();
        filters.Add(TranslateExpression(expression.Body, expression.Parameters[0]));

        // Add keyset pagination filter if enabled
        if (specification.KeysetPaginationEnabled &&
            specification.KeysetProperty is not null &&
            specification.LastKeyValue is not null)
        {
            filters.Add(BuildKeysetFilter(specification.KeysetProperty, specification.LastKeyValue));
        }

        return filters.Count == 1
            ? filters[0]
            : Builders<TEntity>.Filter.And(filters);
    }

    /// <summary>
    /// Builds a MongoDB sort definition from a query specification.
    /// </summary>
    /// <param name="specification">The query specification containing ordering expressions.</param>
    /// <returns>
    /// A MongoDB sort definition, or null if no ordering is specified.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when specification is null.</exception>
    /// <remarks>
    /// <para>
    /// Supports multi-column ordering through ThenBy and ThenByDescending expressions.
    /// The primary ordering (OrderBy or OrderByDescending) is applied first, followed by
    /// secondary ordering expressions in the order they were added.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = new SpecificationFilterBuilder&lt;Order&gt;();
    /// var spec = new OrdersByDateSpec(); // OrderByDescending(Date).ThenBy(Id)
    /// var sort = builder.BuildSortDefinition(spec);
    ///
    /// var orders = await collection.Find(filter).Sort(sort).ToListAsync();
    /// </code>
    /// </example>
    public SortDefinition<TEntity>? BuildSortDefinition(IQuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        SortDefinition<TEntity>? sort = null;

        // Apply primary ordering
        if (specification.OrderBy is not null)
        {
            var fieldName = GetFieldNameFromExpression(specification.OrderBy);
            sort = Builders<TEntity>.Sort.Ascending(fieldName);
        }
        else if (specification.OrderByDescending is not null)
        {
            var fieldName = GetFieldNameFromExpression(specification.OrderByDescending);
            sort = Builders<TEntity>.Sort.Descending(fieldName);
        }

        if (sort is null)
        {
            return null;
        }

        // Apply ThenBy expressions
        foreach (var thenBy in specification.ThenByExpressions)
        {
            var fieldName = GetFieldNameFromExpression(thenBy);
            sort = sort.Ascending(fieldName);
        }

        // Apply ThenByDescending expressions
        foreach (var thenByDesc in specification.ThenByDescendingExpressions)
        {
            var fieldName = GetFieldNameFromExpression(thenByDesc);
            sort = sort.Descending(fieldName);
        }

        return sort;
    }

    /// <summary>
    /// Builds a keyset pagination filter for cursor-based pagination.
    /// </summary>
    /// <param name="keysetProperty">The keyset property expression.</param>
    /// <param name="lastKeyValue">The last key value from the previous page.</param>
    /// <returns>A filter definition for keyset pagination (greater than comparison).</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when keysetProperty or lastKeyValue is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Keyset pagination is more efficient than offset-based pagination for large datasets
    /// as it uses indexed lookups instead of counting and skipping rows.
    /// </para>
    /// <para>
    /// The filter returns documents where the keyset property is greater than the provided value,
    /// enabling efficient "next page" queries.
    /// </para>
    /// </remarks>
    public FilterDefinition<TEntity> BuildKeysetFilter(
        Expression<Func<TEntity, object>> keysetProperty,
        object lastKeyValue)
    {
        return BuildKeysetFilter(keysetProperty, lastKeyValue, isDescending: false, CursorDirection.Forward);
    }

    /// <summary>
    /// Builds a keyset pagination filter for cursor-based pagination with direction support.
    /// </summary>
    /// <param name="keysetProperty">The keyset property expression.</param>
    /// <param name="lastKeyValue">The last key value from the previous page.</param>
    /// <param name="isDescending">Whether the sort order is descending.</param>
    /// <param name="direction">The cursor pagination direction.</param>
    /// <returns>A filter definition for keyset pagination.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when keysetProperty or lastKeyValue is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Keyset pagination is more efficient than offset-based pagination for large datasets
    /// as it uses indexed lookups instead of counting and skipping rows.
    /// </para>
    /// <para>
    /// The filter operator is determined by combining sort direction and pagination direction:
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>Sort</term>
    /// <term>Direction</term>
    /// <term>Operator</term>
    /// </listheader>
    /// <item><term>Ascending</term><term>Forward</term><term>Greater Than (&gt;)</term></item>
    /// <item><term>Ascending</term><term>Backward</term><term>Less Than (&lt;)</term></item>
    /// <item><term>Descending</term><term>Forward</term><term>Less Than (&lt;)</term></item>
    /// <item><term>Descending</term><term>Backward</term><term>Greater Than (&gt;)</term></item>
    /// </list>
    /// </remarks>
    public FilterDefinition<TEntity> BuildKeysetFilter(
        Expression<Func<TEntity, object>> keysetProperty,
        object lastKeyValue,
        bool isDescending,
        CursorDirection direction)
    {
        ArgumentNullException.ThrowIfNull(keysetProperty);
        ArgumentNullException.ThrowIfNull(lastKeyValue);

        var fieldName = GetFieldNameFromExpression(keysetProperty);
        var memberType = GetMemberTypeFromExpression(keysetProperty);
        var convertedValue = ConvertValue(lastKeyValue, memberType);

        // Determine comparison operator based on sort direction and pagination direction:
        // Forward + Ascending = Gt (next items are greater)
        // Forward + Descending = Lt (next items are less)
        // Backward + Ascending = Lt (previous items are less)
        // Backward + Descending = Gt (previous items are greater)
        var isBackward = direction == CursorDirection.Backward;
        var useGreaterThan = isDescending == isBackward;

        return useGreaterThan
            ? Builders<TEntity>.Filter.Gt(fieldName, convertedValue)
            : Builders<TEntity>.Filter.Lt(fieldName, convertedValue);
    }

    /// <summary>
    /// Builds a compound keyset pagination filter for multi-column sorting.
    /// </summary>
    /// <param name="keyColumns">
    /// A list of tuples containing field name, last value, and whether the column is descending.
    /// </param>
    /// <param name="direction">The cursor pagination direction.</param>
    /// <returns>A filter definition for compound keyset pagination.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyColumns is null.</exception>
    /// <exception cref="ArgumentException">Thrown when keyColumns is empty.</exception>
    /// <remarks>
    /// <para>
    /// Compound keyset filters use the following pattern for multi-column sorting:
    /// <code>
    /// (col1 > val1) OR (col1 = val1 AND col2 > val2) OR (col1 = val1 AND col2 = val2 AND col3 > val3)
    /// </code>
    /// </para>
    /// <para>
    /// The comparison operator for each column is determined by its sort direction and the pagination direction.
    /// </para>
    /// </remarks>
    public FilterDefinition<TEntity> BuildCompoundKeysetFilter(
        IReadOnlyList<(string FieldName, object Value, bool IsDescending)> keyColumns,
        CursorDirection direction)
    {
        ArgumentNullException.ThrowIfNull(keyColumns);

        if (keyColumns.Count == 0)
        {
            throw new ArgumentException("At least one key column is required.", nameof(keyColumns));
        }

        // For single column, use simple filter
        if (keyColumns.Count == 1)
        {
            var (fieldName, value, isDescending) = keyColumns[0];
            var isBackward = direction == CursorDirection.Backward;
            var useGreaterThan = isDescending == isBackward;

            return useGreaterThan
                ? Builders<TEntity>.Filter.Gt(fieldName, value)
                : Builders<TEntity>.Filter.Lt(fieldName, value);
        }

        // Compound filter: OR of increasingly specific conditions
        var orFilters = new List<FilterDefinition<TEntity>>();
        var isBackwardDirection = direction == CursorDirection.Backward;

        for (var i = 0; i < keyColumns.Count; i++)
        {
            var andFilters = new List<FilterDefinition<TEntity>>();

            // Add equality conditions for all preceding columns
            for (var j = 0; j < i; j++)
            {
                var (eqFieldName, eqValue, _) = keyColumns[j];
                andFilters.Add(Builders<TEntity>.Filter.Eq(eqFieldName, eqValue));
            }

            // Add comparison condition for current column
            var (fieldName, value, isDescending) = keyColumns[i];
            var useGreaterThan = isDescending == isBackwardDirection;

            andFilters.Add(useGreaterThan
                ? Builders<TEntity>.Filter.Gt(fieldName, value)
                : Builders<TEntity>.Filter.Lt(fieldName, value));

            orFilters.Add(andFilters.Count == 1
                ? andFilters[0]
                : Builders<TEntity>.Filter.And(andFilters));
        }

        return Builders<TEntity>.Filter.Or(orFilters);
    }

    /// <summary>
    /// Gets the field name from a member access expression.
    /// </summary>
    /// <param name="expression">The expression to extract the field name from.</param>
    /// <returns>The dot-notation field name for nested properties.</returns>
    private static string GetFieldNameFromExpression(Expression<Func<TEntity, object>> expression)
    {
        var body = expression.Body;

        // Unwrap Convert expression if present (boxing to object)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand })
        {
            body = operand;
        }

        if (body is MemberExpression member)
        {
            return GetFieldName(member);
        }

        throw new NotSupportedException(
            $"Expression type '{body.NodeType}' is not supported for field name extraction.");
    }

    /// <summary>
    /// Gets the member type from a member access expression.
    /// </summary>
    private static Type GetMemberTypeFromExpression(Expression<Func<TEntity, object>> expression)
    {
        var body = expression.Body;

        // Unwrap Convert expression if present (boxing to object)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand })
        {
            body = operand;
        }

        if (body is MemberExpression member)
        {
            return member.Type;
        }

        throw new NotSupportedException(
            $"Expression type '{body.NodeType}' is not supported for type extraction.");
    }

    /// <summary>
    /// Converts a value to the target type for comparison.
    /// </summary>
    private static object? ConvertValue(object value, Type targetType)
    {
        if (value.GetType() == targetType)
        {
            return value;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
    }

    private FilterDefinition<TEntity> TranslateExpression(Expression expression, ParameterExpression parameter)
    {
        return expression switch
        {
            BinaryExpression binary => TranslateBinaryExpression(binary, parameter),
            UnaryExpression unary => TranslateUnaryExpression(unary, parameter),
            MethodCallExpression methodCall => TranslateMethodCallExpression(methodCall),
            MemberExpression member when member.Type == typeof(bool) => TranslateBooleanMember(member),
            ConstantExpression constant when constant.Type == typeof(bool) => TranslateBooleanConstant(constant),
            _ => throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported for MongoDB filter translation.")
        };
    }

    private FilterDefinition<TEntity> TranslateBinaryExpression(BinaryExpression binary, ParameterExpression parameter)
    {
        return binary.NodeType switch
        {
            ExpressionType.AndAlso => Builders<TEntity>.Filter.And(
                TranslateExpression(binary.Left, parameter),
                TranslateExpression(binary.Right, parameter)),

            ExpressionType.OrElse => Builders<TEntity>.Filter.Or(
                TranslateExpression(binary.Left, parameter),
                TranslateExpression(binary.Right, parameter)),

            ExpressionType.Equal => BuildEqualityFilter(binary, isEqual: true),
            ExpressionType.NotEqual => BuildEqualityFilter(binary, isEqual: false),
            ExpressionType.LessThan => BuildComparisonFilter(binary, ComparisonType.LessThan),
            ExpressionType.LessThanOrEqual => BuildComparisonFilter(binary, ComparisonType.LessThanOrEqual),
            ExpressionType.GreaterThan => BuildComparisonFilter(binary, ComparisonType.GreaterThan),
            ExpressionType.GreaterThanOrEqual => BuildComparisonFilter(binary, ComparisonType.GreaterThanOrEqual),

            _ => throw new NotSupportedException($"Binary operator '{binary.NodeType}' is not supported.")
        };
    }

    private FilterDefinition<TEntity> TranslateUnaryExpression(UnaryExpression unary, ParameterExpression parameter)
    {
        return unary.NodeType switch
        {
            ExpressionType.Not => Builders<TEntity>.Filter.Not(TranslateExpression(unary.Operand, parameter)),
            ExpressionType.Convert => TranslateExpression(unary.Operand, parameter),
            _ => throw new NotSupportedException($"Unary operator '{unary.NodeType}' is not supported.")
        };
    }

    private static FilterDefinition<TEntity> TranslateMethodCallExpression(MethodCallExpression methodCall)
    {
        var methodName = methodCall.Method.Name;

        // Handle string methods: Contains, StartsWith, EndsWith
        if (methodCall.Object is MemberExpression member && methodCall.Method.DeclaringType == typeof(string))
        {
            var fieldName = GetFieldName(member);
            var value = GetValue(methodCall.Arguments[0])?.ToString() ?? string.Empty;

            return methodName switch
            {
                "Contains" => Builders<TEntity>.Filter.Regex(fieldName, new BsonRegularExpression(EscapeRegex(value))),
                "StartsWith" => Builders<TEntity>.Filter.Regex(fieldName, new BsonRegularExpression($"^{EscapeRegex(value)}")),
                "EndsWith" => Builders<TEntity>.Filter.Regex(fieldName, new BsonRegularExpression($"{EscapeRegex(value)}$")),
                _ => throw new NotSupportedException($"String method '{methodName}' is not supported.")
            };
        }

        // Handle Enumerable.Contains for IN queries
        if (methodName == "Contains" && methodCall.Method.DeclaringType?.FullName?.StartsWith("System.Linq.Enumerable", StringComparison.Ordinal) == true)
        {
            // Pattern: collection.Contains(entity.Property)
            if (methodCall.Arguments.Count == 2 &&
                methodCall.Arguments[1] is MemberExpression propertyMember)
            {
                var values = GetValue(methodCall.Arguments[0]);
                var fieldName = GetFieldName(propertyMember);

                if (values is System.Collections.IEnumerable enumerable)
                {
                    var valueList = enumerable.Cast<object?>().ToList();
                    return Builders<TEntity>.Filter.In(fieldName, valueList);
                }
            }
        }

        throw new NotSupportedException($"Method '{methodName}' is not supported for MongoDB filter translation.");
    }

    private static FilterDefinition<TEntity> TranslateBooleanMember(MemberExpression member)
    {
        var fieldExpression = GetFieldExpression<bool>(member);
        return Builders<TEntity>.Filter.Eq(fieldExpression, true);
    }

    private static FilterDefinition<TEntity> TranslateBooleanConstant(ConstantExpression constant)
    {
        var value = (bool)constant.Value!;
        return value
            ? Builders<TEntity>.Filter.Empty
            : Builders<TEntity>.Filter.Where(_ => false);
    }

    private static FilterDefinition<TEntity> BuildEqualityFilter(BinaryExpression binary, bool isEqual)
    {
        var (memberExpression, valueExpression) = ExtractMemberAndValue(binary);
        var value = GetValue(valueExpression);

        // Handle null comparisons
        if (value is null)
        {
            var fieldName = GetFieldName(memberExpression);
            return isEqual
                ? Builders<TEntity>.Filter.Eq(fieldName, BsonNull.Value)
                : Builders<TEntity>.Filter.Ne(fieldName, BsonNull.Value);
        }

        var field = GetFieldName(memberExpression);

        return isEqual
            ? Builders<TEntity>.Filter.Eq(field, value)
            : Builders<TEntity>.Filter.Ne(field, value);
    }

    private static FilterDefinition<TEntity> BuildComparisonFilter(BinaryExpression binary, ComparisonType comparison)
    {
        var (memberExpression, valueExpression) = ExtractMemberAndValue(binary);
        var value = GetValue(valueExpression);
        var fieldName = GetFieldName(memberExpression);

        return comparison switch
        {
            ComparisonType.LessThan => Builders<TEntity>.Filter.Lt(fieldName, value),
            ComparisonType.LessThanOrEqual => Builders<TEntity>.Filter.Lte(fieldName, value),
            ComparisonType.GreaterThan => Builders<TEntity>.Filter.Gt(fieldName, value),
            ComparisonType.GreaterThanOrEqual => Builders<TEntity>.Filter.Gte(fieldName, value),
            _ => throw new NotSupportedException($"Comparison type '{comparison}' is not supported.")
        };
    }

    private static (MemberExpression Member, Expression Value) ExtractMemberAndValue(BinaryExpression binary)
    {
        // Try left as member, right as value
        if (TryGetMemberExpression(binary.Left, out var leftMember))
        {
            return (leftMember, binary.Right);
        }

        // Try right as member, left as value
        if (TryGetMemberExpression(binary.Right, out var rightMember))
        {
            return (rightMember, binary.Left);
        }

        throw new NotSupportedException("Binary expression must have at least one member access expression.");
    }

    private static bool TryGetMemberExpression(Expression expression, out MemberExpression member)
    {
        member = expression switch
        {
            MemberExpression m => m,
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression unaryMember } => unaryMember,
            _ => null!
        };

        return member is not null;
    }

    private static Expression<Func<TEntity, TField>> GetFieldExpression<TField>(MemberExpression member)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var body = RebuildMemberAccess(member, parameter);
        return Expression.Lambda<Func<TEntity, TField>>(body, parameter);
    }

    private static string GetFieldName(MemberExpression member)
    {
        var parts = new List<string>();
        Expression? current = member;

        while (current is MemberExpression memberExpr)
        {
            parts.Insert(0, memberExpr.Member.Name);
            current = memberExpr.Expression;
        }

        return string.Join(".", parts);
    }

    private static MemberExpression RebuildMemberAccess(MemberExpression member, ParameterExpression parameter)
    {
        if (member.Expression is ParameterExpression)
        {
            return Expression.MakeMemberAccess(parameter, member.Member);
        }

        if (member.Expression is MemberExpression parentMember)
        {
            var rebuiltParent = RebuildMemberAccess(parentMember, parameter);
            return Expression.MakeMemberAccess(rebuiltParent, member.Member);
        }

        throw new NotSupportedException($"Cannot rebuild member access for expression type {member.Expression?.NodeType}.");
    }

    private static object? GetValue(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression member => GetMemberValue(member),
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand } => GetValue(operand),
            NewExpression newExpr => Expression.Lambda(newExpr).Compile().DynamicInvoke(),
            MethodCallExpression methodCall => Expression.Lambda(methodCall).Compile().DynamicInvoke(),
            _ => Expression.Lambda(expression).Compile().DynamicInvoke()
        };
    }

    private static object? GetMemberValue(MemberExpression member)
    {
        // Handle captured variables (closures)
        if (member.Expression is ConstantExpression constant)
        {
            var container = constant.Value;
            return member.Member switch
            {
                System.Reflection.FieldInfo field => field.GetValue(container),
                System.Reflection.PropertyInfo prop => prop.GetValue(container),
                _ => throw new NotSupportedException($"Member type {member.Member.GetType()} is not supported.")
            };
        }

        // Compile and evaluate
        return Expression.Lambda(member).Compile().DynamicInvoke();
    }

    private static string EscapeRegex(string value)
    {
        // Escape special regex characters
        return Regex.Escape(value);
    }

    private enum ComparisonType
    {
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }
}
