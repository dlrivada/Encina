using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.Messaging;

namespace Encina.Dapper.SqlServer.Repository;

/// <summary>
/// Translates <see cref="Specification{T}"/> expressions to SQL clauses.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This class provides specification-to-SQL translation for common predicates.
/// It generates parameterized SQL to prevent SQL injection.
/// </para>
/// <para>
/// <b>Supported Operations:</b>
/// <list type="bullet">
/// <item><description>Multiple criteria combined with AND logic</description></item>
/// <item><description>Equality comparisons (==, !=)</description></item>
/// <item><description>Relational comparisons (&lt;, &gt;, &lt;=, &gt;=)</description></item>
/// <item><description>Null checks (== null, != null)</description></item>
/// <item><description>AND/OR combinations</description></item>
/// <item><description>NOT negation</description></item>
/// <item><description>String Contains, StartsWith, EndsWith</description></item>
/// <item><description>Multi-column ORDER BY with ThenBy support</description></item>
/// <item><description>Offset-based pagination (OFFSET/FETCH)</description></item>
/// <item><description>Keyset (cursor-based) pagination</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Limitations:</b>
/// <list type="bullet">
/// <item><description>Complex method calls are not supported</description></item>
/// <item><description>Nested property access has limited support</description></item>
/// <item><description>Custom functions are not translated</description></item>
/// </list>
/// For complex queries, consider using raw SQL or stored procedures.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var spec = new ActiveOrdersSpec(); // o => o.Status == "Active"
/// var builder = new SpecificationSqlBuilder&lt;Order&gt;(mapping);
/// var (whereClause, parameters) = builder.BuildWhereClause(spec);
/// // whereClause: "WHERE [Status] = @p0"
/// // parameters: { p0 = "Active" }
/// </code>
/// </example>
public sealed class SpecificationSqlBuilder<TEntity>
    where TEntity : class
{
    private readonly IReadOnlyDictionary<string, string> _columnMappings;
    private int _parameterIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationSqlBuilder{TEntity}"/> class.
    /// </summary>
    /// <param name="columnMappings">Dictionary mapping property names to column names.</param>
    public SpecificationSqlBuilder(IReadOnlyDictionary<string, string> columnMappings)
    {
        ArgumentNullException.ThrowIfNull(columnMappings);
        _columnMappings = columnMappings;
    }

    /// <summary>
    /// Builds a WHERE clause from a specification.
    /// </summary>
    /// <param name="specification">The specification to translate.</param>
    /// <returns>A tuple containing the WHERE clause and parameters dictionary.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the expression contains unsupported operations.
    /// </exception>
    public (string WhereClause, IDictionary<string, object?> Parameters) BuildWhereClause(
        Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _parameterIndex = 0;
        var parameters = new Dictionary<string, object?>();

        var expression = specification.ToExpression();
        var sql = TranslateExpression(expression.Body, parameters);

        var whereClause = string.IsNullOrWhiteSpace(sql) ? "" : $"WHERE {sql}";

        return (whereClause, parameters);
    }

    /// <summary>
    /// Builds a WHERE clause from a query specification.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>A tuple containing the WHERE clause and parameters dictionary.</returns>
    public (string WhereClause, IDictionary<string, object?> Parameters) BuildWhereClause(
        QuerySpecification<TEntity> specification)
    {
        return BuildWhereClauseInternal((IQuerySpecification<TEntity>)specification);
    }

    /// <summary>
    /// Internal method for building WHERE clause from interface.
    /// </summary>
    internal (string WhereClause, IDictionary<string, object?> Parameters) BuildWhereClauseInternal(
        IQuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _parameterIndex = 0;
        var parameters = new Dictionary<string, object?>();

        var expression = specification.ToExpression();
        var sql = TranslateExpression(expression.Body, parameters);

        // Handle keyset pagination (add filter for cursor)
        if (specification.KeysetPaginationEnabled &&
            specification.KeysetProperty is not null &&
            specification.LastKeyValue is not null)
        {
            var keysetSql = BuildKeysetFilter(specification.KeysetProperty, specification.LastKeyValue, parameters);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                sql = $"({sql}) AND ({keysetSql})";
            }
            else
            {
                sql = keysetSql;
            }
        }

        var whereClause = string.IsNullOrWhiteSpace(sql) ? "" : $"WHERE {sql}";

        return (whereClause, parameters);
    }

    /// <summary>
    /// Builds an ORDER BY clause from a query specification.
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>The ORDER BY clause (without the ORDER BY keywords if empty).</returns>
    public string BuildOrderByClause(IQuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var orderClauses = new List<string>();

        // Primary ordering
        if (specification.OrderBy is not null)
        {
            var columnName = GetColumnNameFromExpression(specification.OrderBy);
            orderClauses.Add($"[{columnName}] ASC");
        }
        else if (specification.OrderByDescending is not null)
        {
            var columnName = GetColumnNameFromExpression(specification.OrderByDescending);
            orderClauses.Add($"[{columnName}] DESC");
        }

        // ThenBy expressions (ascending)
        foreach (var thenBy in specification.ThenByExpressions)
        {
            var columnName = GetColumnNameFromExpression(thenBy);
            orderClauses.Add($"[{columnName}] ASC");
        }

        // ThenByDescending expressions
        foreach (var thenByDesc in specification.ThenByDescendingExpressions)
        {
            var columnName = GetColumnNameFromExpression(thenByDesc);
            orderClauses.Add($"[{columnName}] DESC");
        }

        return orderClauses.Count > 0
            ? $"ORDER BY {string.Join(", ", orderClauses)}"
            : string.Empty;
    }

    /// <summary>
    /// Builds pagination SQL (OFFSET/FETCH for SQL Server).
    /// </summary>
    /// <param name="specification">The query specification to translate.</param>
    /// <returns>The pagination clause.</returns>
    /// <remarks>
    /// For keyset pagination, this returns only FETCH (no OFFSET).
    /// For offset-based pagination, this returns OFFSET and FETCH.
    /// Requires ORDER BY to be present for SQL Server.
    /// </remarks>
    public string BuildPaginationClause(IQuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        if (specification.KeysetPaginationEnabled)
        {
            // Keyset pagination only needs FETCH (filter handles the offset)
            return specification.Take.HasValue
                ? $"FETCH NEXT {specification.Take.Value} ROWS ONLY"
                : string.Empty;
        }

        if (!specification.IsPagingEnabled)
        {
            return string.Empty;
        }

        var skip = specification.Skip ?? 0;
        var take = specification.Take;

        if (take.HasValue)
        {
            return $"OFFSET {skip} ROWS FETCH NEXT {take.Value} ROWS ONLY";
        }

        return skip > 0 ? $"OFFSET {skip} ROWS" : string.Empty;
    }

    /// <summary>
    /// Builds a complete SELECT statement for all rows.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <returns>A tuple containing the SQL statement and empty parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatement(string tableName)
    {
        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _columnMappings.Values.Select(c => $"[{c}]"));

        return ($"SELECT {columns} FROM {validatedTableName}", new Dictionary<string, object?>());
    }

    /// <summary>
    /// Builds a complete SELECT statement with a WHERE clause from a specification.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <param name="specification">The specification for filtering.</param>
    /// <returns>A tuple containing the SQL statement and parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatement(
        string tableName,
        Specification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _columnMappings.Values.Select(c => $"[{c}]"));

        var (whereClause, parameters) = BuildWhereClause(specification);
        var sql = $"SELECT {columns} FROM {validatedTableName} {whereClause}".Trim();

        return (sql, parameters);
    }

    /// <summary>
    /// Builds a complete SELECT statement from a query specification with all features.
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <param name="specification">The query specification for filtering, ordering, and pagination.</param>
    /// <returns>A tuple containing the SQL statement and parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatement(
        string tableName,
        QuerySpecification<TEntity> specification)
    {
        return BuildSelectStatementInternal(tableName, (IQuerySpecification<TEntity>)specification);
    }

    /// <summary>
    /// Internal method for building SELECT statement from interface.
    /// </summary>
    internal (string Sql, IDictionary<string, object?> Parameters) BuildSelectStatementInternal(
        string tableName,
        IQuerySpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);
        var columns = string.Join(", ", _columnMappings.Values.Select(c => $"[{c}]"));

        var (whereClause, parameters) = BuildWhereClauseInternal(specification);
        var orderByClause = BuildOrderByClause(specification);
        var paginationClause = BuildPaginationClause(specification);

        // For keyset pagination, ORDER BY must include OFFSET 0 before FETCH
        var needsOffsetZero = specification.KeysetPaginationEnabled &&
                              specification.Take.HasValue &&
                              !string.IsNullOrEmpty(orderByClause);

        var sqlParts = new List<string>
        {
            $"SELECT {columns} FROM {validatedTableName}"
        };

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sqlParts.Add(whereClause);
        }

        if (!string.IsNullOrWhiteSpace(orderByClause))
        {
            sqlParts.Add(orderByClause);
        }

        if (needsOffsetZero)
        {
            sqlParts.Add("OFFSET 0 ROWS");
        }

        if (!string.IsNullOrWhiteSpace(paginationClause))
        {
            sqlParts.Add(paginationClause);
        }

        var sql = string.Join(" ", sqlParts);

        return (sql, parameters);
    }

    private string BuildKeysetFilter(
        Expression<Func<TEntity, object>> keysetProperty,
        object lastKeyValue,
        Dictionary<string, object?> parameters)
    {
        var columnName = GetColumnNameFromExpression(keysetProperty);
        var paramName = $"p{_parameterIndex++}";
        parameters[paramName] = lastKeyValue;

        return $"[{columnName}] > @{paramName}";
    }

    /// <summary>
    /// Extracts the database column name from a selector expression (e.g., <c>e =&gt; e.Amount</c>).
    /// </summary>
    /// <typeparam name="TValue">The type of the selected property.</typeparam>
    /// <param name="selector">The selector expression.</param>
    /// <returns>The mapped database column name.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the column name cannot be extracted from the expression.
    /// </exception>
    public string GetColumnNameFromSelector<TValue>(Expression<Func<TEntity, TValue>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var body = selector.Body;

        if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var selectorOperand })
        {
            body = selectorOperand;
        }

        if (body is MemberExpression selectorMember)
        {
            var propertyName = selectorMember.Member.Name;
            if (_columnMappings.TryGetValue(propertyName, out var columnName))
            {
                return columnName;
            }

            return SqlIdentifierValidator.ValidateTableName(propertyName, "propertyName");
        }

        throw new NotSupportedException($"Cannot extract column name from selector expression {selector}.");
    }

    /// <summary>
    /// Builds a SQL aggregation query (e.g., <c>SELECT COUNT(*) FROM table WHERE ...</c>).
    /// </summary>
    /// <param name="tableName">The validated table name.</param>
    /// <param name="aggregateExpression">The aggregate expression (e.g., <c>"COUNT(*)"</c>, <c>"SUM([Amount])"</c>).</param>
    /// <param name="predicate">An optional filter predicate.</param>
    /// <returns>A tuple containing the SQL statement and parameters dictionary.</returns>
    public (string Sql, IDictionary<string, object?> Parameters) BuildAggregationSql(
        string tableName,
        string aggregateExpression,
        Expression<Func<TEntity, bool>>? predicate)
    {
        var validatedTableName = SqlIdentifierValidator.ValidateTableName(tableName);

        if (predicate is null)
        {
            return ($"SELECT {aggregateExpression} FROM {validatedTableName}", new Dictionary<string, object?>());
        }

        _parameterIndex = 0;
        var parameters = new Dictionary<string, object?>();
        var sql = TranslateExpression(predicate.Body, parameters);
        var whereClause = string.IsNullOrWhiteSpace(sql) ? "" : $"WHERE {sql}";

        var fullSql = $"SELECT {aggregateExpression} FROM {validatedTableName} {whereClause}".Trim();

        return (fullSql, parameters);
    }

    private string GetColumnNameFromExpression(Expression<Func<TEntity, object>> expression)
    {
        var body = expression.Body;

        // Handle Convert for value types
        if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand })
        {
            body = operand;
        }

        if (body is MemberExpression member)
        {
            var propertyName = member.Member.Name;
            if (_columnMappings.TryGetValue(propertyName, out var columnName))
            {
                return columnName;
            }

            return SqlIdentifierValidator.ValidateTableName(propertyName, "propertyName");
        }

        throw new NotSupportedException($"Cannot extract column name from expression {expression}.");
    }

    private string TranslateExpression(Expression expression, Dictionary<string, object?> parameters)
    {
        return expression switch
        {
            BinaryExpression binary => TranslateBinaryExpression(binary, parameters),
            UnaryExpression unary => TranslateUnaryExpression(unary, parameters),
            MethodCallExpression methodCall => TranslateMethodCallExpression(methodCall, parameters),
            MemberExpression member when member.Type == typeof(bool) => TranslateBooleanMember(member),
            ConstantExpression constant when constant.Type == typeof(bool) => (bool)constant.Value! ? "1=1" : "1=0",
            InvocationExpression invocation => TranslateInvocationExpression(invocation, parameters),
            LambdaExpression lambda => TranslateExpression(lambda.Body, parameters),
            _ => throw new NotSupportedException($"Expression type {expression.NodeType} is not supported for SQL translation.")
        };
    }

    private string TranslateBinaryExpression(BinaryExpression binary, Dictionary<string, object?> parameters)
    {
        // Handle AND/OR
        if (binary.NodeType == ExpressionType.AndAlso)
        {
            var left = TranslateExpression(binary.Left, parameters);
            var right = TranslateExpression(binary.Right, parameters);
            return $"({left}) AND ({right})";
        }

        if (binary.NodeType == ExpressionType.OrElse)
        {
            var left = TranslateExpression(binary.Left, parameters);
            var right = TranslateExpression(binary.Right, parameters);
            return $"({left}) OR ({right})";
        }

        // Handle comparison operators
        var columnName = GetColumnName(binary.Left);
        var value = GetValue(binary.Right);

        var sqlOperator = binary.NodeType switch
        {
            ExpressionType.Equal when value is null => " IS NULL",
            ExpressionType.NotEqual when value is null => " IS NOT NULL",
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " <> ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",
            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            _ => throw new NotSupportedException($"Binary operator {binary.NodeType} is not supported.")
        };

        if (value is null)
        {
            return $"[{columnName}]{sqlOperator}";
        }

        var paramName = $"p{_parameterIndex++}";
        parameters[paramName] = value;
        return $"[{columnName}]{sqlOperator}@{paramName}";
    }

    private string TranslateUnaryExpression(UnaryExpression unary, Dictionary<string, object?> parameters)
    {
        if (unary.NodeType == ExpressionType.Not)
        {
            var operand = TranslateExpression(unary.Operand, parameters);
            return $"NOT ({operand})";
        }

        if (unary.NodeType == ExpressionType.Convert)
        {
            return TranslateExpression(unary.Operand, parameters);
        }

        throw new NotSupportedException($"Unary operator {unary.NodeType} is not supported.");
    }

    private string TranslateMethodCallExpression(MethodCallExpression methodCall, Dictionary<string, object?> parameters)
    {
        var methodName = methodCall.Method.Name;

        // String methods
        if (methodCall.Method.DeclaringType == typeof(string))
        {
            var columnName = GetColumnName(methodCall.Object!);
            var value = GetValue(methodCall.Arguments[0]);
            var paramName = $"p{_parameterIndex++}";

            return methodName switch
            {
                "Contains" => BuildLikeClause(columnName, $"%{value}%", paramName, parameters),
                "StartsWith" => BuildLikeClause(columnName, $"{value}%", paramName, parameters),
                "EndsWith" => BuildLikeClause(columnName, $"%{value}", paramName, parameters),
                "Equals" => BuildEqualsClause(columnName, value, paramName, parameters),
                _ => throw new NotSupportedException($"String method {methodName} is not supported.")
            };
        }

        // Enumerable.Contains() - generates IN clause
        if (methodName == "Contains" && IsEnumerableContainsMethod(methodCall))
        {
            return TranslateEnumerableContains(methodCall, parameters);
        }

        throw new NotSupportedException($"Method {methodCall.Method.DeclaringType?.Name}.{methodName} is not supported for SQL translation.");
    }

    private static bool IsEnumerableContainsMethod(MethodCallExpression methodCall)
    {
        // Static Enumerable.Contains(source, item)
        if (methodCall.Method.DeclaringType == typeof(Enumerable))
        {
            return true;
        }

        // Check if it's MemoryExtensions.Contains<T>(ReadOnlySpan<T>, T) or similar
        // This handles T[].Contains(item) which .NET routes through MemoryExtensions
        if (methodCall.Method.DeclaringType == typeof(MemoryExtensions))
        {
            return true;
        }

        // Instance method on collection types: list.Contains(item)
        var declaringType = methodCall.Method.DeclaringType;
        if (declaringType is not null)
        {
            if (declaringType.IsGenericType)
            {
                var genericTypeDef = declaringType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(HashSet<>))
                {
                    return true;
                }
            }

            // Check if it's an array type (T[])
            if (declaringType.IsArray)
            {
                return true;
            }
        }

        return false;
    }

    private string TranslateEnumerableContains(MethodCallExpression methodCall, Dictionary<string, object?> parameters)
    {
        Expression collectionExpr;
        Expression itemExpr;

        // Static: Enumerable.Contains(collection, item) or MemoryExtensions.Contains(span, item)
        if (methodCall.Object is null)
        {
            collectionExpr = methodCall.Arguments[0];
            collectionExpr = UnwrapImplicitConversion(collectionExpr);
            itemExpr = methodCall.Arguments[1];
        }
        else
        {
            // Instance: collection.Contains(item)
            collectionExpr = methodCall.Object!;
            itemExpr = methodCall.Arguments[0];
        }

        // Get the column name from the item expression (e.g., e => e.Id)
        var columnName = GetColumnName(itemExpr);

        // Get the collection values
        var collectionValue = GetValue(collectionExpr);
        if (collectionValue is not System.Collections.IEnumerable enumerable)
        {
            throw new NotSupportedException("Contains method requires an IEnumerable collection.");
        }

        var values = enumerable.Cast<object?>().ToList();
        if (values.Count == 0)
        {
            // Empty collection - return FALSE (no matches possible)
            return "1 = 0";
        }

        // Build IN clause with parameters
        var paramNames = new List<string>();
        foreach (var value in values)
        {
            var paramName = $"p{_parameterIndex++}";
            parameters[paramName] = value;
            paramNames.Add($"@{paramName}");
        }

        return $"[{columnName}] IN ({string.Join(", ", paramNames)})";
    }

    /// <summary>
    /// Unwraps implicit conversion expressions (e.g., op_Implicit) to get the underlying expression.
    /// This is needed for MemoryExtensions.Contains which wraps arrays in ReadOnlySpan via op_Implicit.
    /// </summary>
    private static Expression UnwrapImplicitConversion(Expression expression)
    {
        if (expression is MethodCallExpression mce &&
            mce.Method.Name == "op_Implicit" &&
            mce.Arguments.Count == 1)
        {
            return UnwrapImplicitConversion(mce.Arguments[0]);
        }

        if (expression is UnaryExpression unary &&
            (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            return UnwrapImplicitConversion(unary.Operand);
        }

        return expression;
    }

    private string TranslateBooleanMember(MemberExpression member)
    {
        var columnName = GetColumnName(member);
        return $"[{columnName}] = 1";
    }

    private string TranslateInvocationExpression(InvocationExpression invocation, Dictionary<string, object?> parameters)
    {
        // Handle specification composition (And, Or, Not)
        return TranslateExpression(invocation.Expression, parameters);
    }

    private static string BuildLikeClause(string columnName, string pattern, string paramName, Dictionary<string, object?> parameters)
    {
        parameters[paramName] = pattern;
        return $"[{columnName}] LIKE @{paramName}";
    }

    private static string BuildEqualsClause(string columnName, object? value, string paramName, Dictionary<string, object?> parameters)
    {
        if (value is null)
        {
            return $"[{columnName}] IS NULL";
        }

        parameters[paramName] = value;
        return $"[{columnName}] = @{paramName}";
    }

    private string GetColumnName(Expression expression)
    {
        var propertyName = expression switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression unaryMember } => unaryMember.Member.Name,
            _ => throw new NotSupportedException($"Cannot extract property name from {expression.NodeType}.")
        };

        if (_columnMappings.TryGetValue(propertyName, out var columnName))
        {
            return columnName;
        }

        // Fall back to property name if no mapping exists
        return SqlIdentifierValidator.ValidateTableName(propertyName, "propertyName");
    }

    private static object? GetValue(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression member => GetMemberValue(member),
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand } => GetValue(operand),
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
}
