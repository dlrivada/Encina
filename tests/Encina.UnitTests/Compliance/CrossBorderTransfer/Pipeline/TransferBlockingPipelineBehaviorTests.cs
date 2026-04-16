#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Attributes;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.Pipeline;
using Encina.Modules.Isolation;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Pipeline;

/// <summary>
/// Unit tests for <see cref="TransferBlockingPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class TransferBlockingPipelineBehaviorTests
{
    private readonly ITransferValidator _validator = Substitute.For<ITransferValidator>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public TransferBlockingPipelineBehaviorTests()
    {
        // IModuleExecutionContext not registered by default
        _serviceProvider.GetService(typeof(IModuleExecutionContext))
            .Returns((object?)null);
    }

    private TransferBlockingPipelineBehavior<TRequest, Unit> CreateSut<TRequest>(
        CrossBorderTransferOptions? options = null)
        where TRequest : IRequest<Unit>
    {
        var opts = Options.Create(options ?? new CrossBorderTransferOptions());
        var logger = NullLogger<TransferBlockingPipelineBehavior<TRequest, Unit>>.Instance;
        return new TransferBlockingPipelineBehavior<TRequest, Unit>(
            _validator, opts, logger, _serviceProvider);
    }

    private static RequestHandlerCallback<Unit> SuccessNext()
        => () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);

    private static RequestHandlerCallback<Unit> FailNext()
        => () => throw new InvalidOperationException("nextStep should not be called");

    #region Disabled Mode

    [Fact]
    public async Task Handle_DisabledMode_SkipsValidationAndCallsNext()
    {
        var sut = CreateSut<TransferCommand>(new CrossBorderTransferOptions
        {
            EnforcementMode = CrossBorderTransferEnforcementMode.Disabled
        });

        var result = await sut.Handle(
            new TransferCommand(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Attribute (Skip)

    [Fact]
    public async Task Handle_NoAttribute_SkipsValidationAndCallsNext()
    {
        var sut = CreateSut<PlainCommand>();

        var result = await sut.Handle(
            new PlainCommand(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Destination Resolution

    [Fact]
    public async Task Handle_EmptyDestination_ReturnsError()
    {
        // TransferCommandEmptyDestination has Destination = "" in the attribute
        var sut = CreateSut<TransferCommandEmptyDestination>();

        var result = await sut.Handle(
            new TransferCommandEmptyDestination(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_DestinationFromProperty_UsesPropertyValue()
    {
        var sut = CreateSut<DynamicDestinationCommand>();
        var allowed = TransferValidationOutcome.Allow(TransferBasis.AdequacyDecision);
        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TransferValidationOutcome>(allowed));

        var command = new DynamicDestinationCommand("JP");

        var result = await sut.Handle(command, _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _validator.Received(1).ValidateAsync(
            Arg.Is<TransferRequest>(r => r.DestinationCountryCode == "JP"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Error (IsLeft)

    [Fact]
    public async Task Handle_ValidatorReturnsError_ReturnsError()
    {
        var sut = CreateSut<TransferCommand>();
        var error = EncinaErrors.Create("crossborder.store_error", "Store unavailable");
        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TransferValidationOutcome>(error));

        var result = await sut.Handle(
            new TransferCommand(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Block Mode

    [Fact]
    public async Task Handle_BlockMode_NotAllowed_ReturnsBlockedError()
    {
        var sut = CreateSut<TransferCommand>(new CrossBorderTransferOptions
        {
            EnforcementMode = CrossBorderTransferEnforcementMode.Block
        });
        var blocked = TransferValidationOutcome.Block("No adequacy decision for US");
        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TransferValidationOutcome>(blocked));

        var result = await sut.Handle(
            new TransferCommand(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Warn Mode

    [Fact]
    public async Task Handle_WarnMode_NotAllowed_LogsWarningAndCallsNext()
    {
        var sut = CreateSut<TransferCommand>(new CrossBorderTransferOptions
        {
            EnforcementMode = CrossBorderTransferEnforcementMode.Warn
        });
        var blocked = TransferValidationOutcome.Block("No adequacy decision for US");
        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TransferValidationOutcome>(blocked));

        var result = await sut.Handle(
            new TransferCommand(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Allowed Transfer

    [Fact]
    public async Task Handle_Allowed_CallsNextAndReturnsResult()
    {
        var sut = CreateSut<TransferCommand>();
        var allowed = TransferValidationOutcome.Allow(TransferBasis.AdequacyDecision);
        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TransferValidationOutcome>(allowed));

        var result = await sut.Handle(
            new TransferCommand(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_AllowedWithWarnings_CallsNextAndReturnsResult()
    {
        var sut = CreateSut<TransferCommand>();
        var allowed = TransferValidationOutcome.Allow(
            TransferBasis.SCCs,
            warnings: ["SCC agreement expires in 15 days"]);
        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TransferValidationOutcome>(allowed));

        var result = await sut.Handle(
            new TransferCommand(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Transfer Request Construction

    [Fact]
    public async Task Handle_UsesDefaultSourceWhenNotSpecified()
    {
        var sut = CreateSut<TransferCommand>(new CrossBorderTransferOptions
        {
            DefaultSourceCountryCode = "FR"
        });
        var allowed = TransferValidationOutcome.Allow(TransferBasis.AdequacyDecision);
        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TransferValidationOutcome>(allowed));

        await sut.Handle(new TransferCommand(), _context, SuccessNext(), CancellationToken.None);

        await _validator.Received(1).ValidateAsync(
            Arg.Is<TransferRequest>(r =>
                r.SourceCountryCode == "FR" &&
                r.DestinationCountryCode == "US" &&
                r.DataCategory == "personal"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Test Request Types

    public sealed record PlainCommand : IRequest<Unit>;

    [RequiresCrossBorderTransfer(Destination = "US", DataCategory = "personal")]
    public sealed record TransferCommand : IRequest<Unit>;

    [RequiresCrossBorderTransfer(Destination = "", DataCategory = "personal")]
    public sealed record TransferCommandEmptyDestination : IRequest<Unit>;

    [RequiresCrossBorderTransfer(DestinationProperty = "TargetCountry", DataCategory = "financial")]
    public sealed record DynamicDestinationCommand(string TargetCountry) : IRequest<Unit>;

    #endregion
}
