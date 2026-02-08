using System.Linq.Expressions;

using Encina.DomainModeling.Pagination;
using Encina.MongoDB.Repository;

using MongoDB.Driver;

namespace Encina.MongoDB.Extensions;

/// <summary>
/// Extension methods for <see cref="IMongoCollection{T}"/> to support cursor-based pagination operations.
/// </summary>
/// <remarks>
/// <para>
/// Cursor-based pagination (also known as keyset pagination) provides O(1) performance
/// regardless of page position, unlike offset-based pagination which degrades linearly.
/// </para>
/// <para>
/// These methods leverage MongoDB's native filter and sort definitions for efficient keyset pagination.
/// The key selector should match the sorting columns to ensure consistent pagination.
/// </para>
/// </remarks>
public static class CursorPaginationExtensions
{
    #region Public API - Simple Key

    /// <summary>
    /// Executes a cursor-paginated query and returns a <see cref="CursorPaginatedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="collection">The MongoDB collection to query.</param>
    /// <param name="filter">The filter to apply to the query.</param>
    /// <param name="cursor">The opaque cursor string from a previous result, or null for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="keySelector">Expression selecting the cursor key property (must match query ordering).</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="isDescending">Whether the sort order is descending. Defaults to false.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{T}"/> containing the paginated items and navigation cursors.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection"/>, <paramref name="keySelector"/>, or <paramref name="cursorEncoder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageSize"/> is less than 1 or greater than 100.
    /// </exception>
    /// <example>
    /// <code>
    /// var result = await collection.ToCursorPaginatedAsync(
    ///     filter: Builders&lt;Order&gt;.Filter.Eq(o => o.CustomerId, customerId),
    ///     cursor: request.Cursor,
    ///     pageSize: 20,
    ///     keySelector: o => o.CreatedAtUtc,
    ///     cursorEncoder: encoder,
    ///     isDescending: true,
    ///     cancellationToken);
    /// </code>
    /// </example>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedAsync<T, TKey>(
        this IMongoCollection<T> collection,
        FilterDefinition<T> filter,
        string? cursor,
        int pageSize,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        var options = new CursorPaginationOptions(cursor, pageSize);
        return await ToCursorPaginatedCoreAsync(
            collection,
            filter,
            options,
            keySelector,
            cursorEncoder,
            isDescending,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a cursor-paginated query using <see cref="CursorPaginationOptions"/>.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="collection">The MongoDB collection to query.</param>
    /// <param name="filter">The filter to apply to the query.</param>
    /// <param name="options">The cursor pagination options.</param>
    /// <param name="keySelector">Expression selecting the cursor key property.</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="isDescending">Whether the query is ordered descending.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{T}"/> containing the paginated items and navigation cursors.
    /// </returns>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedAsync<T, TKey>(
        this IMongoCollection<T> collection,
        FilterDefinition<T> filter,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);

