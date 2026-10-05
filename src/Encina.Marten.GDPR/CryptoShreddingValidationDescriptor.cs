using System.Reflection;

namespace Encina.Marten.GDPR;

/// <summary>
/// Internal descriptor that carries the assembly list of the crypto-shredding startup validation.
/// </summary>
/// <param name="Assemblies">The assemblies to scan for <see cref="CryptoShreddedAttribute"/>.</param>
internal sealed record CryptoShreddingValidationDescriptor(IReadOnlyList<Assembly> Assemblies);
