using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Model;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="DefaultAIActDocumentation"/>.
/// </summary>
public sealed class DefaultAIActDocumentationTests
{
    private readonly IAISystemRegistry _registry;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultAIActDocumentation _sut;

    public DefaultAIActDocumentationTests()
    {
        _registry = Substitute.For<IAISystemRegistry>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        _sut = new DefaultAIActDocumentation(_registry, _timeProvider);
    }

    [Fact]
    public async Task GenerateDocumentationAsync_WhenSystemExists_ReturnsDocumentation()
    {
        // Arrange
        var registration = new AISystemRegistration
        {
            SystemId = "test-system",
            Name = "TestSystem",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            Description = "Custom description",
            RegisteredAtUtc = _timeProvider.GetUtcNow()
        };

#pragma warning disable CA2012
        _registry.GetSystemAsync("test-system", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, AISystemRegistration>(registration)));
#pragma warning restore CA2012

        // Act
        var result = await _sut.GenerateDocumentationAsync("test-system");

        // Assert
        result.IsRight.ShouldBeTrue();
        var doc = (TechnicalDocumentation)result;
        doc.SystemId.ShouldBe("test-system");
        doc.Description.ShouldBe("Custom description");
        doc.GeneratedAtUtc.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task GenerateDocumentationAsync_WhenDescriptionNull_GeneratesFallback()
    {
        // Arrange
        var registration = new AISystemRegistration
        {
            SystemId = "test-system",
            Name = "TestSystem",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            Description = null,
            RegisteredAtUtc = _timeProvider.GetUtcNow()
        };

#pragma warning disable CA2012
        _registry.GetSystemAsync("test-system", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, AISystemRegistration>(registration)));
#pragma warning restore CA2012

        // Act
        var result = await _sut.GenerateDocumentationAsync("test-system");

        // Assert
        result.IsRight.ShouldBeTrue();
        var doc = (TechnicalDocumentation)result;
        doc.Description.ShouldContain("TestSystem");
        doc.Description.ShouldContain("EmploymentWorkersManagement");
    }

    [Fact]
    public async Task GenerateDocumentationAsync_WhenSystemNotFound_ReturnsError()
    {
        // Arrange
#pragma warning disable CA2012
        _registry.GetSystemAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, AISystemRegistration>>(
                EncinaError.New("System not found")));
#pragma warning restore CA2012

        // Act
        var result = await _sut.GenerateDocumentationAsync("nonexistent");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void GenerateDocumentationAsync_WithNullSystemId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.GenerateDocumentationAsync(null!).AsTask());
    }

    [Fact]
    public async Task UpdateDocumentationAsync_WhenSystemExists_ReturnsSuccess()
    {
        // Arrange
        var registration = new AISystemRegistration
        {
            SystemId = "test-system",
            Name = "TestSystem",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = _timeProvider.GetUtcNow()
        };

#pragma warning disable CA2012
        _registry.GetSystemAsync("test-system", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, AISystemRegistration>(registration)));
#pragma warning restore CA2012

        var doc = new TechnicalDocumentation
        {
            SystemId = "test-system",
            Description = "Updated doc",
            GeneratedAtUtc = _timeProvider.GetUtcNow()
        };

        // Act
        var result = await _sut.UpdateDocumentationAsync("test-system", doc);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateDocumentationAsync_WhenSystemNotFound_ReturnsError()
    {
        // Arrange
#pragma warning disable CA2012
        _registry.GetSystemAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, AISystemRegistration>>(
                EncinaError.New("Not found")));
#pragma warning restore CA2012

        var doc = new TechnicalDocumentation
        {
            SystemId = "nonexistent",
            Description = "Test",
            GeneratedAtUtc = _timeProvider.GetUtcNow()
        };

        // Act
        var result = await _sut.UpdateDocumentationAsync("nonexistent", doc);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultAIActDocumentation(null!, _timeProvider));
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultAIActDocumentation(_registry, null!));
    }
}
