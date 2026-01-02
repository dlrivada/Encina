using System.Collections.Generic;

namespace Encina.Testing.Modules;

/// <summary>
/// Represents a circular dependency chain.
/// </summary>
/// <param name="ModulesInCycle">The ordered list of modules in the cycle.</param>
public sealed record CircularDependency(IReadOnlyList<string> ModulesInCycle);
