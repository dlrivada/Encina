using HotChocolate;

namespace Encina.GraphQL.Pagination;

/// <summary>
/// Represents page information in the Relay Connection specification.
/// </summary>
/// <remarks>
/// <para>
/// This type conforms to the GraphQL Relay Connection specification for pagination.
/// It provides information about the current page and navigation capabilities.
/// </para>
/// <para>
/// See: <see href="https://relay.dev/graphql/connections.htm#sec-undefined.PageInfo"/>
/// </para>
/// </remarks>
/// <example>
/// GraphQL schema representation:
/// <code>
/// type PageInfo {
///   hasPreviousPage: Boolean!
///   hasNextPage: Boolean!
///   startCursor: String
///   endCursor: String
/// }
/// </code>
/// </example>
[GraphQLDescription("Information about pagination in a connection.")]
public sealed record RelayPageInfo
{
    /// <summary>
    /// Gets whether there is a previous page of results.
    /// </summary>
    /// <remarks>
    /// When paginating backwards, this will be true if there are more pages before the current one.
    /// </remarks>
    [GraphQLDescription("When paginating backwards, are there more items?")]
    public bool HasPreviousPage { get; init; }

    /// <summary>
    /// Gets whether there is a next page of results.
    /// </summary>
    /// <remarks>
    /// When paginating forwards, this will be true if there are more pages after the current one.
    /// </remarks>
    [GraphQLDescription("When paginating forwards, are there more items?")]
    public bool HasNextPage { get; init; }

    /// <summary>
    /// Gets the cursor of the first item in the current page.
    /// </summary>
    /// <remarks>
    /// This can be used to navigate to the previous page when paginating backwards.
    /// Null if the result set is empty.
    /// </remarks>
    [GraphQLDescription("The cursor of the first edge in the connection.")]
    public string? StartCursor { get; init; }

    /// <summary>
    /// Gets the cursor of the last item in the current page.
    /// </summary>
    /// <remarks>
    /// This can be used to navigate to the next page when paginating forwards.
    /// Null if the result set is empty.
    /// </remarks>
    [GraphQLDescription("The cursor of the last edge in the connection.")]
    public string? EndCursor { get; init; }

    /// <summary>
    /// Creates an empty PageInfo indicating no results.
    /// </summary>
    /// <returns>A PageInfo with all navigation flags set to false and null cursors.</returns>
    public static RelayPageInfo Empty() => new()
    {
        HasPreviousPage = false,
        HasNextPage = false,
        StartCursor = null,
        EndCursor = null
    };
}
