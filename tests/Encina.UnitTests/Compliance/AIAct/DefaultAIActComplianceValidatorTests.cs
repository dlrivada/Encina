using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="DefaultAIActComplianceValidator"/>.
/// </summary>
public class DefaultAIActComplianceValidatorTests
{
    private readonly IAIActClassifier _classifier;
    private readonly IAISystemRegistry _registry;
    private readonly IHumanOversightEnforcer _oversightEnforcer;
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly DefaultAIActComplianceValidator _sut;

    public DefaultAIActComplianceValidatorTests()
    {
        _classifier = Substitute.For<IAIActClassifier>();
        _registry = Substitute.For<IAISystemRegistry>();
        _oversightEnforcer = Substitute.For<IHumanOversightEnforcer>();

        // Default: not registered, oversight not required
        _registry.IsRegistered(Arg.Any<string>()).Returns(false);

#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _oversightEnforcer.RequiresHumanReviewAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
#pragma warning restore CA2012

        _sut = new DefaultAIActComplianceValidator(
            _classifier, _registry, _oversightEnforcer, _timeProvider);
    }

    // -- Unregistered systems --

    [Fact]
    public async Task ValidateAsync_UnregisteredSystem_ShouldReturnMinimalRisk()
    {
        // Arrange
        var request = new SampleNoAttributeRequest();

        // Act
        var result = await _sut.ValidateAsync(request, null);

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.RiskLevel.ShouldBe(AIRiskLevel.MinimalRisk);
        compliance.IsProhibited.ShouldBeFalse();
        compliance.Violations.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_ExplicitSystemId_NotRegistered_ShouldReturnMinimalRisk()
    {
        // Arrange
        var request = new SampleNoAttributeRequest();

        // Act
        var result = await _sut.ValidateAsync(request, "unknown-system");

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.RiskLevel.ShouldBe(AIRiskLevel.MinimalRisk);
    }

    // -- Registered system --

    [Fact]
    public async Task ValidateAsync_RegisteredSystem_ShouldDelegateToClassifier()
    {
        // Arrange
        _registry.IsRegistered("test-system").Returns(true);

        var complianceResult = new AIActComplianceResult
        {
            SystemId = "test-system",
            RiskLevel = AIRiskLevel.HighRisk,
            IsProhibited = false,
            RequiresHumanOversight = true,
            RequiresTransparency = true,
            EvaluatedAtUtc = _timeProvider.GetUtcNow()
        };

#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _classifier.EvaluateComplianceAsync("test-system", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, AIActComplianceResult>(complianceResult)));
#pragma warning restore CA2012

        var request = new SampleNoAttributeRequest();

        // Act
        var result = await _sut.ValidateAsync(request, "test-system");

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = (AIActComplianceResult)result;
        compliance.RiskLevel.ShouldBe(AIRiskLevel.HighRisk);
    }

    // -- Classifier error --

    [Fact]
    public async Task ValidateAsync_ClassifierFails_ShouldReturnError()
    {
        // Arrange
        _registry.IsRegistered("test-system").Returns(true);

#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _classifier.EvaluateComplianceAsync("test-system", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, AIActComplianceResult>>(
                EncinaError.New("Classifier failed")));
#pragma warning restore CA2012

        var request = new SampleNoAttributeRequest();

        // Act
        var result = await _sut.ValidateAsync(request, "test-system");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // -- Attribute-based system ID resolution --

    [Fact]
    public async Task ValidateAsync_HighRiskAttribute_ShouldResolveSystemId()
    {
        // Arrange
        var fullName = typeof(SampleHighRiskRequest).FullName!;
        _registry.IsRegistered(fullName).Returns(true);

        var complianceResult = new AIActComplianceResult
        {
            SystemId = fullName,
            RiskLevel = AIRiskLevel.HighRisk,
            IsProhibited = false,
            RequiresHumanOversight = true,
            RequiresTransparency = false,
            EvaluatedAtUtc = _timeProvider.GetUtcNow()
        };

#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _classifier.EvaluateComplianceAsync(fullName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, AIActComplianceResult>(complianceResult)));
#pragma warning restore CA2012

        var request = new SampleHighRiskRequest();

        // Act
        var result = await _sut.ValidateAsync(request, (string?)null);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_HighRiskAttributeWithExplicitSystemId_ShouldUseAttributeSystemId()
    {
        // Arrange
        _registry.IsRegistered("cv-screener").Returns(true);

        var complianceResult = new AIActComplianceResult
        {
            SystemId = "cv-screener",
            RiskLevel = AIRiskLevel.HighRisk,
            IsProhibited = false,
            RequiresHumanOversight = false,
            RequiresTransparency = false,
            EvaluatedAtUtc = _timeProvider.GetUtcNow()
        };

#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _classifier.EvaluateComplianceAsync("cv-screener", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, AIActComplianceResult>(complianceResult)));
#pragma warning restore CA2012

        var request = new SampleHighRiskWithIdRequest();

        // Act
        var result = await _sut.ValidateAsync(request, (string?)null);

        // Assert
        result.IsRight.ShouldBeTrue();
    }
}
