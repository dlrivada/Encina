using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Runs integration tests with Docker containers for database testing.
/// Usage: dotnet run --file scripts/run-integration-tests.cs -- [--skip-docker] [--database sqlserver|postgres|mysql|oracle|all]
/// </summary>

var skipDocker = Array.Exists(args, arg => arg == "--skip-docker");
var database = GetArgument(args, "--database") ?? "all";
var verbose = Array.Exists(args, arg => arg == "--verbose");

try
{
    var repositoryRoot = FindRepositoryRoot();
    Environment.CurrentDirectory = repositoryRoot;

    if (!skipDocker)
    {
        Console.WriteLine("🐳 Starting Docker containers for database testing...\n");

        var services = database == "all"
            ? new[] { "sqlserver", "postgres", "mysql" } // Oracle takes too long, skip by default
            : new[] { database };

        foreach (var service in services)
        {
            Console.WriteLine($"📦 Starting {service}...");
            RunCommand("docker-compose", $"up -d {service}");
        }

        Console.WriteLine("\n⏳ Waiting for databases to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(15)); // Give databases time to initialize

        // Wait for health checks
        Console.WriteLine("🏥 Checking database health...");
        foreach (var service in services)
        {
            var healthy = await WaitForHealthy(service, TimeSpan.FromMinutes(2));
            if (!healthy)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  Warning: {service} health check timed out, continuing anyway...");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ {service} is healthy");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
    }

    Console.WriteLine("🧪 Running integration tests...\n");

    var testArguments = new System.Collections.Generic.List<string>
    {
        "test",
        "SimpleMediator.slnx",
        "--configuration",
        "Release",
        "--filter",
        "Category=Integration|FullyQualifiedName~.Dapper.|FullyQualifiedName~.ADO.",
        "--logger",
        "console;verbosity=normal"
    };

    if (verbose)
    {
        testArguments.Add("--verbosity");
        testArguments.Add("detailed");
    }

    RunCommand("dotnet", string.Join(" ", testArguments));

    Console.WriteLine("\n✅ Integration tests completed successfully!");

    if (!skipDocker)
    {
        Console.WriteLine("\n🛑 Stopping Docker containers...");
        RunCommand("docker-compose", "down");
    }

    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.ResetColor();

    if (!skipDocker)
    {
        Console.WriteLine("\n🛑 Cleaning up Docker containers...");
        try
        {
            RunCommand("docker-compose", "down");
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    return 1;
}

static async Task<bool> WaitForHealthy(string service, TimeSpan timeout)
{
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.Elapsed < timeout)
    {
        try
        {
            var result = RunCommandSilent("docker", $"inspect --format='{{{{.State.Health.Status}}}}' simplemediator-{service}");
            if (result?.Trim().Trim('\'') == "healthy")
                return true;
        }
        catch
        {
            // Container might not be running yet
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    return false;
}

static void RunCommand(string command, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = command,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = false,
        RedirectStandardError = false
    };

    using var process = Process.Start(psi);
    if (process == null)
        throw new InvalidOperationException($"Failed to start process: {command}");

    process.WaitForExit();

    if (process.ExitCode != 0)
        throw new InvalidOperationException($"Command '{command} {arguments}' failed with exit code {process.ExitCode}");
}

static string? RunCommandSilent(string command, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = command,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using var process = Process.Start(psi);
    if (process == null)
        return null;

    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();

    return process.ExitCode == 0 ? output : null;
}

static string? GetArgument(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name)
            return args[i + 1];
    }
    return null;
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "SimpleMediator.slnx");
        if (File.Exists(candidate))
            return directory.FullName;

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root containing SimpleMediator.slnx.");
}
