using ReflectionAssembly = System.Reflection.Assembly;
using ReflectionType = System.Type;

namespace Encina.Testing.Modules;

/// <summary>
/// Represents information about a discovered module.
/// </summary>
/// <param name="Name">The module name.</param>
/// <param name="Type">The module type.</param>
/// <param name="Assembly">The assembly containing the module.</param>
/// <param name="Namespace">The module's root namespace.</param>
public sealed record ModuleInfo(string Name, ReflectionType Type, ReflectionAssembly Assembly, string Namespace);
