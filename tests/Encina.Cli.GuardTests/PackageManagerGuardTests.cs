using Encina.Cli.Services;
using FluentAssertions;
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
        // Act
        var act = () => PackageManager.AddPackagesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("packageNames");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task AddPackageAsync_InvalidPackageName_ThrowsArgumentException(string? packageName)
    {
        // Act
        var act = () => PackageManager.AddPackageAsync(packageName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemovePackageAsync_InvalidPackageName_ThrowsArgumentException(string? packageName)
    {
        // Act
        var act = () => PackageManager.RemovePackageAsync(packageName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
