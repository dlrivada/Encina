using Encina.Compliance.GDPR;
using Encina.UnitTests.Compliance.GDPR.Attributes;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class LawfulBasisValidationPipelineBehaviorTests
{
    private readonly ILawfulBasisRegistry _registry;
    private readonly ILegitimateInterestAssessment _liaAssessment;
    private readonly ILawfulBasisSubjectIdExtractor _subjectIdExtractor;
    private readonly IConsentStatusProvider _consentProvider;

    // Logger for each closed generic type
    private readonly ILogger<LawfulBasisValidationPipelineBehavior<SampleNoAttributeRequest, Unit>> _loggerNoAttr;
    private readonly ILogger<LawfulBasisValidationPipelineBehavior<SampleLawfulBasisDecoratedRequest, Unit>> _loggerConsent;
    private readonly ILogger<LawfulBasisValidationPipelineBehavior<SampleLegitimateInterestsRequest, Unit>> _loggerLI;
    private readonly ILogger<LawfulBasisValidationPipelineBehavior<SampleContractRequest, Unit>> _loggerContract;
    private readonly ILogger<LawfulBasisValidationPipelineBehavior<SampleMarkerOnlyRequest, Unit>> _loggerMarker;

    public LawfulBasisValidationPipelineBehaviorTests()
    {
        _registry = Substitute.For<ILawfulBasisRegistry>();
        _liaAssessment = Substitute.For<ILegitimateInterestAssessment>();
        _subjectIdExtractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        _consentProvider = Substitute.For<IConsentStatusProvider>();

        _loggerNoAttr = Substitute.For<ILogger<LawfulBasisValidationPipelineBehavior<SampleNoAttributeRequest, Unit>>>();
        _loggerConsent = Substitute.For<ILogger<LawfulBasisValidationPipelineBehavior<SampleLawfulBasisDecoratedRequest, Unit>>>();
        _loggerLI = Substitute.For<ILogger<LawfulBasisValidationPipelineBehavior<SampleLegitimateInterestsRequest, Unit>>>();
        _loggerContract = Substitute.For<ILogger<LawfulBasisValidationPipelineBehavior<SampleContractRequest, Unit>>>();
        _loggerMarker = Substitute.For<ILogger<LawfulBasisValidationPipelineBehavior<SampleMarkerOnlyRequest, Unit>>>();

        // Default registry returns None for any request type
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetByRequestTypeAsync(Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<LawfulBasisRegistration>>>(
                Right<EncinaError, Option<LawfulBasisRegistration>>(Option<LawfulBasisRegistration>.None)));
#pragma warning restore CA2012
    }

    // ================================================================
    // Disabled mode
    // ================================================================

    [Fact]
    public async Task Handle_DisabledMode_ShouldPassthrough()
    {
        // Arrange
        var behavior = CreateConsentBehavior(o => o.EnforcementMode = LawfulBasisEnforcementMode.Disabled);
        var request = new SampleLawfulBasisDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // ================================================================
    // No GDPR attributes (passthrough)
    // ================================================================

    [Fact]
    public async Task Handle_NoAttributes_ShouldPassthrough()
    {
        // Arrange
        var behavior = CreateNoAttrBehavior();
        var request = new SampleNoAttributeRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // ================================================================
    // Contract basis — simple declared basis validation
    // ================================================================

    [Fact]
    public async Task Handle_ContractBasis_ShouldSucceed()
    {
        // Arrange
        var behavior = CreateContractBehavior();
        var request = new SampleContractRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // ================================================================
    // Consent basis validation
    // ================================================================

    [Fact]
    public async Task Handle_ConsentBasis_WithValidConsent_ShouldSucceed()
    {
        // Arrange
#pragma warning disable CA2012
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<SampleLawfulBasisDecoratedRequest>(), Arg.Any<IRequestContext>())
            .Returns("user-123");

        _consentProvider.CheckConsentAsync("user-123", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentCheckResult>>(
                new ConsentCheckResult(true, [])));
#pragma warning restore CA2012

        var behavior = CreateConsentBehavior();
        var request = new SampleLawfulBasisDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(userId: "user-123"), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasis_WithoutConsent_BlockMode_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<SampleLawfulBasisDecoratedRequest>(), Arg.Any<IRequestContext>())
            .Returns("user-123");

        _consentProvider.CheckConsentAsync("user-123", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentCheckResult>>(
                new ConsentCheckResult(false, ["Test consent processing"])));
#pragma warning restore CA2012

        var behavior = CreateConsentBehavior(o => o.EnforcementMode = LawfulBasisEnforcementMode.Block);
        var request = new SampleLawfulBasisDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(userId: "user-123"), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasis_WithoutConsent_WarnMode_ShouldSucceed()
    {
        // Arrange
#pragma warning disable CA2012
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<SampleLawfulBasisDecoratedRequest>(), Arg.Any<IRequestContext>())
            .Returns("user-123");

        _consentProvider.CheckConsentAsync("user-123", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentCheckResult>>(
                new ConsentCheckResult(false, ["Marketing"])));
#pragma warning restore CA2012

        var behavior = CreateConsentBehavior(o => o.EnforcementMode = LawfulBasisEnforcementMode.Warn);
        var request = new SampleLawfulBasisDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(userId: "user-123"), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasis_NoConsentProvider_BlockMode_ShouldReturnError()
    {
        // Arrange — create behavior without consent provider
        var behavior = CreateConsentBehavior(useConsentProvider: false);
        var request = new SampleLawfulBasisDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasis_NoSubjectId_BlockMode_ShouldReturnError()
    {
        // Arrange
        _subjectIdExtractor.ExtractSubjectId(Arg.Any<SampleLawfulBasisDecoratedRequest>(), Arg.Any<IRequestContext>())
            .Returns((string?)null);

        var behavior = CreateConsentBehavior(o => o.EnforcementMode = LawfulBasisEnforcementMode.Block);
        var request = new SampleLawfulBasisDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ConsentBasis_ValidationDisabled_ShouldSkipConsentCheck()
    {
        // Arrange
        var behavior = CreateConsentBehavior(o => o.ValidateConsentForConsentBasis = false);
        var request = new SampleLawfulBasisDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
#pragma warning disable CA2012
        await _consentProvider.DidNotReceive()
            .CheckConsentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    // ================================================================
    // Legitimate interests basis validation
    // ================================================================

    [Fact]
    public async Task Handle_LegitimateInterests_ApprovedLIA_ShouldSucceed()
    {
        // Arrange
#pragma warning disable CA2012
        _liaAssessment.ValidateAsync("LIA-2024-FRAUD-001", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, LIAValidationResult>>(
                LIAValidationResult.Approved()));
#pragma warning restore CA2012

        var behavior = CreateLIBehavior();
        var request = new SampleLegitimateInterestsRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LegitimateInterests_NotApprovedLIA_BlockMode_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012
        _liaAssessment.ValidateAsync("LIA-2024-FRAUD-001", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, LIAValidationResult>>(
                LIAValidationResult.Rejected("Balancing test failed")));
#pragma warning restore CA2012

        var behavior = CreateLIBehavior(o => o.EnforcementMode = LawfulBasisEnforcementMode.Block);
        var request = new SampleLegitimateInterestsRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LegitimateInterests_NotApprovedLIA_WarnMode_ShouldSucceed()
    {
        // Arrange
#pragma warning disable CA2012
        _liaAssessment.ValidateAsync("LIA-2024-FRAUD-001", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, LIAValidationResult>>(
                LIAValidationResult.Rejected("Balancing test failed")));
#pragma warning restore CA2012

        var behavior = CreateLIBehavior(o => o.EnforcementMode = LawfulBasisEnforcementMode.Warn);
        var request = new SampleLegitimateInterestsRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LegitimateInterests_LIAValidationDisabled_ShouldSkipLIACheck()
    {
        // Arrange
        var behavior = CreateLIBehavior(o => o.ValidateLIAForLegitimateInterests = false);
        var request = new SampleLegitimateInterestsRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
#pragma warning disable CA2012
        await _liaAssessment.DidNotReceive()
            .ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    // ================================================================
    // ProcessesPersonalData marker only
    // ================================================================

    [Fact]
    public async Task Handle_MarkerOnly_RequireDeclaredBasisTrue_BlockMode_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateMarkerBehavior(o =>
        {
            o.RequireDeclaredBasis = true;
            o.EnforcementMode = LawfulBasisEnforcementMode.Block;
        });
        var request = new SampleMarkerOnlyRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MarkerOnly_RequireDeclaredBasisFalse_ShouldSucceed()
    {
        // Arrange
        var behavior = CreateMarkerBehavior(o => o.RequireDeclaredBasis = false);
        var request = new SampleMarkerOnlyRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MarkerOnly_WithRegistryFallback_ShouldSucceed()
    {
        // Arrange — set up registry to return a basis for the marker-only request
        var registration = new LawfulBasisRegistration
        {
            RequestType = typeof(SampleMarkerOnlyRequest),
            Basis = global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

#pragma warning disable CA2012
        _registry.GetByRequestTypeAsync(typeof(SampleMarkerOnlyRequest), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<LawfulBasisRegistration>>>(
                Right<EncinaError, Option<LawfulBasisRegistration>>(Some(registration))));
#pragma warning restore CA2012

        var behavior = CreateMarkerBehavior(o => o.RequireDeclaredBasis = true);
        var request = new SampleMarkerOnlyRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // ================================================================
    // Null guard
    // ================================================================

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var behavior = CreateContractBehavior();

        // Act
        var act = async () => await behavior.Handle(
            null!, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    // ================================================================
    // Helpers
    // ================================================================

    private static RequestHandlerCallback<Unit> Next(Unit value) =>
        () => ValueTask.FromResult<Either<EncinaError, Unit>>(value);

    private LawfulBasisValidationPipelineBehavior<SampleNoAttributeRequest, Unit> CreateNoAttrBehavior(
        Action<LawfulBasisOptions>? configure = null)
    {
        var options = new LawfulBasisOptions();
        configure?.Invoke(options);
        return new LawfulBasisValidationPipelineBehavior<SampleNoAttributeRequest, Unit>(
            _registry, _liaAssessment, _subjectIdExtractor, Options.Create(options), _loggerNoAttr, _consentProvider);
    }

    private LawfulBasisValidationPipelineBehavior<SampleLawfulBasisDecoratedRequest, Unit> CreateConsentBehavior(
        Action<LawfulBasisOptions>? configure = null,
        IConsentStatusProvider? consentProvider = null,
        bool useConsentProvider = true)
    {
        var options = new LawfulBasisOptions();
        configure?.Invoke(options);
        var provider = consentProvider ?? (useConsentProvider ? _consentProvider : null);
        return new LawfulBasisValidationPipelineBehavior<SampleLawfulBasisDecoratedRequest, Unit>(
            _registry, _liaAssessment, _subjectIdExtractor, Options.Create(options), _loggerConsent, provider);
    }

    private LawfulBasisValidationPipelineBehavior<SampleLegitimateInterestsRequest, Unit> CreateLIBehavior(
        Action<LawfulBasisOptions>? configure = null)
    {
        var options = new LawfulBasisOptions();
        configure?.Invoke(options);
        return new LawfulBasisValidationPipelineBehavior<SampleLegitimateInterestsRequest, Unit>(
            _registry, _liaAssessment, _subjectIdExtractor, Options.Create(options), _loggerLI, _consentProvider);
    }

    private LawfulBasisValidationPipelineBehavior<SampleContractRequest, Unit> CreateContractBehavior(
        Action<LawfulBasisOptions>? configure = null)
    {
        var options = new LawfulBasisOptions();
        configure?.Invoke(options);
        return new LawfulBasisValidationPipelineBehavior<SampleContractRequest, Unit>(
            _registry, _liaAssessment, _subjectIdExtractor, Options.Create(options), _loggerContract, _consentProvider);
    }

    private LawfulBasisValidationPipelineBehavior<SampleMarkerOnlyRequest, Unit> CreateMarkerBehavior(
        Action<LawfulBasisOptions>? configure = null)
    {
        var options = new LawfulBasisOptions();
        configure?.Invoke(options);
        return new LawfulBasisValidationPipelineBehavior<SampleMarkerOnlyRequest, Unit>(
            _registry, _liaAssessment, _subjectIdExtractor, Options.Create(options), _loggerMarker, _consentProvider);
    }

}
