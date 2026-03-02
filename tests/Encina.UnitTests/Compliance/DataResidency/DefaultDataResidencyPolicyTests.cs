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

public class DefaultDataResidencyPolicyTests
{
    private readonly IResidencyPolicyStore _policyStore = Substitute.For<IResidencyPolicyStore>();
    private readonly IOptions<DataResidencyOptions> _options;
    private readonly ILogger<DefaultDataResidencyPolicy> _logger = Substitute.For<ILogger<DefaultDataResidencyPolicy>>();
    private readonly DefaultDataResidencyPolicy _sut;

    public DefaultDataResidencyPolicyTests()
    {
        _options = Substitute.For<IOptions<DataResidencyOptions>>();
        _options.Value.Returns(new DataResidencyOptions());
        _sut = new DefaultDataResidencyPolicy(_policyStore, _options, _logger);
    }

    [Fact]
    public async Task IsAllowedAsync_WhenRegionIsInPolicy_ShouldReturnTrue()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "personal-data",
            allowedRegions: [RegionRegistry.DE, RegionRegistry.FR]);

        _policyStore.GetByCategoryAsync("personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
                Right<EncinaError, Option<ResidencyPolicyDescriptor>>(Some(policy))));

        // Act
        var result = await _sut.IsAllowedAsync("personal-data", RegionRegistry.DE);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: allowed => allowed.Should().BeTrue(), Left: _ => { });
    }

    [Fact]
    public async Task IsAllowedAsync_WhenRegionNotInPolicy_ShouldReturnFalse()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "personal-data",
            allowedRegions: [RegionRegistry.DE, RegionRegistry.FR]);

        _policyStore.GetByCategoryAsync("personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
                Right<EncinaError, Option<ResidencyPolicyDescriptor>>(Some(policy))));

        // Act
        var result = await _sut.IsAllowedAsync("personal-data", RegionRegistry.IT);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: allowed => allowed.Should().BeFalse(), Left: _ => { });
    }

    [Fact]
    public async Task IsAllowedAsync_WhenNoPolicyExists_ShouldReturnError()
    {
        // Arrange
        _policyStore.GetByCategoryAsync("unknown-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
                Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None)));

        // Act
        var result = await _sut.IsAllowedAsync("unknown-data", RegionRegistry.DE);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenEmptyAllowedRegions_ShouldAllowAnyRegion()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "unrestricted",
            allowedRegions: []);

        _policyStore.GetByCategoryAsync("unrestricted", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
                Right<EncinaError, Option<ResidencyPolicyDescriptor>>(Some(policy))));

        // Act
        var result = await _sut.IsAllowedAsync("unrestricted", RegionRegistry.US);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: allowed => allowed.Should().BeTrue(), Left: _ => { });
    }

    [Fact]
    public async Task GetAllowedRegionsAsync_WhenPolicyExists_ShouldReturnRegions()
    {
        // Arrange
        var regions = new List<Region> { RegionRegistry.DE, RegionRegistry.FR };
        var policy = ResidencyPolicyDescriptor.Create("personal-data", regions);

        _policyStore.GetByCategoryAsync("personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
                Right<EncinaError, Option<ResidencyPolicyDescriptor>>(Some(policy))));

        // Act
        var result = await _sut.GetAllowedRegionsAsync("personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: r => r.Should().HaveCount(2),
            Left: _ => { });
    }

    [Fact]
    public async Task GetAllowedRegionsAsync_WhenNoPolicyExists_ShouldReturnError()
    {
        // Arrange
        _policyStore.GetByCategoryAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
                Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None)));

        // Act
        var result = await _sut.GetAllowedRegionsAsync("unknown");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenStoreReturnsError_ShouldPropagateError()
    {
        // Arrange
        var error = DataResidencyErrors.StoreError("GetByCategory", "DB timeout");
        _policyStore.GetByCategoryAsync("data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
                Left<EncinaError, Option<ResidencyPolicyDescriptor>>(error)));

        // Act
        var result = await _sut.IsAllowedAsync("data", RegionRegistry.DE);

        // Assert
        result.IsLeft.Should().BeTrue();
    }
}
