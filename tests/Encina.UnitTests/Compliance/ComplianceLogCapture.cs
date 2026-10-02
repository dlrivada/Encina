using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Compliance;

/// <summary>
/// Logger that records every entry (level, formatted message, exception) so tests can assert what
/// reached the logs. Optionally signals a <see cref="TaskCompletionSource"/> when an entry matches
/// a predicate, which lets background-service tests wait without sleeping.
/// </summary>
/// <typeparam name="T">The category type.</typeparam>
internal sealed class ComplianceLogCapture<T> : ILogger<T>
{
    private readonly object _gate = new();
    private readonly List<Entry> _entries = [];
    private readonly Func<Entry, bool>? _signalWhen;
    private readonly TaskCompletionSource _signal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ComplianceLogCapture(Func<Entry, bool>? signalWhen = null)
    {
        _signalWhen = signalWhen;
    }

    /// <summary>Completes when an entry matched the predicate given to the constructor.</summary>
    public Task Signaled => _signal.Task;

    /// <summary>A snapshot of the entries recorded so far.</summary>
    public IReadOnlyList<Entry> Entries
    {
        get
        {
            lock (_gate)
            {
                return [.. _entries];
            }
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var entry = new Entry(logLevel, eventId, formatter(state, exception), exception);

        lock (_gate)
        {
            _entries.Add(entry);
        }

        if (_signalWhen?.Invoke(entry) == true)
        {
            _signal.TrySetResult();
        }
    }

    /// <summary>One recorded log entry.</summary>
    internal sealed record Entry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
}
