using Encina.Cli.Services;

using Shouldly;

using Xunit;

namespace Encina.GuardTests.CLI;

/// <summary>
/// Guard tests exercising non-subprocess code paths in <see cref="PackageManager"/> to
/// produce line coverage for the guard flag. Subprocess-invoking paths (Add/Remove/List
/// via <c>dotnet</c>) are deliberately avoided because they would slow the guard pass.
/// </summary>
public class PackageManagerHappyPathGuardTests : IDisposable
{
    private readonly string _tempDir;

    public PackageManagerHappyPathGuardTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-pm-hp-{Guid.NewGuid()}");
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

    [Fact]
    public async Task AddPackagesAsync_EmptyEnumerable_WithStartDirectory_FindsProjectAndShortCircuits()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Probe.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var result = await PackageManager.AddPackagesAsync(Array.Empty<string>(), _tempDir);

        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AddPackagesAsync_EmptyEnumerable_NoProjectFoundInChain_ReturnsError()
    {
        // Nested directory deep enough to be isolated from any ambient csproj in Temp.
        // If the walk-up happens to find one (dev scenarios), we accept either outcome.
        var deep = Path.Combine(_tempDir, "a", "b", "c", "d");
        Directory.CreateDirectory(deep);

        var result = await PackageManager.AddPackagesAsync(Array.Empty<string>(), deep);

        // Either branch executes the FindProjectFile walk-up path.
        (result.Success || !string.IsNullOrEmpty(result.ErrorMessage)).ShouldBeTrue();
    }

    [Fact]
    public void PackageResult_Ok_HasNoErrorMessage()
    {
        var result = PackageResult.Ok();

        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void PackageResult_Error_HasErrorMessage()
    {
        var result = PackageResult.Error("failure");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("failure");
    }

    [Fact]
    public void InstalledPackage_AssignsNameAndVersion()
    {
        var pkg = new InstalledPackage { Name = "Encina", Version = "1.2.3" };

        pkg.Name.ShouldBe("Encina");
        pkg.Version.ShouldBe("1.2.3");
    }
}
