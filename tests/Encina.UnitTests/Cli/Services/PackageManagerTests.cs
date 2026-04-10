using Encina.Cli.Services;

using Shouldly;

using Xunit;

namespace Encina.UnitTests.Cli.Services;

/// <summary>
/// Unit tests for <see cref="PackageManager"/> covering result types and the project-lookup
/// branches that do NOT invoke the <c>dotnet</c> CLI subprocess.
/// </summary>
public class PackageManagerTests : IDisposable
{
    private readonly string _tempDir;

    public PackageManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-pm-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private static async Task<T> RunInDirectoryAsync<T>(string directory, Func<Task<T>> action)
    {
        var original = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(directory);
            return await action();
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
        }
    }

    // ─── PackageResult helpers ───

    [Fact]
    public void PackageResult_Ok_ReturnsSuccess()
    {
        var result = PackageResult.Ok();

        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void PackageResult_Error_ReturnsFailureWithMessage()
    {
        var result = PackageResult.Error("boom");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("boom");
    }

    // ─── InstalledPackage record ───

    [Fact]
    public void InstalledPackage_HoldsNameAndVersion()
    {
        var pkg = new InstalledPackage { Name = "Encina", Version = "1.0.0" };

        pkg.Name.ShouldBe("Encina");
        pkg.Version.ShouldBe("1.0.0");
    }

    // ─── AddPackagesAsync empty enumerable: short-circuits without subprocess ───

    [Fact]
    public async Task AddPackagesAsync_EmptyEnumerable_WithValidProject_ReturnsOk()
    {
        // Creating a csproj and switching CWD lets FindProjectFile succeed without
        // the subprocess loop running (empty enumerable → straight to Ok).
        var projectPath = Path.Combine(_tempDir, "Probe.csproj");
        await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var result = await RunInDirectoryAsync(_tempDir, () =>
            PackageManager.AddPackagesAsync(Array.Empty<string>()));

        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AddPackagesAsync_EmptyEnumerable_WithStartDirectory_ResolvesProject()
    {
        // AddPackagesAsync forwards the second argument as the *start directory* for the
        // csproj walk-up search, not the project file itself. An empty enumerable then
        // short-circuits to Ok without running any dotnet subprocess.
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Probe.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var result = await PackageManager.AddPackagesAsync(Array.Empty<string>(), _tempDir);

        result.Success.ShouldBeTrue();
    }
}
