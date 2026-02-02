namespace Encina.Security.Audit;

/// <summary>
/// Represents a paginated result set from a query operation.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <remarks>
/// <para>
/// This record provides all the metadata needed for building pagination UIs:
/// <list type="bullet">
/// <item><see cref="Items"/> - The actual data for the current page</item>
/// <item><see cref="TotalCount"/> - Total matching items across all pages</item>
/// <item><see cref="PageNumber"/> - Current page (1-based)</item>
/// <item><see cref="PageSize"/> - Items per page</item>
/// <item><see cref="TotalPages"/> - Computed total number of pages</item>
/// <item><see cref="HasPreviousPage"/> / <see cref="HasNextPage"/> - Navigation helpers</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await auditStore.QueryAsync(query);
///
/// Console.WriteLine($"Showing {result.Items.Count} of {result.TotalCount} entries");
/// Console.WriteLine($"Page {result.PageNumber} of {result.TotalPages}");
///
/// if (result.HasNextPage)
/// {
///     // Show "Next" button
/// }
/// </code>
/// </example>
public sealed record PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    /// <remarks>
    /// Computed as ceiling of <see cref="TotalCount"/> divided by <see cref="PageSize"/>.
    /// Returns 0 when there are no items.
    /// </remarks>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// The number of items in the current page.
    /// </summary>
    /// <remarks>
    /// This may be less than <see cref="PageSize"/> for the last page.
    /// </remarks>
    public int Count => Items.Count;

    /// <summary>
    /// Whether the result set is empty.
    /// </summary>
    public bool IsEmpty => TotalCount == 0;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="pageNumber">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>An empty <see cref="PagedResult{T}"/> with zero total count.</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types - factory method is appropriate here
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = AuditQuery.DefaultPageSize) => new()
    {
        Items = Array.Empty<T>(),
        TotalCount = 0,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
#pragma warning restore CA1000

    /// <summary>
    /// Creates a paged result from a collection with pagination metadata.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A new <see cref="PagedResult{T}"/> instance.</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types - factory method is appropriate here
    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize) => new()
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
#pragma warning restore CA1000
}
