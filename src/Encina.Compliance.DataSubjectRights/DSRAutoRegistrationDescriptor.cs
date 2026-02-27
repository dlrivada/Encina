using System.Reflection;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Internal descriptor that carries the assembly list for DSR auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="PersonalDataAttribute"/>.</param>
internal sealed record DSRAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
