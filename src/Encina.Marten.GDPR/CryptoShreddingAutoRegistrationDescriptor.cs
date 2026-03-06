using System.Reflection;

namespace Encina.Marten.GDPR;

/// <summary>
/// Internal descriptor that carries the assembly list for crypto-shredding auto-registration at startup.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="CryptoShreddedAttribute"/>.</param>
internal sealed record CryptoShreddingAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
