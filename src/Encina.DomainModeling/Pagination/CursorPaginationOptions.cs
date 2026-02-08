namespace Encina.DomainModeling.Pagination;

/// <summary>
/// Specifies the direction of cursor-based pagination.
/// </summary>
public enum CursorDirection
{
    /// <summary>
    /// Navigate forward through the result set (fetch items after the cursor).
    /// </summary>
    Forward = 0,

    /// <summary>
    /// Navigate backward through the result set (fetch items before the cursor).
    /// </summary>
    Backward = 1
}

/// <summary>
/// Encapsulates parameters for cursor-based pagination queries.
/// </summary>
/// <param name="Cursor">
/// The opaque cursor string indicating the position to paginate from.
/// Null for the first page.
/// </param>
/// <param name="PageSize">
/// The number of items per page. Defaults to 20.
/// Must be between 1 and <see cref="MaxPageSize"/>.
/// </param>
/// <param name="Direction">
/// The direction of pagination. Defaults to <see cref="CursorDirection.Forward"/>.
/// </param>
/// <remarks>
/// <para>
/// Cursor-based pagination provides O(1) performance regardless of page position,
/// unlike offset-based pagination which degrades linearly with page number.
/// </para>
/// <para>
/// The cursor is an opaque string that should not be parsed or constructed by clients.
/// It encodes the position in the result set using the sort key values.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // First page (no cursor)
/// var options = CursorPaginationOptions.Default;
///
/// // Next page with cursor from previous response
/// var options = new CursorPaginationOptions(
///     Cursor: "eyJjcmVhdGVkQXQiOiIyMDI1LTEyLTI3IiwiaWQiOiIxMjM0In0=",
///     PageSize: 25);
///
/// // Previous page (backward navigation)
/// var options = new CursorPaginationOptions(
///     Cursor: previousCursor,
///     PageSize: 25,
///     Direction: CursorDirection.Backward);
///
/// // Using builder methods
/// var options = CursorPaginationOptions.Default
///     .WithCursor(cursor)
///     .WithSize(50)
///     .WithDirection(CursorDirection.Backward);
/// </code>
/// </example>
public record CursorPaginationOptions(
    string? Cursor = null,
    int PageSize = 20,
    CursorDirection Direction = CursorDirection.Forward)
{
    /// <summary>
    /// The default maximum page size allowed.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// The default page size.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Gets the default cursor pagination options (no cursor, size 20, forward direction).
    /// </summary>
    public static CursorPaginationOptions Default { get; } = new();

    /// <summary>
    /// Creates a new instance with the specified cursor.
    /// </summary>
    /// <param name="cursor">The cursor to paginate from. Null for the first page.</param>
    /// <returns>A new <see cref="CursorPaginationOptions"/> instance with the updated cursor.</returns>
    /// <example>
    /// <code>
    /// var options = CursorPaginationOptions.Default.WithCursor("eyJ...");
    /// </code>
    /// </example>
    public CursorPaginationOptions WithCursor(string? cursor) =>
        this with { Cursor = cursor };

    /// <summary>
    /// Creates a new instance with the specified page size.
    /// </summary>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A new <see cref="CursorPaginationOptions"/> instance with the updated page size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageSize"/> is less than 1 or greater than <see cref="MaxPageSize"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var options = CursorPaginationOptions.Default.WithSize(50);
    /// </code>
    /// </example>
    public CursorPaginationOptions WithSize(int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);
        return this with { PageSize = pageSize };
    }

    /// <summary>
    /// Creates a new instance with the specified direction.
    /// </summary>
    /// <param name="direction">The direction of pagination.</param>
    /// <returns>A new <see cref="CursorPaginationOptions"/> instance with the updated direction.</returns>
    /// <example>
    /// <code>
    /// var options = CursorPaginationOptions.Default.WithDirection(CursorDirection.Backward);
    /// </code>
    /// </example>
    public CursorPaginationOptions WithDirection(CursorDirection direction) =>
        this with { Direction = direction };

    /// <summary>
    /// Gets whether this represents the first page (no cursor specified).
    /// </summary>
    public bool IsFirstPage => string.IsNullOrEmpty(Cursor);
}
