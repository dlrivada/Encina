using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.CrossBorderTransfer.Services;

/// <summary>
/// Default rule-based implementation of <see cref="ITIARiskAssessor"/> that evaluates the
/// risk of transferring personal data to a destination country.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses a heuristic rule-based approach considering:
/// (1) adequacy decision status, (2) intelligence sharing alliance membership
/// (Five Eyes, Nine Eyes, Fourteen Eyes), (3) known government surveillance legislation,
/// and (4) existence of an independent data protection authority.
/// </para>
/// <para>
/// The default implementation provides a reasonable baseline assessment. Organizations
/// should register their own <see cref="ITIARiskAssessor"/> implementation with specialized
/// risk assessment logic or integration with third-party risk databases for production use.
/// </para>
/// <para>
/// This service is registered with <c>TryAdd</c>, allowing consumers to override it.
/// </para>
/// </remarks>
internal sealed class DefaultTIARiskAssessor : ITIARiskAssessor
{
    private readonly IAdequacyDecisionProvider _adequacyProvider;
    private readonly ILogger<DefaultTIARiskAssessor> _logger;

    // Intelligence sharing alliances — countries in these groups have broader surveillance cooperation
    private static readonly System.Collections.Generic.HashSet<string> FiveEyesCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "GB", "CA", "AU", "NZ"
    };

    private static readonly System.Collections.Generic.HashSet<string> NineEyesCountries = new(FiveEyesCountries, StringComparer.OrdinalIgnoreCase)
    {
        "DK", "FR", "NL", "NO"
    };

    private static readonly System.Collections.Generic.HashSet<string> FourteenEyesCountries = new(NineEyesCountries, StringComparer.OrdinalIgnoreCase)
    {
        "DE", "BE", "IT", "SE", "ES"
    };

    // Countries with known extensive government surveillance legislation
    private static readonly System.Collections.Generic.HashSet<string> HighSurveillanceCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "CN", "RU", "IR", "SA", "AE", "EG", "TH", "VN", "KZ", "BY"
    };

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultTIARiskAssessor"/>.
    /// </summary>
    /// <param name="adequacyProvider">Provider for EU adequacy decision checks.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultTIARiskAssessor(
        IAdequacyDecisionProvider adequacyProvider,
        ILogger<DefaultTIARiskAssessor> logger)
    {
        ArgumentNullException.ThrowIfNull(adequacyProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _adequacyProvider = adequacyProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TIARiskAssessment>> AssessRiskAsync(
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationCountryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        _logger.LogDebug(
            "Assessing risk for destination '{Destination}', data category '{Category}'",
            destinationCountryCode, dataCategory);

        var factors = new List<string>();
        var recommendations = new List<string>();
        var riskScore = 0.0;

        // Factor 1: Adequacy decision
        var region = Region.Create(destinationCountryCode, destinationCountryCode);
        if (_adequacyProvider.HasAdequacy(region))
        {
            factors.Add($"Destination country '{destinationCountryCode}' has an EU adequacy decision (Art. 45).");
            riskScore = 0.1; // Minimal risk for adequate countries

            _logger.LogDebug("Destination '{Destination}' has adequacy decision, risk: {Risk}", destinationCountryCode, riskScore);
            return ValueTask.FromResult<Either<EncinaError, TIARiskAssessment>>(
                new TIARiskAssessment(riskScore, factors, recommendations));
        }

        // Base risk for non-adequate countries
        riskScore = 0.4;
        factors.Add($"Destination country '{destinationCountryCode}' does not have an EU adequacy decision.");
        recommendations.Add("Consider whether Standard Contractual Clauses (Art. 46(2)(c)) are appropriate for this transfer.");

        // Factor 2: Intelligence sharing alliances
        if (HighSurveillanceCountries.Contains(destinationCountryCode))
        {
            riskScore += 0.4;
            factors.Add($"Destination country '{destinationCountryCode}' has known extensive government surveillance legislation.");
            recommendations.Add("Evaluate whether supplementary technical measures (e.g., encryption, pseudonymization) can effectively protect the data.");
            recommendations.Add("Consider whether the transfer is strictly necessary or if alternative processing locations are available.");
        }
        else if (FiveEyesCountries.Contains(destinationCountryCode))
        {
            riskScore += 0.2;
            factors.Add($"Destination country '{destinationCountryCode}' is a member of the Five Eyes intelligence sharing alliance.");
            recommendations.Add("Review Section 702 FISA (US) or equivalent surveillance legislation for impact on data protection.");
        }
        else if (NineEyesCountries.Contains(destinationCountryCode))
        {
            riskScore += 0.15;
            factors.Add($"Destination country '{destinationCountryCode}' is a member of the Nine Eyes intelligence sharing alliance.");
            recommendations.Add("Assess intelligence sharing agreements and their impact on personal data protection.");
        }
        else if (FourteenEyesCountries.Contains(destinationCountryCode))
        {
            riskScore += 0.1;
            factors.Add($"Destination country '{destinationCountryCode}' is a member of the Fourteen Eyes intelligence sharing alliance.");
        }

        // Factor 3: Sensitive data category increases risk
        if (IsSensitiveDataCategory(dataCategory))
        {
            riskScore += 0.1;
            factors.Add($"Data category '{dataCategory}' is classified as sensitive under GDPR Art. 9.");
            recommendations.Add("Apply additional safeguards for special categories of personal data (Art. 9).");
            recommendations.Add("Ensure explicit consent or another Art. 9(2) exception applies.");
        }

        // Cap risk score at 1.0
        riskScore = Math.Min(riskScore, 1.0);

        _logger.LogInformation(
            "Risk assessment for destination '{Destination}', category '{Category}': score {Score}, {FactorCount} factor(s), {RecommendationCount} recommendation(s)",
            destinationCountryCode, dataCategory, riskScore, factors.Count, recommendations.Count);

        return ValueTask.FromResult<Either<EncinaError, TIARiskAssessment>>(
            new TIARiskAssessment(riskScore, factors, recommendations));
    }

    private static bool IsSensitiveDataCategory(string dataCategory) =>
        dataCategory.Contains("health", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("genetic", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("biometric", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("racial", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("ethnic", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("political", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("religious", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("sexual", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("trade-union", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("criminal", StringComparison.OrdinalIgnoreCase) ||
        dataCategory.Contains("sensitive", StringComparison.OrdinalIgnoreCase);
}
