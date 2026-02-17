using System.Reflection;
using LanguageExt;
using Shouldly;

namespace Encina.ContractTests;

public sealed class PipelineBehaviorContracts
{
    private static readonly Assembly TargetAssembly = typeof(Encina).Assembly;

    [Fact]
    public void PipelineBehaviorsImplementSpecializedInterfaces()
    {
        var behaviors = GetPipelineBehaviorTypes();

        behaviors.ShouldNotBeEmpty();
        foreach (var behavior in behaviors)
        {
            var implementsCommand = ImplementsGenericInterface(behavior, typeof(ICommandPipelineBehavior<,>));
            var implementsQuery = ImplementsGenericInterface(behavior, typeof(IQueryPipelineBehavior<,>));

            (implementsCommand || implementsQuery)
                .ShouldBeTrue($"Pipeline behavior {behavior.Name} must implement a specialized command/query interface.");
        }
    }

    [Fact]
    public void AssemblyScannerDiscoversAllPipelineBehaviors()
    {
        var expected = new System.Collections.Generic.HashSet<Type>
        {
            typeof(CommandActivityPipelineBehavior<,>),
            typeof(CommandMetricsPipelineBehavior<,>),
            typeof(QueryActivityPipelineBehavior<,>),
            typeof(QueryMetricsPipelineBehavior<,>)
        };

        var result = EncinaAssemblyScanner.GetRegistrations(TargetAssembly);
        var discovered = result.PipelineRegistrations
            .Where(r => r.ImplementationType.Assembly == TargetAssembly)
            .Select(r => r.ImplementationType.IsGenericType ? r.ImplementationType.GetGenericTypeDefinition() : r.ImplementationType)
            .ToHashSet();

        discovered.ShouldBe(expected, comparer: TypeEqualityComparer.Instance);
    }

    private static Type[] GetPipelineBehaviorTypes()
    {
        return TargetAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsGenericTypeDefinition)
            .Where(t => ImplementsGenericInterface(t, typeof(IPipelineBehavior<,>)))
            // Exclude ValidationPipelineBehavior - it's opt-in via validation packages
            // and intentionally works with both commands and queries (no specialized interface)
            .Where(t => t.FullName != "Encina.Validation.ValidationPipelineBehavior`2")
            // Exclude ModuleBehaviorAdapter - it's an internal adapter for module-scoped behaviors
            // with 3 type parameters (TModule, TRequest, TResponse) and doesn't need specialized interfaces
            .Where(t => t.FullName != "Encina.Modules.ModuleBehaviorAdapter`3")
            .ToArray();
    }

    [Fact]
    public void PipelineBehaviorsReturnValueTaskEither()
    {
        var behaviors = GetPipelineBehaviorTypes();

        foreach (var behavior in behaviors)
        {
            var handle = behavior.GetMethod("Handle");
            handle.ShouldNotBeNull();

            var genericArgs = behavior.GetGenericArguments();
            genericArgs.Length.ShouldBe(2);

            var expected = typeof(ValueTask<>).MakeGenericType(typeof(Either<,>).MakeGenericType(typeof(EncinaError), genericArgs[1]));
            handle!.ReturnType.ShouldBe(expected, customMessage: "Behaviors must surface outcomes via ValueTask<Either<EncinaError,TResponse>> (rail funcional, sin throw operativo).");
        }
    }

    private static bool ImplementsGenericInterface(Type candidate, Type genericInterface)
    {
        return candidate
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
    }

    private sealed class TypeEqualityComparer : IEqualityComparer<Type>
    {
        public static readonly TypeEqualityComparer Instance = new();

        public bool Equals(Type? x, Type? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.IsGenericTypeDefinition && y.IsGenericTypeDefinition)
            {
                return x == y;
            }

            if (x.IsGenericTypeDefinition || y.IsGenericTypeDefinition)
            {
                return false;
            }

            return x == y;
        }

        public int GetHashCode(Type obj) => obj.GetHashCode();
    }
}
