using System.Reflection;

using Encina.Compliance.NIS2.Model;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Cached NIS2 attribute information extracted from a request type.
/// </summary>
/// <remarks>
/// Resolved once per closed generic type via the CLR's static field guarantee,
/// eliminating reflection overhead on subsequent requests for the same type.
/// </remarks>
internal sealed record NIS2AttributeInfo
{
    /// <summary>Whether the request is decorated with <see cref="NIS2CriticalAttribute"/>.</summary>
    public required bool IsNIS2Critical { get; init; }

    /// <summary>Optional description from <see cref="NIS2CriticalAttribute.Description"/>.</summary>
    public string? CriticalDescription { get; init; }

    /// <summary>Whether the request is decorated with <see cref="RequireMFAAttribute"/>.</summary>
    public required bool RequiresMFA { get; init; }

    /// <summary>Optional reason from <see cref="RequireMFAAttribute.Reason"/>.</summary>
    public string? MFAReason { get; init; }

    /// <summary>Supplier IDs from all <see cref="NIS2SupplyChainCheckAttribute"/> instances.</summary>
    public required IReadOnlyList<string> SupplyChainChecks { get; init; }

    /// <summary>Minimum risk level thresholds per supplier from <see cref="NIS2SupplyChainCheckAttribute"/>.</summary>
    public required IReadOnlyDictionary<string, SupplierRiskLevel> SupplyChainRiskThresholds { get; init; }

    /// <summary>Whether the request has any NIS2-related attributes.</summary>
    public bool HasAnyAttribute => IsNIS2Critical || RequiresMFA || SupplyChainChecks.Count > 0;

    /// <summary>
    /// Resolves NIS2 attribute information from a request type via reflection.
    /// </summary>
    /// <param name="requestType">The request type to inspect.</param>
    /// <returns>
    /// An <see cref="NIS2AttributeInfo"/> with cached attribute data.
    /// <see cref="HasAnyAttribute"/> is <c>false</c> when no NIS2 attributes are present.
    /// </returns>
    public static NIS2AttributeInfo FromType(Type requestType)
    {
        var criticalAttr = requestType.GetCustomAttribute<NIS2CriticalAttribute>();
        var mfaAttr = requestType.GetCustomAttribute<RequireMFAAttribute>();
        var supplyChainAttrs = requestType.GetCustomAttributes<NIS2SupplyChainCheckAttribute>().ToList();

        var supplierIds = supplyChainAttrs.Select(a => a.SupplierId).ToList();
        var riskThresholds = supplyChainAttrs.ToDictionary(
            a => a.SupplierId,
            a => a.MinimumRiskLevel,
            StringComparer.OrdinalIgnoreCase);

        return new NIS2AttributeInfo
        {
            IsNIS2Critical = criticalAttr is not null,
            CriticalDescription = criticalAttr?.Description,
            RequiresMFA = mfaAttr is not null,
            MFAReason = mfaAttr?.Reason,
            SupplyChainChecks = supplierIds,
            SupplyChainRiskThresholds = riskThresholds
        };
    }
}
