using System.Diagnostics;
using System.Globalization;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.TestHost;

namespace Encina.UnitTests.TestHost;

/// <summary>
/// Microsoft.Testing.Platform builder hook that registers <see cref="StrykerServerRecycler"/> during
/// Stryker mutation runs only. It is wired in by the <c>TestingPlatformBuilderHook</c> item of
/// <c>Encina.UnitTests.csproj</c>, which makes the generated <c>SelfRegisteredExtensions</c> call
/// <see cref="AddExtensions"/> whenever the test application starts in MTP server mode.
/// </summary>
/// <remarks>
/// Why it exists: Stryker.NET 5.0.0 reuses one MTP test server for every mutant and the unit suite
/// leaves native memory behind on each run, so the server eventually hits the job's memory cap
/// (#1441 phase 2e, <c>docs/engineering/stryker-5-mtp-spike-1087.md</c> section 6). Remove this hook
/// once Stryker can recycle the server itself (stryker-mutator/stryker-net#3742).
/// </remarks>
internal static class StrykerServerRecycleBuilderHook
{
    /// <summary>
    /// Adds the recycler as a test session lifetime handler when <c>STRYKER_MUTANT_FILE</c> is set.
    /// Normal test runs, which never set that variable, are left untouched.
    /// </summary>
    /// <param name="testApplicationBuilder">The MTP test application builder.</param>
    /// <param name="args">The command-line arguments of the test application (unused).</param>
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] args)
    {
        ArgumentNullException.ThrowIfNull(testApplicationBuilder);
        _ = args;

        if (!StrykerServerRecycler.IsMutationRun(Environment.GetEnvironmentVariable))
        {
            return;
        }

        var thresholdMb = StrykerServerRecycler.ParseThresholdMb(
            Environment.GetEnvironmentVariable(StrykerServerRecycler.ThresholdVariable));

        testApplicationBuilder.TestHost.AddTestSessionLifetimeHandle(_ => new StrykerServerRecycler(
            thresholdMb,
            StrykerServerRecycler.ProcessSessionCounter,
            ReadPrivateBytes,
            Console.Error,
            TerminateProcess));
    }

    private static long ReadPrivateBytes()
    {
        using var process = Process.GetCurrentProcess();
        return process.PrivateMemorySize64;
    }

    // Kill rather than Environment.Exit: no ProcessExit handler can hang the exit, so Stryker always
    // sees the same abrupt host loss it already handles for a crashed or OOM-killed test server.
    private static void TerminateProcess()
    {
        using var process = Process.GetCurrentProcess();
        process.Kill();
    }
}

/// <summary>
/// Ends the MTP test server at the start of a test session when its private memory is above a
/// threshold, so Stryker discards the server and reruns the same mutant on a fresh one.
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
/// </remarks>
internal sealed class StrykerServerRecycler : ITestSessionLifetimeHandler
{
    /// <summary>The variable Stryker sets on every test server it starts.</summary>
    public const string MutantFileVariable = "STRYKER_MUTANT_FILE";

    /// <summary>The variable that overrides the private-memory threshold, in megabytes.</summary>
    public const string ThresholdVariable = "ENCINA_MTP_RECYCLE_MB";

    /// <summary>The default private-memory threshold: 6 GB.</summary>
    public const long DefaultThresholdMb = 6144;

    private const long BytesPerMb = 1024L * 1024L;

    private readonly long _thresholdMb;
    private readonly SessionCounter _sessions;
    private readonly Func<long> _readPrivateBytes;
    private readonly TextWriter _error;
    private readonly Action _terminate;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrykerServerRecycler"/> class.
    /// </summary>
    /// <param name="thresholdMb">The private-memory threshold in megabytes.</param>
    /// <param name="sessions">The per-process session counter.</param>
    /// <param name="readPrivateBytes">Reads the current private memory of the process, in bytes.</param>
    /// <param name="error">The writer that receives the recycle line (stderr in production).</param>
    /// <param name="terminate">Ends the process.</param>
    public StrykerServerRecycler(
        long thresholdMb,
        SessionCounter sessions,
        Func<long> readPrivateBytes,
        TextWriter error,
        Action terminate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thresholdMb);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(readPrivateBytes);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(terminate);

        _thresholdMb = thresholdMb;
        _sessions = sessions;
        _readPrivateBytes = readPrivateBytes;
        _error = error;
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
    /// Parses the threshold variable. A missing, non-numeric or non-positive value gives the default.
    /// </summary>
    /// <param name="value">The raw value of <c>ENCINA_MTP_RECYCLE_MB</c>.</param>
    /// <returns>The threshold in megabytes.</returns>
    public static long ParseThresholdMb(string? value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mb) && mb > 0
            ? mb
            : DefaultThresholdMb;

    /// <summary>
    /// Returns whether a session should end the process: never for the first session of a process,
    /// otherwise when private memory is strictly above the threshold.
    /// </summary>
    /// <param name="sessionNumber">The 1-based number of the session in this process.</param>
    /// <param name="privateBytes">The current private memory, in bytes.</param>
    /// <param name="thresholdMb">The threshold, in megabytes.</param>
    /// <returns><see langword="true"/> when the process should be recycled.</returns>
    public static bool ShouldRecycle(int sessionNumber, long privateBytes, long thresholdMb) =>
        sessionNumber > 1 && privateBytes > thresholdMb * BytesPerMb;

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    /// <inheritdoc />
    public Task OnTestSessionStartingAsync(SessionUid sessionUid, CancellationToken cancellationToken)
    {
        var sessionNumber = _sessions.Increment();
        var privateBytes = _readPrivateBytes();

        if (ShouldRecycle(sessionNumber, privateBytes, _thresholdMb))
        {
            _error.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"[encina-mtp-recycle] Test server private memory is {privateBytes / BytesPerMb} MB, above the {_thresholdMb} MB threshold ({ThresholdVariable}), at the start of session {sessionNumber}; exiting so Stryker reruns this mutant on a fresh test server."));
            _error.Flush();
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
