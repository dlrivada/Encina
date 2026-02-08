using Encina.DomainModeling.Pagination;

namespace Encina.GraphQL.Pagination;

/// <summary>
/// Extension methods for converting cursor pagination results to GraphQL Connection types.
/// </summary>
/// <remarks>
/// <para>
/// These extensions bridge Encina's cursor pagination with the GraphQL Relay Connection specification.
/// Use these methods to convert your query results to the appropriate format for GraphQL responses.
/// </para>
/// </remarks>
public static class ConnectionExtensions
{
    /// <summary>
    /// Converts internal cursor-paged data to a Relay-compliant Connection.
    /// </summary>
    /// <typeparam name="T">The type of items in the connection.</typeparam>
    /// <param name="data">The internal cursor-paged data containing items with individual cursors.</param>
    /// <returns>A Relay-compliant Connection with edges and page info.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Use this method when you have access to the internal <see cref="CursorPagedData{T}"/>
    /// type, which preserves per-item cursor information needed for GraphQL edges.
    /// </para>
    /// <para>
    /// This method is the preferred way to create Connections as it preserves
    /// the individual cursor for each item, enabling precise navigation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a GraphQL resolver
    /// var pagedData = await query.ToCursorPagedDataAsync(
    ///     cursor: after,
    ///     pageSize: first,
    ///     keySelector: o => o.CreatedAt,
    ///     cursorEncoder: encoder);
    ///
    /// return pagedData.ToConnection();
    /// </code>
    /// </example>
    internal static Connection<T> ToConnection<T>(this CursorPagedData<T> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Connection<T>.FromPagedData(data);
    }

    /// <summary>
    /// Converts a REST-friendly cursor result to a Relay-compliant Connection.
    /// </summary>
    /// <typeparam name="T">The type of items in the connection.</typeparam>
    /// <param name="result">The cursor-paginated result from REST APIs.</param>
    /// <returns>A Relay-compliant Connection with edges and page info.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> This conversion has a limitation - since
    /// <see cref="CursorPaginatedResult{T}"/> doesn't preserve per-item cursors,
    /// all edges in the resulting Connection will share the same cursor values
    /// (StartCursor for first item, EndCursor for last item, interpolated for middle items).
    /// </para>
    /// <para>
    /// For full Relay compliance with accurate per-item cursors, use
    /// <see cref="ToConnection{T}(CursorPagedData{T})"/> with internal paged data instead.
    /// </para>
    /// <para>
    /// This method is useful when you only have access to the public REST result
    /// and need to expose it via GraphQL with basic connection structure.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Converting REST-style result to Connection
    /// // Note: Per-item cursors are approximated
    /// var restResult = await query.ToCursorPaginatedAsync(...);
    /// return restResult.ToConnection();
    /// </code>
    /// </example>
    public static Connection<T> ToConnection<T>(this CursorPaginatedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Since CursorPaginatedResult doesn't have per-item cursors,
        // we use the start/end cursors and create approximate edges
        var items = result.Items;
        var edges = new List<Edge<T>>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            // For edges, we use:
            // - First item gets PreviousCursor (StartCursor equivalent)
            // - Last item gets NextCursor (EndCursor equivalent)
            // - Middle items get a placeholder based on position
            string cursor;
            if (i == 0 && result.PreviousCursor is not null)
            {
                cursor = result.PreviousCursor;
            }
            else if (i == items.Count - 1 && result.NextCursor is not null)
            {
                cursor = result.NextCursor;
            }
            else
            {
                // Middle items don't have accurate cursors in REST format
                // Use end cursor as a reasonable approximation
                cursor = result.NextCursor ?? result.PreviousCursor ?? string.Empty;
            }

            edges.Add(new Edge<T>
            {
                Node = items[i],
                Cursor = cursor
            });
        }

        return new Connection<T>
        {
            Edges = edges,
            PageInfo = new RelayPageInfo
            {
                HasPreviousPage = result.HasPreviousPage,
                HasNextPage = result.HasNextPage,
                StartCursor = result.PreviousCursor,
                EndCursor = result.NextCursor
            },
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Converts a Connection to a different node type using the specified selector.
    /// </summary>
    /// <typeparam name="TSource">The source node type.</typeparam>
    /// <typeparam name="TResult">The target node type.</typeparam>
    /// <param name="connection">The source connection.</param>
    /// <param name="selector">The mapping function to apply to each node.</param>
    /// <returns>A new Connection with mapped nodes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connection"/> or <paramref name="selector"/> is null.
    /// </exception>
    /// <remarks>
    /// This method preserves all cursor and pagination information while
    /// transforming the nodes to a different type. Useful for DTO mapping.
    /// </remarks>
    /// <example>
    /// <code>
    /// var orderConnection = await GetOrdersConnectionAsync();
    /// var dtoConnection = orderConnection.Map(order => new OrderDto(order.Id, order.Total));
    /// </code>
    /// </example>
    public static Connection<TResult> Map<TSource, TResult>(
        this Connection<TSource> connection,
        Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(selector);

        return new Connection<TResult>
        {
            Edges = connection.Edges.Select(edge => new Edge<TResult>
            {
                Node = selector(edge.Node),
                Cursor = edge.Cursor
            }).ToList(),
            PageInfo = connection.PageInfo,
            TotalCount = connection.TotalCount
        };
    }
}
