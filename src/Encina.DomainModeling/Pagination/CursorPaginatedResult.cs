namespace Encina.DomainModeling.Pagination;

/// <summary>
/// Represents the result of a cursor-paginated query with a flat list of items.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
/// <remarks>
/// <para>
/// This is the REST-friendly projection of cursor pagination results. It provides
/// a flat list of items with cursor values for navigation, optimized for REST API responses.
/// </para>
/// <para>
/// For GraphQL APIs requiring the Relay Connection specification (with Edge types),
/// use the <c>Encina.HotChocolate</c> package which provides <c>Connection&lt;T&gt;</c>
/// with per-item cursors.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query handler returning cursor-paginated result
/// public async Task&lt;CursorPaginatedResult&lt;OrderDto&gt;&gt; Handle(GetOrdersQuery query)
/// {
///     return await _dbContext.Orders
///         .Where(o => o.CustomerId == query.CustomerId)
///         .OrderByDescending(o => o.CreatedAt)
///         .ThenBy(o => o.Id)
///         .ToCursorPaginatedAsync(
///             query.Cursor,
///             query.PageSize,
///             o => new { o.CreatedAt, o.Id });
/// }
///
/// // API response
/// // {
/// //   "items": [...],
/// //   "nextCursor": "eyJjcmVhdGVkQXQiOiIyMDI1LTEyLTI3IiwiaWQiOiIxMjM0In0=",
/// //   "previousCursor": null,
/// //   "hasNextPage": true,
/// //   "hasPreviousPage": false
/// // }
/// </code>
/// </example>
public sealed record CursorPaginatedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Gets the cursor for navigating to the next page.
    /// </summary>
    /// <remarks>
    /// This is the cursor of the last item in the current page.
    /// Pass this value as the cursor parameter to fetch the next page.
    /// Null if there are no items or this is the last page.
    /// </remarks>
    public string? NextCursor { get; init; }

    /// <summary>
    /// Gets the cursor for navigating to the previous page.
    /// </summary>
    /// <remarks>
    /// This is the cursor of the first item in the current page.
    /// Pass this value as the cursor parameter with backward direction to fetch the previous page.
    /// Null if there are no items or this is the first page.
    /// </remarks>
    public string? PreviousCursor { get; init; }

    /// <summary>
    /// Gets whether there is a next page of results.
    /// </summary>
    public bool HasNextPage { get; init; }

    /// <summary>
    /// Gets whether there is a previous page of results.
    /// </summary>
    public bool HasPreviousPage { get; init; }

    /// <summary>
    /// Gets the optional total count of items across all pages.
    /// </summary>
    /// <remarks>
    /// This property is optional and may be null when total count calculation
    /// is disabled for performance reasons. Computing total count requires
    /// an additional database query.
    /// </remarks>
    public int? TotalCount { get; init; }

    /// <summary>
    /// Gets whether the result is empty.
    /// </summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>
    /// Creates an empty cursor-paginated result.
    /// </summary>
    /// <param name="pageSize">The page size for metadata. Defaults to 20.</param>
    /// <returns>An empty <see cref="CursorPaginatedResult{T}"/> instance.</returns>
    /// <example>
    /// <code>
    /// var emptyResult = CursorPaginatedResult&lt;Order&gt;.Empty();
    /// </code>
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Factory method pattern - provides convenient typed empty result creation")]
    public static CursorPaginatedResult<T> Empty(int pageSize = 20) => new()
    {
        Items = [],
        NextCursor = null,
        PreviousCursor = null,
        HasNextPage = false,
        HasPreviousPage = false,
        TotalCount = 0
    };

    /// <summary>
    /// Maps the items to a new type using the specified selector function.
    /// </summary>
    /// <typeparam name="TResult">The target type to map to.</typeparam>
    /// <param name="selector">The mapping function to apply to each item.</param>
    /// <returns>A new <see cref="CursorPaginatedResult{TResult}"/> with mapped items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
    /// <example>
    /// <code>
    /// var dtoResult = result.Map(order => new OrderDto(order.Id, order.Total));
    /// </code>
    /// </example>
    public CursorPaginatedResult<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new CursorPaginatedResult<TResult>
        {
            Items = Items.Select(selector).ToList(),
            NextCursor = NextCursor,
            PreviousCursor = PreviousCursor,
            HasNextPage = HasNextPage,
            HasPreviousPage = HasPreviousPage,
            TotalCount = TotalCount
        };
    }

    /// <summary>
    /// Creates a <see cref="CursorPaginatedResult{T}"/> from internal paged data.
    /// </summary>
    /// <param name="data">The internal cursor-paged data containing items with individual cursors.</param>
    /// <returns>A REST-friendly result with flat items list and navigation cursors.</returns>
    /// <remarks>
    /// This factory method projects the internal representation (which has cursor per item)
    /// to the REST-friendly format (flat list with start/end cursors only).
    /// </remarks>
    internal static CursorPaginatedResult<T> FromPagedData(CursorPagedData<T> data) => new()
    {
        Items = data.Items.Select(x => x.Item).ToList(),
        NextCursor = data.PageInfo.HasNextPage ? data.PageInfo.EndCursor : null,
        PreviousCursor = data.PageInfo.HasPreviousPage ? data.PageInfo.StartCursor : null,
        HasNextPage = data.PageInfo.HasNextPage,
        HasPreviousPage = data.PageInfo.HasPreviousPage,
        TotalCount = data.TotalCount
    };
}
