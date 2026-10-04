using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Marten.GDPR;

/// <summary>
/// Recognises the compiler-synthesized primary constructor of a positional record: one that only stores
/// each parameter unchanged into a backing field (and passes the rest to the base constructor).
/// </summary>
/// <remarks>
/// <para>
/// A constructor-bound <c>[CryptoShredded]</c> property receives the ciphertext token on read, before the
/// deferred decryption runs. That is harmless only when the constructor stores the token unchanged into the
/// property's backing field, because decryption then overwrites it through the <c>init</c> setter. Any other
/// constructor could normalize or validate the token, which corrupts or rejects the data.
/// </para>
/// <para>
/// The check reads the constructor IL. Only <c>nop</c>, <c>ldarg</c>, <c>stfld</c>, a <c>call</c> to a base
/// constructor and <c>ret</c> are allowed; the parameter must be stored by <c>ldarg</c> immediately followed by
/// <c>stfld</c> into <c>&lt;Name&gt;k__BackingField</c> of the same type. Anything else is treated as a
/// transforming constructor (fail closed).
/// </para>
/// </remarks>
internal static class RecordConstructorInspector
{
    private const byte Nop = 0x00;
    private const byte LdArg0 = 0x02;
    private const byte LdArg3 = 0x05;
    private const byte LdArgS = 0x0E;
    private const byte Call = 0x28;
    private const byte Ret = 0x2A;
    private const byte StFld = 0x7D;

    private static readonly ConcurrentDictionary<ConstructorInfo, Dictionary<int, FieldInfo>?> Stores = new();

    /// <summary>
    /// Gets whether <paramref name="constructor"/> stores parameter <paramref name="position"/> unchanged into the
    /// backing field of <paramref name="property"/>, and does nothing else that could transform it.
    /// </summary>
    internal static bool StoresUnchanged(ConstructorInfo constructor, int position, PropertyInfo property)
    {
        if (!IsRecord(constructor.DeclaringType!) || property.DeclaringType != constructor.DeclaringType)
        {
            return false;
        }

        var stores = Stores.GetOrAdd(constructor, static c => ReadStores(c));
        return stores is not null
            && stores.TryGetValue(position, out var field)
            && field.Name == $"<{property.Name}>k__BackingField";
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;

    /// <summary>
    /// Reads the parameter-to-field stores of a constructor, or <c>null</c> when the IL contains anything
    /// other than the synthesized shape.
    /// </summary>
    private static Dictionary<int, FieldInfo>? ReadStores(ConstructorInfo constructor)
    {
        try
        {
            var il = constructor.GetMethodBody()?.GetILAsByteArray();
            return il is null ? null : Parse(constructor, il);
        }
        catch (Exception ex) when (ex is ArgumentException or BadImageFormatException or InvalidOperationException)
        {
            return null;
        }
    }

    private static Dictionary<int, FieldInfo>? Parse(ConstructorInfo constructor, byte[] il)
    {
        var stores = new Dictionary<int, FieldInfo>();
        var lastArgument = -1;
        var offset = 0;
        while (offset < il.Length)
        {
            var step = Step(constructor, il, offset, ref lastArgument, stores);
            if (step <= 0)
            {
                return null;
            }

            offset += step;
        }

        return stores;
    }

    /// <summary>Consumes one instruction; returns its length, or 0 when the instruction is not allowed.</summary>
    private static int Step(
        ConstructorInfo constructor, byte[] il, int offset, ref int lastArgument, Dictionary<int, FieldInfo> stores)
    {
        var opcode = il[offset];
        switch (opcode)
        {
            case Nop:
            case Ret:
                lastArgument = -1;
                return 1;
            case >= LdArg0 and <= LdArg3:
                lastArgument = opcode - LdArg0;
                return 1;
            case LdArgS when offset + 1 < il.Length:
                lastArgument = il[offset + 1];
                return 2;
            case StFld when offset + 4 < il.Length:
                return StoreField(constructor, BitConverter.ToInt32(il, offset + 1), ref lastArgument, stores);
            case Call when offset + 4 < il.Length:
                lastArgument = -1;
                return IsBaseConstructor(constructor, BitConverter.ToInt32(il, offset + 1)) ? 5 : 0;
            default:
                return 0;
        }
    }

    private static int StoreField(
        ConstructorInfo constructor, int token, ref int lastArgument, Dictionary<int, FieldInfo> stores)
    {
        if (lastArgument >= 1)
        {
            stores[lastArgument - 1] = constructor.Module.ResolveField(token, TypeArguments(constructor), null)!;
        }

        lastArgument = -1;
        return 5;
    }

    private static bool IsBaseConstructor(ConstructorInfo constructor, int token) =>
        constructor.Module.ResolveMethod(token, TypeArguments(constructor), null) is ConstructorInfo target
        && target.DeclaringType == constructor.DeclaringType!.BaseType;

    private static Type[]? TypeArguments(ConstructorInfo constructor) =>
        constructor.DeclaringType!.IsGenericType ? constructor.DeclaringType.GetGenericArguments() : null;
}
