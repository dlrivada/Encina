using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Risk criterion for processing special categories of personal data per GDPR Article 35(3)(b).
/// </summary>
/// <remarks>
/// <para>
/// Evaluates whether the processing involves special categories of data as defined in
/// Article 9(1): racial or ethnic origin, political opinions, religious or philosophical
/// beliefs, trade union membership, genetic data, biometric data, health data, sex life,
/// and sexual orientation. Also covers criminal convictions data under Article 10.
/// </para>
/// <para>
/// Risk levels assigned:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="RiskLevel.High"/>: Any special category data is present.</description></item>
/// <item><description><see cref="RiskLevel.VeryHigh"/>: Special category data combined with large-scale processing.</description></item>
/// </list>
/// </remarks>
public sealed class SpecialCategoryDataCriterion : IRiskCriterion
{
    private static readonly HashSet<string> SpecialCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "health",
        "biometric",
        "genetic",
        "racial",
        "ethnic",
        "political",
        "religious",
        "philosophical",
        "trade-union",
        "sexual-orientation",
        "criminal",
    };

    /// <inheritdoc />
    public string Name => "Special Category Data (Art. 35(3)(b))";

    /// <inheritdoc />
    public ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var matchedCategories = context.DataCategories
            .Where(c => SpecialCategories.Contains(c))
            .ToList();

        if (matchedCategories.Count == 0)
        {
            return ValueTask.FromResult<RiskItem?>(null);
        }

        var isLargeScale = context.HighRiskTriggers.Contains(HighRiskTriggers.LargeScaleProcessing);
        var level = isLargeScale ? RiskLevel.VeryHigh : RiskLevel.High;

        var categoriesText = string.Join(", ", matchedCategories);
        var description = isLargeScale
            ? $"Large-scale processing of special category data ({categoriesText}) significantly increases risk to data subjects."
            : $"Processing of special category data ({categoriesText}) requires enhanced protection under GDPR Article 9.";

        return ValueTask.FromResult<RiskItem?>(new RiskItem(
            Category: "Special Category Data",
            Level: level,
            Description: description,
            MitigationSuggestion: "Apply strict access controls, encryption, and pseudonymization for special category data."));
    }
}
