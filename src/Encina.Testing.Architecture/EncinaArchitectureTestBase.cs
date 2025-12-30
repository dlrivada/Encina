using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Xunit;
using ReflectionAssembly = System.Reflection.Assembly;

namespace Encina.Testing.Architecture;

/// <summary>
/// Abstract base class for architecture tests that provides common setup and pre-defined test methods.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to create architecture tests for your application.
/// Override the assembly properties to specify which assemblies to analyze.
/// </para>
/// <para>
/// The base class provides pre-defined test methods for common architectural rules.
/// You can override the namespace properties to customize layer separation rules.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyArchitectureTests : EncinaArchitectureTestBase
/// {
///     protected override Assembly ApplicationAssembly => typeof(CreateOrderHandler).Assembly;
///     protected override Assembly? DomainAssembly => typeof(Order).Assembly;
///     protected override Assembly? InfrastructureAssembly => typeof(OrderRepository).Assembly;
///
///     protected override string? DomainNamespace => "MyApp.Domain";
///     protected override string? ApplicationNamespace => "MyApp.Application";
///     protected override string? InfrastructureNamespace => "MyApp.Infrastructure";
/// }
/// </code>
/// </example>
public abstract class EncinaArchitectureTestBase
{
    private readonly Lazy<ArchUnitNET.Domain.Architecture> _architecture;

    /// <summary>
    /// Gets the main application assembly containing handlers.
    /// </summary>
    /// <remarks>
    /// This assembly is required and must contain your command/query handlers.
    /// </remarks>
    protected abstract ReflectionAssembly ApplicationAssembly { get; }

    /// <summary>
    /// Gets the domain assembly containing domain entities and interfaces.
    /// </summary>
    /// <remarks>
    /// Optional. When provided, enables domain layer isolation rules.
    /// </remarks>
    protected virtual ReflectionAssembly? DomainAssembly => null;

    /// <summary>
    /// Gets the infrastructure assembly containing repository implementations and external integrations.
    /// </summary>
    /// <remarks>
    /// Optional. When provided, enables infrastructure layer rules.
    /// </remarks>
    protected virtual ReflectionAssembly? InfrastructureAssembly => null;

    /// <summary>
    /// Gets the domain namespace pattern for layer separation rules.
    /// </summary>
    /// <remarks>
    /// Override to specify your domain namespace (e.g., "MyApp.Domain").
    /// </remarks>
    protected virtual string? DomainNamespace => null;

    /// <summary>
    /// Gets the application namespace pattern for layer separation rules.
    /// </summary>
    /// <remarks>
    /// Override to specify your application namespace (e.g., "MyApp.Application").
    /// </remarks>
    protected virtual string? ApplicationNamespace => null;

    /// <summary>
    /// Gets the infrastructure namespace pattern for layer separation rules.
    /// </summary>
    /// <remarks>
    /// Override to specify your infrastructure namespace (e.g., "MyApp.Infrastructure").
    /// </remarks>
    protected virtual string? InfrastructureNamespace => null;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaArchitectureTestBase"/> class.
    /// </summary>
    protected EncinaArchitectureTestBase()
    {
        // Safe to reference virtual GetAssemblies() here: Lazy<T> defers the lambda invocation
        // until .Value is first accessed, at which point the derived class is fully constructed.
        _architecture = new Lazy<ArchUnitNET.Domain.Architecture>(
            () => new ArchLoader().LoadAssemblies(GetAssemblies().ToArray()).Build());
    }

    /// <summary>
    /// Gets the architecture object built from the specified assemblies.
    /// </summary>
    /// <remarks>
    /// The architecture is lazily loaded and cached for performance in a thread-safe manner.
    /// </remarks>
    protected ArchUnitNET.Domain.Architecture Architecture => _architecture.Value;

    /// <summary>
    /// Gets all assemblies to analyze.
    /// </summary>
    /// <returns>An enumerable of assemblies.</returns>
    protected virtual IEnumerable<ReflectionAssembly> GetAssemblies()
    {
        yield return ApplicationAssembly;

        if (DomainAssembly is not null)
        {
            yield return DomainAssembly;
        }

        if (InfrastructureAssembly is not null)
        {
            yield return InfrastructureAssembly;
        }
    }

    /// <summary>
    /// Checks that the specified rule passes against the architecture.
    /// </summary>
    /// <param name="rule">The architecture rule to check.</param>
    protected void CheckRule(IArchRule rule)
    {
        rule.Check(Architecture);
    }

    /// <summary>
    /// Tests that handlers do not depend on infrastructure implementations.
    /// </summary>
    /// <remarks>
    /// This test verifies that handlers use abstractions (interfaces) rather than
    /// concrete infrastructure types like Entity Framework DbContext, Dapper, etc.
    /// </remarks>
    [Fact]
    public virtual void HandlersShouldNotDependOnInfrastructure()
    {
        CheckRule(EncinaArchitectureRules.HandlersShouldNotDependOnInfrastructure());
    }

    /// <summary>
    /// Tests that notifications and events are sealed.
    /// </summary>
    /// <remarks>
    /// Sealed notifications prevent inheritance issues and ensure proper event handling.
    /// </remarks>
    [Fact]
    public virtual void NotificationsShouldBeSealed()
    {
        CheckRule(EncinaArchitectureRules.NotificationsShouldBeSealed());
    }

    /// <summary>
    /// Tests that handlers are sealed.
    /// </summary>
    /// <remarks>
    /// Sealing handlers improves performance and clarifies that they are not meant to be inherited.
    /// </remarks>
    [Fact]
    public virtual void HandlersShouldBeSealed()
    {
        CheckRule(EncinaArchitectureRules.HandlersShouldBeSealed());
    }

    /// <summary>
    /// Tests that pipeline behaviors are sealed.
    /// </summary>
    [Fact]
    public virtual void BehaviorsShouldBeSealed()
    {
        CheckRule(EncinaArchitectureRules.BehaviorsShouldBeSealed());
    }

    /// <summary>
    /// Tests that validators follow the naming convention.
    /// </summary>
    [Fact]
    public virtual void ValidatorsShouldFollowNamingConvention()
    {
        CheckRule(EncinaArchitectureRules.ValidatorsShouldFollowNamingConvention());
    }

    /// <summary>
    /// Tests that domain layer does not depend on messaging infrastructure.
    /// </summary>
    /// <remarks>
    /// This test is skipped if <see cref="DomainNamespace"/> is not configured.
    /// </remarks>
    [SkippableFact]
    public virtual void DomainShouldNotDependOnMessaging()
    {
        Skip.If(string.IsNullOrWhiteSpace(DomainNamespace), "DomainNamespace not configured; skipping test.");

        CheckRule(EncinaArchitectureRules.DomainShouldNotDependOnMessaging(DomainNamespace));
    }

    /// <summary>
    /// Tests clean architecture layer separation.
    /// </summary>
    /// <remarks>
    /// This test is skipped if layer namespaces are not configured.
    /// </remarks>
    [SkippableFact]
    public virtual void LayersShouldBeProperlySeparated()
    {
        Skip.If(
            string.IsNullOrWhiteSpace(DomainNamespace) ||
            string.IsNullOrWhiteSpace(ApplicationNamespace) ||
            string.IsNullOrWhiteSpace(InfrastructureNamespace),
            "Layer namespaces not configured; skipping test.");

        CheckRule(EncinaArchitectureRules.CleanArchitectureLayersShouldBeSeparated(
            DomainNamespace,
            ApplicationNamespace,
            InfrastructureNamespace));
    }
}
