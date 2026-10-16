namespace Encina.Marten;

/// <summary>
/// An ambient scope that tells a serializer decorating Marten's serializer (for example the crypto-shredding
/// serializer of <c>Encina.Marten.GDPR</c>) not to decrypt what it deserializes in the current async flow.
/// </summary>
/// <remarks>
/// <para>
/// The event metadata readers (<see cref="IEventMetadataQuery"/>) use it: they return identifiers, timestamps and
/// event type names, so they must not fetch a subject key, and one undecryptable event must not make them fail.
/// Protected fields keep their stored token, so do not store an event read inside the scope again.
/// </para>
/// <para>
/// The scope flows with the async context, nests, and is restored when the returned handle is disposed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using (EventReadScope.SuppressDecryption())
/// {
///     var events = await session.Events.QueryAllRawEvents().ToListAsync(cancellationToken);
/// }
/// </code>
/// </example>
public static class EventReadScope
{
    private static readonly AsyncLocal<bool> Suppressed = new();

    /// <summary>Gets a value indicating whether decryption is suppressed in the current async flow.</summary>
    public static bool IsDecryptionSuppressed => Suppressed.Value;

    /// <summary>Suppresses decryption in the current async flow until the returned handle is disposed.</summary>
    /// <returns>A handle that restores the previous state when disposed.</returns>
    public static IDisposable SuppressDecryption()
    {
        var previous = Suppressed.Value;
        Suppressed.Value = true;
        return new Restore(previous);
    }

    private sealed class Restore(bool previous) : IDisposable
    {
        public void Dispose() => Suppressed.Value = previous;
    }
}
