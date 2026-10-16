namespace Encina.Marten;

/// <summary>
/// An ambient scope that tells a serializer decorating Marten's serializer (crypto-shredding) not to decrypt
/// what it deserializes in the current async flow.
/// </summary>
/// <remarks>
/// Metadata readers use it: they return identifiers, timestamps and event type names, so they must not fetch a
/// subject key, and one undecryptable event must not make them fail. Protected fields keep their stored token.
/// </remarks>
internal static class EventReadScope
{
    private static readonly AsyncLocal<bool> Suppressed = new();

    /// <summary>Gets a value indicating whether decryption is suppressed in the current async flow.</summary>
    internal static bool IsDecryptionSuppressed => Suppressed.Value;

    /// <summary>Suppresses decryption in the current async flow until the returned handle is disposed.</summary>
    /// <returns>A handle that restores the previous state.</returns>
    internal static IDisposable SuppressDecryption()
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
