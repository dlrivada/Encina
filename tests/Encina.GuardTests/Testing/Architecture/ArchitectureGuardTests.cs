using System.Reflection;

using Encina.Testing.Architecture;

using Shouldly;

namespace Encina.GuardTests.Testing.Architecture;

/// <summary>
/// Guard tests for Encina.Testing.Architecture covering null-guard clauses and happy paths
/// for <see cref="EncinaArchitectureRules"/>, <see cref="EncinaArchitectureRulesBuilder"/>,
/// and <see cref="EventIdUniquenessRule"/>.
/// </summary>
[Trait("Category", "Guard")]
public sealed class ArchitectureGuardTests
{
    private static readonly Assembly TestAssembly = typeof(ArchitectureGuardTests).Assembly;

    // ─── EncinaArchitectureRules: parameterless factory methods ───

    [Fact]
    public void HandlersShouldNotDependOnInfrastructure_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.HandlersShouldNotDependOnInfrastructure();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void NotificationsShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.NotificationsShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void HandlersShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.HandlersShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void ValidatorsShouldFollowNamingConvention_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.ValidatorsShouldFollowNamingConvention();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void BehaviorsShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.BehaviorsShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void RequestsShouldFollowNamingConvention_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.RequestsShouldFollowNamingConvention();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void ValueObjectsShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.ValueObjectsShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void SagasShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.SagasShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void StoreImplementationsShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.StoreImplementationsShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void EventHandlersShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.EventHandlersShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void HandlersShouldImplementCorrectInterface_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.HandlersShouldImplementCorrectInterface();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void CommandsShouldImplementICommand_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.CommandsShouldImplementICommand();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void QueriesShouldImplementIQuery_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.QueriesShouldImplementIQuery();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void HandlersShouldNotDependOnControllers_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.HandlersShouldNotDependOnControllers();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void PipelineBehaviorsShouldImplementCorrectInterface_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.PipelineBehaviorsShouldImplementCorrectInterface();
        rule.ShouldNotBeNull();
    }

    [Fact]
    public void SagaDataShouldBeSealed_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.SagaDataShouldBeSealed();
        rule.ShouldNotBeNull();
    }

    // ─── EncinaArchitectureRules: parameterized factory methods with guards ───

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DomainShouldNotDependOnMessaging_InvalidNamespace_Throws(string? ns)
    {
        Should.Throw<ArgumentException>(() =>
            EncinaArchitectureRules.DomainShouldNotDependOnMessaging(ns!));
    }

    [Fact]
    public void DomainShouldNotDependOnMessaging_ValidNamespace_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.DomainShouldNotDependOnMessaging("MyApp.Domain");
        rule.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null, "App")]
    [InlineData("", "App")]
    [InlineData("Domain", null)]
    [InlineData("Domain", "")]
    public void DomainShouldNotDependOnApplication_InvalidNamespace_Throws(string? domain, string? app)
    {
        Should.Throw<ArgumentException>(() =>
            EncinaArchitectureRules.DomainShouldNotDependOnApplication(domain!, app!));
    }

    [Fact]
    public void DomainShouldNotDependOnApplication_ValidNamespaces_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.DomainShouldNotDependOnApplication("MyApp.Domain", "MyApp.Application");
        rule.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null, "Infra")]
    [InlineData("App", null)]
    public void ApplicationShouldNotDependOnInfrastructure_InvalidNamespace_Throws(string? app, string? infra)
    {
        Should.Throw<ArgumentException>(() =>
            EncinaArchitectureRules.ApplicationShouldNotDependOnInfrastructure(app!, infra!));
    }

    [Fact]
    public void ApplicationShouldNotDependOnInfrastructure_ValidNamespaces_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.ApplicationShouldNotDependOnInfrastructure("MyApp.Application", "MyApp.Infrastructure");
        rule.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RepositoryInterfacesShouldResideInDomain_InvalidNamespace_Throws(string? ns)
    {
        Should.Throw<ArgumentException>(() =>
            EncinaArchitectureRules.RepositoryInterfacesShouldResideInDomain(ns!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RepositoryImplementationsShouldResideInInfrastructure_InvalidNamespace_Throws(string? ns)
    {
        Should.Throw<ArgumentException>(() =>
            EncinaArchitectureRules.RepositoryImplementationsShouldResideInInfrastructure(ns!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AggregatesShouldFollowPattern_InvalidNamespace_Throws(string? ns)
    {
        Should.Throw<ArgumentException>(() =>
            EncinaArchitectureRules.AggregatesShouldFollowPattern(ns!));
    }

    [Fact]
    public void CleanArchitectureLayersShouldBeSeparated_ValidNamespaces_ReturnsRule()
    {
        var rule = EncinaArchitectureRules.CleanArchitectureLayersShouldBeSeparated(
            "MyApp.Domain", "MyApp.Application", "MyApp.Infrastructure");
        rule.ShouldNotBeNull();
    }

    // ─── EncinaArchitectureRulesBuilder ───

    [Fact]
    public void Builder_NoAssemblies_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new EncinaArchitectureRulesBuilder(Array.Empty<Assembly>()));
    }

    [Fact]
    public void Builder_ValidAssembly_Constructs()
    {
        var builder = new EncinaArchitectureRulesBuilder(TestAssembly);
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void Builder_AddCustomRule_NullRule_Throws()
    {
        var builder = new EncinaArchitectureRulesBuilder(TestAssembly);
        Should.Throw<ArgumentNullException>(() => builder.AddCustomRule(null!));
    }

    [Fact]
    public void Builder_EnforceHandlerAbstractions_ReturnsSelf()
    {
        var builder = new EncinaArchitectureRulesBuilder(TestAssembly);
        var result = builder.EnforceHandlerAbstractions();
        result.ShouldBe(builder);
    }

    [Fact]
    public void Builder_EnforceSealedNotifications_ReturnsSelf()
    {
        var builder = new EncinaArchitectureRulesBuilder(TestAssembly);
        var result = builder.EnforceSealedNotifications();
        result.ShouldBe(builder);
    }

    [Fact]
    public void Builder_EnforceSealedHandlers_ReturnsSelf()
    {
        var builder = new EncinaArchitectureRulesBuilder(TestAssembly);
        var result = builder.EnforceSealedHandlers();
        result.ShouldBe(builder);
    }

    [Fact]
    public void Builder_EnforceRequestNaming_ReturnsSelf()
    {
        var builder = new EncinaArchitectureRulesBuilder(TestAssembly);
        var result = builder.EnforceRequestNaming();
        result.ShouldBe(builder);
    }

    // ─── EventIdUniquenessRule ───

    [Fact]
    public void ExtractEventIds_NullAssemblies_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EventIdUniquenessRule.ExtractEventIds(null!));
    }

    [Fact]
    public void ExtractEventIds_EmptyAssemblies_ReturnsEmpty()
    {
        var result = EventIdUniquenessRule.ExtractEventIds([]);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ExtractEventIds_ValidAssembly_ReturnsResults()
    {
        var result = EventIdUniquenessRule.ExtractEventIds([TestAssembly]);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void AssertEventIdsAreGloballyUnique_NullAssemblies_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(null!));
    }

    [Fact]
    public void AssertEventIdsAreGloballyUnique_EmptyAssemblies_ReturnsEmpty()
    {
        var result = EventIdUniquenessRule.AssertEventIdsAreGloballyUnique([]);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_NullAssemblies_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
                null!, new Dictionary<string, string>()));
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_NullMapping_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
                [], (IReadOnlyDictionary<string, string>)null!));
    }

    [Fact]
    public void AssertNoRangeOverlaps_ReturnsResults()
    {
        var result = EventIdUniquenessRule.AssertNoRangeOverlaps();
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GenerateAllocationReport_ReturnsString()
    {
        var result = EventIdUniquenessRule.GenerateAllocationReport();
        result.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAllocationReport_WithAssemblies_ReturnsString()
    {
        var result = EventIdUniquenessRule.GenerateAllocationReport([TestAssembly]);
        result.ShouldNotBeNullOrEmpty();
    }

    // ─── ArchitectureVerificationResult + ArchitectureRuleViolation ───

    [Fact]
    public void ArchitectureRuleViolation_PropertiesAssignable()
    {
        var v = new ArchitectureRuleViolation("RuleName", "Message");
        v.RuleName.ShouldBe("RuleName");
        v.Message.ShouldBe("Message");
    }
}
