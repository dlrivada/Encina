namespace Encina;

/// <summary>
/// Represents a streaming request that produces a sequence of items asynchronously.
/// </summary>
/// <typeparam name="TItem">Type of each item produced by the stream.</typeparam>
/// <remarks>
/// <para>
/// Stream requests are ideal for scenarios where data is produced incrementally:
/// large result sets, real-time data feeds, batch processing with backpressure,
/// or Server-Sent Events (SSE) in web applications.
/// </para>
/// <para>
/// Unlike <see cref="IRequest{TResponse}"/>, stream requests return an async enumerable
/// that yields items one at a time, allowing efficient memory usage and early cancellation.
/// </para>
/// <para>
/// Each yielded item is wrapped in <c>Either&lt;EncinaError, TItem&gt;</c> to support
/// Railway Oriented Programming. Errors can be yielded mid-stream without terminating
/// the entire sequence, allowing partial results and graceful degradation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Large query with implicit pagination
/// public sealed record StreamProductsQuery(int PageSize = 100) : IStreamRequest&lt;Product&gt;;
///
/// // Real-time event stream
/// public sealed record StreamOrderUpdatesQuery(Guid OrderId) : IStreamRequest&lt;OrderUpdate&gt;;
///
/// // Batch processing with backpressure
/// public sealed record ProcessLargeFileQuery(string FilePath) : IStreamRequest&lt;ProcessedRecord&gt;;
/// </code>
/// </example>
public interface IStreamRequest<out TItem>
{
}
