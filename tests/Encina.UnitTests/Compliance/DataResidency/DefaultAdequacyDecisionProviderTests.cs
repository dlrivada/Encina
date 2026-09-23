using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

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
        sut.HasAdequacy(RegionRegistry.DE).ShouldBeTrue();
        sut.HasAdequacy(RegionRegistry.FR).ShouldBeTrue();
        sut.HasAdequacy(RegionRegistry.IT).ShouldBeTrue();
    }

    [Fact]
    public void HasAdequacy_EEACountry_ShouldReturnTrue()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.NO).ShouldBeTrue();
        sut.HasAdequacy(RegionRegistry.IS).ShouldBeTrue();
        sut.HasAdequacy(RegionRegistry.LI).ShouldBeTrue();
    }

    [Fact]
    public void HasAdequacy_AdequacyCountry_ShouldReturnTrue()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.JP).ShouldBeTrue();
        sut.HasAdequacy(RegionRegistry.GB).ShouldBeTrue();
        sut.HasAdequacy(RegionRegistry.CH).ShouldBeTrue();
        sut.HasAdequacy(RegionRegistry.KR).ShouldBeTrue();
    }

    [Fact]
    public void HasAdequacy_NonAdequateCountry_ShouldReturnFalse()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.CN).ShouldBeFalse();
        sut.HasAdequacy(RegionRegistry.IN).ShouldBeFalse();
    }

    [Fact]
    public void HasAdequacy_UsUncertifiedRecipient_ShouldReturnFalse()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert — no certification confirmed: the DPF decision does not cover this recipient.
        sut.HasAdequacy(RegionRegistry.US).ShouldBeFalse();
        sut.HasAdequacy(RegionRegistry.US, isRecipientCertified: false).ShouldBeFalse();
    }

    [Fact]
    public void HasAdequacy_UsCertifiedRecipient_ShouldReturnTrue()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.US, isRecipientCertified: true).ShouldBeTrue();
    }

    [Fact]
    public void HasAdequacy_CaNonCommercialRecipient_ShouldReturnFalse()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert — Canada's adequacy finding only covers PIPEDA-covered commercial organisations.
        sut.HasAdequacy(RegionRegistry.CA).ShouldBeFalse();
    }

    [Fact]
    public void HasAdequacy_CaCommercialRecipient_ShouldReturnTrue()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act & Assert
        sut.HasAdequacy(RegionRegistry.CA, isRecipientCertified: true).ShouldBeTrue();
    }

    [Fact]
    public void HasAdequacy_UsBareRegionConstructedByCode_ResolvesCanonicalRequirement()
    {
        // Arrange — mirrors how DefaultTransferValidator.CheckAdequacyDecision builds a Region
        // from a bare country code, without the RequiresRecipientCertification metadata.
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);
        var bareUsRegion = Region.Create("US", "US");

        // Act & Assert — the provider resolves the canonical registered US region by code and
        // still requires certification, even though the caller's Region instance does not
        // carry that metadata itself.
        sut.HasAdequacy(bareUsRegion).ShouldBeFalse();
        sut.HasAdequacy(bareUsRegion, isRecipientCertified: true).ShouldBeTrue();
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
        sut.HasAdequacy(customRegion).ShouldBeTrue();
    }

    [Fact]
    public void GetAdequateRegions_ShouldIncludeEEAAndAdequacyCountries()
    {
        // Arrange
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);

        // Act
        var regions = sut.GetAdequateRegions();

        // Assert
        regions.ShouldNotBeEmpty();
        regions.ShouldContain(r => r.Code == "DE");
        regions.ShouldContain(r => r.Code == "JP");
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
        regions.ShouldContain(r => r.Code == "XX");
    }
}
