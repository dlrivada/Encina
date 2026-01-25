using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Runs integration tests with Docker containers for database testing.
/// Usage: dotnet run --file scripts/run-integration-tests.cs -- [options]
///
/// Options:
///   --skip-docker             Skip Docker container management
///   --database NAME           Run tests for a specific database (sqlserver|postgres|mysql|oracle|sqlite|all)
///   --provider PROVIDER       Run tests for a specific provider (ado|dapper|efcore|all)
///   --verbose                 Show detailed test output
///
/// Examples:
///   # Run all integration tests
///   dotnet run --file scripts/run-integration-tests.cs
///
///   # Run only EF Core PostgreSQL tests
///   dotnet run --file scripts/run-integration-tests.cs -- --provider efcore --database postgres
///
///   # Run all EF Core multi-provider tests
///   dotnet run --file scripts/run-integration-tests.cs -- --provider efcore --database all
///
///   # Run SQLite tests without Docker (in-process database)
///   dotnet run --file scripts/run-integration-tests.cs -- --database sqlite --skip-docker
/// </summary>

var skipDocker = Array.Exists(args, arg => arg == "--skip-docker");
var database = GetArgument(args, "--database") ?? "all";
var provider = GetArgument(args, "--provider") ?? "all";
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

    // Build the test filter based on provider and database options
    var filterParts = new System.Collections.Generic.List<string>();

    // Provider filter
    if (provider != "all")
    {
        var providerFilter = provider.ToLowerInvariant() switch
        {
            "efcore" => "FullyQualifiedName~EntityFrameworkCore",
            "dapper" => "FullyQualifiedName~.Dapper.",
            "ado" => "FullyQualifiedName~.ADO.",
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };
        filterParts.Add(providerFilter);
    }
    else
    {
        // Default: all integration tests
        filterParts.Add("Category=Integration|FullyQualifiedName~.Dapper.|FullyQualifiedName~.ADO.|FullyQualifiedName~EntityFrameworkCore");
    }

    // Database trait filter (for EF Core multi-provider tests)
    if (database != "all" && database.ToLowerInvariant() != "sqlite")
    {
        var databaseTrait = database.ToLowerInvariant() switch
        {
            "sqlserver" => "SqlServer",
            "postgres" => "PostgreSQL",
            "postgresql" => "PostgreSQL",
            "mysql" => "MySQL",
            "oracle" => "Oracle",
            _ => throw new ArgumentException($"Unknown database: {database}")
        };
        filterParts.Add($"Database={databaseTrait}");
    }
    else if (database.ToLowerInvariant() == "sqlite")
    {
        filterParts.Add("Database=Sqlite");
    }

    var filter = string.Join("&", filterParts);

    Console.WriteLine($"📋 Test filter: {filter}\n");

    var testArguments = new System.Collections.Generic.List<string>
    {
        "test",
        "Encina.slnx",
        "--configuration",
        "Release",
        "--filter",
        filter,
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
            var result = RunCommandSilent("docker", $"inspect --format='{{{{.State.Health.Status}}}}' encina-{service}");
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
        var candidate = Path.Combine(directory.FullName, "Encina.slnx");
        if (File.Exists(candidate))
            return directory.FullName;

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root containing Encina.slnx.");
}
