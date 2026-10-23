using System.Diagnostics;
using Microsoft.Testing.Platform.Builder;

namespace Encina.UnitTests.TestHost;

/// <summary>
/// Microsoft.Testing.Platform builder hook that registers <see cref="StrykerServerRecycler"/> during
/// Stryker mutation runs only. It is wired in by the <c>TestingPlatformBuilderHook</c> item of
/// <c>Encina.UnitTests.csproj</c>, which makes the generated <c>SelfRegisteredExtensions</c> call
/// <see cref="AddExtensions(ITestApplicationBuilder, string[])"/> whenever the test application starts
/// in MTP server mode.
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
        _ = args;
        AddExtensions(testApplicationBuilder, Environment.GetEnvironmentVariable);
    }

    /// <summary>
    /// Adds the recycler when the environment read through <paramref name="getEnvironmentVariable"/>
    /// marks a Stryker run.
    /// </summary>
    /// <param name="testApplicationBuilder">The MTP test application builder.</param>
    /// <param name="getEnvironmentVariable">Reads an environment variable.</param>
    /// <returns><see langword="true"/> when the recycler was registered.</returns>
    internal static bool AddExtensions(
        ITestApplicationBuilder testApplicationBuilder,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(testApplicationBuilder);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        if (!StrykerServerRecycler.IsMutationRun(getEnvironmentVariable))
        {
            return false;
        }

        var thresholdMb = StrykerServerRecycler.ParseThresholdMb(
            getEnvironmentVariable(StrykerServerRecycler.ThresholdVariable));
        var logFile = getEnvironmentVariable(StrykerServerRecycler.LogFileVariable);

        testApplicationBuilder.TestHost.AddTestSessionLifetimeHandle(_ => new StrykerServerRecycler(
            thresholdMb,
            StrykerServerRecycler.ProcessSessionCounter,
            ReadPrivateBytes,
            line => Report(line, logFile),
            TerminateProcess));
        return true;
    }

    private static long ReadPrivateBytes()
    {
        using var process = Process.GetCurrentProcess();
        return process.PrivateMemorySize64;
    }

    // Stryker 5.0.0 sends the test server's stdout and stderr to Stream.Null unless --log-to-file is
    // set, so the line also goes to a file when ENCINA_MTP_RECYCLE_LOG names one.
    private static void Report(string line, string? logFile)
    {
        Console.Error.WriteLine(line);
        Console.Error.Flush();

        if (string.IsNullOrWhiteSpace(logFile))
        {
            return;
        }

        try
        {
            File.AppendAllText(logFile, line + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"[encina-mtp-recycle] Could not append to {logFile}: {ex.GetType().Name}");
        }
    }

    // Kill rather than Environment.Exit: no ProcessExit handler can hang the exit, so Stryker always
    // sees the same abrupt host loss it already handles for a crashed or OOM-killed test server.
    private static void TerminateProcess()
    {
        using var process = Process.GetCurrentProcess();
        process.Kill();
    }
}
