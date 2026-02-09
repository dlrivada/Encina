using Encina.Cdc;
using Encina.Cdc.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Cdc.Helpers;

/// <summary>
/// Interceptor that tracks all dispatched events for integration test assertions.
/// </summary>
internal sealed class TrackingInterceptor : ICdcEventInterceptor
{
    private readonly List<ChangeEvent> _interceptedEvents = [];
    private readonly object _lock = new();

    /// <summary>
    /// Gets a snapshot of all intercepted events.
    /// </summary>
    public IReadOnlyList<ChangeEvent> InterceptedEvents
    {
        get { lock (_lock) { return [.. _interceptedEvents]; } }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> OnEventDispatchedAsync(
        ChangeEvent changeEvent,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _interceptedEvents.Add(changeEvent);
        }
        return new(Right(unit));
    }
}
