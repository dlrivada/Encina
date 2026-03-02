using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Attributes;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataResidency;

public class DefaultRegionRouterTests
{
    private readonly IDataResidencyPolicy _policy = Substitute.For<IDataResidencyPolicy>();
    private readonly IRegionContextProvider _contextProvider = Substitute.For<IRegionContextProvider>();
    private readonly ILogger<DefaultRegionRouter> _logger = Substitute.For<ILogger<DefaultRegionRouter>>();
    private readonly DefaultRegionRouter _sut;

    public DefaultRegionRouterTests()
    {
        _sut = new DefaultRegionRouter(_policy, _contextProvider, _logger);
    }

    [Fact]
    public async Task DetermineTargetRegionAsync_WhenCurrentRegionIsAllowed_ShouldReturnCurrentRegion()
    {
        // Arrange
        _contextProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Region>>(
                Right<EncinaError, Region>(RegionRegistry.DE)));

        _policy.IsAllowedAsync("personal-data", RegionRegistry.DE, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(
                Right<EncinaError, bool>(true)));

        var request = new TestDataResidencyRequest();

        // Act
        var result = await _sut.DetermineTargetRegionAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: region => region.Should().Be(RegionRegistry.DE),
            Left: _ => { });
    }

    [Fact]
    public async Task DetermineTargetRegionAsync_WhenRegionResolutionFails_ShouldReturnError()
    {
        // Arrange
        var error = DataResidencyErrors.RegionNotResolved("No default configured");
        _contextProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Region>>(
                Left<EncinaError, Region>(error)));

        var request = new TestDataResidencyRequest();

        // Act
        var result = await _sut.DetermineTargetRegionAsync(request);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullPolicy_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(null!, _contextProvider, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("residencyPolicy");
    }

    [Fact]
    public void Constructor_NullContextProvider_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(_policy, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("regionContextProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(_policy, _contextProvider, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // Helper test type decorated with [DataResidency]
    [DataResidency("DE", "FR", DataCategory = "personal-data")]
    private sealed class TestDataResidencyRequest;
}
