using ArchUnitNET.Fluent;
using Encina.Testing.Architecture;
using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Tests for <see cref="EncinaArchitectureRulesBuilder"/>.
/// </summary>
public sealed class EncinaArchitectureRulesBuilderTests
{
    /// <summary>
    /// Expected number of rules added by <see cref="EncinaArchitectureRulesBuilder.ApplyAllStandardRules"/>.
    /// </summary>
    /// <remarks>
    /// Includes: EnforceHandlerAbstractions, EnforceSealedNotifications, EnforceSealedHandlers,
    /// EnforceSealedBehaviors, EnforceValidatorNaming, EnforceSealedEventHandlers,
    /// EnforceHandlerInterfaces, EnforceCommandInterfaces, EnforceQueryInterfaces,
    /// EnforceHandlerControllerIsolation, EnforcePipelineBehaviorInterfaces.
    /// Excludes saga rules (EnforceSealedSagas, EnforceSealedSagaData).
    /// </remarks>
    private const int ExpectedStandardRuleCount = 11;

    /// <summary>
    /// Expected number of rules added by <see cref="EncinaArchitectureRulesBuilder.ApplyAllSagaRules"/>.
    /// </summary>
    /// <remarks>
    /// Includes: EnforceSealedSagas, EnforceSealedSagaData.
    /// </remarks>
    private const int ExpectedSagaRuleCount = 2;

    [Fact]
    public void Constructor_ThrowsForNullAssemblies()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new EncinaArchitectureRulesBuilder(null!));
    }

    [Fact]
    public void Constructor_ThrowsForEmptyAssemblies()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new EncinaArchitectureRulesBuilder([]));
    }

    [Fact]
    public void Constructor_SucceedsWithValidAssembly()
    {
        // Arrange & Act
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Assert
        builder.ShouldNotBeNull();
        builder.Architecture.ShouldNotBeNull();
    }

    [Fact]
    public void EnforceHandlerAbstractions_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceHandlerAbstractions();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceSealedNotifications_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceSealedNotifications();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceSealedHandlers_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceSealedHandlers();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceSealedBehaviors_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceSealedBehaviors();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceValidatorNaming_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceValidatorNaming();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceDomainMessagingIsolation_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceDomainMessagingIsolation("MyApp.Domain");

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceLayerSeparation_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceLayerSeparation("MyApp.Domain", "MyApp.Application", "MyApp.Infrastructure");

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceRepositoryInterfacesInDomain_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceRepositoryInterfacesInDomain("MyApp.Domain");

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceRepositoryImplementationsInInfrastructure_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceRepositoryImplementationsInInfrastructure("MyApp.Infrastructure");

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void AddCustomRule_ThrowsForNullRule()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddCustomRule(null!));
    }

    [Fact]
    public void AddCustomRule_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;
        var rule = EncinaArchitectureRules.HandlersShouldBeSealed();

        // Act
        var result = builder.AddCustomRule(rule);

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void ApplyAllStandardRules_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.ApplyAllStandardRules();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + ExpectedStandardRuleCount);
    }

    [Fact]
    public void ApplyAllSagaRules_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.ApplyAllSagaRules();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + ExpectedSagaRuleCount);
    }

    [Fact]
    public void Verify_SucceedsWithNoRules()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act & Assert - Should not throw
        Should.NotThrow(() => builder.Verify());
    }

    [Fact]
    public void Verify_SucceedsWithCompliantRules()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly)
            .EnforceHandlerAbstractions()
            .EnforceValidatorNaming();

        // Act & Assert - Should not throw
        Should.NotThrow(() => builder.Verify());
    }

    [Fact]
    public void VerifyWithResult_ReturnsSuccessWithNoRules()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Violations.ShouldBeEmpty();
    }

    [Fact]
    public void VerifyWithResult_ReturnsSuccessWithCompliantRules()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly)
            .EnforceHandlerAbstractions()
            .EnforceValidatorNaming();

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Violations.ShouldBeEmpty();
    }

    [Fact]
    public void FluentChaining_WorksCorrectly()
    {
        // Arrange & Act
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly)
            .EnforceHandlerAbstractions()
            .EnforceSealedNotifications()
            .EnforceSealedHandlers()
            .EnforceSealedBehaviors()
            .EnforceValidatorNaming()
            .EnforceDomainMessagingIsolation("MyApp.Domain")
            .EnforceLayerSeparation("MyApp.Domain", "MyApp.Application", "MyApp.Infrastructure");

        // Assert
        builder.ShouldNotBeNull();
    }

    #region CQRS Pattern Rules Builder Tests

    [Fact]
    public void EnforceHandlerInterfaces_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceHandlerInterfaces();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceCommandInterfaces_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceCommandInterfaces();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceQueryInterfaces_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceQueryInterfaces();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    [Fact]
    public void EnforceHandlerControllerIsolation_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceHandlerControllerIsolation();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    #endregion

    #region Pipeline Behavior Builder Tests

    [Fact]
    public void EnforcePipelineBehaviorInterfaces_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforcePipelineBehaviorInterfaces();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    #endregion

    #region Saga Rules Builder Tests

    [Fact]
    public void EnforceSealedSagaData_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);
        var initialCount = builder.RuleCount;

        // Act
        var result = builder.EnforceSealedSagaData();

        // Assert
        result.ShouldBeSameAs(builder);
        builder.RuleCount.ShouldBe(initialCount + 1);
    }

    #endregion

    #region Complete Fluent Chaining Tests

    [Fact]
    public void FluentChaining_WithAllNewRules_WorksCorrectly()
    {
        // Arrange & Act
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly)
            .EnforceHandlerInterfaces()
            .EnforceCommandInterfaces()
            .EnforceQueryInterfaces()
            .EnforceHandlerControllerIsolation()
            .EnforcePipelineBehaviorInterfaces()
            .EnforceSealedSagaData();

        // Assert
        builder.ShouldNotBeNull();
        builder.RuleCount.ShouldBe(6);
    }

    #endregion
}
