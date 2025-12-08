using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

var extraArguments = Environment.GetCommandLineArgs().Skip(1).ToArray();

var runArguments = new List<string>
{
    "run",
    "--configuration",
    "Release",
    "--project",
    "load/SimpleMediator.LoadTests/SimpleMediator.LoadTests.csproj"
};

if (extraArguments.Length > 0)
{
    runArguments.Add("--");
    runArguments.AddRange(extraArguments.Select(Quote));
}

var psi = new ProcessStartInfo("dotnet", string.Join(' ', runArguments))
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};

Console.WriteLine("Starting load harness...");

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

Console.WriteLine("Load harness completed successfully.");

var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summaryFile))
{
    using var writer = File.AppendText(summaryFile);
    writer.WriteLine("### Load Harness Run");
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
