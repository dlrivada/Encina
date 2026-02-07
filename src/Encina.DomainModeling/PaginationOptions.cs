namespace Encina.DomainModeling;

/// <summary>
/// Encapsulates pagination parameters for paginated queries.
/// </summary>
/// <param name="PageNumber">The page number (1-based). Defaults to 1.</param>
/// <param name="PageSize">The number of items per page. Defaults to 20.</param>
/// <remarks>
/// <para>
/// This record provides a standardized way to express pagination parameters across
/// all database providers. It uses 1-based page numbering for a more natural user experience.
/// </para>
/// <para>
/// The record is immutable and supports fluent builder methods using <c>with</c> expressions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using default values (page 1, size 20)
/// var options = PaginationOptions.Default;
///
/// // Creating with specific values
/// var options = new PaginationOptions(PageNumber: 2, PageSize: 50);
///
/// // Using builder methods
/// var options = PaginationOptions.Default
///     .WithPage(3)
///     .WithSize(25);
/// </code>
/// </example>
public record PaginationOptions(int PageNumber = 1, int PageSize = 20)
{
    /// <summary>
    /// Gets the number of items to skip based on page number and size.
    /// </summary>
    /// <remarks>
    /// Calculated as <c>(PageNumber - 1) * PageSize</c>.
    /// For example, page 1 with size 20 skips 0 items, page 2 skips 20 items.
    /// </remarks>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the default pagination options (page 1, size 20).
    /// </summary>
    public static PaginationOptions Default { get; } = new();

    /// <summary>
    /// Creates a new instance with the specified page number.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <returns>A new <see cref="PaginationOptions"/> instance with the updated page number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageNumber"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var options = PaginationOptions.Default.WithPage(5);
    /// </code>
    /// </example>
    public PaginationOptions WithPage(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        return this with { PageNumber = pageNumber };
    }

    /// <summary>
    /// Creates a new instance with the specified page size.
    /// </summary>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A new <see cref="PaginationOptions"/> instance with the updated page size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var options = PaginationOptions.Default.WithSize(50);
    /// </code>
    /// </example>
    public PaginationOptions WithSize(int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return this with { PageSize = pageSize };
    }
}

/// <summary>
/// Extends <see cref="PaginationOptions"/> with sorting capabilities.
/// </summary>
/// <param name="PageNumber">The page number (1-based). Defaults to 1.</param>
/// <param name="PageSize">The number of items per page. Defaults to 20.</param>
/// <param name="SortBy">The property name to sort by. Null for no sorting.</param>
/// <param name="SortDescending">Whether to sort in descending order. Defaults to false (ascending).</param>
/// <remarks>
/// <para>
/// This record provides pagination with sorting capabilities. The <paramref name="SortBy"/>
/// property should contain the name of the entity property to sort by.
/// </para>
/// <para>
/// When <paramref name="SortBy"/> is null, no sorting is applied and the default
/// database order is used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Sort by CreatedAtUtc descending, page 1, size 25
/// var options = new SortedPaginationOptions(
///     PageNumber: 1,
///     PageSize: 25,
///     SortBy: "CreatedAtUtc",
///     SortDescending: true);
///
/// // Using builder methods
/// var options = SortedPaginationOptions.DefaultSorted
///     .WithSort("Name")
///     .WithPage(2);
/// </code>
/// </example>
public record SortedPaginationOptions(
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDescending = false) : PaginationOptions(PageNumber, PageSize)
{
    /// <summary>
    /// Gets the default sorted pagination options (page 1, size 20, no sorting).
    /// </summary>
    public static new SortedPaginationOptions Default { get; } = new();

    /// <summary>
    /// Creates a new instance with the specified sort property and direction.
    /// </summary>
    /// <param name="sortBy">The property name to sort by.</param>
    /// <param name="descending">Whether to sort in descending order. Defaults to false (ascending).</param>
    /// <returns>A new <see cref="SortedPaginationOptions"/> instance with the updated sorting.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sortBy"/> is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// var options = SortedPaginationOptions.Default.WithSort("CreatedAtUtc", descending: true);
    /// </code>
    /// </example>
    public SortedPaginationOptions WithSort(string sortBy, bool descending = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortBy);
        return this with { SortBy = sortBy, SortDescending = descending };
    }

    /// <summary>
    /// Creates a new instance with the specified page number.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <returns>A new <see cref="SortedPaginationOptions"/> instance with the updated page number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageNumber"/> is less than 1.</exception>
    public new SortedPaginationOptions WithPage(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        return this with { PageNumber = pageNumber };
    }

    /// <summary>
    /// Creates a new instance with the specified page size.
    /// </summary>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A new <see cref="SortedPaginationOptions"/> instance with the updated page size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is less than 1.</exception>
    public new SortedPaginationOptions WithSize(int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return this with { PageSize = pageSize };
    }
}
