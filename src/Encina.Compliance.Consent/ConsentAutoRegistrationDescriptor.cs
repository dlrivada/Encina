using System.Reflection;

namespace Encina.Compliance.Consent;

/// <summary>
/// Internal descriptor that carries the assembly list for consent auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="RequireConsentAttribute"/>.</param>
internal sealed record ConsentAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
