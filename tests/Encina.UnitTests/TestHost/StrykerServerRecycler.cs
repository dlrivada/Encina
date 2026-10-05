using System.Globalization;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.TestHost;

namespace Encina.UnitTests.TestHost;

/// <summary>
/// Ends the MTP test server at the start of a test session when its private memory is above a
/// threshold, so Stryker discards the server and reruns the same mutant on a fresh one.
/// Registered by <see cref="StrykerServerRecycleBuilderHook"/> during Stryker runs only.
/// </summary>
/// <remarks>
/// <para>
/// Stryker.NET 5.0.0 retries a test run once on a new server when the host process exits during the
/// run (<c>MicrosoftTestingPlatformRunner.RunAssemblyTestsInternalAsync</c>, <c>maxRunAttempts = 2</c>).
/// Exiting at session start, before any test runs, means the first attempt reports nothing and the
/// second attempt produces the mutant's verdict.
/// </para>
/// <para>
/// The first session of a process never recycles. A fresh server is therefore never ended by this
/// handler, so the retry attempt cannot fail because of it and turn the mutant into a RuntimeError.
/// </para>
/// <para>
/// Private memory is <see cref="System.Diagnostics.Process.PrivateMemorySize64"/>: private bytes on
/// Windows and <c>VmData</c> on Linux, the metric of the phase 2e measurements.
/// </para>
/// </remarks>
internal sealed class StrykerServerRecycler : ITestSessionLifetimeHandler
{
    /// <summary>The variable Stryker sets on every test server it starts.</summary>
    public const string MutantFileVariable = "STRYKER_MUTANT_FILE";

    /// <summary>The variable that overrides the private-memory threshold, in megabytes.</summary>
    public const string ThresholdVariable = "ENCINA_MTP_RECYCLE_MB";

    /// <summary>The variable that names a file the recycle line is also appended to.</summary>
    public const string LogFileVariable = "ENCINA_MTP_RECYCLE_LOG";

    /// <summary>The default private-memory threshold: 6 GB.</summary>
    public const long DefaultThresholdMb = 6144;

    private const long BytesPerMb = 1024L * 1024L;

    /// <summary>The largest threshold whose byte count fits in a <see cref="long"/>.</summary>
    internal const long MaxThresholdMb = long.MaxValue / BytesPerMb;

    private readonly long _thresholdMb;
    private readonly SessionCounter _sessions;
    private readonly Func<long> _readPrivateBytes;
    private readonly Action<string> _report;
    private readonly Action _terminate;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrykerServerRecycler"/> class.
    /// </summary>
    /// <param name="thresholdMb">The private-memory threshold in megabytes.</param>
    /// <param name="sessions">The per-process session counter.</param>
    /// <param name="readPrivateBytes">Reads the current private memory of the process, in bytes.</param>
    /// <param name="report">Receives the recycle line (stderr and the optional log file in production).</param>
    /// <param name="terminate">Ends the process.</param>
    public StrykerServerRecycler(
        long thresholdMb,
        SessionCounter sessions,
        Func<long> readPrivateBytes,
        Action<string> report,
        Action terminate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thresholdMb);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(thresholdMb, MaxThresholdMb);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(readPrivateBytes);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(terminate);

        _thresholdMb = thresholdMb;
        _sessions = sessions;
        _readPrivateBytes = readPrivateBytes;
        _report = report;
        _terminate = terminate;
    }

    /// <summary>
    /// The session counter of this process. MTP may build a new handler for each server request, so
    /// the count lives outside the handler instance.
    /// </summary>
    public static SessionCounter ProcessSessionCounter { get; } = new();

    /// <inheritdoc />
    public string Uid => nameof(StrykerServerRecycler);

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string DisplayName => "Encina Stryker test server recycler";

    /// <inheritdoc />
    public string Description =>
        "Ends the test server at session start above a private-memory threshold during Stryker mutation runs.";

    /// <summary>
    /// Returns whether the process runs under Stryker, that is whether <c>STRYKER_MUTANT_FILE</c> is set.
    /// </summary>
    /// <param name="getEnvironmentVariable">Reads an environment variable.</param>
    /// <returns><see langword="true"/> when the variable has a non-empty value.</returns>
    public static bool IsMutationRun(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        return !string.IsNullOrWhiteSpace(getEnvironmentVariable(MutantFileVariable));
    }

    /// <summary>
    /// Parses the threshold variable. A missing, non-numeric or non-positive value gives the default;
    /// a value too large to express in bytes is capped at <see cref="MaxThresholdMb"/>, which never
    /// recycles in practice.
    /// </summary>
    /// <param name="value">The raw value of <c>ENCINA_MTP_RECYCLE_MB</c>.</param>
    /// <returns>The threshold in megabytes.</returns>
    public static long ParseThresholdMb(string? value)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mb) || mb <= 0)
        {
            return DefaultThresholdMb;
        }

        return Math.Min(mb, MaxThresholdMb);
    }

    /// <summary>
    /// Returns whether a session should end the process: never for the first session of a process,
    /// otherwise when private memory is strictly above the threshold.
    /// </summary>
    /// <param name="sessionNumber">The 1-based number of the session in this process.</param>
    /// <param name="privateBytes">The current private memory, in bytes.</param>
    /// <param name="thresholdMb">The threshold, in megabytes, at most <see cref="MaxThresholdMb"/>.</param>
    /// <returns><see langword="true"/> when the process should be recycled.</returns>
    public static bool ShouldRecycle(int sessionNumber, long privateBytes, long thresholdMb) =>
        sessionNumber > 1 && privateBytes > Math.Min(thresholdMb, MaxThresholdMb) * BytesPerMb;

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    /// <inheritdoc />
    public Task OnTestSessionStartingAsync(SessionUid sessionUid, CancellationToken cancellationToken)
    {
        var sessionNumber = _sessions.Increment();
        var privateBytes = _readPrivateBytes();

        if (ShouldRecycle(sessionNumber, privateBytes, _thresholdMb))
        {
            _report(string.Create(
                CultureInfo.InvariantCulture,
                $"[encina-mtp-recycle] Test server private memory is {privateBytes / BytesPerMb} MB, above the {_thresholdMb} MB threshold ({ThresholdVariable}), at the start of session {sessionNumber}; exiting so Stryker reruns this mutant on a fresh test server."));
            _terminate();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnTestSessionFinishingAsync(SessionUid sessionUid, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <summary>
    /// Thread-safe count of the test sessions started in one process.
    /// </summary>
    internal sealed class SessionCounter
    {
        private int _count;

        /// <summary>Increments the count and returns the new value.</summary>
        /// <returns>The 1-based number of the session that is starting.</returns>
        public int Increment() => Interlocked.Increment(ref _count);
    }
}
