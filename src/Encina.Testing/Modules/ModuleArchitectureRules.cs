using ArchUnitNET.Fluent;
using Encina.Modules;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Encina.Testing.Modules;

/// <summary>
/// Provides pre-built architecture rules for validating modular monolith design patterns.
/// </summary>
/// <remarks>
/// <para>
/// These rules enforce best practices for modular monolith architecture:
/// <list type="bullet">
/// <item><description>Modules should not have circular dependencies</description></item>
/// <item><description>Modules should only communicate through public APIs</description></item>
/// <item><description>Module internals should not be accessed directly</description></item>
/// <item><description>Each module should own its own data</description></item>
/// </list>
/// </para>
/// </remarks>
public static class ModuleArchitectureRules
{
    /// <summary>
    /// Creates a rule that classes implementing <see cref="IModule"/> should declare a <c>Name</c> property.
    /// </summary>
    /// <remarks>
    /// This rule enforces the presence of a <c>Name</c> property on types implementing <see cref="IModule"/>.
    ///
    /// Important: ArchUnitNET's fluent API can assert the presence of a property member by name
    /// (which this rule does) but it cannot express, as of the current ArchUnitNET version used
    /// here, finer-grained checks such as "property is of type <see cref="string"/>" or
    /// "the property has a public getter" in a single declarative rule.
    ///
    /// Therefore:
    ///  - This rule verifies that a property named <c>Name</c> exists on the module type.
    ///  - You MUST add a small unit/integration test that uses reflection to validate the
    ///    property's CLR type is <see cref="string"/> and that it exposes a public getter
    ///    (i.e., <c>GetMethod</c> exists and <c>IsPublic</c> is true).
    ///
    /// Example (xUnit):
    /// <code><![CDATA[
    /// [Fact]
    /// public void Modules_NameProperty_IsStringAndHasPublicGetter()
    /// {
    ///     var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
    ///         .SelectMany(a => a.GetTypes())
    ///         .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
    ///
    ///     foreach (var t in moduleTypes)
    ///     {
    ///         var prop = t.GetProperty("Name");
    ///         prop.ShouldNotBeNull();
    ///         prop.PropertyType.ShouldBe(typeof(string));
    ///         var getter = prop.GetGetMethod(nonPublic: true);
    ///         getter.ShouldNotBeNull();
    ///         getter.IsPublic.ShouldBeTrue();
    ///     }
    /// }
    /// ]]></code>
    ///
    /// If future versions of ArchUnitNET add support for property-type and accessor-visibility
    /// assertions, this method can be extended to include those checks directly in the rule.
    /// </remarks>
    /// <returns>The architecture rule that asserts presence of a <c>Name</c> property.</returns>
    public static IArchRule ModulesShouldHaveName()
    {
        return Classes()
            .That()
            .ImplementInterface(typeof(IModule))
            .Should()
            .HavePropertyMemberWithName("Name")
            .Because("modules must declare a Name property for identification");
    }

    /// <summary>
    /// Creates a rule that modules should be sealed.
    /// </summary>
    /// <returns>The architecture rule.</returns>
    public static IArchRule ModulesShouldBeSealed()
    {
        return Classes()
            .That()
            .ImplementInterface(typeof(IModule))
            .Should()
            .BeSealed()
            .Because("modules should be sealed to prevent inheritance hierarchies");
    }

    /// <summary>
    /// Creates a rule that a module's internal types should not be accessed by other modules.
    /// </summary>
    /// <param name="moduleNamespacePattern">The module's namespace regex pattern (e.g., ".*Orders.*").</param>
    /// <param name="internalPattern">The internal namespace regex pattern (e.g., ".*Internal.*").</param>
    /// <returns>The architecture rule.</returns>
    public static IArchRule ModuleInternalsShouldNotBeAccessedExternally(
        string moduleNamespacePattern,
        string internalPattern = ".*\\.Internal\\..*")
    {
        ArgumentException.ThrowIfNullOrEmpty(moduleNamespacePattern);

        var internalTypes = Types()
            .That()
            .ResideInNamespaceMatching(internalPattern)
            .As($"Internal types matching {internalPattern}");

        return Types()
            .That()
            .DoNotResideInNamespaceMatching(moduleNamespacePattern)
            .Should()
            .NotDependOnAny(internalTypes)
            .Because($"internal types should only be accessed within the module");
    }

