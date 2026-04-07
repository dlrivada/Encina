using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Method-level guard tests for <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/>
/// Handle method null parameter handling across all enforcement modes.
/// </summary>
/// <remarks>
/// The Handle method guards <c>request</c>, <c>context</c>, and <c>nextStep</c> with
/// <c>ArgumentNullException.ThrowIfNull</c>. This test class exercises those guards
/// across all three enforcement modes (Block, Warn, Disabled) to ensure the guards
/// fire before any mode-specific logic runs.
/// </remarks>
public sealed class ProcessorValidationPipelineBehaviorMethodGuardTests
{
    private readonly IDPAService _dpaService = Substitute.For<IDPAService>();

    #region Handle Guards — Disabled Mode

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null
    /// even in Disabled enforcement mode (guard fires before mode check).
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_DisabledMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Disabled);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Either<EncinaError, string>.Right("ok"));

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(null!, context, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null
    /// even in Disabled enforcement mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_DisabledMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Disabled);
        var request = new TestCommand();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Either<EncinaError, string>.Right("ok"));

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, null!, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null
    /// even in Disabled enforcement mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_DisabledMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Disabled);
        var request = new TestCommand();
        var context = Substitute.For<IRequestContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, context, null!, CancellationToken.None));
        ex.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Handle Guards — Block Mode

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null in Block mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_BlockMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Block);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Either<EncinaError, string>.Right("ok"));

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(null!, context, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null in Block mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_BlockMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Block);
        var request = new TestCommand();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Either<EncinaError, string>.Right("ok"));

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, null!, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null in Block mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_BlockMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Block);
        var request = new TestCommand();
        var context = Substitute.For<IRequestContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, context, null!, CancellationToken.None));
        ex.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Handle Guards — Warn Mode

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null in Warn mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_WarnMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Warn);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Either<EncinaError, string>.Right("ok"));

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(null!, context, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null in Warn mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_WarnMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Warn);
        var request = new TestCommand();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Either<EncinaError, string>.Right("ok"));

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, null!, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null in Warn mode.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_WarnMode_ThrowsArgumentNullException()
    {
        var sut = CreateSut(ProcessorAgreementEnforcementMode.Warn);
        var request = new TestCommand();
        var context = Substitute.For<IRequestContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, context, null!, CancellationToken.None));
        ex.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Helpers

    private ProcessorValidationPipelineBehavior<TestCommand, string> CreateSut(
        ProcessorAgreementEnforcementMode mode)
    {
        var options = Options.Create(new ProcessorAgreementOptions { EnforcementMode = mode });
        return new ProcessorValidationPipelineBehavior<TestCommand, string>(
            _dpaService, options,
            NullLogger<ProcessorValidationPipelineBehavior<TestCommand, string>>.Instance);
    }

    public sealed record TestCommand : ICommand<string>;

    #endregion
}
