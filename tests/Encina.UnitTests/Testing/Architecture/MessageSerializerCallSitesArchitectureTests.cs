using System.Reflection;
using System.Reflection.Emit;
using Encina.Messaging.Serialization;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Architecture rule of the persisted message payloads (#3780): inside <c>Encina.Messaging</c>, nothing
/// but the serializer implementations calls <see cref="IMessageSerializer.Serialize{T}"/> directly.
/// Every persistence call site goes through <c>SerializeAsRuntimeType</c>, which closes the call over
/// the message's runtime type, so a concrete type marked <c>[EncryptedMessage]</c> is encrypted
/// whatever static type the caller used.
/// </summary>
/// <remarks>
/// The call graph is read from the IL of every method of the scanned types (as in
/// <see cref="IdentityMintingArchitectureTests"/>), so it sees calls inside lambdas, local functions and
/// async state machines; each one is reported under the source type and method it comes from.
/// </remarks>
public sealed class MessageSerializerCallSitesArchitectureTests
{
    private const BindingFlags AllDeclared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Known direct callers still to be migrated. Each entry names the issue that removes it; the end
    /// goal is an empty list. The inbox response envelope is the third pull request of #3780.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> AllowList = new(StringComparer.Ordinal)
    {
        // #3780, pull request 3: the inbox caches its response envelope through Serialize<T>.
        "Encina.Messaging.Inbox.InboxOrchestrator.SerializeResponse"
    };

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(static field => (OpCode)field.GetValue(null)!)
        .ToDictionary(static code => code.Value);

    [Fact]
    public void EncinaMessaging_CallsSerializeOnlyFromTheSerializers()
    {
        // Arrange
        var messagingTypes = LoadableTypes(typeof(IMessageSerializer).Assembly).ToList();

        // Act
        var callers = DirectSerializeCallers(messagingTypes);

        // Assert
        callers.Where(caller => !AllowList.Contains(caller)).ShouldBeEmpty(
            "persist payloads through SerializeAsRuntimeType, never IMessageSerializer.Serialize<T> directly (#3780)");
    }

    [Fact]
    public void TheAllowList_HasNoStaleEntry()
    {
        // Arrange
        var messagingTypes = LoadableTypes(typeof(IMessageSerializer).Assembly).ToList();

        // Act
        var callers = DirectSerializeCallers(messagingTypes);

        // Assert: an allow-listed caller that no longer calls Serialize<T> must leave the list
        AllowList.Where(entry => !callers.Contains(entry)).ShouldBeEmpty();
    }

    [Fact]
    public void TheScanner_ReportsADirectSerializeCall_ByMethodName()
    {
        // Arrange: a fixture that breaks the rule in a plain method, an async method and a lambda,
        // next to a serializer implementation that is allowed to call its inner serializer.
        Type[] fixtures = [.. WithNestedTypes(typeof(ViolatingFixture)), .. WithNestedTypes(typeof(FixtureSerializer))];

        // Act
        var callers = DirectSerializeCallers(fixtures);

        // Assert
        var prefix = typeof(ViolatingFixture).FullName!;
        callers.ShouldContain($"{prefix}.{nameof(ViolatingFixture.Leak)}");
        callers.ShouldContain($"{prefix}.{nameof(ViolatingFixture.LeakAsync)}");
        callers.ShouldContain($"{prefix}.{nameof(ViolatingFixture.LeakInLambda)}");
        callers.ShouldNotContain($"{prefix}.{nameof(ViolatingFixture.Compliant)}");
        callers.ShouldNotContain(static caller => caller.StartsWith(typeof(FixtureSerializer).FullName!, StringComparison.Ordinal));
    }

    private static System.Collections.Generic.HashSet<string> DirectSerializeCallers(IEnumerable<Type> types)
    {
        var callers = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types.Where(static type => !IsExempt(type)))
        {
            foreach (var method in DeclaredMethods(type).Where(static method => CalledMethods(method).Any(IsSerializeOfAMessageSerializer)))
            {
                callers.Add(Describe(method));
            }
        }

        return callers;
    }

    // The serializers themselves (with their compiler-generated helpers) and the runtime-type extension, the
    // one sanctioned route (it reaches Serialize<T> through reflection), may call Serialize<T>.
    private static bool IsExempt(Type type)
    {
        var source = SourceType(type);
        return typeof(IMessageSerializer).IsAssignableFrom(source)
            || source == typeof(MessageSerializerRuntimeType);
    }

    // The type and every type nested in it, compiler-generated closures and state machines included.
    private static IEnumerable<Type> WithNestedTypes(Type type) =>
        type.GetNestedTypes(AllDeclared).SelectMany(WithNestedTypes).Prepend(type);

    private static bool IsSerializeOfAMessageSerializer(MethodBase called) =>
        called is MethodInfo { Name: nameof(IMessageSerializer.Serialize), IsGenericMethodDefinition: true } method
        && method.DeclaringType is { } declaring
        && typeof(IMessageSerializer).IsAssignableFrom(declaring);

    // "Namespace.Type.SourceMethod", recovering the source type and method of a lambda, local function or
    // state machine from the compiler-generated names (<SourceMethod>d__3, <SourceMethod>b__0_0, ...).
    private static string Describe(MethodBase method)
    {
        var declaring = method.DeclaringType!;
        var sourceName = SourceName(declaring.Name) ?? SourceName(method.Name) ?? method.Name;
        return $"{SourceType(declaring).FullName}.{sourceName}";
    }

    // The nearest enclosing type written in source: compiler-generated types have names starting with '<'.
    private static Type SourceType(Type type)
    {
        while (type.Name.StartsWith('<') && type.DeclaringType is { } declaring)
        {
            type = declaring;
        }

        return type;
    }

    private static string? SourceName(string generatedName)
    {
        if (!generatedName.StartsWith('<'))
        {
            return null;
        }

        var end = generatedName.IndexOf('>', StringComparison.Ordinal);
        return end > 1 ? generatedName[1..end] : null;
    }

    private static IEnumerable<MethodBase> DeclaredMethods(Type type) =>
        type.GetMethods(AllDeclared).Cast<MethodBase>().Concat(type.GetConstructors(AllDeclared));

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

    /// <summary>A deliberate violation the scanner must report (negative check of the rule).</summary>
    public sealed class ViolatingFixture(IMessageSerializer serializer)
    {
        public string Leak(object message) => serializer.Serialize(message);

        public async Task<string> LeakAsync(object message)
        {
            await Task.Yield();
            return serializer.Serialize(message);
        }

        public string LeakInLambda(object message)
        {
            Func<string> serialize = () => serializer.Serialize(message);
            return serialize();
        }

        public string Compliant(object message) => serializer.SerializeAsRuntimeType(message);
    }

    /// <summary>A serializer decorator: calling its inner serializer is the allowed case.</summary>
    public sealed class FixtureSerializer(IMessageSerializer inner) : IMessageSerializer
    {
        public string Serialize<T>(T message) => inner.Serialize(message);

        public T? Deserialize<T>(string content) => inner.Deserialize<T>(content);

        public object? Deserialize(string content, Type type) => inner.Deserialize(content, type);
    }
}
