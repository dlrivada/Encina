using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(d): Supply chain security.
/// </summary>
internal sealed class SupplyChainSecurityEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.SupplyChainSecurity;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var suppliers = context.Options.Suppliers;

        if (suppliers.Count == 0)
        {
            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.NotSatisfied(Measure,
                    "No suppliers registered for supply chain security assessment.",
                    ["Register suppliers via NIS2Options.AddSupplier()", "Assess security posture of all direct suppliers and service providers"])));
        }

        var hasCriticalSuppliers = suppliers.Values.Any(s => s.RiskLevel >= SupplierRiskLevel.Critical);
        if (hasCriticalSuppliers)
        {
            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.NotSatisfied(Measure,
                    "One or more suppliers have a Critical risk level.",
                    ["Immediately review critical-risk suppliers", "Implement additional contractual controls", "Evaluate alternative suppliers"])));
        }

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.Satisfied(Measure,
                $"Supply chain security is configured with {suppliers.Count} registered supplier(s) and no critical risks.")));
    }
}
