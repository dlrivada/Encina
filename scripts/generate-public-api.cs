#!/usr/bin/env dotnet run
// Generates PublicAPI.Unshipped.txt entries from the compiled assembly using reflection.

using System.Reflection;
using System.Text;

var repoRoot = args.Length > 0 ? args[0] : @"D:\Proyectos\Encina";
var assemblyPath = Path.Combine(repoRoot,
    "src", "Encina.Security.ABAC", "bin", "Debug", "net10.0", "Encina.Security.ABAC.dll");

if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
    return 1;
}

// Set up assembly resolver for dependencies (LanguageExt, etc.)
var assemblyDir = Path.GetDirectoryName(assemblyPath)!;
System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    var depPath = Path.Combine(assemblyDir, name.Name + ".dll");
    if (File.Exists(depPath))
        return context.LoadFromAssemblyPath(depPath);
    return null;
};

var assembly = Assembly.LoadFrom(assemblyPath);
var entries = new SortedSet<string>(StringComparer.Ordinal);

foreach (var type in assembly.GetExportedTypes())
{
    var ns = type.Namespace ?? "";
    if (!ns.StartsWith("Encina.Security.ABAC", StringComparison.Ordinal))
        continue;

    // Type declaration
    var typePrefix = "";
    if (type.IsAbstract && type.IsSealed) // static class
        typePrefix = "static ";

    var typeName = FormatTypeName(type);
    entries.Add($"{typePrefix}{typeName}");

    // Enum members
    if (type.IsEnum)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var value = Convert.ToInt32(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture);
            entries.Add($"{typeName}.{field.Name} = {value} -> {typeName}");
        }
        continue;
    }

    // Constants
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        if (field.IsLiteral && !field.IsInitOnly)
        {
            var val = field.GetRawConstantValue();
            var valStr = val is string s ? $"\"{s}\"" : val?.ToString() ?? "null";
            entries.Add($"const {typeName}.{field.Name} = {valStr} -> {FormatReturnType(field.FieldType)}");
        }
        else if (field.IsStatic && field.IsPublic)
        {
            entries.Add($"static {typeName}.{field.Name} -> {FormatReturnType(field.FieldType)}");
        }
    }

    // Properties (instance and static)
    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        var isStatic = (prop.GetMethod?.IsStatic ?? prop.SetMethod?.IsStatic) == true;
        var prefix = isStatic ? "static " : "";
        var getter = prop.GetMethod;
        var setter = prop.SetMethod;

        if (getter is not null && getter.IsPublic)
        {
            entries.Add($"{prefix}{typeName}.{prop.Name}.get -> {FormatReturnType(prop.PropertyType)}");
        }
        if (setter is not null && setter.IsPublic)
        {
            // Detect init-only setters
            var isInit = setter.ReturnParameter.GetRequiredCustomModifiers()
                .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            var accessorName = isInit ? "init" : "set";
            entries.Add($"{prefix}{typeName}.{prop.Name}.{accessorName} -> void");
        }
    }

    // Constructors
    foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    {
        var parameters = FormatParameters(ctor.GetParameters());
        entries.Add($"{typeName}.{type.Name}({parameters}) -> void");
    }

    // Methods (instance and static, excluding property accessors and record methods)
    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        if (method.IsSpecialName) continue; // Skip property accessors, event methods, operators
        if (method.DeclaringType != type) continue;

        // Skip compiler-generated methods for records
        var name = method.Name;
        if (name is "Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "<Clone>$" or "PrintMembers")
            continue;

        var isStatic = method.IsStatic;
        var prefix = isStatic ? "static " : "";
        var genericSuffix = "";
        if (method.IsGenericMethod)
        {
            var tparams = method.GetGenericArguments();
            genericSuffix = "<" + string.Join(", ", tparams.Select(t => t.Name)) + ">";
        }

        var parameters = FormatParameters(method.GetParameters());
        var returnType = FormatReturnType(method.ReturnType);
        entries.Add($"{prefix}{typeName}.{name}{genericSuffix}({parameters}) -> {returnType}");
    }
}

// Output
Console.WriteLine("#nullable enable");
foreach (var entry in entries)
{
    Console.WriteLine(entry);
}

return 0;

// ─── Helper Methods ─────────────────────────────────────────────────────

string FormatTypeName(Type type)
{
    if (type.IsGenericType)
    {
        var name = type.FullName ?? type.Name;
        var backtick = name.IndexOf('`');
        if (backtick > 0) name = name[..backtick];
        name = name.Replace('+', '.');
        var args = type.GetGenericArguments();
        return $"{name}<{string.Join(", ", args.Select(FormatTypeName))}>";
    }
    if (type.IsNested)
    {
        return $"{FormatTypeName(type.DeclaringType!)}.{type.Name}";
    }
    return type.FullName ?? type.Name;
}

string FormatReturnType(Type type)
{
    if (type == typeof(void)) return "void";
    if (type == typeof(string)) return "string!";
    if (type == typeof(bool)) return "bool";
    if (type == typeof(int)) return "int";
    if (type == typeof(double)) return "double";
    if (type == typeof(object)) return "object?";
    if (type == typeof(TimeSpan)) return "System.TimeSpan";
    if (type == typeof(Type)) return "System.Type!";
    if (type == typeof(Guid)) return "System.Guid";

    if (type.IsGenericType)
    {
        var def = type.GetGenericTypeDefinition();
        var args = type.GetGenericArguments();

        if (def == typeof(Nullable<>))
        {
            return $"{FormatReturnType(args[0])}?";
        }

        // Format generic name
        var name = def.FullName ?? def.Name;
        var backtick = name.IndexOf('`');
        if (backtick > 0) name = name[..backtick];

        var formattedArgs = string.Join(", ", args.Select(FormatReturnType));
        var formatted = $"{name}<{formattedArgs}>";

        // Add ! for reference types
        if (!type.IsValueType)
            formatted += "!";
        return formatted;
    }

    if (type.IsArray)
    {
        return $"{FormatReturnType(type.GetElementType()!)}[]!";
    }

    if (type.IsEnum)
    {
        return FormatTypeName(type);
    }

    // For Encina namespace types
    var fullName = type.FullName ?? type.Name;
    if (!type.IsValueType)
        fullName += "!";
    return fullName;
}

string FormatParameterType(ParameterInfo param)
{
    var type = param.ParameterType;
    var result = FormatReturnType(type);

    // Handle nullable reference type annotations
    if (!type.IsValueType)
    {
        var nullableAttr = param.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
        if (nullableAttr != null)
        {
            var args = nullableAttr.ConstructorArguments;
            if (args.Count > 0)
            {
                if (args[0].Value is byte b)
                {
                    if (b == 2) // nullable
                        result = result.TrimEnd('!') + "?";
                }
            }
        }
    }

    return result;
}

string FormatParameters(ParameterInfo[] parameters)
{
    if (parameters.Length == 0) return "";

    var parts = new List<string>();
    foreach (var p in parameters)
    {
        var typeName = FormatParameterType(p);
        var paramStr = $"{typeName} {p.Name}";

        if (p.HasDefaultValue)
        {
            var defaultVal = p.DefaultValue;
            if (defaultVal == null)
                paramStr += " = null";
            else if (defaultVal is bool b)
                paramStr += b ? " = true" : " = false";
            else if (defaultVal is string s)
                paramStr += $" = \"{s}\"";
            else if (defaultVal is CancellationToken)
                paramStr += $" = default(System.Threading.CancellationToken)";
            else
                paramStr += $" = {defaultVal}";
        }

        parts.Add(paramStr);
    }

    return string.Join(", ", parts);
}
