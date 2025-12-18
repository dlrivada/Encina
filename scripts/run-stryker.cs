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

    Console.WriteLine($"Executing Stryker mutation analysis (build configuration: {configuration})...");

    var outputPath = Path.Combine(repositoryRoot, "artifacts", "mutation");
    var strykerArguments = new List<string>
    {
        "tool",
        "run",
        "dotnet-stryker",
        "--config-file",
        Path.Combine(repositoryRoot, "stryker-config.json"),
        "--output",
        outputPath,
        "--verbosity",
        "info"
    };

    if (passThrough.Count > 0)
    {
        Console.WriteLine($"Forwarding extra arguments to Stryker: {string.Join(' ', passThrough)}");
        strykerArguments.AddRange(passThrough);
    }

    RunOrThrow("dotnet", strykerArguments);
    Console.WriteLine("Stryker run completed successfully.");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}

static (string Configuration, IReadOnlyList<string> PassThrough) ParseArguments(string[] rawArgs)
{
    var configuration = "Release";
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
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
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

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Command '{fileName} {string.Join(' ', arguments)}' failed with exit code {process.ExitCode}.");
    }
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "SimpleMediator.slnx");
        if (File.Exists(candidate))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root containing SimpleMediator.slnx.");
}
