namespace Encina;

/// <summary>
/// Identity projections over <see cref="IRequestContext"/>.
/// </summary>
/// <remarks>
/// The <c>UserId</c> extension property is derived from <see cref="IRequestContext.Identity"/>
/// by construction, so no <see cref="IRequestContext"/> implementation can report a user id that
/// differs from the identity the gates authorized.
/// </remarks>
/// <example>
/// <code>
/// string? actor = context.UserId; // same as context.Identity.UserId
/// </code>
/// </example>
public static class RequestContextIdentityExtensions
{
    extension(IRequestContext context)
    {
        /// <summary>
        /// Gets the user id of the request's identity: the identity-provider subject for a user,
        /// <c>service:&lt;name&gt;</c> for a service, and <see langword="null"/> for an anonymous caller
        /// (or a non-conforming context whose <see cref="IRequestContext.Identity"/> is <see langword="null"/>).
        /// </summary>
        public string? UserId => context.Identity?.UserId;
    }
}
