using Marten;

namespace Encina.Marten;

/// <summary>
/// Flush helper that keeps an <see cref="IDocumentSession"/> usable after a failed save.
/// </summary>
internal static class DocumentSessionFlushExtensions
{
    /// <summary>
    /// Calls <see cref="IDocumentSession.SaveChangesAsync(CancellationToken)"/> and, when the flush fails,
    /// ejects every pending change before rethrowing.
    /// </summary>
    /// <remarks>
    /// Marten does not clear the operations of a failed batch, so the next <c>SaveChangesAsync</c> on the same
    /// session would replay them (#3787). The eject is scoped to the flush on purpose: a failure before the
    /// operations are queued (validation, an invalid stream id) must not discard unrelated work the caller
    /// queued on the session.
    /// </remarks>
    /// <param name="session">The session to flush.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal static async Task SaveChangesOrEjectAsync(this IDocumentSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            session.EjectAllPendingChanges();
            throw;
        }
    }
}
