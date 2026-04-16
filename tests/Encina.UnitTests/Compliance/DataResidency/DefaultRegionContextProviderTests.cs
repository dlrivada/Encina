using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataResidency;

public class DefaultRegionContextProviderTests
{
    private readonly ILogger<DefaultRegionContextProvider> _logger = Substitute.For<ILogger<DefaultRegionContextProvider>>();

    [Fact]
    public async Task GetCurrentRegionAsync_WhenDefaultRegionConfigured_ShouldReturnRegion()
    {
        // Arrange
        var options = new DataResidencyOptions { DefaultRegion = RegionRegistry.DE };
        var optionsMock = Substitute.For<IOptions<DataResidencyOptions>>();
        optionsMock.Value.Returns(options);

        var sut = new DefaultRegionContextProvider(optionsMock, _logger);

        // Act
        var result = await sut.GetCurrentRegionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var region = result.Match(Right: r => r, Left: _ => default!);
        region.ShouldBe(RegionRegistry.DE);
    }

    [Fact]
    public async Task GetCurrentRegionAsync_WhenNoDefaultRegion_ShouldReturnError()
    {
        // Arrange
        var options = new DataResidencyOptions { DefaultRegion = null };
        var optionsMock = Substitute.For<IOptions<DataResidencyOptions>>();
        optionsMock.Value.Returns(options);

        var sut = new DefaultRegionContextProvider(optionsMock, _logger);

        // Act
        var result = await sut.GetCurrentRegionAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRegionContextProvider(null!, _logger);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRegionContextProvider(
            Substitute.For<IOptions<DataResidencyOptions>>(), null!);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
