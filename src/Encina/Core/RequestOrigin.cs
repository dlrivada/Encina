namespace Encina;

/// <summary>
/// Where a <see cref="RequestContext"/> was opened. Internal and typed, so application code cannot
/// spoof it through metadata.
/// </summary>
/// <remarks>
/// Copies of a context (<c>With*</c>, nested dispatch) keep the origin. Identity scopes refuse to
/// open over an <see cref="Inbound"/> context unless the caller opts in explicitly.
/// </remarks>
internal enum RequestOrigin
{
    /// <summary>Not opened by an identity entry point (a fresh context, a test context).</summary>
    Unspecified = 0,

    /// <summary>An inbound request: an HTTP request or a Blazor circuit activity.</summary>
    Inbound = 1,

    /// <summary>An identity scope opened through the scope factory.</summary>
    Scope = 2,

    /// <summary>A deferred message dispatched with its originating actor restored.</summary>
    Restored = 3,

    /// <summary>
    /// A long-lived connection (a SignalR or Blazor hub connection, a WebSocket, a server-sent event
    /// stream) that runs anonymous under the connection marker. Service, principal and restored
    /// scopes treat it as <see cref="Inbound"/>; per-activity and per-invocation inbound scopes are
    /// permitted over it.
    /// </summary>
    Connection = 4
}
