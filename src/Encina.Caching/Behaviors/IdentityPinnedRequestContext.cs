namespace Encina.Caching;

/// <summary>
/// A view of a request context whose <see cref="IRequestContext.Identity"/> is pinned to the identity
/// read once by <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/>, so the key generator
/// (default or custom) builds a <c>VaryByUser</c> key from the same identity the bypass decision saw.
/// </summary>
/// <remarks>
/// <see cref="RequestContext.Identity"/> reads <see cref="RequestIdentity.Anonymous"/> once its
/// identity scope ends (#1892), so two reads of the live context can disagree. The view never reaches
/// handlers: the behavior uses it only to build the key, and re-checks the live context before every
/// cache read and write.
/// </remarks>
internal sealed class IdentityPinnedRequestContext : IRequestContext
{
    private readonly IRequestContext _inner;

    internal IdentityPinnedRequestContext(IRequestContext inner, RequestIdentity identity)
    {
        _inner = inner;
        Identity = identity;
    }

    /// <inheritdoc />
    public string CorrelationId => _inner.CorrelationId;

    /// <inheritdoc />
    public string? CausationId => _inner.CausationId;

    /// <inheritdoc />
    public RequestIdentity Identity { get; }

    /// <inheritdoc />
    public string? IdempotencyKey => _inner.IdempotencyKey;

    /// <inheritdoc />
    public string? TenantId => _inner.TenantId;

    /// <inheritdoc />
    public DateTimeOffset Timestamp => _inner.Timestamp;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Metadata => _inner.Metadata;

    /// <inheritdoc />
    public IRequestContext WithMetadata(string key, object? value) => new IdentityPinnedRequestContext(_inner.WithMetadata(key, value), Identity);

    /// <inheritdoc />
    public IRequestContext WithIdempotencyKey(string? idempotencyKey) => new IdentityPinnedRequestContext(_inner.WithIdempotencyKey(idempotencyKey), Identity);

    /// <inheritdoc />
    public IRequestContext WithTenantId(string? tenantId) => new IdentityPinnedRequestContext(_inner.WithTenantId(tenantId), Identity);
}
