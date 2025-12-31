using Encina.Cli.Services;
using Shouldly;
using Xunit;

namespace Encina.Cli.GuardTests;

/// <summary>
/// Guard clause tests for PackageManager methods.
/// </summary>
public class PackageManagerGuardTests
{
    [Fact]
    public async Task AddPackagesAsync_NullPackageNames_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => PackageManager.AddPackagesAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("packageNames");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task AddPackageAsync_InvalidPackageName_ThrowsArgumentException(string? packageName)
    {
        // Act & Assert
        var act = () => PackageManager.AddPackageAsync(packageName!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("packageName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemovePackageAsync_InvalidPackageName_ThrowsArgumentException(string? packageName)
    {
        // Act & Assert
        var act = () => PackageManager.RemovePackageAsync(packageName!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("packageName");
    }
}
