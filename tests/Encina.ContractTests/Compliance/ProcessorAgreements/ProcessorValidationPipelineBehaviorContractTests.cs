#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

/// <summary>
/// Contract tests for <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/>
/// verifying behavioral contracts across enforcement modes and attribute presence.
/// </summary>
[Trait("Category", "Contract")]
public class ProcessorValidationPipelineBehaviorContractTests
{
    private readonly IDPAService _dpaService = Substitute.For<IDPAService>();

    #region Disabled Mode Contract

    /// <summary>
    /// Contract: When enforcement mode is Disabled, the pipeline must pass through
    /// to the next handler without any DPA validation, regardless of attribute presence.
    /// </summary>
    [Fact]
    public async Task Handle_DisabledMode_SkipsValidation_CallsNext()
    {
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Disabled
        };
        var sut = CreateBehavior<PlainCommand, string>(options);

        var result = await sut.Handle(
            new PlainCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("ok")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("ok");

        // DPA service should NOT be called
        await _dpaService.DidNotReceive().HasValidDPAAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Attribute Contract

    /// <summary>
    /// Contract: When the request type has no [RequiresProcessor] attribute, the pipeline
    /// must pass through without any DPA validation, regardless of enforcement mode.
    /// </summary>
    [Theory]
    [InlineData(ProcessorAgreementEnforcementMode.Block)]
    [InlineData(ProcessorAgreementEnforcementMode.Warn)]
    public async Task Handle_NoAttribute_SkipsValidation_CallsNext(ProcessorAgreementEnforcementMode mode)
    {
        var options = new ProcessorAgreementOptions { EnforcementMode = mode };
        var sut = CreateBehavior<PlainCommand, string>(options);

        var result = await sut.Handle(
            new PlainCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("pass-through")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("pass-through");

        // DPA service should NOT be called
        await _dpaService.DidNotReceive().HasValidDPAAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Valid DPA Contract

    /// <summary>
    /// Contract: When the request has [RequiresProcessor] with a valid GUID processor ID
    /// and the DPA service reports a valid DPA, the pipeline must call next and return the response.
    /// </summary>
    [Fact]
    public async Task Handle_ValidDPA_CallsNext()
    {
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };
        var sut = CreateBehavior<DecoratedCommand, string>(options);

        _dpaService.HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(true)));

        var result = await sut.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("processed")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("processed");
    }

    #endregion

    #region Invalid DPA Block Mode Contract

    /// <summary>
    /// Contract: In Block mode, when the DPA service reports an invalid DPA (false),
    /// the pipeline must return an error (Left) and NOT call next.
    /// </summary>
    [Fact]
    public async Task Handle_BlockMode_InvalidDPA_ReturnsError()
    {
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };
        var sut = CreateBehavior<DecoratedCommand, string>(options);

        _dpaService.HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        // ValidateDPAAsync is called for detailed error in Block mode
        _dpaService.ValidateDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, DPAValidationResult>>(
                Right<EncinaError, DPAValidationResult>(new DPAValidationResult
                {
                    ProcessorId = DecoratedCommand.TestProcessorId,
                    IsValid = false,
                    MissingTerms = [],
                    Warnings = ["No active DPA found"],
                    ValidatedAtUtc = DateTimeOffset.UtcNow
                })));

        var nextCalled = false;

        var result = await sut.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () =>
            {
                nextCalled = true;
                return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("should-not-reach"));
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        nextCalled.ShouldBeFalse();
    }

    #endregion

    #region Warn Mode Contract

    /// <summary>
    /// Contract: In Warn mode, when the DPA service reports an invalid DPA (false),
    /// the pipeline must still call next and return the response (Right).
    /// </summary>
    [Fact]
    public async Task Handle_WarnMode_InvalidDPA_CallsNextAnyway()
    {
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Warn
        };
        var sut = CreateBehavior<DecoratedCommand, string>(options);

        _dpaService.HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        var result = await sut.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("warn-allowed")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("warn-allowed");
    }

    #endregion

    #region Constructor Guard Contract

    /// <summary>
    /// Contract: Constructor must reject null dpaService.
    /// </summary>
    [Fact]
    public void Constructor_NullDPAService_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<PlainCommand, string>(
            null!,
            Options.Create(new ProcessorAgreementOptions()),
            NullLogger<ProcessorValidationPipelineBehavior<PlainCommand, string>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("dpaService");
    }

    /// <summary>
    /// Contract: Constructor must reject null options.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<PlainCommand, string>(
            _dpaService,
            null!,
            NullLogger<ProcessorValidationPipelineBehavior<PlainCommand, string>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Contract: Constructor must reject null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<PlainCommand, string>(
            _dpaService,
            Options.Create(new ProcessorAgreementOptions()),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Null Request Guard Contract

    /// <summary>
    /// Contract: Handle must reject null request with ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateBehavior<PlainCommand, string>(new ProcessorAgreementOptions());

        var act = async () => await sut.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("x")),
            CancellationToken.None);

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region Helper Methods and Test Types

    private ProcessorValidationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        ProcessorAgreementOptions options)
        where TRequest : IRequest<TResponse>
    {
        return new ProcessorValidationPipelineBehavior<TRequest, TResponse>(
            _dpaService,
            Options.Create(options),
            NullLogger<ProcessorValidationPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    /// <summary>
    /// A command type without [RequiresProcessor] attribute.
    /// </summary>
    public sealed record PlainCommand : IRequest<string>;

    /// <summary>
    /// A command type decorated with [RequiresProcessor] pointing to a valid GUID processor ID.
    /// </summary>
    [RequiresProcessor(ProcessorId = "00000000-0000-0000-0000-000000000001")]
    public sealed record DecoratedCommand : IRequest<string>
    {
        public const string TestProcessorId = "00000000-0000-0000-0000-000000000001";
    }

    #endregion
}
