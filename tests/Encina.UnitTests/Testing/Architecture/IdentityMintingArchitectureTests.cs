using System.Reflection;
using System.Reflection.Emit;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Architecture rules of the request identity model (#1705, M3): among production assemblies,
/// identity-minting members are called only by the scope factory (and the claim mapper it alone
/// uses), and no production assembly references the <c>Encina.Testing</c> test seam.
/// </summary>
/// <remarks>
/// The call graph is read from the IL of every method of every <c>Encina*</c> production assembly
/// in the test output folder, so it sees calls inside lambdas, iterators and async state machines
/// (attributed to their outermost declaring type).
/// </remarks>
public sealed class IdentityMintingArchitectureTests
{
    private const BindingFlags AllDeclared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Lazy<IReadOnlyList<Assembly>> Production = new(LoadProductionAssemblies);

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(static field => (OpCode)field.GetValue(null)!)
        .ToDictionary(static code => code.Value);

    [Fact]
    public void IdentityMintingMembers_AreCalledOnlyByTheScopeFactory()
    {
        MethodBase[] minting =
        [
            Method(typeof(RequestIdentity), nameof(RequestIdentity.ForUser)),
            Method(typeof(RequestIdentity), nameof(RequestIdentity.ForService)),
            Method(typeof(RequestContext), nameof(RequestContext.CreateAt))
        ];
        System.Collections.Generic.HashSet<Type> allowed = [typeof(RequestContextScopeFactory), typeof(ClaimsRequestIdentityFactory)];

        var callers = CallersOf(minting);

        callers.ShouldNotBeEmpty("the scanner must find the scope factory's own calls");
        callers.Where(caller => !allowed.Contains(caller)).Select(static caller => caller.FullName).ShouldBeEmpty();
    }

    [Fact]
    public void TheClaimMapper_IsUsedOnlyByTheScopeFactory()
    {
        MethodBase[] mapping =
        [
            Method(typeof(IRequestIdentityFactory), nameof(IRequestIdentityFactory.Create)),
            Method(typeof(ClaimsRequestIdentityFactory), nameof(ClaimsRequestIdentityFactory.Create))
        ];

        CallersOf(mapping).Where(caller => caller != typeof(RequestContextScopeFactory)).Select(static caller => caller.FullName).ShouldBeEmpty();
    }

    [Fact]
    public void TheBindingPrimitives_AreCalledOnlyByTheScopeFactoryAndTheDispatcher()
    {
        MethodBase[] binding =
        [
            Method(typeof(RequestContextAccessor), nameof(RequestContextAccessor.Push)),
            Method(typeof(RequestContextAccessor), nameof(RequestContextAccessor.SetUnchecked)),
            Method(typeof(RequestContextAccessor), nameof(RequestContextAccessor.Install)),
            Method(typeof(RequestContextAccessor), nameof(RequestContextAccessor.End)),
            Method(typeof(RequestContext), nameof(RequestContext.WithIdentity)),
            Method(typeof(RequestContext), nameof(RequestContext.WithOrigin)),
            Method(typeof(IdentityIssuer), nameof(IdentityIssuer.Bind))
        ];
        System.Collections.Generic.HashSet<Type> allowed =
            [typeof(RequestContextScopeFactory), typeof(RequestContextAccessor), typeof(AmbientRequestContext)];

        CallersOf(binding).Where(caller => !allowed.Contains(caller)).Select(static caller => caller.FullName).ShouldBeEmpty();
    }

    [Fact]
    public void NoProductionAssembly_ReferencesEncinaTesting()
    {
        Production.Value.Count.ShouldBeGreaterThan(10);

        var offenders = Production.Value
            .Where(static assembly => assembly.GetReferencedAssemblies().Any(static reference => IsTestingAssembly(reference.Name)))
            .Select(static assembly => assembly.GetName().Name)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethods(AllDeclared).Single(method => method.Name == name);

    private static System.Collections.Generic.HashSet<Type> CallersOf(IReadOnlyCollection<MethodBase> targets)
    {
        var targetSet = targets.ToHashSet();
        var callers = new System.Collections.Generic.HashSet<Type>();
        foreach (var method in Production.Value.SelectMany(LoadableTypes).SelectMany(static type => type.GetMethods(AllDeclared).Cast<MethodBase>().Concat(type.GetConstructors(AllDeclared))))
        {
            if (CalledMethods(method).Any(targetSet.Contains))
            {
                callers.Add(Outermost(method.DeclaringType!));
            }
        }

        return callers;
    }

    private static IEnumerable<MethodBase> CalledMethods(MethodBase method)
    {
        byte[]? il;
        try
        {
            il = method.GetMethodBody()?.GetILAsByteArray();
        }
        catch (Exception ex) when (ex is BadImageFormatException or InvalidOperationException or TypeLoadException or FileNotFoundException)
        {
            yield break;
        }

        if (il is null)
        {
            yield break;
        }

        for (var position = 0; position < il.Length;)
        {
            var code = ReadOpCode(il, ref position);
            if (code.OperandType == OperandType.InlineMethod
                && ResolveMethod(method, BitConverter.ToInt32(il, position)) is { } called)
            {
                yield return called is MethodInfo { IsGenericMethod: true } generic ? generic.GetGenericMethodDefinition() : called;
            }

            position += OperandSize(code, il, position);
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int position)
    {
        short value = il[position++];
        if (value == 0xFE)
        {
            value = unchecked((short)(0xFE00 | il[position++]));
        }

        return OpCodesByValue[value];
    }

    // crap-exempt: single-question switch — the operand size of each IL operand type.
    private static int OperandSize(OpCode code, byte[] il, int position) => code.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, position)),
        _ => 4
    };

    private static MethodBase? ResolveMethod(MethodBase method, int token)
    {
        try
        {
            var typeArguments = method.DeclaringType is { IsGenericType: true } type ? type.GetGenericArguments() : null;
            var methodArguments = method is MethodInfo { IsGenericMethod: true } ? method.GetGenericArguments() : null;
            return method.Module.ResolveMethod(token, typeArguments, methodArguments);
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or FileNotFoundException or MissingMethodException or BadImageFormatException)
        {
            return null;
        }
    }

    private static Type Outermost(Type type)
    {
        while (type.DeclaringType is { } declaring)
        {
            type = declaring;
        }

        return type;
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static bool IsTestingAssembly(string? name) =>
        name is not null && (name == "Encina.Testing" || name.StartsWith("Encina.Testing.", StringComparison.Ordinal));

    // Test packages of every area (Encina.Testing.*, Encina.Aspire.Testing) are test seams, not production.
    private static bool IsProductionAssemblyName(string name) =>
        name.StartsWith("Encina", StringComparison.Ordinal)
        && !IsTestingAssembly(name)
        && !name.Split('.').Contains("Testing", StringComparer.Ordinal)
        && !name.Contains("Tests", StringComparison.Ordinal)
        && !name.Contains("Benchmarks", StringComparison.Ordinal)
        && !name.Contains("TestInfrastructure", StringComparison.Ordinal);

    private static List<Assembly> LoadProductionAssemblies()
    {
        var assemblies = new List<Assembly>();
        foreach (var path in Directory.GetFiles(AppContext.BaseDirectory, "Encina*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!IsProductionAssemblyName(name))
            {
                continue;
            }

            try
            {
                assemblies.Add(Assembly.Load(AssemblyName.GetAssemblyName(path)));
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                // Not loadable in this test host: it is not referenced by any test either.
            }
        }

        return assemblies;
    }
}
