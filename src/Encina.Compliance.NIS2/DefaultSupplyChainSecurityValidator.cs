using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Default implementation of <see cref="ISupplyChainSecurityValidator"/> that evaluates
/// suppliers against the configured <see cref="NIS2Options.Suppliers"/> registry.
/// </summary>
internal sealed class DefaultSupplyChainSecurityValidator : ISupplyChainSecurityValidator
{
    private readonly IOptions<NIS2Options> _options;
    private readonly TimeProvider _timeProvider;

    public DefaultSupplyChainSecurityValidator(
        IOptions<NIS2Options> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, SupplyChainAssessment>> AssessSupplierAsync(
        string supplierId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplierId);

        if (!_options.Value.Suppliers.TryGetValue(supplierId, out var config))
        {
            return ValueTask.FromResult<Either<EncinaError, SupplyChainAssessment>>(
                NIS2Errors.SupplierNotFound(supplierId));
        }

        var now = _timeProvider.GetUtcNow();
        var supplierInfo = config.ToSupplierInfo();
        var risks = new List<SupplierRisk>();

        // Risk: High or Critical risk level
        if (supplierInfo.RiskLevel >= SupplierRiskLevel.High)
        {
            risks.Add(new SupplierRisk
            {
                SupplierId = supplierId,
                RiskLevel = supplierInfo.RiskLevel,
                RiskDescription = $"Supplier has a {supplierInfo.RiskLevel} risk level.",
                RecommendedActions = ["Review supplier security posture", "Consider alternative suppliers", "Implement additional contractual controls"]
            });
        }

        // Risk: No assessment or stale assessment
        if (supplierInfo.LastAssessmentAtUtc is null)
        {
            risks.Add(new SupplierRisk
            {
                SupplierId = supplierId,
                RiskLevel = SupplierRiskLevel.Medium,
                RiskDescription = "Supplier has never been assessed.",
                RecommendedActions = ["Conduct initial security assessment", "Request supplier security questionnaire"]
            });
        }
        else if (supplierInfo.LastAssessmentAtUtc.Value.AddMonths(12) < now)
        {
            risks.Add(new SupplierRisk
            {
                SupplierId = supplierId,
                RiskLevel = SupplierRiskLevel.Medium,
                RiskDescription = "Supplier assessment is older than 12 months.",
                RecommendedActions = ["Schedule reassessment", "Request updated security documentation"]
            });
        }

        // Risk: No certification
        if (string.IsNullOrEmpty(supplierInfo.CertificationStatus))
        {
            risks.Add(new SupplierRisk
            {
                SupplierId = supplierId,
                RiskLevel = SupplierRiskLevel.Low,
                RiskDescription = "Supplier has no security certifications on file.",
                RecommendedActions = ["Request certification evidence (ISO 27001, SOC 2, etc.)"]
            });
        }

        var overallRisk = risks.Count > 0
            ? risks.Max(r => r.RiskLevel)
            : supplierInfo.RiskLevel;

        var nextAssessmentMonths = overallRisk switch
        {
            SupplierRiskLevel.Critical => 1,
            SupplierRiskLevel.High => 3,
            SupplierRiskLevel.Medium => 6,
            _ => 12
        };

        var assessment = SupplyChainAssessment.Create(
            supplierId,
            overallRisk,
            risks,
            now,
            now.AddMonths(nextAssessmentMonths));

        return ValueTask.FromResult(Right<EncinaError, SupplyChainAssessment>(assessment));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<SupplierRisk>>> GetSupplierRisksAsync(
        CancellationToken cancellationToken = default)
    {
        var allRisks = new List<SupplierRisk>();

        foreach (var supplierId in _options.Value.Suppliers.Keys)
        {
            var config = _options.Value.Suppliers[supplierId];

            if (config.RiskLevel >= SupplierRiskLevel.High)
            {
                allRisks.Add(new SupplierRisk
                {
                    SupplierId = supplierId,
                    RiskLevel = config.RiskLevel,
                    RiskDescription = $"Supplier '{config.Name}' has risk level {config.RiskLevel}.",
                    RecommendedActions = ["Review and mitigate supplier risk"]
                });
            }
        }

        return ValueTask.FromResult(
            Right<EncinaError, IReadOnlyList<SupplierRisk>>(allRisks));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> ValidateSupplierForOperationAsync(
        string supplierId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplierId);

        if (!_options.Value.Suppliers.TryGetValue(supplierId, out var config))
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(
                NIS2Errors.SupplierNotFound(supplierId));
        }

        // Critical suppliers are never acceptable; High depends on enforcement mode
        var isAcceptable = config.RiskLevel < SupplierRiskLevel.Critical;

        return ValueTask.FromResult(Right<EncinaError, bool>(isAcceptable));
    }
}
