namespace Encina.DomainModeling.Pagination;

/// <summary>
/// Internal record that holds cursor pagination data with cursor per item.
/// This type preserves full granularity for both REST and GraphQL projections.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
/// <param name="Items">The list of items with their individual cursors.</param>
/// <param name="PageInfo">Navigation information for the current page.</param>
/// <param name="TotalCount">Optional total count of items (expensive to compute).</param>
/// <remarks>
/// <para>
/// This is an internal type that serves as the foundation for cursor pagination.
/// It contains cursor information for each individual item, which enables:
/// </para>
/// <list type="bullet">
/// <item><description>REST projections via <see cref="CursorPaginatedResult{T}"/> (flat items list)</description></item>
/// <item><description>GraphQL projections via Connection types (edges with cursor per item)</description></item>
/// </list>
/// <para>
/// Provider implementations (EF Core, Dapper, MongoDB) create this type internally,
/// then project to the appropriate public result type based on consumer needs.
/// </para>
/// </remarks>
internal sealed record CursorPagedData<T>(
    IReadOnlyList<CursorItem<T>> Items,
    CursorPageInfo PageInfo,
    int? TotalCount = null)
{
    /// <summary>
    /// Creates an empty cursor-paginated data set.
    /// </summary>
    /// <returns>An empty <see cref="CursorPagedData{T}"/> instance.</returns>
    public static CursorPagedData<T> Empty() => new(
        [],
        new CursorPageInfo(
            HasPreviousPage: false,
            HasNextPage: false,
            StartCursor: null,
            EndCursor: null),
        TotalCount: 0);
}

/// <summary>
/// Internal record representing an item with its cursor value.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
/// <param name="Item">The actual item value.</param>
/// <param name="Cursor">The opaque cursor string for this item.</param>
/// <remarks>
/// The cursor is an opaque string that encodes the position of this item
/// in the result set. It can be used to navigate to this exact position.
/// </remarks>
internal sealed record CursorItem<T>(T Item, string Cursor);

/// <summary>
/// Internal record containing navigation information for cursor pagination.
/// </summary>
/// <param name="HasPreviousPage">Indicates whether there is a previous page of results.</param>
/// <param name="HasNextPage">Indicates whether there is a next page of results.</param>
/// <param name="StartCursor">The cursor of the first item in the current page, or null if empty.</param>
/// <param name="EndCursor">The cursor of the last item in the current page, or null if empty.</param>
/// <remarks>
/// <para>
/// This type follows the GraphQL Relay Connection specification for page info,
/// enabling seamless projection to GraphQL Connection types when needed.
/// </para>
/// <para>
/// For REST APIs, <see cref="StartCursor"/> maps to <c>PreviousCursor</c> and
/// <see cref="EndCursor"/> maps to <c>NextCursor</c> in the public result type.
/// </para>
/// </remarks>
internal sealed record CursorPageInfo(
    bool HasPreviousPage,
    bool HasNextPage,
    string? StartCursor,
    string? EndCursor);