        return await ToCursorPaginatedCoreAsync(
            collection,
            filter,
            options,
            keySelector,
            cursorEncoder,
            isDescending,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Public API - Composite Key

    /// <summary>
    /// Executes a cursor-paginated query with a composite key and returns a <see cref="CursorPaginatedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <typeparam name="TKey">The type of the composite cursor key (anonymous type).</typeparam>
    /// <param name="collection">The MongoDB collection to query.</param>
    /// <param name="filter">The filter to apply to the query.</param>
    /// <param name="cursor">The opaque cursor string from a previous result, or null for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="keySelector">Expression selecting the composite cursor key (anonymous type).</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="keyDescending">Array indicating which key components are descending. Must match key property count.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{T}"/> containing the paginated items and navigation cursors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this overload when your query has multiple sort columns. The key selector
    /// should return an anonymous type matching the sorting, e.g., <c>o => new { o.CreatedAt, o.Id }</c>.
    /// </para>
    /// <para>
    /// The <paramref name="keyDescending"/> array specifies the sort direction for each key component.
    /// For example, <c>new[] { true, false }</c> means "CreatedAt DESC, Id ASC".
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await collection.ToCursorPaginatedCompositeAsync(
    ///     filter: Builders&lt;Order&gt;.Filter.Eq(o => o.CustomerId, customerId),
    ///     cursor: request.Cursor,
    ///     pageSize: 20,
    ///     keySelector: o => new { o.CreatedAtUtc, o.Id },
    ///     cursorEncoder: encoder,
    ///     keyDescending: [true, false],
    ///     cancellationToken);
    /// </code>
    /// </example>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedCompositeAsync<T, TKey>(
        this IMongoCollection<T> collection,
        FilterDefinition<T> filter,
        string? cursor,
        int pageSize,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool[] keyDescending,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentNullException.ThrowIfNull(keyDescending);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        var options = new CursorPaginationOptions(cursor, pageSize);
        return await ToCursorPaginatedCompositeCoreAsync(
            collection,
            filter,
            options,
            keySelector,
            cursorEncoder,
            keyDescending,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a cursor-paginated query with a composite key using <see cref="CursorPaginationOptions"/>.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <typeparam name="TKey">The type of the composite cursor key (anonymous type).</typeparam>
    /// <param name="collection">The MongoDB collection to query.</param>
    /// <param name="filter">The filter to apply to the query.</param>
    /// <param name="options">The cursor pagination options.</param>
    /// <param name="keySelector">Expression selecting the composite cursor key (anonymous type).</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="keyDescending">Array indicating which key components are descending.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{T}"/> containing the paginated items and navigation cursors.
    /// </returns>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedCompositeAsync<T, TKey>(
        this IMongoCollection<T> collection,
        FilterDefinition<T> filter,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool[] keyDescending,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentNullException.ThrowIfNull(keyDescending);

        return await ToCursorPaginatedCompositeCoreAsync(
            collection,
            filter,
            options,
            keySelector,
            cursorEncoder,
            keyDescending,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Internal Core Implementation - Simple Key

    /// <summary>
    /// Core implementation for simple key cursor pagination.
    /// </summary>
    internal static async Task<CursorPaginatedResult<T>> ToCursorPaginatedCoreAsync<T, TKey>(
        IMongoCollection<T> collection,
        FilterDefinition<T> baseFilter,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool isDescending,
        CancellationToken cancellationToken)
        where T : class
    {
        var filterBuilder = new SpecificationFilterBuilder<T>();
        var keyExpression = ConvertToObjectExpression(keySelector);

        // Decode cursor to get the key value to continue from
        var cursorKeyValue = !string.IsNullOrEmpty(options.Cursor)
            ? cursorEncoder.Decode<TKey>(options.Cursor)
            : default;

        var isBackward = options.Direction == CursorDirection.Backward;

        // Combine base filter with keyset filter if cursor is provided
        var combinedFilter = baseFilter;
        if (cursorKeyValue is not null && !string.IsNullOrEmpty(options.Cursor))
        {
            var keysetFilter = filterBuilder.BuildKeysetFilter(
                keyExpression,
                cursorKeyValue,
                isDescending,
                options.Direction);

            combinedFilter = Builders<T>.Filter.And(baseFilter, keysetFilter);
        }

        // Build sort definition based on direction
        var sortDefinition = BuildSortDefinition<T, TKey>(keySelector, isDescending, isBackward);

        // Fetch one extra item to determine if there's a next/previous page
        var itemsWithExtra = await collection
            .Find(combinedFilter)
            .Sort(sortDefinition)
            .Limit(options.PageSize + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Determine if we have more items
        var hasMoreItems = itemsWithExtra.Count > options.PageSize;
        if (hasMoreItems)
        {
            itemsWithExtra.RemoveAt(itemsWithExtra.Count - 1);
        }

        // For backward pagination, reverse the results back to original order
        if (isBackward)
        {
            itemsWithExtra.Reverse();
        }

        // If no items, return empty result
        if (itemsWithExtra.Count == 0)
        {
            return CursorPaginatedResult<T>.Empty(options.PageSize);
        }

        // Compile the key selector for generating cursors
        var keySelectorFunc = keySelector.Compile();

        // Build cursor items with individual cursors
        var cursorItems = itemsWithExtra
            .Select(item => new CursorItem<T>(
                item,
                cursorEncoder.Encode(keySelectorFunc(item))!))
            .ToList();

        // Build page info
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: isBackward ? hasMoreItems : !string.IsNullOrEmpty(options.Cursor),
            HasNextPage: isBackward ? !string.IsNullOrEmpty(options.Cursor) : hasMoreItems,
            StartCursor: cursorItems[0].Cursor,
            EndCursor: cursorItems[^1].Cursor);

        // Create internal paged data and project to public result
        var pagedData = new CursorPagedData<T>(cursorItems, pageInfo);
        return CursorPaginatedResult<T>.FromPagedData(pagedData);
    }

    /// <summary>
    /// Builds a MongoDB sort definition based on key selector and direction.
    /// </summary>
    private static SortDefinition<T> BuildSortDefinition<T, TKey>(
        Expression<Func<T, TKey>> keySelector,
        bool isDescending,
        bool isBackward)
    {
        var fieldName = GetFieldName(keySelector);

        // For backward pagination, reverse the sort order
        var effectiveDescending = isBackward ? !isDescending : isDescending;

        return effectiveDescending
            ? Builders<T>.Sort.Descending(fieldName)
            : Builders<T>.Sort.Ascending(fieldName);
    }

    #endregion

    #region Internal Core Implementation - Composite Key

    /// <summary>
    /// Core implementation for composite key cursor pagination.
    /// </summary>
    internal static async Task<CursorPaginatedResult<T>> ToCursorPaginatedCompositeCoreAsync<T, TKey>(
        IMongoCollection<T> collection,
        FilterDefinition<T> baseFilter,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool[] keyDescending,
        CancellationToken cancellationToken)
        where T : class
    {
        var filterBuilder = new SpecificationFilterBuilder<T>();

        // Validate key selector is an anonymous type
        var keyType = typeof(TKey);
        var keyProperties = keyType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (keyDescending.Length != keyProperties.Length)
        {
            throw new ArgumentException(
                $"keyDescending array length ({keyDescending.Length}) must match the number of key properties ({keyProperties.Length}).",
                nameof(keyDescending));
        }

        // Decode cursor
        var cursorKeyValue = !string.IsNullOrEmpty(options.Cursor)
            ? cursorEncoder.Decode<TKey>(options.Cursor)
            : default;

        var isBackward = options.Direction == CursorDirection.Backward;

        // Combine base filter with keyset filter if cursor is provided
        var combinedFilter = baseFilter;
        if (cursorKeyValue is not null && !string.IsNullOrEmpty(options.Cursor))
        {
            var keyColumns = new List<(string FieldName, object Value, bool IsDescending)>();

            for (var i = 0; i < keyProperties.Length; i++)
            {
                var fieldName = GetFieldNameFromAnonymousType(keySelector, keyProperties[i].Name);
                var value = keyProperties[i].GetValue(cursorKeyValue)!;
                keyColumns.Add((fieldName, value, keyDescending[i]));
            }

            var keysetFilter = filterBuilder.BuildCompoundKeysetFilter(keyColumns, options.Direction);
            combinedFilter = Builders<T>.Filter.And(baseFilter, keysetFilter);
        }

        // Build composite sort definition
        var sortDefinition = BuildCompositeSortDefinition<T, TKey>(
            keySelector,
            keyProperties,
            keyDescending,
            isBackward);

        // Fetch with extra
        var itemsWithExtra = await collection
            .Find(combinedFilter)
            .Sort(sortDefinition)
            .Limit(options.PageSize + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasMoreItems = itemsWithExtra.Count > options.PageSize;
        if (hasMoreItems)
        {
            itemsWithExtra.RemoveAt(itemsWithExtra.Count - 1);
        }

        if (isBackward)
        {
            itemsWithExtra.Reverse();
        }

        if (itemsWithExtra.Count == 0)
        {
            return CursorPaginatedResult<T>.Empty(options.PageSize);
        }

        var keySelectorFunc = keySelector.Compile();
        var cursorItems = itemsWithExtra
            .Select(item => new CursorItem<T>(
                item,
                cursorEncoder.Encode(keySelectorFunc(item))!))
            .ToList();

        var pageInfo = new CursorPageInfo(
            HasPreviousPage: isBackward ? hasMoreItems : !string.IsNullOrEmpty(options.Cursor),
            HasNextPage: isBackward ? !string.IsNullOrEmpty(options.Cursor) : hasMoreItems,
            StartCursor: cursorItems[0].Cursor,
            EndCursor: cursorItems[^1].Cursor);

        var pagedData = new CursorPagedData<T>(cursorItems, pageInfo);
        return CursorPaginatedResult<T>.FromPagedData(pagedData);
    }

    /// <summary>
    /// Builds a composite MongoDB sort definition.
    /// </summary>
    private static SortDefinition<T> BuildCompositeSortDefinition<T, TKey>(
        Expression<Func<T, TKey>> keySelector,
        System.Reflection.PropertyInfo[] keyProperties,
        bool[] keyDescending,
        bool isBackward)
    {
        SortDefinition<T>? sort = null;

        for (var i = 0; i < keyProperties.Length; i++)
        {
            var fieldName = GetFieldNameFromAnonymousType(keySelector, keyProperties[i].Name);

            // For backward pagination, reverse all sort directions
            var effectiveDescending = isBackward ? !keyDescending[i] : keyDescending[i];

            var currentSort = effectiveDescending
                ? Builders<T>.Sort.Descending(fieldName)
                : Builders<T>.Sort.Ascending(fieldName);

            sort = sort is null
                ? currentSort
                : Builders<T>.Sort.Combine(sort, currentSort);
        }

        return sort!;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Converts a typed key selector to an object-returning expression for SpecificationFilterBuilder.
    /// </summary>
    private static Expression<Func<T, object>> ConvertToObjectExpression<T, TKey>(
        Expression<Func<T, TKey>> keySelector)
    {
        var parameter = keySelector.Parameters[0];
        var body = Expression.Convert(keySelector.Body, typeof(object));
        return Expression.Lambda<Func<T, object>>(body, parameter);
    }

    /// <summary>
    /// Gets the field name from a simple member expression.
    /// </summary>
    private static string GetFieldName<T, TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var body = keySelector.Body;

        // Unwrap Convert expression if present
        if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand })
        {
            body = operand;
        }

        if (body is MemberExpression member)
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

        throw new NotSupportedException(
            $"Expression type '{body.NodeType}' is not supported for field name extraction.");
    }

    /// <summary>
    /// Gets the field name from an anonymous type key selector for a specific property.
    /// </summary>
    private static string GetFieldNameFromAnonymousType<T, TKey>(
        Expression<Func<T, TKey>> keySelector,
        string propertyName)
    {
        if (keySelector.Body is NewExpression newExpr)
        {
            for (var i = 0; i < newExpr.Members!.Count; i++)
            {
                if (newExpr.Members[i].Name == propertyName)
                {
                    var argument = newExpr.Arguments[i];

                    // Unwrap Convert if present
                    if (argument is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand })
                    {
                        argument = operand;
                    }

                    if (argument is MemberExpression member)
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

                    throw new NotSupportedException(
                        $"Argument type '{argument.NodeType}' is not supported for field name extraction.");
                }
            }

            throw new InvalidOperationException($"Property '{propertyName}' not found in key selector.");
        }

        throw new NotSupportedException(
            $"Key selector must be a NewExpression for composite keys, but was {keySelector.Body.GetType().Name}.");
    }

    #endregion
}
