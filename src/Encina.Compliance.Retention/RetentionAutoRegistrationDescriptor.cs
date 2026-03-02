using System.Reflection;

namespace Encina.Compliance.Retention;

/// <summary>
/// Internal descriptor that carries the assembly list for retention auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="RetentionPeriodAttribute"/> decorations.</param>
internal sealed record RetentionAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
