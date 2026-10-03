using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

try
{
    var (configuration, passThrough) = ParseArguments(args);
    var repositoryRoot = FindRepositoryRoot();
    Environment.CurrentDirectory = repositoryRoot;

    Console.WriteLine("Restoring dotnet tools...");
    RunOrThrow("dotnet", ["tool", "restore"]);

    Console.WriteLine($"Executing Stryker mutation analysis (build configuration: {configuration ?? "project default"})...");

    // No --log-to-file by default: file logging is always trace level, and
    // under the MTP runner it also writes a JSON-RPC log per test server that
    // grows by about 40 MB for every run of the ~22,000 Encina.UnitTests
    // tests, so a 150-mutant shard would write gigabytes. Pass it through
    // (`-- --log-to-file`) for a short diagnostic run.
    var outputPath = Path.Combine(repositoryRoot, "artifacts", "mutation");
    var strykerArguments = new List<string>
    {
        "tool",
        "run",
        "dotnet-stryker",
        "--config-file",
        Path.Combine(repositoryRoot, ".github/stryker-config.json"),
        "--output",
        outputPath,
        "--verbosity",
        "info"
    };

    // Without -c/--configuration Stryker builds the project's default
    // configuration (Debug), which the workflow's Build step pre-builds.
    if (configuration is not null)
    {
        strykerArguments.Add("--configuration");
        strykerArguments.Add(configuration);
    }

    if (passThrough.Count > 0)
    {
        Console.WriteLine($"Forwarding extra arguments to Stryker: {string.Join(' ', passThrough)}");
        strykerArguments.AddRange(passThrough);
    }

    // Stryker runs in project mode from the Encina.UnitTests directory: the
    // config names only the project under test (Encina.csproj) and the
    // Microsoft Testing Platform runner, which ignores both `test-projects`
    // and `test-case-filter`. Run from the repository root (solution mode),
    // the MTP runner starts every test project of the solution, including
    // IntegrationTests and benchmark executables, and the initial test run
    // fails (#1087, #1441).
    var testProjectDirectory = Path.Combine(repositoryRoot, "tests", "Encina.UnitTests");
    Console.WriteLine($"Stryker working directory: {testProjectDirectory}");
    var strykerExitCode = Run("dotnet", strykerArguments, testProjectDirectory);

    // Exit code 2 = break threshold hit (score below configured minimum).
    // The mutation report was still generated — let downstream steps consume it.
    if (strykerExitCode == 2)
    {
        Console.WriteLine($"Stryker finished with mutation score below break threshold (exit code {strykerExitCode}).");
    }
    else if (strykerExitCode != 0)
    {
        throw new InvalidOperationException(
            $"Stryker failed with exit code {strykerExitCode}.");
    }
    else
    {
        Console.WriteLine("Stryker run completed successfully.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}

static (string? Configuration, IReadOnlyList<string> PassThrough) ParseArguments(string[] rawArgs)
{
    string? configuration = null;
    var passThrough = new List<string>();

    var index = 0;
    while (index < rawArgs.Length)
    {
        var current = rawArgs[index];

        if (current is "-c" or "--configuration")
        {
            index++;
            if (index >= rawArgs.Length)
            {
                throw new ArgumentException("Missing value for --configuration.");
            }

            configuration = rawArgs[index];
            index++;
            continue;
        }

        if (current == "--")
        {
            for (var passthroughIndex = index + 1; passthroughIndex < rawArgs.Length; passthroughIndex++)
            {
                passThrough.Add(rawArgs[passthroughIndex]);
            }

            break;
        }

        passThrough.Add(current);
        index++;
    }

    return (configuration, passThrough);
}

static void RunOrThrow(string fileName, IReadOnlyList<string> arguments)
{
    var exitCode = Run(fileName, arguments);
    if (exitCode != 0)
    {
        throw new InvalidOperationException($"Command '{fileName} {string.Join(' ', arguments)}' failed with exit code {exitCode}.");
    }
}

static int Run(string fileName, IReadOnlyList<string> arguments, string? workingDirectory = null)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
        UseShellExecute = false
    };

    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start process '{fileName}'.");
    process.WaitForExit();

    return process.ExitCode;
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "Encina.slnx");
        if (File.Exists(candidate))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root containing Encina.slnx.");
}
