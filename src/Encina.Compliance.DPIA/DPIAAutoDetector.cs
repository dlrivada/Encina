using System.Collections.Frozen;
using System.Reflection;

using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;

using Microsoft.Extensions.Logging;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Heuristic auto-detector that identifies request types likely to involve high-risk processing.
/// </summary>
/// <remarks>
/// <para>
/// Per EDPB WP 248 rev.01, organizations should proactively identify processing operations
/// that are likely to result in a high risk to the rights and freedoms of natural persons.
/// This detector supplements the declarative <see cref="RequiresDPIAAttribute"/> approach
/// by analyzing type names and property names for patterns that indicate high-risk processing.
/// </para>
/// <para>
/// The detector checks for naming patterns associated with the nine high-risk criteria
/// identified by the EDPB (see <see cref="HighRiskTriggers"/>). A type is considered
/// high-risk when <b>two or more</b> distinct triggers are detected, consistent with the
/// EDPB guidance that the presence of two or more criteria generally triggers the need for a DPIA.
/// </para>
/// <para>
/// This class is only invoked when <see cref="DPIAOptions.AutoDetectHighRisk"/> is <c>true</c>.
/// It is used by the <see cref="DPIAAutoRegistrationHostedService"/> to discover types
/// that might need DPIA assessment even without explicit attribute decoration.
/// </para>
/// </remarks>
internal sealed class DPIAAutoDetector
{
    // Minimum number of distinct triggers required for a type to be considered high-risk.
    // Aligned with EDPB WP 248 rev.01 guidance.
    private const int MinimumTriggersForHighRisk = 2;

    /// <summary>
    /// Keyword-to-trigger mapping. Each keyword, when found in a type name or property name,
    /// maps to a specific <see cref="HighRiskTriggers"/> value.
    /// </summary>
    private static readonly FrozenDictionary<string, string> KeywordTriggerMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Biometric data (Art. 9)
            ["biometric"] = HighRiskTriggers.BiometricData,
            ["fingerprint"] = HighRiskTriggers.BiometricData,
            ["facial"] = HighRiskTriggers.BiometricData,
            ["facerecognition"] = HighRiskTriggers.BiometricData,
            ["iris"] = HighRiskTriggers.BiometricData,
            ["voiceprint"] = HighRiskTriggers.BiometricData,

            // Health data (Art. 9)
            ["health"] = HighRiskTriggers.HealthData,
            ["medical"] = HighRiskTriggers.HealthData,
            ["patient"] = HighRiskTriggers.HealthData,
            ["diagnosis"] = HighRiskTriggers.HealthData,
            ["clinical"] = HighRiskTriggers.HealthData,

            // Automated decision-making (Art. 22)
            ["creditscore"] = HighRiskTriggers.AutomatedDecisionMaking,
            ["creditscoring"] = HighRiskTriggers.AutomatedDecisionMaking,
            ["automateddecision"] = HighRiskTriggers.AutomatedDecisionMaking,
            ["eligibility"] = HighRiskTriggers.AutomatedDecisionMaking,

            // Systematic profiling (Art. 35(3)(a))
            ["profiling"] = HighRiskTriggers.SystematicProfiling,
            ["scoring"] = HighRiskTriggers.SystematicProfiling,
            ["behavioral"] = HighRiskTriggers.SystematicProfiling,

            // Public monitoring (Art. 35(3)(c))
            ["surveillance"] = HighRiskTriggers.PublicMonitoring,
            ["cctv"] = HighRiskTriggers.PublicMonitoring,
            ["tracking"] = HighRiskTriggers.PublicMonitoring,
            ["geolocation"] = HighRiskTriggers.PublicMonitoring,

            // Special category data (Art. 9)
            ["ethnicity"] = HighRiskTriggers.SpecialCategoryData,
            ["religion"] = HighRiskTriggers.SpecialCategoryData,
            ["political"] = HighRiskTriggers.SpecialCategoryData,
            ["tradeunion"] = HighRiskTriggers.SpecialCategoryData,
            ["sexualorientation"] = HighRiskTriggers.SpecialCategoryData,
            ["genetic"] = HighRiskTriggers.SpecialCategoryData,

            // Large-scale processing (Art. 35(3)(b))
            ["largescale"] = HighRiskTriggers.LargeScaleProcessing,
            ["bulk"] = HighRiskTriggers.LargeScaleProcessing,
            ["massprocessing"] = HighRiskTriggers.LargeScaleProcessing,
            ["batch"] = HighRiskTriggers.LargeScaleProcessing,

            // Vulnerable subjects
            ["child"] = HighRiskTriggers.VulnerableSubjects,
            ["minor"] = HighRiskTriggers.VulnerableSubjects,
            ["elderly"] = HighRiskTriggers.VulnerableSubjects,
            ["vulnerable"] = HighRiskTriggers.VulnerableSubjects,

            // Novel technology
            ["machinelearning"] = HighRiskTriggers.NovelTechnology,
            ["deeplearning"] = HighRiskTriggers.NovelTechnology,
            ["neuralnetwork"] = HighRiskTriggers.NovelTechnology,
            ["blockchain"] = HighRiskTriggers.NovelTechnology,

            // Cross-border transfer
            ["crossborder"] = HighRiskTriggers.CrossBorderTransfer,
            ["internationaltransfer"] = HighRiskTriggers.CrossBorderTransfer,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAAutoDetector"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DPIAAutoDetector(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a type for high-risk processing patterns and returns the detected triggers.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns>
    /// A list of detected high-risk trigger names, or an empty list if the type is not
    /// considered high-risk. A type is high-risk when two or more distinct triggers are detected.
    /// </returns>
    public IReadOnlyList<string> DetectHighRiskTriggers(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var detectedTriggers = new HashSet<string>(StringComparer.Ordinal);
        var typeName = type.Name;

        // Check type name against keyword map
        ScanText(typeName, detectedTriggers);

        // Check property names and types
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            ScanText(property.Name, detectedTriggers);
        }

        if (detectedTriggers.Count >= MinimumTriggersForHighRisk)
        {
            var triggersString = string.Join(", ", detectedTriggers);
            _logger.AutoDetectionHighRisk(typeName, triggersString);
            return [.. detectedTriggers];
        }

        _logger.AutoDetectionNotHighRisk(typeName);
        return [];
    }

    /// <summary>
    /// Determines whether a type represents high-risk processing based on heuristic analysis.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns><see langword="true"/> if two or more high-risk triggers are detected.</returns>
    public bool IsHighRisk(Type type)
    {
        return DetectHighRiskTriggers(type).Count >= MinimumTriggersForHighRisk;
    }

    private static void ScanText(string text, HashSet<string> detectedTriggers)
    {
        foreach (var (keyword, trigger) in KeywordTriggerMap)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                detectedTriggers.Add(trigger);
            }
        }
    }
}
