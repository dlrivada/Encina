using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

var extraArguments = Environment.GetCommandLineArgs().Skip(1).ToArray();

var forwardedArguments = new List<string>();
var runNbomber = false;
string? nbomberAlias = null;

for (var index = 0; index < extraArguments.Length; index++)
{
    var current = extraArguments[index];
    if (string.Equals(current, "--nbomber", StringComparison.OrdinalIgnoreCase))
    {
        runNbomber = true;

        if (index + 1 < extraArguments.Length && !extraArguments[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            nbomberAlias = extraArguments[++index];
        }

        continue;
    }

    forwardedArguments.Add(current);
}

var projectPath = runNbomber
    ? "load/Encina.NBomber/Encina.NBomber.csproj"
    : "load/Encina.LoadTests/Encina.LoadTests.csproj";

if (runNbomber && !ContainsOption(forwardedArguments, "--profile") && !string.IsNullOrEmpty(nbomberAlias))
{
    var profilePath = Path.Combine("load", "profiles", $"nbomber.{nbomberAlias}.json");
    if (File.Exists(profilePath))
    {
        forwardedArguments.Insert(0, profilePath);
        forwardedArguments.Insert(0, "--profile");
    }
    else
    {
        Console.Error.WriteLine($"NBomber profile alias '{nbomberAlias}' not found at {profilePath}. Continuing without it.");
    }
}

var runArguments = new List<string>
{
    "run",
    "--configuration",
    "Release",
    "--project",
    projectPath
};

if (forwardedArguments.Count > 0)
{
    runArguments.Add("--");
    runArguments.AddRange(forwardedArguments.Select(Quote));
}

var psi = new ProcessStartInfo("dotnet", string.Join(' ', runArguments))
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};

Console.WriteLine(runNbomber ? "Starting NBomber harness..." : "Starting load harness...");

using var process = Process.Start(psi);
if (process is null)
{
    Console.Error.WriteLine("Failed to start load harness process.");
    Environment.Exit(1);
}

var stdOut = new List<string>();
var stdErr = new List<string>();

process.OutputDataReceived += (_, data) =>
{
    if (data.Data is not null)
    {
        stdOut.Add(data.Data);
        Console.WriteLine(data.Data);
    }
};
process.ErrorDataReceived += (_, data) =>
{
    if (data.Data is not null)
    {
        stdErr.Add(data.Data);
        Console.Error.WriteLine(data.Data);
    }
};

process.BeginOutputReadLine();
process.BeginErrorReadLine();
process.WaitForExit();

if (process.ExitCode != 0)
{
    Console.Error.WriteLine($"Load harness failed with exit code {process.ExitCode}.");
    Environment.Exit(process.ExitCode);
}

Console.WriteLine(runNbomber ? "NBomber harness completed successfully." : "Load harness completed successfully.");

var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summaryFile))
{
    using var writer = File.AppendText(summaryFile);
    writer.WriteLine(runNbomber ? "### NBomber Harness Run" : "### Load Harness Run");
    writer.WriteLine();
    writer.WriteLine("```");
    foreach (var line in stdOut.TakeLast(50))
    {
        writer.WriteLine(line);
    }
    writer.WriteLine("```");
    writer.WriteLine();
}

static string Quote(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    return value.Contains(' ') ? $"\"{value}\"" : value;
}

static bool ContainsOption(IEnumerable<string> values, string option)
    => values.Any(value => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
