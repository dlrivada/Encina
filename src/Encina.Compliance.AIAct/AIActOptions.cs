using System.Reflection;

using Encina.Compliance.AIAct.Model;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Configuration options for the AI Act compliance pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="AIActCompliancePipelineBehavior{TRequest, TResponse}"/>
/// and how AI system registrations are validated at runtime.
/// Register via <c>AddEncinaAIAct(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAIAct(options =>
/// {
///     options.EnforcementMode = AIActEnforcementMode.Block;
///     options.AutoRegisterFromAttributes = true;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
/// });
/// </code>
/// </example>
public sealed class AIActOptions
{
    /// <summary>
    /// Gets or sets the enforcement mode for AI Act compliance checks.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="AIActEnforcementMode.Block"/> — non-compliant requests are blocked with an error (default).</description></item>
    /// <item><description><see cref="AIActEnforcementMode.Warn"/> — non-compliant requests log a warning but proceed.</description></item>
    /// <item><description><see cref="AIActEnforcementMode.Disabled"/> — no enforcement checks are performed.</description></item>
    /// </list>
    /// Default is <see cref="AIActEnforcementMode.Block"/>.
    /// </remarks>
    public AIActEnforcementMode EnforcementMode { get; set; } = AIActEnforcementMode.Block;

    /// <summary>
    /// Gets or sets whether to automatically register AI systems from
    /// <see cref="Attributes.HighRiskAIAttribute"/> at startup.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the assemblies in <see cref="AssembliesToScan"/> are scanned for
    /// <see cref="Attributes.HighRiskAIAttribute"/> decorations and the systems are registered
    /// in the <see cref="Abstractions.IAISystemRegistry"/>.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register an AI Act health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the AI system registry
    /// is populated and the compliance validator is registered. Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets the assemblies to scan for <see cref="Attributes.HighRiskAIAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your request types decorated with
    /// <see cref="Attributes.HighRiskAIAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the entry assembly will be scanned by default.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];
}
