namespace Encina.DomainModeling.Pagination;

/// <summary>
/// Marker interface for queries that return cursor-paginated results.
/// </summary>
/// <typeparam name="TItem">The type of items in the paginated result.</typeparam>
/// <remarks>
/// <para>
/// This interface provides cursor pagination parameters for queries. Query types
/// implementing this interface indicate that they support keyset-based pagination
/// and should return <see cref="CursorPaginatedResult{T}"/>.
/// </para>
/// <para>
/// <b>Integration with Encina CQRS:</b> To use with Encina's CQRS infrastructure,
/// your query should implement both this interface and <c>IQuery&lt;CursorPaginatedResult&lt;TItem&gt;&gt;</c>:
/// </para>
/// <code>
/// public sealed record GetOrdersQuery(...)
///     : IQuery&lt;CursorPaginatedResult&lt;OrderDto&gt;&gt;, ICursorPaginatedQuery&lt;OrderDto&gt;;
/// </code>
/// <para>
/// <b>Cursor vs Offset Pagination:</b> Cursor-based pagination (also known as keyset pagination)
/// provides O(1) performance regardless of page position, unlike offset-based pagination which
/// degrades as the page number increases. Use cursor pagination for:
/// <list type="bullet">
/// <item><description>Infinite scroll or "load more" patterns</description></item>
/// <item><description>Large datasets where performance is critical</description></item>
/// <item><description>Real-time feeds where data changes frequently</description></item>
/// <item><description>APIs consumed by mobile clients with limited resources</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Implementation Pattern:</b> Query handlers should use the pagination parameters
/// to construct efficient keyset queries. The cursor encodes the position in the result
/// set based on the sort key values (e.g., <c>{ CreatedAt, Id }</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a cursor-paginated query
/// public sealed record GetOrdersQuery(
///     Guid CustomerId,
///     string? Cursor = null,
///     int PageSize = 20,
///     CursorDirection Direction = CursorDirection.Forward)
///     : IQuery&lt;CursorPaginatedResult&lt;OrderDto&gt;&gt;, ICursorPaginatedQuery&lt;OrderDto&gt;;
///
/// // Implement the query handler
/// public sealed class GetOrdersQueryHandler
///     : IQueryHandler&lt;GetOrdersQuery, CursorPaginatedResult&lt;OrderDto&gt;&gt;
/// {
///     private readonly AppDbContext _db;
///     private readonly ICursorEncoder _cursorEncoder;
///
///     public GetOrdersQueryHandler(AppDbContext db, ICursorEncoder cursorEncoder)
///     {
///         _db = db;
///         _cursorEncoder = cursorEncoder;
///     }
///
///     public async Task&lt;CursorPaginatedResult&lt;OrderDto&gt;&gt; Handle(
///         GetOrdersQuery query,
///         CancellationToken cancellationToken)
///     {
///         // Decode the cursor to get the keyset values
///         var cursorKey = _cursorEncoder.Decode&lt;OrderCursorKey&gt;(query.Cursor);
///
///         // Build the keyset query
///         var ordersQuery = _db.Orders
///             .Where(o => o.CustomerId == query.CustomerId)
///             .OrderByDescending(o => o.CreatedAt)
///             .ThenBy(o => o.Id);
///
///         // Apply keyset filter if cursor provided
///         if (cursorKey != null)
///         {
///             ordersQuery = ordersQuery.Where(o =>
///                 o.CreatedAt &lt; cursorKey.CreatedAt ||
///                 (o.CreatedAt == cursorKey.CreatedAt &amp;&amp; o.Id &gt; cursorKey.Id));
///         }
///
///         // Fetch one extra to check for next page
///         var items = await ordersQuery
///             .Take(query.PageSize + 1)
///             .Select(o => new OrderDto(o.Id, o.Total, o.CreatedAt))
///             .ToListAsync(cancellationToken);
///
///         var hasNextPage = items.Count > query.PageSize;
///         if (hasNextPage)
///         {
///             items.RemoveAt(items.Count - 1);
///         }
///
///         // Build result with cursors
///         return new CursorPaginatedResult&lt;OrderDto&gt;
///         {
///             Items = items,
///             HasNextPage = hasNextPage,
///             HasPreviousPage = query.Cursor != null,
///             NextCursor = items.Count > 0
///                 ? _cursorEncoder.Encode(new { items[^1].CreatedAt, items[^1].Id })
///                 : null,
///             PreviousCursor = items.Count > 0
///                 ? _cursorEncoder.Encode(new { items[0].CreatedAt, items[0].Id })
///                 : null
///         };
///     }
/// }
///
/// // Usage
/// var result = await encina.SendAsync(new GetOrdersQuery(customerId));
/// // Navigate to next page
/// var nextPage = await encina.SendAsync(new GetOrdersQuery(customerId, result.NextCursor));
/// </code>
/// </example>
/// <seealso cref="CursorPaginatedResult{T}"/>
/// <seealso cref="CursorPaginationOptions"/>
/// <seealso cref="ICursorEncoder"/>
public interface ICursorPaginatedQuery<TItem>
{
    /// <summary>
    /// Gets the opaque cursor string indicating the position to paginate from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>null</c> or empty, the query starts from the beginning of the result set
    /// (or end, if <see cref="Direction"/> is <see cref="CursorDirection.Backward"/>).
    /// </para>
    /// <para>
    /// The cursor is an opaque string that should not be parsed or constructed by clients.
    /// It encodes the position in the result set using the sort key values.
    /// </para>
    /// </remarks>
    string? Cursor { get; }

