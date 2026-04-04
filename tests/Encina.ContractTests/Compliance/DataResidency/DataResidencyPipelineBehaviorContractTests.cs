using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Attributes;
using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.DataResidency;

/// <summary>
/// Contract tests for <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/>
/// verifying behavioral contracts for enforcement modes and attribute resolution.
/// </summary>
[Trait("Category", "Contract")]
public class DataResidencyPipelineBehaviorContractTests
{
    // ================================================================
    // Contract: Disabled mode passes through without checks
    // ================================================================

    [Fact]
    public async Task Handle_DisabledMode_ShouldPassThroughWithoutChecks()
    {
        // Arrange
        var (sut, _, _) = CreateBehavior<PlainRequest, PlainResponse>(
            DataResidencyEnforcementMode.Disabled);

        var response = new PlainResponse { Value = "ok" };
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<PlainResponse> next = () =>
            ValueTask.FromResult<Either<EncinaError, PlainResponse>>(response);

        // Act
        var result = await sut.Handle(new PlainRequest(), context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((PlainResponse)result).Value.ShouldBe("ok");
    }

    // ================================================================
    // Contract: No attributes skips residency checks
    // ================================================================

    [Fact]
    public async Task Handle_NoAttributes_ShouldSkipResidencyChecks()
    {
        // Arrange
        var (sut, _, _) = CreateBehavior<PlainRequest, PlainResponse>(
            DataResidencyEnforcementMode.Block);

        var response = new PlainResponse { Value = "ok" };
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<PlainResponse> next = () =>
            ValueTask.FromResult<Either<EncinaError, PlainResponse>>(response);

        // Act
        var result = await sut.Handle(new PlainRequest(), context, next, CancellationToken.None);

        // Assert — no attribute means no checks, passes through
        result.IsRight.ShouldBeTrue();
    }

    // ================================================================
    // Contract: Block mode with region resolution failure returns error
    // ================================================================

    [Fact]
    public async Task Handle_BlockMode_RegionResolutionFailure_ShouldReturnError()
    {
        // Arrange
        var (sut, regionProvider, _) = CreateBehavior<DecoratedRequest, DecoratedResponse>(
            DataResidencyEnforcementMode.Block);

        regionProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Region>(
                DataResidencyErrors.RegionNotResolved("No region configured")));

        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<DecoratedResponse> next = () =>
            ValueTask.FromResult<Either<EncinaError, DecoratedResponse>>(new DecoratedResponse());

        // Act
        var result = await sut.Handle(new DecoratedRequest(), context, next, CancellationToken.None);

        // Assert — Block mode returns error when region cannot be resolved
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // Contract: Warn mode with region resolution failure passes through
    // ================================================================

    [Fact]
    public async Task Handle_WarnMode_RegionResolutionFailure_ShouldPassThrough()
    {
        // Arrange
        var (sut, regionProvider, _) = CreateBehavior<DecoratedRequest, DecoratedResponse>(
            DataResidencyEnforcementMode.Warn);

        regionProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Region>(
                DataResidencyErrors.RegionNotResolved("No region configured")));

        var response = new DecoratedResponse();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<DecoratedResponse> next = () =>
            ValueTask.FromResult<Either<EncinaError, DecoratedResponse>>(response);

        // Act
        var result = await sut.Handle(new DecoratedRequest(), context, next, CancellationToken.None);

        // Assert — Warn mode passes through despite failure
        result.IsRight.ShouldBeTrue();
    }

    // ================================================================
    // Contract: Allowed region passes through
    // ================================================================

    [Fact]
    public async Task Handle_AllowedRegion_ShouldPassThrough()
    {
        // Arrange
        var (sut, regionProvider, policyService) = CreateBehavior<DecoratedRequest, DecoratedResponse>(
            DataResidencyEnforcementMode.Block);

        regionProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Region>(RegionRegistry.DE));

        policyService.IsAllowedAsync(Arg.Any<string>(), RegionRegistry.DE, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var response = new DecoratedResponse();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<DecoratedResponse> next = () =>
            ValueTask.FromResult<Either<EncinaError, DecoratedResponse>>(response);

        // Act
        var result = await sut.Handle(new DecoratedRequest(), context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ================================================================
    // Contract: Blocked region in Block mode returns error
    // ================================================================

    [Fact]
    public async Task Handle_BlockedRegion_BlockMode_ShouldReturnError()
    {
        // Arrange
        var (sut, regionProvider, policyService) = CreateBehavior<DecoratedRequest, DecoratedResponse>(
            DataResidencyEnforcementMode.Block);

        regionProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Region>(RegionRegistry.CN));

        policyService.IsAllowedAsync(Arg.Any<string>(), RegionRegistry.CN, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<DecoratedResponse> next = () =>
            ValueTask.FromResult<Either<EncinaError, DecoratedResponse>>(new DecoratedResponse());

        // Act
        var result = await sut.Handle(new DecoratedRequest(), context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // Helpers
    // ================================================================

    private static (
        DataResidencyPipelineBehavior<TRequest, TResponse> Behavior,
        IRegionContextProvider RegionProvider,
        IResidencyPolicyService PolicyService)
        CreateBehavior<TRequest, TResponse>(DataResidencyEnforcementMode mode)
        where TRequest : IRequest<TResponse>
    {
        var regionProvider = Substitute.For<IRegionContextProvider>();
        var policyService = Substitute.For<IResidencyPolicyService>();
        var transferValidator = Substitute.For<ICrossBorderTransferValidator>();
        var locationService = Substitute.For<IDataLocationService>();

        var options = Options.Create(new DataResidencyOptions
        {
            EnforcementMode = mode,
            TrackDataLocations = false
        });

        var timeProvider = TimeProvider.System;
        var logger = NullLogger<DataResidencyPipelineBehavior<TRequest, TResponse>>.Instance;

        var behavior = new DataResidencyPipelineBehavior<TRequest, TResponse>(
            regionProvider, policyService, transferValidator, locationService,
            options, timeProvider, logger);

        return (behavior, regionProvider, policyService);
    }

    // ---- Test request/response types ----

    private sealed record PlainRequest : IRequest<PlainResponse>;

    private sealed record PlainResponse
    {
        public string Value { get; init; } = string.Empty;
    }

    [DataResidency("DE", "FR", DataCategory = "test-data")]
    private sealed record DecoratedRequest : IRequest<DecoratedResponse>;

    private sealed record DecoratedResponse;
}
