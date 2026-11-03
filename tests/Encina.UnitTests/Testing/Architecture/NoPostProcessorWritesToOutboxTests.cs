using System.Reflection;

using Encina.Messaging.Outbox;

using LanguageExt;

using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Issue #718 (Phase 9): the outbox row must be written inside the request's transaction by
/// <c>TransactionalOutboxPipelineBehavior</c>. A request post-processor runs after the whole pipeline, when the
/// business transaction has already ended, so a post-processor that writes to <see cref="IOutboxStore"/> would
/// bring back the lost and phantom outbox events the transactional behavior removed.
/// </summary>
/// <remarks>
/// The scan covers every shipped <c>Encina*</c> assembly in the test output directory, so a post-processor added
/// to any package is checked without editing this file. A violation is a class implementing
/// <see cref="IRequestPostProcessor{TRequest, TResponse}"/> that holds an <see cref="IOutboxStore"/> (or the
/// internal writer) in a field, property or constructor parameter.
/// </remarks>
public sealed class NoPostProcessorWritesToOutboxTests
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    [Fact]
    public void NoEncinaRequestPostProcessorWritesToTheOutbox()
    {
        var types = LoadEncinaAssemblies().SelectMany(GetLoadableTypes).ToList();

        var violations = FindViolations(types);

        violations.ShouldBeEmpty(
            "a request post-processor runs after the business transaction ended, so it must not write to IOutboxStore; "
            + "use TransactionalOutboxPipelineBehavior (#718)");
    }

    [Fact]
    public void Scan_SeesTheOutboxStoresOfTheProviderPackages()
    {
        var types = LoadEncinaAssemblies().SelectMany(GetLoadableTypes).ToList();

        // The rule checks nothing if the scan does not load the provider packages; every outbox store is in one.
        types.Count(t => t.IsClass && !t.IsAbstract && typeof(IOutboxStore).IsAssignableFrom(t))
            .ShouldBeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Rule_FailsForPostProcessorWithOutboxStoreInConstructor()
    {
        FindViolations([typeof(BadCtorPostProcessor)]).ShouldHaveSingleItem().ShouldContain(".ctor");
    }

    [Fact]
    public void Rule_FailsForPostProcessorWithOutboxStoreField()
    {
        FindViolations([typeof(BadFieldPostProcessor)]).ShouldHaveSingleItem().ShouldContain("_store");
    }

    [Fact]
    public void Rule_FailsForPostProcessorWithOutboxWriterProperty()
    {
        FindViolations([typeof(BadPropertyPostProcessor)]).ShouldHaveSingleItem().ShouldContain("Writer");
    }

    [Fact]
    public void Rule_PassesForPostProcessorWithoutTheOutbox()
    {
        FindViolations([typeof(GoodPostProcessor)]).ShouldBeEmpty();
    }

    [Fact]
    public void Rule_IgnoresNonPostProcessorsThatUseTheOutbox()
    {
        FindViolations([typeof(NotAPostProcessor)]).ShouldBeEmpty();
    }

    private static List<string> FindViolations(IEnumerable<Type> types)
    {
        var violations = new List<string>();

        foreach (var type in types.Where(IsRequestPostProcessor))
        {
            violations.AddRange(type.GetConstructors(All)
                .Where(c => c.GetParameters().Any(p => TouchesOutbox(p.ParameterType)))
                .Select(c => $"{type.FullName}.{c.Name}"));
            violations.AddRange(type.GetFields(All)
                .Where(f => TouchesOutbox(f.FieldType))
                .Select(f => $"{type.FullName}.{f.Name}"));
            violations.AddRange(type.GetProperties(All)
                .Where(p => TouchesOutbox(p.PropertyType))
                .Select(p => $"{type.FullName}.{p.Name}"));
        }

        return violations;
    }

    private static bool TouchesOutbox(Type type)
    {
        var t = type.IsByRef ? type.GetElementType()! : type;
        return typeof(IOutboxStore).IsAssignableFrom(t) || t == typeof(OutboxWriter);
    }

    private static bool IsRequestPostProcessor(Type type) =>
        type.IsClass
        && !type.IsAbstract
        && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPostProcessor<,>));

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

    private sealed class BadCtorPostProcessor : IRequestPostProcessor<FixtureRequest, string>
    {
        public BadCtorPostProcessor(IOutboxStore store)
        {
            GC.KeepAlive(store);
        }

        public ValueTask<Either<EncinaError, Unit>> Process(
            FixtureRequest request, IRequestContext context, string response, CancellationToken cancellationToken) =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private sealed class BadFieldPostProcessor : IRequestPostProcessor<FixtureRequest, string>
    {
        private static IOutboxStore? _store;

        public ValueTask<Either<EncinaError, Unit>> Process(
            FixtureRequest request, IRequestContext context, string response, CancellationToken cancellationToken)
        {
            _store ??= null;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }
    }

    private sealed class BadPropertyPostProcessor : IRequestPostProcessor<FixtureRequest, string>
    {
        internal static OutboxWriter? Writer => null;

        public ValueTask<Either<EncinaError, Unit>> Process(
            FixtureRequest request, IRequestContext context, string response, CancellationToken cancellationToken) =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private sealed class GoodPostProcessor : IRequestPostProcessor<FixtureRequest, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            FixtureRequest request, IRequestContext context, string response, CancellationToken cancellationToken) =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private sealed class NotAPostProcessor
    {
        public NotAPostProcessor(IOutboxStore store)
        {
            GC.KeepAlive(store);
        }
    }

    private sealed record FixtureRequest : IRequest<string>;
}
