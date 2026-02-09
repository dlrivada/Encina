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
