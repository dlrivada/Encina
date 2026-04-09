#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="DefaultTransferValidator"/> verifying
/// cascading validation invariants across randomized inputs.
/// </summary>
[Trait("Category", "Property")]
public class DefaultTransferValidatorPropertyTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
    private readonly IApprovedTransferService _transferService = Substitute.For<IApprovedTransferService>();
    private readonly ISCCService _sccService = Substitute.For<ISCCService>();
    private readonly ITIAService _tiaService = Substitute.For<ITIAService>();
    private readonly ILogger<DefaultTransferValidator> _logger = NullLogger<DefaultTransferValidator>.Instance;

    #region Adequacy Decision Invariants

    /// <summary>
    /// Invariant: If the destination has an adequacy decision, the transfer is always allowed
    /// with AdequacyDecision basis regardless of data category.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property AdequateDestination_AlwaysAllowed()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("JP", "KR", "GB", "NZ", "IL", "CH")),
            Arb.From(Gen.Elements("personal-data", "health-data", "financial-data", "sensitive-data")),
            (destination, category) =>
            {
                _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(true);
                var sut = CreateSut();

                var request = new TransferRequest
                {
                    SourceCountryCode = "DE",
                    DestinationCountryCode = destination,
                    DataCategory = category
                };

                var result = sut.ValidateAsync(request).AsTask().GetAwaiter().GetResult();

                if (result.IsLeft) return false;

                var outcome = (TransferValidationOutcome)result;
                return outcome.IsAllowed && outcome.Basis == TransferBasis.AdequacyDecision;
            });
    }

    #endregion

    #region No Mechanism Invariants

    /// <summary>
    /// Invariant: If no adequacy decision, no approved transfer, no SCC, and no TIA exist,
    /// the transfer is always blocked.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property NoMechanism_AlwaysBlocked()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("US", "CN", "IN", "BR", "RU")),
            Arb.From(Gen.Elements("personal-data", "health-data")),
            (destination, category) =>
            {
                _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
                _transferService.IsTransferApprovedAsync(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
                _tiaService.GetTIAByRouteAsync(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(ValueTask.FromResult(Left<EncinaError, TIAReadModel>(
                        EncinaErrors.Create("encina.not_found", "TIA not found"))));

                var sut = CreateSut();
                var request = new TransferRequest
                {
                    SourceCountryCode = "DE",
                    DestinationCountryCode = destination,
                    DataCategory = category
                };

                var result = sut.ValidateAsync(request).AsTask().GetAwaiter().GetResult();

                if (result.IsLeft) return false;

                var outcome = (TransferValidationOutcome)result;
                return !outcome.IsAllowed && outcome.Basis == TransferBasis.Blocked;
            });
    }

    #endregion

    #region NullRequest Guard

    [Fact]
    public async Task ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ValidateAsync(null!));
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region Approved Transfer Invariants

    /// <summary>
    /// Invariant: If an approved transfer exists, the transfer is allowed with SCCs basis.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property ApprovedTransferExists_AlwaysAllowed()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("US", "IN", "BR")),
            destination =>
            {
                _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
                _transferService.IsTransferApprovedAsync(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));

                var sut = CreateSut();
                var request = new TransferRequest
                {
                    SourceCountryCode = "DE",
                    DestinationCountryCode = destination,
                    DataCategory = "personal-data"
                };

                var result = sut.ValidateAsync(request).AsTask().GetAwaiter().GetResult();

                if (result.IsLeft) return false;

                var outcome = (TransferValidationOutcome)result;
                return outcome.IsAllowed;
            });
    }

    #endregion

    #region Helpers

    private DefaultTransferValidator CreateSut() =>
        new(_adequacyProvider, _transferService, _sccService, _tiaService, _logger);

    #endregion
}
