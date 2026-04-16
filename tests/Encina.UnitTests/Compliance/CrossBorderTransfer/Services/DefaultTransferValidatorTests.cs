#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Services;

public class DefaultTransferValidatorTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider;
    private readonly IApprovedTransferService _transferService;
    private readonly ISCCService _sccService;
    private readonly ITIAService _tiaService;
    private readonly ILogger<DefaultTransferValidator> _logger;
    private readonly DefaultTransferValidator _sut;

    public DefaultTransferValidatorTests()
    {
        _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        _transferService = Substitute.For<IApprovedTransferService>();
        _sccService = Substitute.For<ISCCService>();
        _tiaService = Substitute.For<ITIAService>();
        _logger = NullLogger<DefaultTransferValidator>.Instance;

        _sut = new DefaultTransferValidator(
            _adequacyProvider, _transferService, _sccService, _tiaService, _logger);
    }

    [Fact]
    public async Task ValidateAsync_NullRequest_ThrowsArgumentNull()
    {
        // Act
        var act = async () => await _sut.ValidateAsync(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ValidateAsync_AdequacyDecision_ReturnsAllowedWithAdequacyBasis()
    {
        // Arrange
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "JP",
            DataCategory = "personal-data"
        };

        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(true);

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var outcome = result.Match(Right: o => o, Left: _ => throw new InvalidOperationException("Expected Right"));
        outcome.IsAllowed.ShouldBeTrue();
        outcome.Basis.ShouldBe(TransferBasis.AdequacyDecision);
    }

    [Fact]
    public async Task ValidateAsync_ApprovedTransfer_ReturnsAllowed()
    {
        // Arrange
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data"
        };

        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        _transferService.IsTransferApprovedAsync("DE", "US", "personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<LanguageExt.Either<EncinaError, bool>>(Right<EncinaError, bool>(true)));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var outcome = result.Match(Right: o => o, Left: _ => throw new InvalidOperationException("Expected Right"));
        outcome.IsAllowed.ShouldBeTrue();
        outcome.Basis.ShouldBe(TransferBasis.SCCs);
    }

    [Fact]
    public async Task ValidateAsync_ApprovedTransferError_ReturnsLeft()
    {
        // Arrange
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data"
        };

        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        var error = EncinaErrors.Create(code: "store.error", message: "Store failure");
        _transferService.IsTransferApprovedAsync("DE", "US", "personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<LanguageExt.Either<EncinaError, bool>>(Left<EncinaError, bool>(error)));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_SCCAgreement_WithApprovedTransfer_ReturnsAllowed()
    {
        // Arrange — mock IsTransferApprovedAsync to return true so validation
        // finds an existing approved transfer at step 2.
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data",
            ProcessorId = "processor-1"
        };

        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        _transferService.IsTransferApprovedAsync("DE", "US", "personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<LanguageExt.Either<EncinaError, bool>>(Right<EncinaError, bool>(true)));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var outcome = result.Match(Right: o => o, Left: _ => throw new InvalidOperationException("Expected Right"));
        outcome.IsAllowed.ShouldBeTrue();
        outcome.Basis.ShouldBe(TransferBasis.SCCs);
    }

    [Fact]
    public async Task ValidateAsync_NoMechanismMatches_ReturnsBlocked()
    {
        // Arrange — no adequacy, transfer not approved, no SCC, TIA not found
        // → validator reaches the block step
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "IN",
            DataCategory = "personal-data"
        };

        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        _transferService.IsTransferApprovedAsync("DE", "IN", "personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<LanguageExt.Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        // SCC check skipped (no ProcessorId)

        // TIA not found → validator treats this as "no TIA" and falls through to Block
        var tiaNotFound = EncinaErrors.Create(code: "crossborder.tia_not_found", message: "Not found");
        _tiaService.GetTIAByRouteAsync("DE", "IN", "personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<LanguageExt.Either<EncinaError, TIAReadModel>>(
                Left<EncinaError, TIAReadModel>(tiaNotFound)));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert — all checks fail, transfer is blocked
        result.IsRight.ShouldBeTrue();
        var outcome = result.Match(Right: o => o, Left: _ => throw new InvalidOperationException("Expected Right"));
        outcome.IsAllowed.ShouldBeFalse();
        outcome.Basis.ShouldBe(TransferBasis.Blocked);
    }

    [Fact]
    public async Task ValidateAsync_NoMechanism_WithTransferServiceError_ReturnsLeft()
    {
        // Arrange — when transfer service returns an error, the validator returns Left
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "CN",
            DataCategory = "personal-data"
        };

        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        var error = EncinaErrors.Create(code: "store.error", message: "Store failure");
        _transferService.IsTransferApprovedAsync("DE", "CN", "personal-data", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<LanguageExt.Either<EncinaError, bool>>(Left<EncinaError, bool>(error)));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }
}
