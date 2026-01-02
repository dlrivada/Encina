namespace Encina.Testing.Modules;

/// <summary>
/// Represents a dependency between two modules.
/// </summary>
/// <param name="SourceModule">The source module name.</param>
/// <param name="TargetModule">The target module name.</param>
/// <param name="DependencyType">The type of dependency.</param>
public sealed record ModuleDependency(string SourceModule, string TargetModule, DependencyType DependencyType);
