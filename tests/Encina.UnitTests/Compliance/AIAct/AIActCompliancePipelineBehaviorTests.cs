using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Model;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="AIActCompliancePipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class AIActCompliancePipelineBehaviorTests
{
    private readonly IAIActComplianceValidator _validator;

    public AIActCompliancePipelineBehaviorTests()
    {
        _validator = Substitute.For<IAIActComplianceValidator>();
    }

    // -- Disabled mode --

    [Fact]
    public async Task Handle_DisabledMode_ShouldPassthrough()
    {
        // Arrange
        var behavior = CreateHighRiskBehavior(opts => opts.EnforcementMode = AIActEnforcementMode.Disabled);

        // Act
        var result = await behavior.Handle(
            new SampleHighRiskRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // -- No attributes (passthrough) --

    [Fact]
    public async Task Handle_NoAttributes_ShouldPassthrough()
    {
        // Arrange
        var behavior = CreateNoAttrBehavior();

        // Act
        var result = await behavior.Handle(
            new SampleNoAttributeRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // -- Prohibited use (always blocked) --

    [Fact]
    public async Task Handle_ProhibitedUse_BlockMode_ShouldReturnError()
    {
        // Arrange
        SetupValidator(prohibited: true, violations: ["Art. 5: Social scoring"]);
        var behavior = CreateHighRiskBehavior();

        // Act
        var result = await behavior.Handle(
            new SampleHighRiskRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ProhibitedUse_WarnMode_ShouldStillBlock()
    {
        // Arrange — prohibited practices are ALWAYS blocked regardless of enforcement mode
        SetupValidator(prohibited: true, violations: ["Art. 5: Social scoring"]);
        var behavior = CreateHighRiskBehavior(opts => opts.EnforcementMode = AIActEnforcementMode.Warn);

        // Act
        var result = await behavior.Handle(
            new SampleHighRiskRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // -- Violations in Block mode --

    [Fact]
    public async Task Handle_ViolationsInBlockMode_ShouldReturnError()
    {
        // Arrange
        SetupValidator(violations: ["Missing Art. 14 oversight documentation"]);
        var behavior = CreateHighRiskBehavior(opts => opts.EnforcementMode = AIActEnforcementMode.Block);

        // Act
        var result = await behavior.Handle(
            new SampleHighRiskRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // -- Violations in Warn mode --

    [Fact]
    public async Task Handle_ViolationsInWarnMode_ShouldProceed()
    {
        // Arrange
        SetupValidator(violations: ["Minor transparency gap"]);
        var behavior = CreateHighRiskBehavior(opts => opts.EnforcementMode = AIActEnforcementMode.Warn);

        // Act
        var result = await behavior.Handle(
            new SampleHighRiskRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // -- No violations --

    [Fact]
    public async Task Handle_Compliant_ShouldProceed()
    {
        // Arrange
        SetupValidator();
        var behavior = CreateHighRiskBehavior();

        // Act
        var result = await behavior.Handle(
            new SampleHighRiskRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // -- Validator error --

    [Fact]
    public async Task Handle_ValidatorReturnsError_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _validator.ValidateAsync(Arg.Any<SampleHighRiskRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, AIActComplianceResult>>(
                Left<EncinaError, AIActComplianceResult>(EncinaError.New("Validator crashed"))));
#pragma warning restore CA2012

        var behavior = CreateHighRiskBehavior();

        // Act
        var result = await behavior.Handle(
            new SampleHighRiskRequest(), RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // -- Null request --

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var behavior = CreateHighRiskBehavior();

        // Act
        var act = async () => await behavior.Handle(
            null!, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("request");
    }

    // -- Helpers --

    private static RequestHandlerCallback<Unit> Next(Unit value) =>
        () => ValueTask.FromResult<Either<EncinaError, Unit>>(value);

    private void SetupValidator(
        bool prohibited = false,
        IReadOnlyList<string>? violations = null,
        AIRiskLevel riskLevel = AIRiskLevel.HighRisk)
    {
        var complianceResult = new AIActComplianceResult
        {
            SystemId = "test-system",
            RiskLevel = riskLevel,
            IsProhibited = prohibited,
            RequiresHumanOversight = false,
            RequiresTransparency = false,
            Violations = violations ?? [],
            EvaluatedAtUtc = DateTimeOffset.UtcNow
        };

#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _validator.ValidateAsync(Arg.Any<object>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, AIActComplianceResult>(complianceResult)));
#pragma warning restore CA2012
    }

    private AIActCompliancePipelineBehavior<SampleNoAttributeRequest, Unit> CreateNoAttrBehavior(
        Action<AIActOptions>? configure = null)
    {
        var options = new AIActOptions();
        configure?.Invoke(options);
        return new AIActCompliancePipelineBehavior<SampleNoAttributeRequest, Unit>(
            _validator, Options.Create(options),
            new NullLogger<AIActCompliancePipelineBehavior<SampleNoAttributeRequest, Unit>>());
    }

    private AIActCompliancePipelineBehavior<SampleHighRiskRequest, Unit> CreateHighRiskBehavior(
        Action<AIActOptions>? configure = null)
    {
        var options = new AIActOptions();
        configure?.Invoke(options);
        return new AIActCompliancePipelineBehavior<SampleHighRiskRequest, Unit>(
            _validator, Options.Create(options),
            new NullLogger<AIActCompliancePipelineBehavior<SampleHighRiskRequest, Unit>>());
    }
}
