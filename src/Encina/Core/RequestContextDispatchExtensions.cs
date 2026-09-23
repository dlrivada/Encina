namespace Encina;

/// <summary>
/// Extension methods that tell how an <see cref="IRequestContext"/> entered the pipeline.
/// </summary>
public static class RequestContextDispatchExtensions
{
    /// <summary>
    /// The metadata key that marks the context of a nested dispatch.
    /// </summary>
    internal const string NestedDispatchKey = "Encina.NestedDispatch";

    /// <summary>
    /// Determines whether the context belongs to a dispatch nested inside another one.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>
    /// <c>true</c> when <see cref="IEncina"/> derived the context from the ambient context of an
    /// outer dispatch (a <c>Send</c>, <c>Publish</c> or <c>Stream</c> issued from a handler, a
    /// behavior, a notification handler or an EF Core interceptor while another dispatch was
    /// running); <c>false</c> for an entry-point context.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A nested context carries no <see cref="IRequestContext.IdempotencyKey"/>: the key belongs to
    /// the entry point's logical request, and the entry point's idempotency check already covers
    /// everything its handler does. Idempotency behaviors use this method to let a nested idempotent
    /// request without a key of its own run, instead of rejecting it as a request that is missing
    /// its key.
    /// </para>
    /// <para>
    /// A nested request that needs its own deduplication passes an explicit context with its own key
    /// through <see cref="IEncina.Send{TResponse}(IRequest{TResponse}, IRequestContext, CancellationToken)"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
    public static bool IsNestedDispatch(this IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.TryGetValue(NestedDispatchKey, out var value) && value is true;
    }
}
