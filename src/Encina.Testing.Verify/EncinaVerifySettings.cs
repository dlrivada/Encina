using System.Text;
using System.Text.RegularExpressions;

namespace Encina.Testing.Verify;

/// <summary>
/// Provides initialization and configuration for Verify snapshot testing with Encina types.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="Initialize"/> once in your test project (typically in a ModuleInitializer)
/// to configure Verify with Encina-specific scrubbers and converters.
/// </para>
/// <para>
/// <b>Automatic Scrubbing</b>:
/// <list type="bullet">
/// <item><description>UTC timestamps are replaced with <c>[TIMESTAMP]</c></description></item>
/// <item><description>GUIDs are replaced with <c>Guid_N</c> where N is a sequential number</description></item>
/// <item><description>Stack traces are removed from error snapshots</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your test project
/// [ModuleInitializer]
/// public static void Initialize()
/// {
///     EncinaVerifySettings.Initialize();
/// }
/// </code>
/// </example>
public static partial class EncinaVerifySettings
{
    private static bool _initialized;
    private static readonly Lock InitLock = new();

    /// <summary>
    /// Gets a value indicating whether the settings have been initialized.
    /// </summary>
    /// <remarks>This property is internal for test verification purposes.</remarks>
    internal static bool IsInitialized => _initialized;

    /// <summary>
    /// Initializes Verify settings with Encina-specific scrubbers and converters.
    /// </summary>
    /// <remarks>
    /// This method is idempotent and can be called multiple times safely.
    /// </remarks>
    public static void Initialize()
    {
        lock (InitLock)
        {
            if (_initialized)
            {
                return;
            }

            ConfigureScrubbers();
            ConfigureConverters();

            _initialized = true;
        }
    }

    private static void ConfigureScrubbers()
    {
        // Scrub UTC timestamps (ISO 8601 format)
        VerifierSettings.AddScrubber(ScrubTimestamps);

        // Scrub stack traces
        VerifierSettings.AddScrubber(ScrubStackTraces);

        // Scrub common Encina timestamp property names
        VerifierSettings.ScrubMember("CreatedAtUtc");
        VerifierSettings.ScrubMember("ProcessedAtUtc");
        VerifierSettings.ScrubMember("ReceivedAtUtc");
        VerifierSettings.ScrubMember("ScheduledAtUtc");
        VerifierSettings.ScrubMember("StartedAtUtc");
        VerifierSettings.ScrubMember("CompletedAtUtc");
        VerifierSettings.ScrubMember("LastUpdatedAtUtc");
        VerifierSettings.ScrubMember("ExpiresAtUtc");
        VerifierSettings.ScrubMember("NextRetryAtUtc");
        VerifierSettings.ScrubMember("DeadLetteredAtUtc");
        VerifierSettings.ScrubMember("FirstFailedAtUtc");
        VerifierSettings.ScrubMember("ReplayedAtUtc");
        VerifierSettings.ScrubMember("LastExecutedAtUtc");
        VerifierSettings.ScrubMember("TimeoutAtUtc");
    }

    private static void ConfigureConverters()
    {
        // Configure EncinaError to show only relevant information
        VerifierSettings.AddExtraSettings(settings =>
        {
            settings.Converters.Add(new EncinaErrorConverter());
        });

        // Don't ignore default values for EventSnapshot.Index to ensure consistent output
        VerifierSettings.DontIgnoreEmptyCollections();
        VerifierSettings.MemberConverter<EventSnapshot, int>(
            x => x.Index,
            (_, value) => value);
    }

    private static void ScrubTimestamps(StringBuilder builder)
    {
        // Match ISO 8601 timestamps: 2025-12-28T10:30:45.123Z or similar
        var result = TimestampRegex().Replace(builder.ToString(), "[TIMESTAMP]");
        builder.Clear();
        builder.Append(result);
    }

    private static void ScrubStackTraces(StringBuilder builder)
    {
        // Remove stack trace lines that start with "   at "
        var lines = builder.ToString().Split([Environment.NewLine], StringSplitOptions.None);
        var filteredLines = lines.Where(line => !line.TrimStart().StartsWith("at ", StringComparison.Ordinal));

        builder.Clear();
        builder.Append(string.Join(Environment.NewLine, filteredLines));
    }

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?")]
    private static partial Regex TimestampRegex();
}
