namespace Encina;

/// <summary>
/// The anonymous scopes a host adapter opens through
/// <see cref="IInternalRequestContextScopeFactory.RunAnonymousMarkerAsync"/>. Both read as the
/// anonymous identity; they differ in what they add to the holder chain.
/// </summary>
internal enum AnonymousMarker
{
    /// <summary>
    /// A long-lived connection (a hub connection, a WebSocket, a server-sent event stream). The holder
    /// has origin <see cref="RequestOrigin.Connection"/>: service, principal, built-in and restored
    /// scopes treat it as an inbound request, while inbound scopes (one per circuit activity or hub
    /// invocation) are permitted over it.
    /// </summary>
    Connection = 1,

    /// <summary>
    /// A masking scope: it hides the ambient context (reads anonymous) and keeps every fact of the
    /// chain it covers, so the refusals still see the inbound or user context it hides.
    /// </summary>
    Mask = 2
}
