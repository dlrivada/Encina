#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Attributes;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.Pipeline;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Model;
using Encina.Modules.Isolation;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Pipeline;

/// <summary>
/// Wires <see cref="TransferBlockingPipelineBehavior{TRequest, TResponse}"/> to the real
/// <see cref="DefaultTransferValidator"/> and <see cref="DefaultAdequacyDecisionProvider"/>
/// (instead of a mocked <see cref="ITransferValidator"/>) to verify the fail-closed behavior
/// documented for the United States (EU-US Data Privacy Framework) and Canada (PIPEDA) partial
/// adequacy decisions end to end (#1145, #1155).
/// </summary>
public class TransferBlockingPipelineBehaviorAdequacyTests
{
    private readonly IApprovedTransferService _transferService = Substitute.For<IApprovedTransferService>();
    private readonly ISCCService _sccService = Substitute.For<ISCCService>();
    private readonly ITIAService _tiaService = Substitute.For<ITIAService>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public TransferBlockingPipelineBehaviorAdequacyTests()
    {
        _serviceProvider.GetService(typeof(IModuleExecutionContext)).Returns((object?)null);

        // No approved transfer, no SCC agreement (no ProcessorId on the test command), and no
        // TIA on file — so any destination that does not qualify via adequacy falls through the
        // whole chain to Blocked rather than accidentally passing some other mechanism.
        _transferService
            .IsTransferApprovedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        var tiaNotFound = EncinaErrors.Create(code: "crossborder.tia_not_found", message: "Not found");
        _tiaService
            .GetTIAByRouteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, TIAReadModel>>(Left<EncinaError, TIAReadModel>(tiaNotFound)));
    }

    private static DefaultAdequacyDecisionProvider CreateRealAdequacyProvider() => new(
        Options.Create(new DataResidencyOptions()),
        NullLogger<DefaultAdequacyDecisionProvider>.Instance);

    private DefaultTransferValidator CreateRealValidator() => new(
        CreateRealAdequacyProvider(),
        _transferService,
        _sccService,
        _tiaService,
        NullLogger<DefaultTransferValidator>.Instance);

    private TransferBlockingPipelineBehavior<TransferCommand, Unit> CreateSut(
        IRecipientCertificationResolver certificationResolver)
    {
        var options = Options.Create(new CrossBorderTransferOptions());

        return new TransferBlockingPipelineBehavior<TransferCommand, Unit>(
            CreateRealValidator(),
            certificationResolver,
            options,
            NullLogger<TransferBlockingPipelineBehavior<TransferCommand, Unit>>.Instance,
            _serviceProvider);
    }

    private static RequestHandlerCallback<Unit> SuccessNext()
        => () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);

    private static IRecipientCertificationResolver CreateResolver(bool isCertified)
    {
        var resolver = Substitute.For<IRecipientCertificationResolver>();
        resolver.IsCertifiedAsync(Arg.Any<Region>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(isCertified));
        return resolver;
    }

    #region United States (EU-US Data Privacy Framework)

    [Fact]
    public async Task Handle_UsCertifiedRecipient_AllowsTransfer()
    {
        var sut = CreateSut(CreateResolver(isCertified: true));

        var result = await sut.Handle(new TransferCommand("US"), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UsUncertifiedRecipient_BlocksTransfer()
    {
        var sut = CreateSut(CreateResolver(isCertified: false));

        var result = await sut.Handle(
            new TransferCommand("US"), _context, () => throw new InvalidOperationException("nextStep should not be called"), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UsNoResolverRegistered_FailsClosedAndBlocksTransfer()
    {
        // The application registered no IRecipientCertificationResolver, so the default
        // NullRecipientCertificationResolver answers false — fail closed.
        var sut = CreateSut(new NullRecipientCertificationResolver());

        var result = await sut.Handle(
            new TransferCommand("US"), _context, () => throw new InvalidOperationException("nextStep should not be called"), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Canada (PIPEDA)

    [Fact]
    public async Task Handle_CaCertifiedRecipient_AllowsTransfer()
    {
        var sut = CreateSut(CreateResolver(isCertified: true));

        var result = await sut.Handle(new TransferCommand("CA"), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CaUncertifiedRecipient_BlocksTransfer()
    {
        var sut = CreateSut(CreateResolver(isCertified: false));

        var result = await sut.Handle(
            new TransferCommand("CA"), _context, () => throw new InvalidOperationException("nextStep should not be called"), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CaNoResolverRegistered_FailsClosedAndBlocksTransfer()
    {
        var sut = CreateSut(new NullRecipientCertificationResolver());

        var result = await sut.Handle(
            new TransferCommand("CA"), _context, () => throw new InvalidOperationException("nextStep should not be called"), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Transfer basis (real validator, bypassing the pipeline behavior to inspect the outcome)

    [Fact]
    public async Task ValidateAsync_UsCertified_UsesDataPrivacyFrameworkBasis()
    {
        var validator = CreateRealValidator();
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "health-data",
            IsRecipientCertified = true
        };

        var result = await validator.ValidateAsync(request);

        var outcome = result.Match(Right: o => o, Left: _ => throw new InvalidOperationException("Expected Right"));
        outcome.IsAllowed.ShouldBeTrue();
        outcome.Basis.ShouldBe(TransferBasis.DataPrivacyFramework);
    }

    [Fact]
    public async Task ValidateAsync_CaCertified_UsesGenericAdequacyDecisionBasis_NotDataPrivacyFramework()
    {
        var validator = CreateRealValidator();
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "CA",
            DataCategory = "health-data",
            IsRecipientCertified = true
        };

        var result = await validator.ValidateAsync(request);

        var outcome = result.Match(Right: o => o, Left: _ => throw new InvalidOperationException("Expected Right"));
        outcome.IsAllowed.ShouldBeTrue();
        outcome.Basis.ShouldBe(TransferBasis.AdequacyDecision);
        outcome.Basis.ShouldNotBe(TransferBasis.DataPrivacyFramework);
    }

    #endregion

    [RequiresCrossBorderTransfer(DestinationProperty = "TargetCountry", DataCategory = "health-data")]
    public sealed record TransferCommand(string TargetCountry) : IRequest<Unit>;
}
