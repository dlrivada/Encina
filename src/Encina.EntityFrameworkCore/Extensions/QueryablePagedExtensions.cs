using Encina.DomainModeling;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/> to support pagination operations.
/// </summary>
public static class QueryablePagedExtensions
{
    /// <summary>
    /// Executes a paginated query and returns a <see cref="PagedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The query to paginate.</param>
    /// <param name="pagination">The pagination options specifying page number and size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="PagedResult{T}"/> containing the paginated items and metadata.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method executes two database queries:
    /// <list type="number">
    /// <item><description>A count query to determine the total number of items.</description></item>
    /// <item><description>A paginated query to retrieve only the items for the requested page.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For better performance with large datasets, ensure the query has appropriate
    /// indexes and consider using keyset pagination for very large result sets.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pagination = new PaginationOptions(PageNumber: 2, PageSize: 25);
    /// var result = await dbContext.Orders
    ///     .Where(o => o.Status == OrderStatus.Active)
    ///     .OrderByDescending(o => o.CreatedAtUtc)
    ///     .ToPagedResultAsync(pagination, cancellationToken);
    ///
    /// Console.WriteLine($"Page {result.PageNumber} of {result.TotalPages}");
    /// Console.WriteLine($"Showing items {result.FirstItemIndex}-{result.LastItemIndex} of {result.TotalCount}");
    /// </code>
    /// </example>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(pagination);

        // Execute count query first
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // If no items, return empty result early
        if (totalCount == 0)
        {
            return PagedResult<T>.Empty(pagination.PageNumber, pagination.PageSize);
        }

        // Execute paginated query
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<T>(
            Items: items,
            PageNumber: pagination.PageNumber,
            PageSize: pagination.PageSize,
            TotalCount: totalCount);
    }

    /// <summary>
    /// Executes a paginated query with projection and returns a <see cref="PagedResult{TResult}"/>.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="query">The query to paginate.</param>
    /// <param name="selector">The projection selector.</param>
    /// <param name="pagination">The pagination options specifying page number and size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="PagedResult{TResult}"/> containing the projected and paginated items.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The projection is applied at the database level, selecting only the required columns.
    /// This improves performance compared to loading full entities and mapping in memory.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
    /// var result = await dbContext.Orders
    ///     .Where(o => o.CustomerId == customerId)
    ///     .ToPagedResultAsync(
    ///         o => new OrderSummaryDto(o.Id, o.Total, o.Status.ToString()),
    ///         pagination,
    ///         cancellationToken);
    /// </code>
    /// </example>
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<T, TResult>(
        this IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, TResult>> selector,
        PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(pagination);

        // Execute count query on the source query (before projection)
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // If no items, return empty result early
        if (totalCount == 0)
        {
            return PagedResult<TResult>.Empty(pagination.PageNumber, pagination.PageSize);
        }

        // Execute paginated query with projection
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(selector)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TResult>(
            Items: items,
            PageNumber: pagination.PageNumber,
            PageSize: pagination.PageSize,
            TotalCount: totalCount);
    }
}
