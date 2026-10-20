namespace Encina.Testing.Identity;

/// <summary>
/// Builds <see cref="IRequestContext"/> instances that carry a given identity, for tests.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RequestContext.CreateForTest"/> is anonymous-only and the identity-taking factory is
/// internal, so this class builds the contexts that handlers, behaviors and gates receive when a
/// test calls them directly (ADR-035, declared test seam). Pair it with <see cref="TestIdentity"/>.
/// </para>
/// <para>
/// It never binds an identity: the contexts it builds have no issuing scope, so passing one with an
/// authenticated identity to <c>IEncina.Send</c>, <c>Publish</c> or <c>Stream</c> is refused
/// (<see cref="RequestIdentityErrorCodes.ScopeConflict"/>) unless an active scope of the same
/// identity is ambient. A test that dispatches with an identity binds it through
/// <see cref="IRequestContextScopeFactory"/>.
/// </para>
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
