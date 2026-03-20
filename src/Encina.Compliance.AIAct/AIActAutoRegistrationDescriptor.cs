using System.Reflection;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Internal descriptor that carries the assembly list for AI Act auto-registration at startup.
/// </summary>
/// <param name="Assemblies">
/// The assemblies to scan for <see cref="Attributes.HighRiskAIAttribute"/>,
/// <see cref="Attributes.RequireHumanOversightAttribute"/>, and
/// <see cref="Attributes.AITransparencyAttribute"/>.
/// </param>
internal sealed record AIActAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies);