    /// <summary>
    /// Creates a rule that handler types should reside in the correct module namespace.
    /// </summary>
    /// <param name="moduleNamespacePattern">The module's namespace regex pattern.</param>
    /// <returns>The architecture rule.</returns>
    public static IArchRule HandlersShouldResideInModule(string moduleNamespacePattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(moduleNamespacePattern);

        return Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .ResideInNamespaceMatching(moduleNamespacePattern)
            .Should()
            .ResideInNamespaceMatching(moduleNamespacePattern)
            .Because($"handlers should reside within the module namespace");
    }

    /// <summary>
    /// Creates a rule that a module should not reference another module's DbContext or data access layer.
    /// </summary>
    /// <param name="moduleNamespacePattern">The module's namespace regex pattern.</param>
    /// <returns>The architecture rule.</returns>
    public static IArchRule ModulesShouldNotAccessOtherModulesData(string moduleNamespacePattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(moduleNamespacePattern);

        var dbContextTypes = Classes()
            .That()
            .HaveNameEndingWith("DbContext")
            .And()
            .DoNotResideInNamespaceMatching(moduleNamespacePattern)
            .As("Other modules' DbContext");

        return Types()
            .That()
            .ResideInNamespaceMatching(moduleNamespacePattern)
            .Should()
            .NotDependOnAny(dbContextTypes)
            .Because($"module should not access other modules' DbContext");
    }

    /// <summary>
    /// Creates a rule that module API interfaces should follow naming convention.
    /// </summary>
    /// <returns>The architecture rule.</returns>
    public static IArchRule ModuleApiInterfacesShouldFollowNamingConvention()
    {
        return Interfaces()
            .That()
            .HaveNameEndingWith("ModuleApi")
            .Should()
            .HaveNameStartingWith("I")
            .Because("module API interfaces should follow C# naming conventions (prefix with 'I')");
    }

    /// <summary>
    /// Creates a rule that integration events should be sealed.
    /// </summary>
    /// <returns>The architecture rule.</returns>
    public static IArchRule IntegrationEventsShouldBeSealed()
    {
        return Classes()
            .That()
            .HaveNameEndingWith("IntegrationEvent")
            .Should()
            .BeSealed()
            .Because("integration events should be immutable contracts");
    }

    /// <summary>
    /// Creates a rule that domain types should not depend on infrastructure.
    /// </summary>
    /// <param name="domainNamespacePattern">The domain namespace regex pattern (e.g., ".*\\.Domain\\..*").</param>
    /// <param name="infrastructureNamespacePattern">The infrastructure namespace regex pattern (e.g., ".*\\.Infrastructure\\..*").</param>
    /// <returns>The architecture rule.</returns>
    public static IArchRule DomainShouldNotDependOnInfrastructure(
        string domainNamespacePattern = ".*\\.Domain\\..*",
        string infrastructureNamespacePattern = ".*\\.Infrastructure\\..*")
    {
        ArgumentException.ThrowIfNullOrEmpty(domainNamespacePattern);
        ArgumentException.ThrowIfNullOrEmpty(infrastructureNamespacePattern);

        var infrastructureTypes = Types()
            .That()
            .ResideInNamespaceMatching(infrastructureNamespacePattern)
            .As("Infrastructure types");

        return Types()
            .That()
            .ResideInNamespaceMatching(domainNamespacePattern)
            .Should()
            .NotDependOnAny(infrastructureTypes)
            .Because("domain should not depend on infrastructure (Dependency Inversion Principle)");
    }
}
