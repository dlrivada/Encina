using ArchUnitNET.Fluent;

namespace Encina.Testing.Architecture.Tests;

/// <summary>
/// Tests for <see cref="EncinaArchitectureRulesBuilder"/>.
/// </summary>
public sealed class EncinaArchitectureRulesBuilderTests
{
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

        // Act
        var result = builder.EnforceHandlerAbstractions();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceSealedNotifications_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceSealedNotifications();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceSealedHandlers_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceSealedHandlers();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceSealedBehaviors_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceSealedBehaviors();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceValidatorNaming_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceValidatorNaming();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceDomainMessagingIsolation_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceDomainMessagingIsolation("MyApp.Domain");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceLayerSeparation_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceLayerSeparation("MyApp.Domain", "MyApp.Application", "MyApp.Infrastructure");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceRepositoryInterfacesInDomain_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceRepositoryInterfacesInDomain("MyApp.Domain");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void EnforceRepositoryImplementationsInInfrastructure_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.EnforceRepositoryImplementationsInInfrastructure("MyApp.Infrastructure");

        // Assert
        result.ShouldBeSameAs(builder);
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
        var rule = EncinaArchitectureRules.HandlersShouldBeSealed();

        // Act
        var result = builder.AddCustomRule(rule);

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void ApplyAllStandardRules_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(EncinaArchitectureRulesBuilderTests).Assembly);

        // Act
        var result = builder.ApplyAllStandardRules();

        // Assert
        result.ShouldBeSameAs(builder);
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
}
