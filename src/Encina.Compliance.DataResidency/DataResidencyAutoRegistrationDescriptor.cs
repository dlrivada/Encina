using System.Reflection;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Internal descriptor that carries the assembly list for data residency auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="Attributes.DataResidencyAttribute"/> decorations.</param>
internal sealed record DataResidencyAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
