using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Encina.Testing.Architecture;
using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Tests for <see cref="EncinaArchitectureRules"/>.
/// </summary>
public sealed class EncinaArchitectureRulesTests
{
    private static readonly ArchUnitNET.Domain.Architecture TestArchitecture;

    static EncinaArchitectureRulesTests()
    {
        TestArchitecture = new ArchLoader()
            .LoadAssemblies(typeof(EncinaArchitectureRulesTests).Assembly)
            .Build();
    }

    [Fact]
    public void HandlersShouldNotDependOnInfrastructure_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.HandlersShouldNotDependOnInfrastructure();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Handler");
    }

    [Fact]
    public void HandlersShouldNotDependOnInfrastructure_PassesForCompliantHandlers()
    {
        // Arrange
        var rule = EncinaArchitectureRules.HandlersShouldNotDependOnInfrastructure();

        // Act & Assert - Should not throw
        rule.Check(TestArchitecture);
    }

    [Fact]
    public void NotificationsShouldBeSealed_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.NotificationsShouldBeSealed();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("sealed");
    }

    [Fact]
    public void HandlersShouldBeSealed_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.HandlersShouldBeSealed();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("sealed");
    }

    [Fact]
    public void BehaviorsShouldBeSealed_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.BehaviorsShouldBeSealed();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("sealed");
    }

    [Fact]
    public void ValidatorsShouldFollowNamingConvention_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.ValidatorsShouldFollowNamingConvention();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Validator");
    }

    [Fact]
    public void ValidatorsShouldFollowNamingConvention_PassesForCompliantValidators()
    {
        // Arrange
        var rule = EncinaArchitectureRules.ValidatorsShouldFollowNamingConvention();

        // Act & Assert - Should not throw
        rule.Check(TestArchitecture);
    }

    [Fact]
    public void DomainShouldNotDependOnMessaging_ThrowsForNullNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.DomainShouldNotDependOnMessaging(null!));
    }

    [Fact]
    public void DomainShouldNotDependOnMessaging_ThrowsForEmptyNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.DomainShouldNotDependOnMessaging(""));
    }

    [Fact]
    public void DomainShouldNotDependOnMessaging_ThrowsForWhitespaceNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.DomainShouldNotDependOnMessaging("   "));
    }

    [Fact]
    public void DomainShouldNotDependOnMessaging_ReturnsValidRuleForValidNamespace()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.DomainShouldNotDependOnMessaging("MyApp.Domain");

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("messaging");
    }

    [Fact]
    public void DomainShouldNotDependOnApplication_ThrowsForNullDomainNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.DomainShouldNotDependOnApplication(null!, "MyApp.Application"));
    }

    [Fact]
    public void DomainShouldNotDependOnApplication_ThrowsForNullApplicationNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.DomainShouldNotDependOnApplication("MyApp.Domain", null!));
    }

    [Fact]
    public void DomainShouldNotDependOnApplication_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.DomainShouldNotDependOnApplication("MyApp.Domain", "MyApp.Application");

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Application");
    }

    [Fact]
    public void ApplicationShouldNotDependOnInfrastructure_ThrowsForNullApplicationNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.ApplicationShouldNotDependOnInfrastructure(null!, "MyApp.Infrastructure"));
    }

    [Fact]
    public void ApplicationShouldNotDependOnInfrastructure_ThrowsForNullInfrastructureNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.ApplicationShouldNotDependOnInfrastructure("MyApp.Application", null!));
    }

    [Fact]
    public void ApplicationShouldNotDependOnInfrastructure_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.ApplicationShouldNotDependOnInfrastructure("MyApp.Application", "MyApp.Infrastructure");

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Infrastructure");
    }

    [Fact]
    public void RepositoryInterfacesShouldResideInDomain_ThrowsForNullNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.RepositoryInterfacesShouldResideInDomain(null!));
    }

    [Fact]
    public void RepositoryInterfacesShouldResideInDomain_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.RepositoryInterfacesShouldResideInDomain("MyApp.Domain");

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Domain");
    }

    [Fact]
    public void RepositoryImplementationsShouldResideInInfrastructure_ThrowsForNullNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.RepositoryImplementationsShouldResideInInfrastructure(null!));
    }

    [Fact]
    public void RepositoryImplementationsShouldResideInInfrastructure_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.RepositoryImplementationsShouldResideInInfrastructure("MyApp.Infrastructure");

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Infrastructure");
    }

    [Fact]
    public void CleanArchitectureLayersShouldBeSeparated_ThrowsForNullDomainNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.CleanArchitectureLayersShouldBeSeparated(null!, "MyApp.Application", "MyApp.Infrastructure"));
    }

    [Fact]
    public void CleanArchitectureLayersShouldBeSeparated_ThrowsForNullApplicationNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.CleanArchitectureLayersShouldBeSeparated("MyApp.Domain", null!, "MyApp.Infrastructure"));
    }

    [Fact]
    public void CleanArchitectureLayersShouldBeSeparated_ThrowsForNullInfrastructureNamespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => EncinaArchitectureRules.CleanArchitectureLayersShouldBeSeparated("MyApp.Domain", "MyApp.Application", null!));
    }

    [Fact]
    public void CleanArchitectureLayersShouldBeSeparated_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.CleanArchitectureLayersShouldBeSeparated(
            "MyApp.Domain",
            "MyApp.Application",
            "MyApp.Infrastructure");

        // Assert
        rule.ShouldNotBeNull();
    }

    #region CQRS Pattern Rules Tests

    [Fact]
    public void HandlersShouldImplementCorrectInterface_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.HandlersShouldImplementCorrectInterface();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Handler");
    }

    [Fact]
    public void CommandsShouldImplementICommand_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.CommandsShouldImplementICommand();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Command");
    }

    [Fact]
    public void QueriesShouldImplementIQuery_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.QueriesShouldImplementIQuery();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Query");
    }

    [Fact]
    public void HandlersShouldNotDependOnControllers_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.HandlersShouldNotDependOnControllers();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("presentation");
    }

    [Fact]
    public void HandlersShouldNotDependOnControllers_PassesForCompliantHandlers()
    {
        // Arrange
        var rule = EncinaArchitectureRules.HandlersShouldNotDependOnControllers();

        // Act & Assert - Should not throw (test handlers don't depend on controllers)
        rule.Check(TestArchitecture);
    }

    #endregion

    #region Pipeline Behavior Rules Tests

    [Fact]
    public void PipelineBehaviorsShouldImplementCorrectInterface_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.PipelineBehaviorsShouldImplementCorrectInterface();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("Pipeline");
    }

    #endregion

    #region Saga Pattern Rules Tests

    [Fact]
    public void SagaDataShouldBeSealed_ReturnsValidRule()
    {
        // Arrange & Act
        var rule = EncinaArchitectureRules.SagaDataShouldBeSealed();

        // Assert
        rule.ShouldNotBeNull();
        rule.Description.ShouldContain("sealed");
    }

    // Note: SagaDataShouldBeSealed integration test requires a test assembly with *SagaData classes.
    // The rule itself is tested in SagaDataShouldBeSealed_ReturnsValidRule above.

    #endregion
}
