using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataResidency;

public class DefaultCrossBorderTransferValidatorTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
    private readonly IOptions<DataResidencyOptions> _options;
    private readonly ILogger<DefaultCrossBorderTransferValidator> _logger = Substitute.For<ILogger<DefaultCrossBorderTransferValidator>>();
    private readonly DefaultCrossBorderTransferValidator _sut;

    public DefaultCrossBorderTransferValidatorTests()
    {
        _options = Substitute.For<IOptions<DataResidencyOptions>>();
        _options.Value.Returns(new DataResidencyOptions());
        _sut = new DefaultCrossBorderTransferValidator(_adequacyProvider, _options, _logger);
    }

    [Fact]
    public async Task ValidateTransferAsync_SameRegion_ShouldAllow()
    {
        // Act
        var result = await _sut.ValidateTransferAsync(RegionRegistry.DE, RegionRegistry.DE, "data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r => r.IsAllowed.Should().BeTrue(),
            Left: _ => { });
    }

    [Fact]
    public async Task ValidateTransferAsync_IntraEEA_ShouldAllow()
    {
        // Act (DE → FR are both EU/EEA)
        var result = await _sut.ValidateTransferAsync(RegionRegistry.DE, RegionRegistry.FR, "data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r =>
            {
                r.IsAllowed.Should().BeTrue();
                r.LegalBasis.Should().Be(TransferLegalBasis.AdequacyDecision);
            },
            Left: _ => { });
    }

    [Fact]
    public async Task ValidateTransferAsync_ToAdequacyCountry_ShouldAllow()
    {
        // Arrange (JP has adequacy decision)
        _adequacyProvider.HasAdequacy(RegionRegistry.JP).Returns(true);

        // Act
        var result = await _sut.ValidateTransferAsync(RegionRegistry.DE, RegionRegistry.JP, "data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r =>
            {
                r.IsAllowed.Should().BeTrue();
                r.LegalBasis.Should().Be(TransferLegalBasis.AdequacyDecision);
            },
            Left: _ => { });
    }

    [Fact]
    public async Task ValidateTransferAsync_ToMediumProtection_ShouldAllowWithSCCs()
    {
        // Arrange - country with Medium protection, no adequacy
        var destination = Region.Create("XX", "XX", isEU: false, isEEA: false,
            hasAdequacyDecision: false, DataProtectionLevel.Medium);
        _adequacyProvider.HasAdequacy(destination).Returns(false);

        // Act
        var result = await _sut.ValidateTransferAsync(RegionRegistry.DE, destination, "data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r =>
            {
                r.IsAllowed.Should().BeTrue();
                r.LegalBasis.Should().Be(TransferLegalBasis.StandardContractualClauses);
                r.RequiredSafeguards.Should().NotBeEmpty();
                r.Warnings.Should().NotBeEmpty();
            },
            Left: _ => { });
    }

    [Fact]
    public async Task ValidateTransferAsync_ToLowProtectionNoAdequacy_ShouldDeny()
    {
        // Arrange - country with Low protection, no adequacy
        var destination = Region.Create("YY", "YY", isEU: false, isEEA: false,
            hasAdequacyDecision: false, DataProtectionLevel.Low);
        _adequacyProvider.HasAdequacy(destination).Returns(false);

        // Act
        var result = await _sut.ValidateTransferAsync(RegionRegistry.DE, destination, "data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r =>
            {
                r.IsAllowed.Should().BeFalse();
                r.DenialReason.Should().NotBeNullOrEmpty();
            },
            Left: _ => { });
    }

    [Fact]
    public async Task ValidateTransferAsync_ToUnknownProtectionNoAdequacy_ShouldDeny()
    {
        // Arrange
        var destination = Region.Create("ZZ", "ZZ", isEU: false, isEEA: false,
            hasAdequacyDecision: false, DataProtectionLevel.Unknown);
        _adequacyProvider.HasAdequacy(destination).Returns(false);

        // Act
        var result = await _sut.ValidateTransferAsync(RegionRegistry.DE, destination, "data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r => r.IsAllowed.Should().BeFalse(),
            Left: _ => { });
    }

    [Fact]
    public async Task ValidateTransferAsync_EEAToEEA_ShouldUseFreeMovement()
    {
        // Act (NO is EEA-only, not EU)
        var result = await _sut.ValidateTransferAsync(RegionRegistry.DE, RegionRegistry.NO, "data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r => r.IsAllowed.Should().BeTrue(),
            Left: _ => { });
    }

    [Fact]
    public void Constructor_NullAdequacyProvider_ShouldThrow()
    {
        // Act
        var act = () => new DefaultCrossBorderTransferValidator(null!, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("adequacyProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act
        var act = () => new DefaultCrossBorderTransferValidator(_adequacyProvider, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new DefaultCrossBorderTransferValidator(_adequacyProvider, _options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
