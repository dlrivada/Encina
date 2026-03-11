using System.Reflection;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Internal descriptor that carries the assembly list for DPIA auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="RequiresDPIAAttribute"/>.</param>
internal sealed record DPIAAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
