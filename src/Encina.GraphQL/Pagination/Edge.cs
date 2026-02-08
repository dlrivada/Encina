using HotChocolate;

namespace Encina.GraphQL.Pagination;

/// <summary>
/// Represents an edge in the Relay Connection specification.
/// </summary>
/// <typeparam name="T">The type of the node (item) in this edge.</typeparam>
/// <remarks>
/// <para>
/// An edge represents a single item in a connection, along with its cursor
/// for navigation purposes. This enables precise pagination to any item.
/// </para>
/// <para>
/// See: <see href="https://relay.dev/graphql/connections.htm#sec-Edge-Types"/>
/// </para>
/// </remarks>
/// <example>
/// GraphQL schema representation:
/// <code>
/// type OrderEdge {
///   node: Order!
///   cursor: String!
/// }
/// </code>
/// </example>
[GraphQLDescription("An edge in a connection containing a cursor and the node.")]
public sealed record Edge<T>
{
    /// <summary>
    /// Gets the item (node) at this position in the connection.
    /// </summary>
    /// <remarks>
    /// The node represents the actual data item. In GraphQL terms,
    /// this is the object that the connection provides access to.
    /// </remarks>
    [GraphQLDescription("The item at the end of the edge.")]
    public required T Node { get; init; }

    /// <summary>
    /// Gets the opaque cursor for this edge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cursor is an opaque string that uniquely identifies this item's
    /// position in the result set. It can be used with the <c>after</c> or
    /// <c>before</c> arguments to paginate from this exact position.
    /// </para>
    /// <para>
    /// Cursors are designed to be opaque to clients - their format should
    /// not be relied upon and may change between implementations.
    /// </para>
    /// </remarks>
    [GraphQLDescription("A cursor for use in pagination.")]
    public required string Cursor { get; init; }
}
