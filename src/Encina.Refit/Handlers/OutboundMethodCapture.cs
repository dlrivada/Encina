namespace Encina.Refit;

/// <summary>
/// Remembers the HTTP method of the call in flight, so a failure without a response (a timeout, a broken
/// connection) can still be judged ambiguous for a non-idempotent method.
/// </summary>
/// <remarks>
/// <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/> opens a scope before the call; the
/// <see cref="OutboundMethodCaptureHandler"/> that <c>AddEncinaRefitClient</c> adds to the client pipeline
/// stores the method of the request it sends. A client registered without it leaves the method unknown, which
/// the handler treats as non-idempotent (the safe side).
/// </remarks>
internal sealed class OutboundMethodCapture
{
    private static readonly AsyncLocal<OutboundMethodCapture?> Current = new();

    public HttpMethod? Method { get; private set; }

    public static Scope Begin()
    {
        var capture = new OutboundMethodCapture();
        Current.Value = capture;
        return new Scope(capture);
    }

    internal static void Record(HttpMethod method)
    {
        if (Current.Value is { } capture)
        {
            capture.Method = method;
        }
    }

    /// <summary>The scope of one call.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly OutboundMethodCapture _capture;

        internal Scope(OutboundMethodCapture capture)
        {
            _capture = capture;
        }

        public OutboundMethodCapture Capture => _capture;

        public void Dispose()
        {
            if (ReferenceEquals(Current.Value, _capture))
            {
                Current.Value = null;
            }
        }
    }
}

/// <summary>
/// Message handler that reports the method of each request to <see cref="OutboundMethodCapture"/>.
/// </summary>
internal sealed class OutboundMethodCaptureHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        OutboundMethodCapture.Record(request.Method);
        return base.SendAsync(request, cancellationToken);
    }
}
