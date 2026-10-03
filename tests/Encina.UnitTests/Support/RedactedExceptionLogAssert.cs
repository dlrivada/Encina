using Encina.Diagnostics;

using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Support;

/// <summary>
/// Assertions over a <see cref="FakeLogger"/>: every exception handed to the logger is a
/// <see cref="RedactedException"/> and no rendered log line or exception text carries a sentinel
/// message (AGENTS.md section 3, #1557).
/// </summary>
internal static class RedactedExceptionLogAssert
{
    /// <summary>
    /// Asserts that at least one log record carries an exception, that every logged exception is
    /// redacted, and that <paramref name="sentinel"/> appears nowhere in the logs.
    /// </summary>
    public static void LoggedOnlyRedacted(FakeLogger logger, string sentinel)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var records = logger.Collector.GetSnapshot();
        var withException = records.Where(r => r.Exception is not null).ToList();

        withException.ShouldNotBeEmpty("the failure must be logged with its exception");
        withException.ShouldAllBe(r => r.Exception is RedactedException);

        foreach (var record in records)
        {
            record.Message.ShouldNotContain(sentinel);
            record.Exception?.Message.ShouldNotContain(sentinel);
            record.Exception?.ToString().ShouldNotContain(sentinel);
        }
    }

    /// <summary>
    /// Asserts that no log record carries a raw (non-redacted) exception and that
    /// <paramref name="sentinel"/> appears nowhere in the logs.
    /// </summary>
    public static void NeverLoggedRaw(FakeLogger logger, string sentinel)
    {
        ArgumentNullException.ThrowIfNull(logger);

        foreach (var record in logger.Collector.GetSnapshot())
        {
            if (record.Exception is not null)
            {
                record.Exception.ShouldBeOfType<RedactedException>();
            }

            record.Message.ShouldNotContain(sentinel);
            record.Exception?.ToString().ShouldNotContain(sentinel);
        }
    }
}
