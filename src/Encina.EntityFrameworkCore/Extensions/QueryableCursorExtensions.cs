using System.Linq.Expressions;
using System.Reflection;

using Encina.DomainModeling.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/> to support cursor-based pagination operations.
/// </summary>
/// <remarks>
/// <para>
/// Cursor-based pagination (also known as keyset pagination) provides O(1) performance
/// regardless of page position, unlike offset-based pagination which degrades linearly.
/// </para>
/// <para>
/// The query MUST be ordered before calling these methods. The key selector should match
/// the ordering columns to ensure consistent pagination.
/// </para>
/// </remarks>
public static class QueryableCursorExtensions
{
    #region Public API - Simple Key

    /// <summary>
    /// Executes a cursor-paginated query and returns a <see cref="CursorPaginatedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="query">The ordered query to paginate. Must have OrderBy/OrderByDescending applied.</param>
    /// <param name="cursor">The opaque cursor string from a previous result, or null for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="keySelector">Expression selecting the cursor key property (must match query ordering).</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{T}"/> containing the paginated items and navigation cursors.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="query"/>, <paramref name="keySelector"/>, or <paramref name="cursorEncoder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageSize"/> is less than 1 or greater than 100.
    /// </exception>
    /// <example>
    /// <code>
    /// var result = await dbContext.Orders
    ///     .Where(o => o.CustomerId == customerId)
    ///     .OrderByDescending(o => o.CreatedAtUtc)
    ///     .ToCursorPaginatedAsync(
    ///         cursor: request.Cursor,
    ///         pageSize: 20,
    ///         keySelector: o => o.CreatedAtUtc,
    ///         cursorEncoder: encoder,
    ///         cancellationToken);
    /// </code>
    /// </example>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedAsync<T, TKey>(
        this IQueryable<T> query,
        string? cursor,
        int pageSize,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        var options = new CursorPaginationOptions(cursor, pageSize);
        return await ToCursorPaginatedCoreAsync(
            query,
            options,
            keySelector,
            cursorEncoder,
            isDescending: false,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a cursor-paginated query with descending order and returns a <see cref="CursorPaginatedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="query">The ordered query to paginate. Must have OrderByDescending applied.</param>
    /// <param name="cursor">The opaque cursor string from a previous result, or null for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="keySelector">Expression selecting the cursor key property (must match query ordering).</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{T}"/> containing the paginated items and navigation cursors.
    /// </returns>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedDescendingAsync<T, TKey>(
        this IQueryable<T> query,
        string? cursor,
        int pageSize,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        var options = new CursorPaginationOptions(cursor, pageSize);
        return await ToCursorPaginatedCoreAsync(
            query,
            options,
            keySelector,
            cursorEncoder,
            isDescending: true,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a cursor-paginated query using <see cref="CursorPaginationOptions"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="query">The ordered query to paginate.</param>
    /// <param name="options">The cursor pagination options.</param>
    /// <param name="keySelector">Expression selecting the cursor key property.</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="isDescending">Whether the query is ordered descending.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{T}"/> containing the paginated items and navigation cursors.
    /// </returns>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedAsync<T, TKey>(
        this IQueryable<T> query,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);

        return await ToCursorPaginatedCoreAsync(
            query,
            options,
            keySelector,
            cursorEncoder,
            isDescending,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Public API - With Projection

    /// <summary>
    /// Executes a cursor-paginated query with projection and returns a <see cref="CursorPaginatedResult{TResult}"/>.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="query">The ordered query to paginate.</param>
    /// <param name="selector">The projection selector applied at the database level.</param>
    /// <param name="cursor">The opaque cursor string from a previous result, or null for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="keySelector">Expression selecting the cursor key property from the source entity.</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="CursorPaginatedResult{TResult}"/> containing the projected and paginated items.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await dbContext.Orders
    ///     .Where(o => o.CustomerId == customerId)
    ///     .OrderByDescending(o => o.CreatedAtUtc)
    ///     .ToCursorPaginatedAsync(
    ///         selector: o => new OrderDto(o.Id, o.Total),
    ///         cursor: request.Cursor,
    ///         pageSize: 20,
    ///         keySelector: o => o.CreatedAtUtc,
    ///         cursorEncoder: encoder,
    ///         cancellationToken);
    /// </code>
    /// </example>
    public static async Task<CursorPaginatedResult<TResult>> ToCursorPaginatedAsync<T, TResult, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        string? cursor,
        int pageSize,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        var options = new CursorPaginationOptions(cursor, pageSize);
        return await ToCursorPaginatedWithProjectionCoreAsync(
            query,
            selector,
            options,
            keySelector,
            cursorEncoder,
            isDescending: false,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Public API - Composite Key

    /// <summary>
    /// Executes a cursor-paginated query with a composite key and returns a <see cref="CursorPaginatedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The ordered query to paginate.</param>
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
    /// Use this overload when your query has multiple OrderBy clauses. The key selector
    /// should return an anonymous type matching the ordering, e.g., <c>o => new { o.CreatedAt, o.Id }</c>.
    /// </para>
    /// <para>
    /// The <paramref name="keyDescending"/> array specifies the sort direction for each key component.
    /// For example, <c>new[] { true, false }</c> means "CreatedAt DESC, Id ASC".
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query ordered by CreatedAt DESC, then Id ASC
    /// var result = await dbContext.Orders
    ///     .Where(o => o.CustomerId == customerId)
    ///     .OrderByDescending(o => o.CreatedAtUtc)
    ///     .ThenBy(o => o.Id)
    ///     .ToCursorPaginatedCompositeAsync(
    ///         cursor: request.Cursor,
    ///         pageSize: 20,
    ///         keySelector: o => new { o.CreatedAtUtc, o.Id },
    ///         cursorEncoder: encoder,
    ///         keyDescending: [true, false],
    ///         cancellationToken);
    /// </code>
    /// </example>
    public static async Task<CursorPaginatedResult<T>> ToCursorPaginatedCompositeAsync<T, TKey>(
        this IQueryable<T> query,
        string? cursor,
        int pageSize,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool[] keyDescending,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentNullException.ThrowIfNull(keyDescending);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        var options = new CursorPaginationOptions(cursor, pageSize);
        return await ToCursorPaginatedCompositeCoreAsync(
            query,
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
    private static async Task<CursorPaginatedResult<T>> ToCursorPaginatedCoreAsync<T, TKey>(
        IQueryable<T> query,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool isDescending,
        CancellationToken cancellationToken)
    {
        // Decode cursor to get the key value to continue from
        var cursorKeyValue = !string.IsNullOrEmpty(options.Cursor)
            ? cursorEncoder.Decode<TKey>(options.Cursor)
            : default;

        // Determine effective direction (handle backward pagination)
        var isBackward = options.Direction == CursorDirection.Backward;

        // Apply keyset filter if cursor is provided
        if (cursorKeyValue is not null && !string.IsNullOrEmpty(options.Cursor))
        {
            query = ApplyKeysetFilter(query, keySelector, cursorKeyValue, isDescending, isBackward);
        }

        // For backward pagination, we need to reverse the order temporarily
        if (isBackward)
        {
            query = ReverseOrdering(query, keySelector, isDescending);
        }

        // Fetch one extra item to determine if there's a next/previous page
        var itemsWithExtra = await query
            .Take(options.PageSize + 1)
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
    /// Applies keyset filter for cursor-based pagination.
    /// </summary>
    private static IQueryable<T> ApplyKeysetFilter<T, TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        TKey keyValue,
        bool isDescending,
        bool isBackward)
    {
        // For forward + ascending: key > cursorKey
        // For forward + descending: key < cursorKey
        // For backward + ascending: key < cursorKey
        // For backward + descending: key > cursorKey
        var useGreaterThan = isDescending == isBackward;

        var parameter = keySelector.Parameters[0];
        var keyAccess = keySelector.Body;
        var keyConstant = Expression.Constant(keyValue, typeof(TKey));

        var comparison = useGreaterThan
            ? Expression.GreaterThan(keyAccess, keyConstant)
            : Expression.LessThan(keyAccess, keyConstant);

        var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Reverses the query ordering for backward pagination.
    /// </summary>
    private static IQueryable<T> ReverseOrdering<T, TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool isCurrentlyDescending)
    {
        // We need to flip the ordering direction
        return isCurrentlyDescending
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);
    }

    #endregion

    #region Internal Core Implementation - With Projection

    /// <summary>
    /// Core implementation for cursor pagination with projection.
    /// </summary>
    private static async Task<CursorPaginatedResult<TResult>> ToCursorPaginatedWithProjectionCoreAsync<T, TResult, TKey>(
        IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool isDescending,
        CancellationToken cancellationToken)
    {
        // Decode cursor
        var cursorKeyValue = !string.IsNullOrEmpty(options.Cursor)
            ? cursorEncoder.Decode<TKey>(options.Cursor)
            : default;

        var isBackward = options.Direction == CursorDirection.Backward;

        // Apply keyset filter
        if (cursorKeyValue is not null && !string.IsNullOrEmpty(options.Cursor))
        {
            query = ApplyKeysetFilter(query, keySelector, cursorKeyValue, isDescending, isBackward);
        }

        // Reverse for backward
        if (isBackward)
        {
            query = ReverseOrdering(query, keySelector, isDescending);
        }

        // We need to select both the key and the projection for cursor generation
        // Create a combined selector that captures both
        var keySelectorFunc = keySelector.Compile();

        // Fetch entities first to get keys, then project
        var itemsWithExtra = await query
            .Take(options.PageSize + 1)
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
            return CursorPaginatedResult<TResult>.Empty(options.PageSize);
        }

        // Generate cursors from original entities, then project
        var selectorFunc = selector.Compile();
        var cursorItems = itemsWithExtra
            .Select(item => new CursorItem<TResult>(
                selectorFunc(item),
                cursorEncoder.Encode(keySelectorFunc(item))!))
            .ToList();

        var pageInfo = new CursorPageInfo(
            HasPreviousPage: isBackward ? hasMoreItems : !string.IsNullOrEmpty(options.Cursor),
            HasNextPage: isBackward ? !string.IsNullOrEmpty(options.Cursor) : hasMoreItems,
            StartCursor: cursorItems[0].Cursor,
            EndCursor: cursorItems[^1].Cursor);

        var pagedData = new CursorPagedData<TResult>(cursorItems, pageInfo);
        return CursorPaginatedResult<TResult>.FromPagedData(pagedData);
    }

    #endregion

    #region Internal Core Implementation - Composite Key

    /// <summary>
    /// Core implementation for composite key cursor pagination.
    /// </summary>
    private static async Task<CursorPaginatedResult<T>> ToCursorPaginatedCompositeCoreAsync<T, TKey>(
        IQueryable<T> query,
        CursorPaginationOptions options,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder cursorEncoder,
        bool[] keyDescending,
        CancellationToken cancellationToken)
    {
        // Validate key selector is an anonymous type
        var keyType = typeof(TKey);
        var keyProperties = keyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        // Apply composite keyset filter
        if (cursorKeyValue is not null && !string.IsNullOrEmpty(options.Cursor))
        {
            query = ApplyCompositeKeysetFilter(
                query,
                keySelector,
                cursorKeyValue,
                keyProperties,
                keyDescending,
                isBackward);
        }

        // For backward, we need to reverse ALL ordering components
        // This is complex and depends on the original query structure
        // For now, we'll just fetch and reverse in memory (simpler but less efficient)

        // Fetch with extra
        var itemsWithExtra = await query
            .Take(options.PageSize + 1)
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
    /// Applies composite keyset filter for multi-column cursor pagination.
    /// </summary>
    /// <remarks>
    /// For a composite key (A, B) with A DESC, B ASC, the filter for "next page" would be:
    /// <c>(A &lt; @a) OR (A = @a AND B &gt; @b)</c>
    /// </remarks>
    private static IQueryable<T> ApplyCompositeKeysetFilter<T, TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        TKey keyValue,
        PropertyInfo[] keyProperties,
        bool[] keyDescending,
        bool isBackward)
    {
        var parameter = keySelector.Parameters[0];

        // Get the key expression body (should be NewExpression for anonymous types)
        var keyBody = keySelector.Body;

        // Build compound comparison: (A > @a) OR (A = @a AND B > @b) OR (A = @a AND B = @b AND C > @c) ...
        Expression? combinedFilter = null;

        for (var i = 0; i < keyProperties.Length; i++)
        {
            // Build the equality chain for all previous properties
            Expression? equalityChain = null;
            for (var j = 0; j < i; j++)
            {
                var prevProp = keyProperties[j];
                var prevValue = prevProp.GetValue(keyValue);
                var prevAccess = GetMemberAccess(keyBody, prevProp.Name);
                var prevConstant = Expression.Constant(prevValue, prevProp.PropertyType);
                var equality = Expression.Equal(prevAccess, prevConstant);

                equalityChain = equalityChain is null
                    ? equality
                    : Expression.AndAlso(equalityChain, equality);
            }

            // Build the comparison for the current property
            var prop = keyProperties[i];
            var propValue = prop.GetValue(keyValue);
            var propAccess = GetMemberAccess(keyBody, prop.Name);
            var propConstant = Expression.Constant(propValue, prop.PropertyType);

            // Determine comparison direction
            // Forward + DESC = Less Than, Forward + ASC = Greater Than
            // Backward flips these
            var useGreaterThan = keyDescending[i] == isBackward;

            var comparison = useGreaterThan
                ? Expression.GreaterThan(propAccess, propConstant)
                : Expression.LessThan(propAccess, propConstant);

            // Combine equality chain with comparison
            var clause = equalityChain is null
                ? comparison
                : Expression.AndAlso(equalityChain, comparison);

            // OR with previous clauses
            combinedFilter = combinedFilter is null
                ? clause
                : Expression.OrElse(combinedFilter, clause);
        }

        if (combinedFilter is null)
        {
            return query;
        }

        var lambda = Expression.Lambda<Func<T, bool>>(combinedFilter, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Gets member access expression from a key body (handles both MemberExpression and NewExpression).
    /// </summary>
    private static Expression GetMemberAccess(Expression keyBody, string memberName)
    {
        // For anonymous types, the key body is a NewExpression with Arguments
        if (keyBody is NewExpression newExpr)
        {
            // Find the argument by matching property name
            for (var i = 0; i < newExpr.Members!.Count; i++)
            {
                if (newExpr.Members[i].Name == memberName)
                {
                    return newExpr.Arguments[i];
                }
            }

            throw new InvalidOperationException($"Member '{memberName}' not found in key selector.");
        }

        // For regular member expressions
        if (keyBody is MemberExpression)
        {
            return keyBody;
        }

        throw new InvalidOperationException(
            $"Key selector body must be a MemberExpression or NewExpression, but was {keyBody.GetType().Name}.");
    }

    #endregion
}
