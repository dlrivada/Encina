using LanguageExt;

namespace Encina.Messaging.Transactions;

/// <summary>
/// Helpers for the behaviors that take a lease from an <see cref="ITransactionLeaseSource"/>.
/// </summary>
internal static class TransactionLeaseExtensions
{
    /// <summary>
    /// Gets the lease of a begin-or-join result that is known to be <c>Right</c>.
    /// </summary>
    /// <typeparam name="TLease">The lease type.</typeparam>
    /// <param name="begun">A result whose <c>IsLeft</c> was already checked.</param>
    /// <returns>The lease.</returns>
    /// <exception cref="InvalidOperationException">The result is a <c>Left</c> (a caller bug).</exception>
    internal static TLease RequireLease<TLease>(this Either<EncinaError, TLease> begun)
        where TLease : class, ITransactionLease
        => begun.IfLeft(static _ => throw new InvalidOperationException("The transaction result was a Left; check IsLeft before taking the lease."));
}