    /// <summary>
    /// Gets the number of items to return per page.
    /// </summary>
    /// <remarks>
    /// This value should be between 1 and <see cref="CursorPaginationOptions.MaxPageSize"/> (100).
    /// The default value is typically 20.
    /// </remarks>
    int PageSize { get; }

    /// <summary>
    /// Gets the direction of pagination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CursorDirection.Forward"/>: Fetch items after the cursor position.
    /// This is the default direction for "next page" navigation.
    /// </para>
    /// <para>
    /// <see cref="CursorDirection.Backward"/>: Fetch items before the cursor position.
    /// Used for "previous page" navigation.
    /// </para>
    /// </remarks>
    CursorDirection Direction { get; }
}

/// <summary>
/// Extension methods for <see cref="ICursorPaginatedQuery{TItem}"/>.
/// </summary>
public static class CursorPaginatedQueryExtensions
{
    /// <summary>
    /// Creates <see cref="CursorPaginationOptions"/> from a cursor-paginated query.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the query result.</typeparam>
    /// <param name="query">The cursor-paginated query.</param>
    /// <returns>
    /// A <see cref="CursorPaginationOptions"/> instance with the query's pagination parameters.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="query"/> is null.
    /// </exception>
    /// <remarks>
    /// This helper method simplifies passing pagination parameters to repository methods
    /// or other services that accept <see cref="CursorPaginationOptions"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// public async Task&lt;CursorPaginatedResult&lt;OrderDto&gt;&gt; Handle(
    ///     GetOrdersQuery query,
    ///     CancellationToken cancellationToken)
    /// {
    ///     var options = query.ToPaginationOptions();
    ///     return await _orderRepository.GetPagedAsync(options, cancellationToken);
    /// }
    /// </code>
    /// </example>
    public static CursorPaginationOptions ToPaginationOptions<TItem>(
        this ICursorPaginatedQuery<TItem> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return new CursorPaginationOptions(
            Cursor: query.Cursor,
            PageSize: query.PageSize,
            Direction: query.Direction);
    }

    /// <summary>
    /// Determines whether this query represents the first page (no cursor specified).
    /// </summary>
    /// <typeparam name="TItem">The type of items in the query result.</typeparam>
    /// <param name="query">The cursor-paginated query.</param>
    /// <returns>
    /// <c>true</c> if the cursor is null or empty; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="query"/> is null.
    /// </exception>
    public static bool IsFirstPage<TItem>(this ICursorPaginatedQuery<TItem> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return string.IsNullOrEmpty(query.Cursor);
    }
}
