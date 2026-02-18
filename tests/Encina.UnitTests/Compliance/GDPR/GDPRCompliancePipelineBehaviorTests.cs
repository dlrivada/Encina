using Encina.Compliance.GDPR;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class GDPRCompliancePipelineBehaviorTests
{
    private readonly IProcessingActivityRegistry _registry;
    private readonly IGDPRComplianceValidator _validator;
    private readonly ILogger<GDPRCompliancePipelineBehavior<SampleNoAttributeRequest, Unit>> _loggerNoAttr;
    private readonly ILogger<GDPRCompliancePipelineBehavior<SampleDecoratedRequest, Unit>> _loggerDecorated;
    private readonly ILogger<GDPRCompliancePipelineBehavior<SampleMarkerOnlyRequest, Unit>> _loggerMarker;

    public GDPRCompliancePipelineBehaviorTests()
    {
        _registry = Substitute.For<IProcessingActivityRegistry>();
        _validator = Substitute.For<IGDPRComplianceValidator>();
        _loggerNoAttr = Substitute.For<ILogger<GDPRCompliancePipelineBehavior<SampleNoAttributeRequest, Unit>>>();
        _loggerDecorated = Substitute.For<ILogger<GDPRCompliancePipelineBehavior<SampleDecoratedRequest, Unit>>>();
        _loggerMarker = Substitute.For<ILogger<GDPRCompliancePipelineBehavior<SampleMarkerOnlyRequest, Unit>>>();

        // Default: registry returns None (not registered), validator returns compliant
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetActivityByRequestTypeAsync(Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(
                Right<EncinaError, Option<ProcessingActivity>>(Option<ProcessingActivity>.None)));

        _validator.ValidateAsync(Arg.Any<object>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(ComplianceResult.Compliant()));
#pragma warning restore CA2012
    }

    // -- No attributes (passthrough) --

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
        await _registry.DidNotReceive().GetActivityByRequestTypeAsync(Arg.Any<Type>(), Arg.Any<CancellationToken>());
    }

    // -- ProcessingActivity attribute --

    [Fact]
    public async Task Handle_WithProcessingActivityAttribute_RegisteredActivity_ShouldSucceed()
    {
        // Arrange
        var activity = CreateActivity(typeof(SampleDecoratedRequest));
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetActivityByRequestTypeAsync(typeof(SampleDecoratedRequest), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(
                Right<EncinaError, Option<ProcessingActivity>>(Some(activity))));
#pragma warning restore CA2012

        var behavior = CreateDecoratedBehavior();
        var request = new SampleDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnregisteredActivity_BlockEnabled_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior(opts =>
        {
            opts.BlockUnregisteredProcessing = true;
        });
        var request = new SampleDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("No processing activity is registered");
    }

    [Fact]
    public async Task Handle_UnregisteredActivity_BlockDisabled_ShouldProceed()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior(opts =>
        {
            opts.BlockUnregisteredProcessing = false;
        });
        var request = new SampleDecoratedRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- Enforcement modes --

    [Fact]
    public async Task Handle_NonCompliant_EnforceMode_ShouldReturnError()
    {
        // Arrange
        var activity = CreateActivity(typeof(SampleDecoratedRequest));
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetActivityByRequestTypeAsync(typeof(SampleDecoratedRequest), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(
                Right<EncinaError, Option<ProcessingActivity>>(Some(activity))));

        _validator.ValidateAsync(Arg.Any<SampleDecoratedRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(
                ComplianceResult.NonCompliant("Missing consent")));
#pragma warning restore CA2012

        var behavior = CreateDecoratedBehavior(opts =>
        {
            opts.EnforcementMode = GDPREnforcementMode.Enforce;
        });

        // Act
        var result = await behavior.Handle(
            new SampleDecoratedRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("compliance validation failed");
    }

    [Fact]
    public async Task Handle_NonCompliant_WarnOnlyMode_ShouldProceed()
    {
        // Arrange
        var activity = CreateActivity(typeof(SampleDecoratedRequest));
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetActivityByRequestTypeAsync(typeof(SampleDecoratedRequest), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(
                Right<EncinaError, Option<ProcessingActivity>>(Some(activity))));

        _validator.ValidateAsync(Arg.Any<SampleDecoratedRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(
                ComplianceResult.NonCompliant("Missing consent")));
#pragma warning restore CA2012

        var behavior = CreateDecoratedBehavior(opts =>
        {
            opts.EnforcementMode = GDPREnforcementMode.WarnOnly;
        });

        // Act
        var result = await behavior.Handle(
            new SampleDecoratedRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- Registry lookup error --

    [Fact]
    public async Task Handle_RegistryLookupFails_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetActivityByRequestTypeAsync(typeof(SampleDecoratedRequest), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(
                Left<EncinaError, Option<ProcessingActivity>>(EncinaError.New("Registry unavailable"))));
#pragma warning restore CA2012

        var behavior = CreateDecoratedBehavior();

        // Act
        var result = await behavior.Handle(
            new SampleDecoratedRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("Failed to look up");
    }

    // -- Validator error --

    [Fact]
    public async Task Handle_ValidatorReturnsError_ShouldReturnError()
    {
        // Arrange
        var activity = CreateActivity(typeof(SampleDecoratedRequest));
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetActivityByRequestTypeAsync(typeof(SampleDecoratedRequest), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(
                Right<EncinaError, Option<ProcessingActivity>>(Some(activity))));

        _validator.ValidateAsync(Arg.Any<SampleDecoratedRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(
                Left<EncinaError, ComplianceResult>(EncinaError.New("Validator error"))));
#pragma warning restore CA2012

        var behavior = CreateDecoratedBehavior();

        // Act
        var result = await behavior.Handle(
            new SampleDecoratedRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    // -- ProcessesPersonalData marker attribute --

    [Fact]
    public async Task Handle_MarkerAttribute_UnregisteredBlocked_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateMarkerBehavior(opts =>
        {
            opts.BlockUnregisteredProcessing = true;
        });

        // Act
        var result = await behavior.Handle(
            new SampleMarkerOnlyRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    // -- Warnings --

    [Fact]
    public async Task Handle_CompliantWithWarnings_ShouldProceed()
    {
        // Arrange
        var activity = CreateActivity(typeof(SampleDecoratedRequest));
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _registry.GetActivityByRequestTypeAsync(typeof(SampleDecoratedRequest), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(
                Right<EncinaError, Option<ProcessingActivity>>(Some(activity))));

        _validator.ValidateAsync(Arg.Any<SampleDecoratedRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(
                ComplianceResult.CompliantWithWarnings("Retention approaching limit")));
#pragma warning restore CA2012

        var behavior = CreateDecoratedBehavior();

        // Act
        var result = await behavior.Handle(
            new SampleDecoratedRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- Null request --

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior();

        // Act
        var act = async () => await behavior.Handle(
            null!, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    // -- Helpers --

    private static RequestHandlerCallback<Unit> Next(Unit value) =>
        () => ValueTask.FromResult<Either<EncinaError, Unit>>(value);

    private static ProcessingActivity CreateActivity(Type requestType) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        Purpose = "Testing",
        LawfulBasis = LawfulBasis.Contract,
        CategoriesOfDataSubjects = ["Users"],
        CategoriesOfPersonalData = ["Email"],
        Recipients = [],
        RetentionPeriod = TimeSpan.FromDays(365),
        SecurityMeasures = "Encryption",
        RequestType = requestType,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private GDPRCompliancePipelineBehavior<SampleNoAttributeRequest, Unit> CreateNoAttrBehavior(
        Action<GDPROptions>? configure = null)
    {
        var options = new GDPROptions();
        configure?.Invoke(options);
        return new GDPRCompliancePipelineBehavior<SampleNoAttributeRequest, Unit>(
            _registry, _validator, Options.Create(options), _loggerNoAttr);
    }

    private GDPRCompliancePipelineBehavior<SampleDecoratedRequest, Unit> CreateDecoratedBehavior(
        Action<GDPROptions>? configure = null)
    {
        var options = new GDPROptions();
        configure?.Invoke(options);
        return new GDPRCompliancePipelineBehavior<SampleDecoratedRequest, Unit>(
            _registry, _validator, Options.Create(options), _loggerDecorated);
    }

    private GDPRCompliancePipelineBehavior<SampleMarkerOnlyRequest, Unit> CreateMarkerBehavior(
        Action<GDPROptions>? configure = null)
    {
        var options = new GDPROptions();
        configure?.Invoke(options);
        return new GDPRCompliancePipelineBehavior<SampleMarkerOnlyRequest, Unit>(
            _registry, _validator, Options.Create(options), _loggerMarker);
    }
}
