using System.Reflection;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Internal descriptor that carries the assembly list for auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="ProcessingActivityAttribute"/>.</param>
internal sealed record GDPRAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
