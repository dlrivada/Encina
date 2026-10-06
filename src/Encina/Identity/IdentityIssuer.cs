namespace Encina;

/// <summary>
/// The stamp an identity scope puts on the <see cref="RequestIdentity"/> it creates, so the identity
/// stops reading as a caller once that scope has ended.
/// </summary>
/// <remarks>
/// <para>
/// The scope factory creates the issuer first, builds a new identity with it, pushes the scope
/// holder and then binds the issuer to that holder once. The identity stays immutable and never
/// references the holder; it reaches it only through this token.
/// </para>
/// <para>
/// The issuer is live while its holder is valid (<see cref="RequestContextAccessor.ContextHolder.IsValid"/>),
/// so ending an enclosing scope ends every issuer nested in it. An unbound issuer is not live.
/// Builders (<c>Encina.Testing</c>) create identities without an issuer.
/// </para>
/// </remarks>
internal sealed class IdentityIssuer
{
    private RequestContextAccessor.ContextHolder? _holder;

    /// <summary>
    /// Gets a value indicating whether the scope that issued the identity is still active.
    /// </summary>
    internal bool IsLive => Volatile.Read(ref _holder) is { } holder && holder.IsValid();

    /// <summary>
    /// Binds this issuer to the holder of the scope that issued the identity. Called once.
    /// </summary>
    /// <param name="holder">The scope holder.</param>
    /// <exception cref="InvalidOperationException">The issuer is already bound.</exception>
    internal void Bind(RequestContextAccessor.ContextHolder holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        if (Interlocked.CompareExchange(ref _holder, holder, null) is not null)
        {
            throw new InvalidOperationException("An identity issuer is bound to one scope holder only.");
        }
    }
}
