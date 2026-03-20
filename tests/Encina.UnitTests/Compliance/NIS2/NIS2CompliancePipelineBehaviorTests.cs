using Encina;
using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="NIS2CompliancePipelineBehavior{TRequest, TResponse}"/>.
/// Covers all enforcement modes, attribute combinations, and exception handling.
/// </summary>
public class NIS2CompliancePipelineBehaviorTests
{
    private readonly IMFAEnforcer _mfaEnforcer = Substitute.For<IMFAEnforcer>();
    private readonly ISupplyChainSecurityValidator _supplyChainValidator = Substitute.For<ISupplyChainSecurityValidator>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public NIS2CompliancePipelineBehaviorTests()
    {
        // Default: MFA always succeeds
        _mfaEnforcer
            .RequireMFAAsync(Arg.Any<object>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Default: supply chain always valid
        _supplyChainValidator
            .ValidateSupplierForOperationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
    }

    private static RequestHandlerCallback<Unit> NextReturning(Unit value) =>
        () => ValueTask.FromResult(Right<EncinaError, Unit>(value));

    private static NIS2Options CreateOptions(NIS2EnforcementMode mode = NIS2EnforcementMode.Block) =>
        new() { EnforcementMode = mode, EnforceMFA = true };

    private NIS2CompliancePipelineBehavior<TRequest, Unit> CreateBehavior<TRequest>(NIS2Options options)
        where TRequest : IRequest<Unit> =>
        new(
            _mfaEnforcer,
            _supplyChainValidator,
            Options.Create(options),
            _serviceProvider,
            NullLogger<NIS2CompliancePipelineBehavior<TRequest, Unit>>.Instance);

    #region Disabled Mode

    [Fact]
    public async Task Handle_EnforcementDisabled_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Disabled);
        var behavior = CreateBehavior<MFARequiredRequest>(options);
        var request = new MFARequiredRequest();
        var next = NextReturning(Unit.Default);

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _mfaEnforcer
            .DidNotReceive()
            .RequireMFAAsync(Arg.Any<MFARequiredRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Attributes

    [Fact]
    public async Task Handle_NoAttributes_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Block);
        var behavior = CreateBehavior<PlainRequest>(options);
        var request = new PlainRequest();
        var next = NextReturning(Unit.Default);

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _mfaEnforcer
            .DidNotReceive()
            .RequireMFAAsync(Arg.Any<PlainRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>());
        await _supplyChainValidator
            .DidNotReceive()
            .ValidateSupplierForOperationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region MFA Enforcement

    [Fact]
    public async Task Handle_RequireMFA_MFASuccess_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Block);
        var behavior = CreateBehavior<MFARequiredRequest>(options);
        var request = new MFARequiredRequest();
        var next = NextReturning(Unit.Default);

        _mfaEnforcer
            .RequireMFAAsync(Arg.Any<MFARequiredRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _mfaEnforcer
            .Received(1)
            .RequireMFAAsync(Arg.Any<MFARequiredRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequireMFA_MFAFails_BlockMode_ShouldReturnError()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Block);
        var behavior = CreateBehavior<MFARequiredRequest>(options);
        var request = new MFARequiredRequest();
        var next = NextReturning(Unit.Default);

        var mfaError = NIS2Errors.MFARequired(nameof(MFARequiredRequest));
        _mfaEnforcer
            .RequireMFAAsync(Arg.Any<MFARequiredRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(mfaError));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone(string.Empty).Should().Be(NIS2Errors.MFARequiredCode);
    }

    [Fact]
    public async Task Handle_RequireMFA_MFAFails_WarnMode_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Warn);
        var behavior = CreateBehavior<MFARequiredRequest>(options);
        var request = new MFARequiredRequest();
        var next = NextReturning(Unit.Default);

        var mfaError = NIS2Errors.MFARequired(nameof(MFARequiredRequest));
        _mfaEnforcer
            .RequireMFAAsync(Arg.Any<MFARequiredRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(mfaError));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Supply Chain

    [Fact]
    public async Task Handle_SupplyChainCheck_ValidSupplier_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Block);
        var behavior = CreateBehavior<SupplyChainRequest>(options);
        var request = new SupplyChainRequest();
        var next = NextReturning(Unit.Default);

        _supplyChainValidator
            .ValidateSupplierForOperationAsync("supplier-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _supplyChainValidator
            .Received(1)
            .ValidateSupplierForOperationAsync("supplier-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SupplyChainCheck_RiskySupplier_BlockMode_ShouldReturnError()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Block);
        var behavior = CreateBehavior<SupplyChainRequest>(options);
        var request = new SupplyChainRequest();
        var next = NextReturning(Unit.Default);

        _supplyChainValidator
            .ValidateSupplierForOperationAsync("supplier-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone(string.Empty).Should().Be(NIS2Errors.PipelineBlockedCode);
    }

    [Fact]
    public async Task Handle_SupplyChainCheck_RiskySupplier_WarnMode_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Warn);
        var behavior = CreateBehavior<SupplyChainRequest>(options);
        var request = new SupplyChainRequest();
        var next = NextReturning(Unit.Default);

        _supplyChainValidator
            .ValidateSupplierForOperationAsync("supplier-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region NIS2 Critical

    [Fact]
    public async Task Handle_NIS2Critical_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Block);
        var behavior = CreateBehavior<CriticalRequest>(options);
        var request = new CriticalRequest();
        var next = NextReturning(Unit.Default);

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task Handle_MFAThrows_BlockMode_ShouldReturnError()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Block);
        var behavior = CreateBehavior<MFARequiredRequest>(options);
        var request = new MFARequiredRequest();
        var next = NextReturning(Unit.Default);

        _mfaEnforcer
            .RequireMFAAsync(Arg.Any<MFARequiredRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("MFA service unavailable"));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone(string.Empty).Should().Be(NIS2Errors.PipelineBlockedCode);
        error.Message.Should().Contain("MFA service unavailable");
    }

    [Fact]
    public async Task Handle_MFAThrows_WarnMode_ShouldPassThrough()
    {
        // Arrange
        var options = CreateOptions(NIS2EnforcementMode.Warn);
        var behavior = CreateBehavior<MFARequiredRequest>(options);
        var request = new MFARequiredRequest();
        var next = NextReturning(Unit.Default);

        _mfaEnforcer
            .RequireMFAAsync(Arg.Any<MFARequiredRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("MFA service unavailable"));

        // Act
        var result = await behavior.Handle(request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Test Request Types

    /// <summary>No NIS2 attributes — plain request.</summary>
    public sealed record PlainRequest : ICommand<Unit>;

    /// <summary>Has <see cref="RequireMFAAttribute"/>.</summary>
    [RequireMFA]
    public sealed record MFARequiredRequest : ICommand<Unit>;

    /// <summary>Has <see cref="NIS2CriticalAttribute"/>.</summary>
    [NIS2Critical(Description = "Critical test operation")]
    public sealed record CriticalRequest : ICommand<Unit>;

    /// <summary>Has <see cref="NIS2SupplyChainCheckAttribute"/>.</summary>
    [NIS2SupplyChainCheck("supplier-1")]
    public sealed record SupplyChainRequest : ICommand<Unit>;

    /// <summary>Has all NIS2 attributes combined.</summary>
    [NIS2Critical]
    [RequireMFA]
    [NIS2SupplyChainCheck("supplier-1")]
    public sealed record FullyDecoratedRequest : ICommand<Unit>;

    #endregion
}
