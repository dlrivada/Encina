using System.Runtime.CompilerServices;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Cdc.Helpers;

/// <summary>
/// In-memory connector that yields pre-loaded change events for integration testing.
/// Allows tests to control exactly which events flow through the pipeline.
/// </summary>
internal sealed class TestCdcConnector : ICdcConnector
{
    private readonly List<Either<EncinaError, ChangeEvent>> _events = [];
    private long _currentPosition;
    private readonly object _streamCallLock = new();
    private readonly List<(int Count, TaskCompletionSource Waiter)> _streamCallWaiters = [];
    private int _streamCallCount;

    public string ConnectorId { get; }

    public TestCdcConnector(string connectorId = "test-connector")
    {
        ConnectorId = connectorId;
    }

    /// <summary>
    /// Adds a successful change event that will be yielded on the next stream call.
    /// </summary>
    public TestCdcConnector AddEvent(ChangeEvent changeEvent)
    {
        lock (_events) { _events.Add(Right(changeEvent)); }
        return this;
    }

    /// <summary>
    /// Adds multiple change events.
    /// </summary>
    public TestCdcConnector AddEvents(IEnumerable<ChangeEvent> events)
    {
        lock (_events)
        {
            foreach (var evt in events)
            {
                _events.Add(Right(evt));
            }
        }
        return this;
    }

    /// <summary>
    /// Adds an error event that will be yielded on the next stream call.
    /// </summary>
    public TestCdcConnector AddError(EncinaError error)
    {
        lock (_events) { _events.Add(Left(error)); }
        return this;
    }

    /// <summary>
    /// Gets the number of times <see cref="StreamChangesAsync"/> has started enumerating.
    /// </summary>
    public int StreamCallCount
    {
        get { lock (_streamCallLock) { return _streamCallCount; } }
    }

    /// <summary>
    /// Completes when <see cref="StreamChangesAsync"/> has started at least
    /// <paramref name="count"/> times. Because the processor consumes one stream call
    /// completely before polling again, stream call <c>N + 1</c> proves that call <c>N</c>
    /// (its dispatches and position saves) has finished.
    /// </summary>
    public Task WaitForStreamCallsAsync(int count)
    {
        lock (_streamCallLock)
        {
            if (_streamCallCount >= count)
            {
                return Task.CompletedTask;
            }

            var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _streamCallWaiters.Add((count, waiter));
            return waiter.Task;
        }
    }

    private void RecordStreamCall()
    {
        List<TaskCompletionSource>? ready = null;
        lock (_streamCallLock)
        {
            _streamCallCount++;
            for (var i = _streamCallWaiters.Count - 1; i >= 0; i--)
            {
                if (_streamCallWaiters[i].Count <= _streamCallCount)
                {
                    (ready ??= []).Add(_streamCallWaiters[i].Waiter);
                    _streamCallWaiters.RemoveAt(i);
                }
            }
        }

        if (ready is not null)
        {
            foreach (var waiter in ready)
            {
                waiter.TrySetResult();
            }
        }
    }

    /// <summary>
    /// Clears all queued events.
    /// </summary>
    public void ClearEvents()
    {
        lock (_events) { _events.Clear(); }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Take a snapshot and clear immediately so events are only yielded once,
        // even when the consumer breaks early (e.g., batch size limit).
        List<Either<EncinaError, ChangeEvent>> snapshot;
        lock (_events)
        {
            snapshot = [.. _events];
            _events.Clear();
        }

        RecordStreamCall();

        foreach (var evt in snapshot)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            _currentPosition++;
            yield return evt;
            await Task.Yield();
        }
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Either<EncinaError, CdcPosition>>(
            Right<EncinaError, CdcPosition>(new TestCdcPosition(_currentPosition)));
    }
}
