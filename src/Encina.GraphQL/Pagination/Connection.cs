using Encina.DomainModeling.Pagination;
using HotChocolate;

namespace Encina.GraphQL.Pagination;

/// <summary>
/// Represents a Relay-compliant connection for cursor-based pagination in GraphQL.
/// </summary>
/// <typeparam name="T">The type of items in the connection.</typeparam>
/// <remarks>
/// <para>
/// This type follows the GraphQL Relay Connection specification, providing:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Edges"/>: List of edges containing items with individual cursors</description></item>
/// <item><description><see cref="PageInfo"/>: Navigation information for pagination</description></item>
/// <item><description><see cref="TotalCount"/>: Optional total count (may be expensive to compute)</description></item>
/// </list>
/// <para>
/// See: <see href="https://relay.dev/graphql/connections.htm"/>
/// </para>
/// </remarks>
/// <example>
/// GraphQL schema representation:
/// <code>
/// type OrderConnection {
///   edges: [OrderEdge!]!
///   pageInfo: PageInfo!
///   totalCount: Int
/// }
/// </code>
///
/// Example query:
/// <code>
/// query {
///   orders(first: 10, after: "cursor123") {
///     edges {
///       node {
///         id
///         total
///       }
///       cursor
///     }
///     pageInfo {
///       hasNextPage
///       endCursor
///     }
///     totalCount
///   }
/// }
/// </code>
/// </example>
[GraphQLDescription("A connection to a list of items with cursor-based pagination.")]
public sealed record Connection<T>
{
    /// <summary>
    /// Gets the list of edges in this connection.
    /// </summary>
    /// <remarks>
    /// Each edge contains a node (the actual item) and its cursor.
    /// The edges are ordered according to the query's sort criteria.
    /// </remarks>
    [GraphQLDescription("A list of edges containing the nodes and their cursors.")]
    public required IReadOnlyList<Edge<T>> Edges { get; init; }

    /// <summary>
    /// Gets the page information for navigation.
    /// </summary>
    /// <remarks>
    /// Contains information about whether there are more pages and
    /// the cursors at the boundaries of the current page.
    /// </remarks>
    [GraphQLDescription("Information about pagination in this connection.")]
    public required RelayPageInfo PageInfo { get; init; }

    /// <summary>
    /// Gets the optional total count of items across all pages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is optional and may be null when total count calculation
    /// is disabled for performance reasons. Computing total count may require
    /// an additional database query.
    /// </para>
    /// <para>
    /// Use with caution on large datasets as it can be expensive to compute.
    /// </para>
    /// </remarks>
    [GraphQLDescription("The total number of items in the connection (may be null for performance).")]
    public int? TotalCount { get; init; }

    /// <summary>
    /// Gets the nodes (items) in this connection without edge information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience property that provides direct access to the items
    /// without the cursor information. Useful when you don't need pagination metadata.
    /// </para>
    /// <para>
    /// Note: This is a computed property that creates a new list on each access.
    /// </para>
    /// </remarks>
    [GraphQLDescription("The nodes (items) in this connection.")]
    public IReadOnlyList<T> Nodes => Edges.Select(e => e.Node).ToList();

    /// <summary>
    /// Creates a Connection from internal cursor-paged data.
    /// </summary>
    /// <param name="data">The internal cursor-paged data containing items with individual cursors.</param>
    /// <returns>A Relay-compliant Connection with edges and page info.</returns>
    /// <remarks>
    /// <para>
    /// This factory method projects the internal representation (which has cursor per item)
    /// to the GraphQL Relay Connection format with edges containing nodes and cursors.
    /// </para>
    /// <para>
    /// Use this method when you have access to the internal <see cref="CursorPagedData{T}"/>
    /// type (e.g., in EF Core providers with InternalsVisibleTo).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a GraphQL resolver with EF Core
    /// var pagedData = await query.ToCursorPagedDataAsync(...);
    /// return Connection&lt;Order&gt;.FromPagedData(pagedData);
    /// </code>
    /// </example>
    internal static Connection<T> FromPagedData(CursorPagedData<T> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new Connection<T>
        {
            Edges = data.Items.Select(item => new Edge<T>
            {
                Node = item.Item,
                Cursor = item.Cursor
            }).ToList(),
            PageInfo = new RelayPageInfo
            {
                HasPreviousPage = data.PageInfo.HasPreviousPage,
                HasNextPage = data.PageInfo.HasNextPage,
                StartCursor = data.PageInfo.StartCursor,
                EndCursor = data.PageInfo.EndCursor
            },
            TotalCount = data.TotalCount
        };
    }

    /// <summary>
    /// Creates an empty Connection with no items.
    /// </summary>
    /// <returns>An empty Connection with appropriate PageInfo.</returns>
    /// <example>
    /// <code>
    /// // Return empty connection for no results
    /// if (orders.Count == 0)
    /// {
    ///     return Connection&lt;Order&gt;.Empty();
    /// }
    /// </code>
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Factory method pattern - provides convenient typed empty result creation")]
    public static Connection<T> Empty() => new()
    {
        Edges = [],
        PageInfo = RelayPageInfo.Empty(),
        TotalCount = 0
    };
}
