using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Encina.Cli.Services;

/// <summary>
/// Result of a package operation.
/// </summary>
public sealed class PackageResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static PackageResult Ok() => new() { Success = true };
    public static PackageResult Error(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Service for managing NuGet packages in a project.
/// </summary>
public static partial class PackageManager
{
    public static async Task<PackageResult> AddPackagesAsync(IEnumerable<string> packageNames, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(packageNames);

        var project = FindProjectFile(projectPath);
        if (project is null)
        {
            return PackageResult.Error("No .csproj file found in the current directory or its parents.");
        }

        foreach (var packageName in packageNames)
        {
            var result = await AddPackageAsync(packageName, project);
            if (!result.Success)
            {
                return result;
            }
        }

        return PackageResult.Ok();
    }

    public static async Task<PackageResult> AddPackageAsync(string packageName, string? projectPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var project = projectPath ?? FindProjectFile(null);
        if (project is null)
        {
            return PackageResult.Error("No .csproj file found in the current directory or its parents.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"add \"{project}\" package {packageName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return PackageResult.Error("Failed to start dotnet process.");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var errorMessage = !string.IsNullOrWhiteSpace(error) ? error : output;
                return PackageResult.Error($"Failed to add package: {errorMessage.Trim()}");
            }

            return PackageResult.Ok();
        }
        catch (Exception ex)
        {
            return PackageResult.Error($"Error running dotnet command: {ex.Message}");
        }
    }

    public static async Task<PackageResult> RemovePackageAsync(string packageName, string? projectPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var project = projectPath ?? FindProjectFile(null);
        if (project is null)
        {
            return PackageResult.Error("No .csproj file found in the current directory or its parents.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"remove \"{project}\" package {packageName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return PackageResult.Error("Failed to start dotnet process.");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var errorMessage = !string.IsNullOrWhiteSpace(error) ? error : output;
                return PackageResult.Error($"Failed to remove package: {errorMessage.Trim()}");
            }

            return PackageResult.Ok();
        }
        catch (Exception ex)
        {
            return PackageResult.Error($"Error running dotnet command: {ex.Message}");
        }
    }

    public static async Task<IReadOnlyList<InstalledPackage>> ListPackagesAsync(string? projectPath = null)
    {
        var project = projectPath ?? FindProjectFile(null);
        if (project is null)
        {
            return [];
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"list \"{project}\" package",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return [];
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return [];
            }

            return ParsePackageList(output);
        }
        catch
        {
            return [];
        }
    }

    private static string? FindProjectFile(string? startPath)
    {
        var directory = startPath is not null
            ? new DirectoryInfo(startPath)
            : new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var projectFiles = directory.GetFiles("*.csproj");
            if (projectFiles.Length > 0)
            {
                return projectFiles[0].FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static List<InstalledPackage> ParsePackageList(string output)
    {
        var packages = new List<InstalledPackage>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var match = PackageLineRegex().Match(line);
            if (match.Success)
            {
                packages.Add(new InstalledPackage
                {
                    Name = match.Groups["name"].Value.Trim(),
                    Version = match.Groups["version"].Value.Trim()
                });
            }
        }

        return packages;
    }

    [GeneratedRegex(@">\s+(?<name>\S+)\s+(?<version>\S+)", RegexOptions.Compiled)]
    private static partial Regex PackageLineRegex();
}

/// <summary>
/// Represents an installed NuGet package.
/// </summary>
public sealed class InstalledPackage
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}
