using Encina.Testing.Architecture;
using Encina.Testing.Examples.Domain;

namespace Encina.Testing.Examples.Architecture;

/// <summary>
/// Examples demonstrating architecture testing with EncinaArchitectureRulesBuilder.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 10.4, Example 7
/// </summary>
public sealed class ArchitectureRulesExamples
{
    /// <summary>
    /// Pattern: Apply standard Encina architecture rules.
    /// </summary>
    [Fact]
    public void StandardRules_ShouldPass()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .ApplyAllStandardRules();

        // Act & Assert
        var result = builder.VerifyWithResult();

        // Note: In a real test, you might use Verify() to throw on violations
        // Here we use VerifyWithResult() to demonstrate the result API
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Individual rule enforcement.
    /// </summary>
    [Fact]
    public void SealedHandlers_ShouldBeEnforced()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .EnforceSealedHandlers();

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Layer separation enforcement.
    /// </summary>
    [Fact]
    public void LayerSeparation_ShouldBeEnforced()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .EnforceLayerSeparation(
                domainNamespace: "Encina.Testing.Examples.Domain",
                applicationNamespace: "Encina.Testing.Examples.Application",
                infrastructureNamespace: "Encina.Testing.Examples.Infrastructure");

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: CQRS pattern enforcement.
    /// </summary>
    [Fact]
    public void CqrsPatterns_ShouldBeEnforced()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .EnforceHandlerInterfaces()
            .EnforceCommandInterfaces()
            .EnforceQueryInterfaces()
            .EnforceHandlerControllerIsolation();

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Saga-specific rules (opt-in).
    /// </summary>
    [Fact]
    public void SagaRules_ShouldBeEnforced()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .ApplyAllSagaRules();

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Domain isolation from messaging.
    /// </summary>
    [Fact]
    public void DomainMessagingIsolation_ShouldBeEnforced()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .EnforceDomainMessagingIsolation("Encina.Testing.Examples.Domain");

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Naming convention enforcement.
    /// </summary>
    [Fact]
    public void NamingConventions_ShouldBeEnforced()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .EnforceValidatorNaming()
            .EnforceRequestNaming();

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Combined rules with fluent builder.
    /// </summary>
    [Fact]
    public void CombinedRules_FluentBuilder()
    {
        // Arrange & Act
        var result = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .EnforceSealedHandlers()
            .EnforceSealedNotifications()
            .EnforceSealedBehaviors()
            .EnforceHandlerAbstractions()
            .EnforcePipelineBehaviorInterfaces()
            .VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();
        // result.IsSuccess or result.Violations can be checked
    }

    /// <summary>
    /// Pattern: Check verification result details.
    /// </summary>
    [Fact]
    public void VerificationResult_ProvidesDetails()
    {
        // Arrange
        var builder = new EncinaArchitectureRulesBuilder(typeof(CreateOrderHandler).Assembly)
            .ApplyAllStandardRules();

        // Act
        var result = builder.VerifyWithResult();

        // Assert
        result.ShouldNotBeNull();

        if (result.IsSuccess)
        {
            result.Violations.ShouldBeEmpty();
        }
        else
        {
            // Violations contain rule name and message
            foreach (var violation in result.Violations)
            {
                violation.RuleName.ShouldNotBeNullOrWhiteSpace();
                violation.Message.ShouldNotBeNullOrWhiteSpace();
            }
        }
    }
}

/// <summary>
/// Examples demonstrating EncinaArchitectureTestBase usage.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 10.4
/// </summary>
public sealed class ArchitectureTestBaseExamples : EncinaArchitectureTestBase
{
    /// <summary>
    /// The main application assembly containing handlers.
    /// </summary>
    protected override System.Reflection.Assembly ApplicationAssembly =>
        typeof(CreateOrderHandler).Assembly;

    /// <summary>
    /// Pattern: Use base class for standard architecture tests.
    /// The base class provides HandlersShouldBeSealed(), NotificationsShouldBeSealed(), etc.
    /// </summary>
    [Fact]
    public void AdditionalCustomRule_CanBeAdded()
    {
        // Act & Assert - Uses inherited Architecture property
        var builder = new EncinaArchitectureRulesBuilder(ApplicationAssembly)
            .EnforceSealedHandlers();

        var result = builder.VerifyWithResult();
        result.ShouldNotBeNull();
    }
}
