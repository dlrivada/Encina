using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Validation.Endpoints;

/// <summary>
/// Logger provider that records the event id and level of every entry, used to assert that the
/// endpoint validation opt-out warnings are logged exactly once per options instance (#852).
/// </summary>
internal sealed class CollectingLoggerProvider : ILoggerProvider
{
    private readonly List<(int EventId, LogLevel Level)> _entries = [];

    public int Count(int eventId, LogLevel level)
    {
        lock (_entries)
        {
            return _entries.Count(e => e.EventId == eventId && e.Level == level);
        }
    }

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(_entries);

    public void Dispose()
    {
    }

    private sealed class CollectingLogger(List<(int EventId, LogLevel Level)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (entries)
            {
                entries.Add((eventId.Id, logLevel));
            }
        }
    }
}
