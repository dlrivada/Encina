using System.Reflection;

using Encina.Marten.Projections;

using Marten;
using Marten.Events.Projections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Issue #952 (class of bug found in #949): JasperFx validates the parameters of projection methods at store boot and
/// rejects <see cref="IServiceProvider"/>, so no projection may take it. Dependencies come through the constructor,
/// with the projection built in an <c>IConfigureOptions&lt;StoreOptions&gt;</c> implementation (AGENTS.md section 3, ADR-019).
/// </summary>
/// <remarks>
/// The scan covers every shipped <c>Encina*</c> assembly in the test output directory, so a projection added to any
/// package is checked without editing this file. A projection is a class that derives from a JasperFx/Marten
/// projection base type or implements an <c>IProjection</c> interface.
/// </remarks>
public sealed class ProjectionsNeverTakeServiceProviderTests
{
    private static readonly string[] ProjectionBaseTypeNames =
    [
        "EventProjection", "SingleStreamProjection", "MultiStreamProjection", "ProjectionBase", "GeneratedProjection",
    ];

    [Fact]
    public void NoEncinaProjectionTakesIServiceProvider()
    {
        var types = LoadEncinaAssemblies().SelectMany(GetLoadableTypes).ToList();

        var violations = FindViolations(types);

        violations.ShouldBeEmpty(
            "projections must not take IServiceProvider (JasperFx rejects it at store boot, #949/#952); "
            + "inject dependencies through the constructor instead");
    }

    [Fact]
    public void Scan_FindsEncinaProjectionsToCheck()
    {
        var projections = LoadEncinaAssemblies().SelectMany(GetLoadableTypes).Where(IsProjection).ToList();

        projections.ShouldContain(t => t.Name == "OperationAuditEntryProjection");
        projections.ShouldContain(t => t.Name == "ConsentProjection");
        projections.ShouldContain(t => t.Name == "DPAProjection" || t.Name == "DataLocationProjection");
    }

    [Fact]
    public void Rule_FailsForIProjectionImplementationWithServiceProvider()
    {
        var violations = FindViolations([typeof(BadIProjectionServiceProvider)]);

        violations.ShouldHaveSingleItem().ShouldContain(".ctor");
    }

    [Fact]
    public void Rule_FailsForProjectionWithServiceScopeFactory()
    {
        var violations = FindViolations([typeof(BadIProjectionScopeFactory)]);

        violations.ShouldHaveSingleItem().ShouldContain(".ctor");
    }

    [Fact]
    public void Rule_FailsForProjectionWithServiceProviderOnMethod()
    {
        var violations = FindViolations([typeof(BadMethodProjection)]);

        violations.ShouldHaveSingleItem().ShouldContain("Create");
    }

    [Fact]
    public void Rule_FailsForProjectionWithServiceProviderOnConstructor()
    {
        var violations = FindViolations([typeof(BadConstructorProjection)]);

        violations.ShouldHaveSingleItem().ShouldContain(".ctor");
    }

    [Fact]
    public void Rule_PassesForProjectionWithSupportedParameters()
    {
        FindViolations([typeof(GoodProjection)]).ShouldBeEmpty();
    }

    private static List<string> FindViolations(IEnumerable<Type> types)
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var violations = new List<string>();

        foreach (var type in types.Where(IsProjection))
        {
            var members = type.GetConstructors(all).Cast<MethodBase>().Concat(type.GetMethods(all));
            foreach (var member in members)
            {
                if (member.GetParameters().Any(p => IsServiceLocator(p.ParameterType)))
                {
                    violations.Add($"{type.FullName}.{member.Name}");
                }
            }
        }

        return violations;
    }

    private static bool IsServiceLocator(Type parameterType)
    {
        var type = parameterType.IsByRef ? parameterType.GetElementType()! : parameterType;
        return typeof(IServiceProvider).IsAssignableFrom(type) || type == typeof(IServiceScopeFactory);
    }

    private static bool IsProjection(Type type)
    {
        if (!type.IsClass || type.IsAbstract)
        {
            return false;
        }

        for (var b = type.BaseType; b is not null; b = b.BaseType)
        {
            var name = b.IsGenericType ? b.GetGenericTypeDefinition().Name.Split('`')[0] : b.Name;
            if (ProjectionBaseTypeNames.Contains(name, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return type.GetInterfaces().Any(i => i.Name.Split('`')[0] == "IProjection");
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static List<Assembly> LoadEncinaAssemblies() =>
        Directory.EnumerateFiles(AppContext.BaseDirectory, "Encina*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n is not null
                && !n.EndsWith("Tests", StringComparison.Ordinal)
                && !n.Contains(".TestInfrastructure", StringComparison.Ordinal)
                && !n.Contains(".Testing.Examples", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(n => Assembly.Load(new AssemblyName(n!)))
            .ToList();

    private sealed class BadMethodProjection : EventProjection
    {
        public static void Create(object evt, IServiceProvider services, IDocumentOperations ops)
        {
        }
    }

    private sealed class BadConstructorProjection : EventProjection
    {
        public BadConstructorProjection(IServiceProvider services)
        {
        }
    }

    private sealed class FixtureReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }

    private abstract class FixtureIProjection : IProjection<FixtureReadModel>
    {
        public string ProjectionName => "fixture";
    }

    private sealed class BadIProjectionServiceProvider : FixtureIProjection
    {
        public BadIProjectionServiceProvider(IServiceProvider services)
        {
        }
    }

    private sealed class BadIProjectionScopeFactory : FixtureIProjection
    {
        public BadIProjectionScopeFactory(IServiceScopeFactory scopes)
        {
        }
    }

    private sealed class GoodProjection : EventProjection
    {
        public static void Create(object evt, IDocumentOperations ops, CancellationToken ct)
        {
        }
    }
}
