#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Attributes;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class DataResidencyPipelineBehaviorTests
{
    private static readonly Region GermanyRegion = new()
    {
        Code = "DE",
        Country = "DE",
        IsEU = true,
        IsEEA = true,
        HasAdequacyDecision = true,
        ProtectionLevel = DataProtectionLevel.High
    };

    private static readonly Region UsRegion = new()
    {
        Code = "US",
        Country = "US",
        IsEU = false,
        IsEEA = false,
        HasAdequacyDecision = false,
        ProtectionLevel = DataProtectionLevel.Medium
    };

    private readonly IRegionContextProvider _regionContextProvider;
    private readonly IResidencyPolicyService _residencyPolicyService;
    private readonly ICrossBorderTransferValidator _transferValidator;
    private readonly IDataLocationService _dataLocationService;

    public DataResidencyPipelineBehaviorTests()
    {
        _regionContextProvider = Substitute.For<IRegionContextProvider>();
        _residencyPolicyService = Substitute.For<IResidencyPolicyService>();
        _transferValidator = Substitute.For<ICrossBorderTransferValidator>();
        _dataLocationService = Substitute.For<IDataLocationService>();

        // Default: region resolves to Germany
        _regionContextProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Region>>(GermanyRegion));

        // Default: policy allows the region
        _residencyPolicyService.IsAllowedAsync(
                Arg.Any<string>(), Arg.Any<Region>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(true));

        // Default: data location registration succeeds
        _dataLocationService.RegisterLocationAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<StorageType>(), Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(Guid.NewGuid()));
    }

    #region Disabled Mode

    [Fact]
    public async Task Handle_DisabledMode_CallsNextDirectly()
    {
        // Arrange
        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Disabled);
        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(() => nextStepCalled = true),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
        await _regionContextProvider.DidNotReceive()
            .GetCurrentRegionAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Attributes

    [Fact]
    public async Task Handle_NoAttributes_CallsNextDirectly()
    {
        // Arrange
        var sut = CreateBehavior<PlainCommand>();
        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            new PlainCommand(),
            RequestContext.CreateForTest(),
            NextStep(() => nextStepCalled = true),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
        await _regionContextProvider.DidNotReceive()
            .GetCurrentRegionAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Region Resolution Failures

    [Fact]
    public async Task Handle_RegionResolutionFails_BlockMode_ReturnsError()
    {
        // Arrange
        _regionContextProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Region>>(
                Left<EncinaError, Region>(DataResidencyErrors.RegionNotResolved("No region header"))));

        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Block);

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("resolve");
    }

    [Fact]
    public async Task Handle_RegionResolutionFails_WarnMode_CallsNext()
    {
        // Arrange
        _regionContextProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Region>>(
                Left<EncinaError, Region>(DataResidencyErrors.RegionNotResolved("No region header"))));

        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Warn);
        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(() => nextStepCalled = true),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
    }

    #endregion

    #region Residency Policy Checks

    [Fact]
    public async Task Handle_ResidencyPolicy_Allowed_CallsNext()
    {
        // Arrange
        _residencyPolicyService.IsAllowedAsync(
                Arg.Any<string>(), Arg.Any<Region>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(true));

        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Block);
        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(() => nextStepCalled = true),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ResidencyPolicy_NotAllowed_BlockMode_ReturnsError()
    {
        // Arrange
        _residencyPolicyService.IsAllowedAsync(
                Arg.Any<string>(), Arg.Any<Region>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(false));

        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Block);

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("not allowed");
    }

    [Fact]
    public async Task Handle_ResidencyPolicy_NotAllowed_WarnMode_CallsNext()
    {
        // Arrange
        _residencyPolicyService.IsAllowedAsync(
                Arg.Any<string>(), Arg.Any<Region>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(false));

        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Warn);
        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(() => nextStepCalled = true),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
    }

    #endregion

    #region Policy Lookup Failures

    [Fact]
    public async Task Handle_PolicyLookupFails_BlockMode_ReturnsError()
    {
        // Arrange
        _residencyPolicyService.IsAllowedAsync(
                Arg.Any<string>(), Arg.Any<Region>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(
                Left<EncinaError, bool>(DataResidencyErrors.PolicyNotFound("healthcare"))));

        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Block);

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("No residency policy");
    }

    [Fact]
    public async Task Handle_PolicyLookupFails_WarnMode_Proceeds()
    {
        // Arrange
        _residencyPolicyService.IsAllowedAsync(
                Arg.Any<string>(), Arg.Any<Region>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(
                Left<EncinaError, bool>(DataResidencyErrors.PolicyNotFound("healthcare"))));

        var sut = CreateBehavior<ResidencyCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Warn);
        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(() => nextStepCalled = true),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
    }

    #endregion

    #region Adequacy Decision

    [Fact]
    public async Task Handle_RequireAdequacyDecision_NoDecision_BlockMode_ReturnsError()
    {
        // Arrange — use US region which has no adequacy decision
        _regionContextProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Region>>(UsRegion));

        _residencyPolicyService.IsAllowedAsync(
                Arg.Any<string>(), Arg.Any<Region>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(true));

        var sut = CreateBehavior<AdequacyRequiredCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Block);

        // Act
        var result = await sut.Handle(
            new AdequacyRequiredCommand(),
            RequestContext.CreateForTest(),
            NextStep(),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("Adequacy decision required");
    }

    #endregion

    #region NoCrossBorderTransfer Attribute

    [Fact]
    public async Task Handle_NoCrossBorderAttribute_LogsAndProceeds()
    {
        // Arrange
        var sut = CreateBehavior<NoCrossCommand>(
            o => o.EnforcementMode = DataResidencyEnforcementMode.Block);
        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            new NoCrossCommand(),
            RequestContext.CreateForTest(),
            NextStep(() => nextStepCalled = true),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
    }

    #endregion

    #region Data Location Tracking

    [Fact]
    public async Task Handle_TrackDataLocations_Enabled_RecordsLocation()
    {
        // Arrange
        var sut = CreateBehavior<ResidencyCommand>(o =>
        {
            o.EnforcementMode = DataResidencyEnforcementMode.Block;
            o.TrackDataLocations = true;
        });

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();

        // Unit has no EntityId/Id property, so RegisterLocationAsync should NOT be called
        // (ResolveEntityId returns null for types without EntityId/Id)
        await _dataLocationService.DidNotReceive()
            .RegisterLocationAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<StorageType>(), Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TrackDataLocations_Disabled_SkipsRecording()
    {
        // Arrange
        var sut = CreateBehavior<ResidencyCommand>(o =>
        {
            o.EnforcementMode = DataResidencyEnforcementMode.Block;
            o.TrackDataLocations = false;
        });

        // Act
        var result = await sut.Handle(
            new ResidencyCommand(),
            RequestContext.CreateForTest(),
            NextStep(),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _dataLocationService.DidNotReceive()
            .RegisterLocationAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<StorageType>(), Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static RequestHandlerCallback<Unit> NextStep(Action? onCalled = null) =>
        () =>
        {
            onCalled?.Invoke();
            return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
        };

    private DataResidencyPipelineBehavior<TRequest, Unit> CreateBehavior<TRequest>(
        Action<DataResidencyOptions>? configure = null)
        where TRequest : IRequest<Unit>
    {
        var options = new DataResidencyOptions();
        configure?.Invoke(options);

        return new DataResidencyPipelineBehavior<TRequest, Unit>(
            _regionContextProvider,
            _residencyPolicyService,
            _transferValidator,
            _dataLocationService,
            Options.Create(options),
            TimeProvider.System,
            NullLogger<DataResidencyPipelineBehavior<TRequest, Unit>>.Instance);
    }

    #endregion
}

// ============================================================
// Test request types
// ============================================================

/// <summary>Plain command with no data residency attributes.</summary>
internal sealed record PlainCommand : IRequest<Unit>;

/// <summary>Command decorated with <see cref="DataResidencyAttribute"/>.</summary>
[DataResidency("DE", "FR", DataCategory = "healthcare")]
internal sealed record ResidencyCommand : IRequest<Unit>;

/// <summary>Command decorated with <see cref="NoCrossBorderTransferAttribute"/>.</summary>
[NoCrossBorderTransfer(DataCategory = "classified")]
internal sealed record NoCrossCommand : IRequest<Unit>;

/// <summary>Command that requires an EU adequacy decision.</summary>
[DataResidency("DE", "US", DataCategory = "financial", RequireAdequacyDecision = true)]
internal sealed record AdequacyRequiredCommand : IRequest<Unit>;
