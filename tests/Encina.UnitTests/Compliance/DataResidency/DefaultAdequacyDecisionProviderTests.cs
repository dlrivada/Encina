using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DefaultAdequacyDecisionProviderTests
{
    private readonly IOptions<DataResidencyOptions> _options;
    private readonly ILogger<DefaultAdequacyDecisionProvider> _logger = Substitute.For<ILogger<DefaultAdequacyDecisionProvider>>();

    public DefaultAdequacyDecisionProviderTests()
    {
        _options = Substitute.For<IOptions<DataResidencyOptions>>();
        _options.Value.Returns(new DataResidencyOptions());
    }

    [Fact]
    public void HasAdequacy_EUCountry_ShouldReturnTrue()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.DE).Should().BeTrue();
        sut.HasAdequacy(RegionRegistry.FR).Should().BeTrue();
        sut.HasAdequacy(RegionRegistry.IT).Should().BeTrue();
    }

    [Fact]
    public void HasAdequacy_EEACountry_ShouldReturnTrue()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.NO).Should().BeTrue();
        sut.HasAdequacy(RegionRegistry.IS).Should().BeTrue();
        sut.HasAdequacy(RegionRegistry.LI).Should().BeTrue();
    }

    [Fact]
    public void HasAdequacy_AdequacyCountry_ShouldReturnTrue()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.JP).Should().BeTrue();
        sut.HasAdequacy(RegionRegistry.GB).Should().BeTrue();
        sut.HasAdequacy(RegionRegistry.CH).Should().BeTrue();
        sut.HasAdequacy(RegionRegistry.KR).Should().BeTrue();
    }

    [Fact]
    public void HasAdequacy_NonAdequateCountry_ShouldReturnFalse()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.CN).Should().BeFalse();
        sut.HasAdequacy(RegionRegistry.IN).Should().BeFalse();
    }

    [Fact]
    public void HasAdequacy_WithAdditionalAdequateRegion_ShouldReturnTrue()
    {
        // Arrange
        var customRegion = Region.Create("XX", "XX", isEU: false, isEEA: false,
            hasAdequacyDecision: false, DataProtectionLevel.Medium);
        var opts = new DataResidencyOptions();
        opts.AdditionalAdequateRegions.Add(customRegion);

        var optionsMock = Substitute.For<IOptions<DataResidencyOptions>>();
        optionsMock.Value.Returns(opts);

        var sut = new DefaultAdequacyDecisionProvider(optionsMock, _logger);

        // Act & Assert
        sut.HasAdequacy(customRegion).Should().BeTrue();
    }

    [Fact]
    public void GetAdequateRegions_ShouldIncludeEEAAndAdequacyCountries()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act
        var regions = sut.GetAdequateRegions();

        // Assert
        regions.Should().NotBeEmpty();
        regions.Should().Contain(r => r.Code == "DE");
        regions.Should().Contain(r => r.Code == "JP");
    }

    [Fact]
    public void GetAdequateRegions_WithAdditionalRegions_ShouldIncludeThem()
    {
        // Arrange
        var customRegion = Region.Create("XX", "XX", isEU: false, isEEA: false,
            hasAdequacyDecision: false, DataProtectionLevel.Medium);
        var opts = new DataResidencyOptions();
        opts.AdditionalAdequateRegions.Add(customRegion);

        var optionsMock = Substitute.For<IOptions<DataResidencyOptions>>();
        optionsMock.Value.Returns(opts);

        var sut = new DefaultAdequacyDecisionProvider(optionsMock, _logger);

        // Act
        var regions = sut.GetAdequateRegions();

        // Assert
        regions.Should().Contain(r => r.Code == "XX");
    }
}
