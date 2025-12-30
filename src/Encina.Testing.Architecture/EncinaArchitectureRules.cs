using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Encina.Testing.Architecture;

/// <summary>
/// Provides pre-built architectural rules for enforcing Encina best practices and clean architecture patterns.
/// </summary>
/// <remarks>
/// <para>
/// These rules help ensure that your codebase follows established architectural patterns:
/// </para>
/// <list type="bullet">
/// <item><description>Handlers don't depend on infrastructure directly</description></item>
/// <item><description>Domain layer doesn't depend on messaging infrastructure</description></item>
/// <item><description>Notifications are sealed for proper event handling</description></item>
/// <item><description>Proper layer separation is maintained</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class ArchitectureTests
/// {
///     private static readonly Architecture Architecture = new ArchLoader()
///         .LoadAssemblies(typeof(MyHandler).Assembly)
///         .Build();
///
///     [Fact]
///     public void Handlers_ShouldNotDependOnInfrastructure()
///     {
///         EncinaArchitectureRules
///             .HandlersShouldNotDependOnInfrastructure()
///             .Check(Architecture);
///     }
/// }
/// </code>
/// </example>
public static class EncinaArchitectureRules
{
    /// <summary>
    /// Creates a rule that handlers should not depend on infrastructure namespaces directly.
    /// </summary>
    /// <remarks>
    /// Handlers should depend on abstractions (repositories, services) rather than concrete
    /// infrastructure implementations like Entity Framework, Dapper, or specific database drivers.
    /// </remarks>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule HandlersShouldNotDependOnInfrastructure()
    {
        var infrastructureTypes = Classes()
            .That()
            .ResideInNamespaceMatching(".*EntityFrameworkCore.*")
            .Or()
            .ResideInNamespaceMatching(".*Dapper.*")
            .Or()
            .ResideInNamespaceMatching(".*SqlClient.*")
            .Or()
            .ResideInNamespaceMatching(".*Npgsql.*")
            .Or()
            .ResideInNamespaceMatching(".*MongoDB.*")
            .As("Infrastructure Types");

        return Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .DoNotHaveNameEndingWith("NotificationHandler")
            .Should()
            .NotDependOnAny(infrastructureTypes)
            .Because("Handlers should depend on abstractions, not infrastructure implementations");
    }

    /// <summary>
    /// Creates a rule that notification types should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule NotificationsShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Notification")
            .Or()
            .HaveNameEndingWith("Event")
            .Should()
            .BeSealed()
            .Because("Notifications and Events should be sealed to prevent inheritance issues");
    }

    /// <summary>
    /// Creates a rule that handler classes should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule HandlersShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .Because("Handlers should be sealed to prevent inheritance and improve performance");
    }

    /// <summary>
    /// Creates a rule that types in the specified domain namespace should not depend on messaging infrastructure.
    /// </summary>
    /// <param name="domainNamespace">The namespace pattern for domain types (e.g., "MyApp.Domain").</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule DomainShouldNotDependOnMessaging(string domainNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);

        var messagingTypes = Classes()
            .That()
            .ResideInNamespaceMatching(".*Encina\\.Messaging.*")
            .As("Messaging Types");

        return Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(domainNamespace)}.*")
            .Should()
            .NotDependOnAny(messagingTypes)
            .Because("Domain layer should be independent of messaging infrastructure");
    }

    /// <summary>
    /// Creates a rule that types in the specified domain namespace should not depend on application layer.
    /// </summary>
    /// <param name="domainNamespace">The namespace pattern for domain types.</param>
    /// <param name="applicationNamespace">The namespace pattern for application types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule DomainShouldNotDependOnApplication(string domainNamespace, string applicationNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationNamespace);

        var applicationTypes = Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(applicationNamespace)}.*")
            .As("Application Types");

        return Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(domainNamespace)}.*")
            .Should()
            .NotDependOnAny(applicationTypes)
            .Because("Domain layer should not depend on Application layer");
    }

    /// <summary>
    /// Creates a rule that application layer types should not depend on infrastructure.
    /// </summary>
    /// <param name="applicationNamespace">The namespace pattern for application types.</param>
    /// <param name="infrastructureNamespace">The namespace pattern for infrastructure types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule ApplicationShouldNotDependOnInfrastructure(
        string applicationNamespace,
        string infrastructureNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(infrastructureNamespace);

        var infrastructureTypes = Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(infrastructureNamespace)}.*")
            .As("Infrastructure Types");

        return Classes()
            .That()
            .ResideInNamespaceMatching($".*{EscapeForRegex(applicationNamespace)}.*")
            .Should()
            .NotDependOnAny(infrastructureTypes)
            .Because("Application layer should not depend on Infrastructure layer");
    }

    /// <summary>
    /// Creates a rule that validators should follow naming convention.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule ValidatorsShouldFollowNamingConvention()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .BeSealed()
            .Because("Validators should be sealed and follow the naming convention *Validator");
    }

    /// <summary>
    /// Creates a rule that pipeline behaviors should be sealed.
    /// </summary>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule BehaviorsShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("Behavior")
            .Should()
            .BeSealed()
            .Because("Pipeline behaviors should be sealed");
    }

    /// <summary>
    /// Creates a rule that repository interfaces should reside in the domain layer.
    /// </summary>
    /// <param name="domainNamespace">The namespace pattern for domain types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule RepositoryInterfacesShouldResideInDomain(string domainNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);

        return Interfaces()
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespaceMatching($".*{EscapeForRegex(domainNamespace)}.*")
            .Because("Repository interfaces should be defined in the Domain layer");
    }

    /// <summary>
    /// Creates a rule that repository implementations should reside in the infrastructure layer.
    /// </summary>
    /// <param name="infrastructureNamespace">The namespace pattern for infrastructure types.</param>
    /// <returns>An architecture rule that can be checked against an architecture.</returns>
    public static IArchRule RepositoryImplementationsShouldResideInInfrastructure(string infrastructureNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(infrastructureNamespace);

        return Classes()
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespaceMatching($".*{EscapeForRegex(infrastructureNamespace)}.*")
            .Because("Repository implementations should be in the Infrastructure layer");
    }

    private static string EscapeForRegex(string input) =>
        Regex.Escape(input);

    /// <summary>
    /// Creates a combined rule for clean architecture layer separation.
    /// </summary>
    /// <param name="domainNamespace">The namespace for domain types.</param>
    /// <param name="applicationNamespace">The namespace for application types.</param>
    /// <param name="infrastructureNamespace">The namespace for infrastructure types.</param>
    /// <returns>A combined architecture rule for layer separation.</returns>
    public static IArchRule CleanArchitectureLayersShouldBeSeparated(
        string domainNamespace,
        string applicationNamespace,
        string infrastructureNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(infrastructureNamespace);

        var domainRule = DomainShouldNotDependOnApplication(domainNamespace, applicationNamespace);
        var appRule = ApplicationShouldNotDependOnInfrastructure(applicationNamespace, infrastructureNamespace);

        return domainRule.And(appRule);
    }
}
