using System.Reflection;

using GDPR = Encina.Compliance.GDPR;

namespace Encina.Compliance.LawfulBasis.AutoRegistration;

/// <summary>
/// Internal descriptor that carries the assembly list and default bases for lawful basis auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="GDPR.LawfulBasisAttribute"/>.</param>
/// <param name="DefaultBases">
/// Programmatic default lawful bases from <see cref="LawfulBasisOptions.DefaultBases"/>.
/// </param>
internal sealed record LawfulBasisAutoRegistrationDescriptor(
    IReadOnlyList<Assembly> Assemblies,
    IReadOnlyDictionary<Type, GDPR.LawfulBasis> DefaultBases);
