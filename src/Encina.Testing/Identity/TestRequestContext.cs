namespace Encina.Testing.Identity;

/// <summary>
/// Builds <see cref="IRequestContext"/> instances that carry a given identity, for tests.
/// </summary>
/// <remarks>
/// <see cref="RequestContext.CreateForTest"/> is anonymous-only and the identity-taking factory is
/// internal, so this class is the supported way for tests to run a request as a user (ADR-035,
/// declared test seam). Pair it with <see cref="TestIdentity"/>.
/// </remarks>
/// <example>
/// <code>
/// var context = TestRequestContext.For(TestIdentity.User("alice"), tenantId: "tenant-1");
/// context.UserId.ShouldBe("alice");
/// </code>
/// </example>
public static class TestRequestContext
{
    /// <summary>
    /// Creates a context carrying <paramref name="identity"/>.
    /// </summary>
    /// <param name="identity">The identity (see <see cref="TestIdentity"/>).</param>
    /// <param name="tenantId">The tenant id, if any.</param>
    /// <param name="idempotencyKey">The idempotency key, if any.</param>
    /// <param name="correlationId">The correlation id; a new <c>test-</c> id when omitted.</param>
    /// <param name="timestamp">The timestamp; the current system time when omitted.</param>
    /// <returns>A new context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
    public static IRequestContext For(
        RequestIdentity identity,
        string? tenantId = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return RequestContext.CreateAt(
            timestamp ?? TimeProvider.System.GetUtcNow(),
            correlationId ?? $"test-{Guid.NewGuid():N}",
            identity,
            tenantId,
            idempotencyKey);
    }

    /// <summary>
    /// Creates a copy of <paramref name="context"/> carrying <paramref name="identity"/>.
    /// </summary>
    /// <param name="context">The context to copy (correlation id, tenant, idempotency key, timestamp, metadata).</param>
    /// <param name="identity">The identity of the copy.</param>
    /// <returns>A new context; <paramref name="context"/> is not modified.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static IRequestContext WithIdentity(IRequestContext context, RequestIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(identity);

        return RequestContext.CopyOf(context).WithIdentity(identity);
    }
}
