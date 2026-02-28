using System.Reflection;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Internal descriptor that carries the assembly list for anonymization auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for anonymization attributes
/// (<see cref="AnonymizeAttribute"/>, <see cref="PseudonymizeAttribute"/>,
/// <see cref="TokenizeAttribute"/>).</param>
internal sealed record AnonymizationAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
