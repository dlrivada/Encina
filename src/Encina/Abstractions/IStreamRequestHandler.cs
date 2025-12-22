using LanguageExt;

namespace Encina;

/// <summary>
/// Executes the logic associated with a streaming request using Railway Oriented Programming.
/// </summary>
/// <typeparam name="TRequest">Handled stream request type.</typeparam>
/// <typeparam name="TItem">Type of each item yielded by the stream.</typeparam>
/// <remarks>
/// <para>
/// Stream handlers produce items asynchronously using <c>IAsyncEnumerable&lt;T&gt;</c>,
/// enabling efficient processing of large datasets, real-time feeds, and batch operations.
/// </para>
/// <para>
/// Each item is wrapped in <c>Either&lt;EncinaError, TItem&gt;</c> to maintain Railway
/// Oriented Programming semantics. Handlers can yield errors mid-stream without terminating
/// the sequence, allowing partial results and graceful degradation.
/// </para>
/// <para>
/// Use <c>yield return Right(item)</c> for successful items and <c>yield return Left(error)</c>
/// for errors. The consumer decides whether to continue or terminate on errors.
/// </para>
/// <para>
/// Always use <c>[EnumeratorCancellation]</c> attribute on the cancellation token parameter
/// to ensure proper cancellation when iteration stops early.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class StreamProductsHandler : IStreamRequestHandler&lt;StreamProductsQuery, Product&gt;
/// {
///     public async IAsyncEnumerable&lt;Either&lt;EncinaError, Product&gt;&gt; Handle(
///         StreamProductsQuery request,
///         [EnumeratorCancellation] CancellationToken cancellationToken)
///     {
///         var pageNumber = 0;
///         while (true)
///         {
///             var products = await _repository.GetPageAsync(pageNumber, request.PageSize, cancellationToken);
///             if (products.Count == 0)
///                 yield break;
///
///             foreach (var product in products)
///             {
///                 if (!product.IsValid)
///                 {
///                     yield return Left(EncinaErrors.ValidationFailed($"Invalid product: {product.Id}"));
///                     continue;
///                 }
///
///                 yield return Right(product);
///             }
///
///             pageNumber++;
///         }
///     }
/// }
/// </code>
/// </example>
public interface IStreamRequestHandler<in TRequest, TItem>
    where TRequest : IStreamRequest<TItem>
{
    /// <summary>
    /// Processes the incoming stream request and yields a sequence of results.
    /// </summary>
    /// <param name="request">Stream request to handle.</param>
    /// <param name="cancellationToken">
    /// Cancellation token that triggers when iteration stops or times out.
    /// Must be decorated with <c>[EnumeratorCancellation]</c> attribute.
    /// </param>
    /// <returns>
    /// Async enumerable of <c>Either&lt;EncinaError, TItem&gt;</c>, where each element
    /// represents either an error (Left) or a successful item (Right).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use <c>static LanguageExt.Prelude</c> to access <c>Left</c> and <c>Right</c> factory methods.
    /// Unlike regular handlers, stream handlers do NOT short-circuit on errors - errors are yielded
    /// as part of the stream, and the consumer decides whether to continue or stop.
    /// </para>
    /// <para>
    /// Always check <paramref name="cancellationToken"/>.IsCancellationRequested before expensive
    /// operations, especially when fetching additional pages or performing I/O.
    /// </para>
    /// <para>
    /// When implementing this interface, decorate the cancellation token parameter with
    /// <c>[EnumeratorCancellation]</c> attribute to ensure proper cancellation when iteration stops early.
    /// </para>
    /// </remarks>
    IAsyncEnumerable<Either<EncinaError, TItem>> Handle(
        TRequest request,
        CancellationToken cancellationToken);
}
