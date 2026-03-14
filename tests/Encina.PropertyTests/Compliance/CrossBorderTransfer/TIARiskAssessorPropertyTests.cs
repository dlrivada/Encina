using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.DataResidency;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="ITIARiskAssessor"/> implementations verifying
/// risk score invariants across randomized inputs using FsCheck.
/// </summary>
public class TIARiskAssessorPropertyTests
{
    private readonly ITIARiskAssessor _assessor;

    public TIARiskAssessorPropertyTests()
    {
        // Build the assessor via DI to resolve internal DefaultTIARiskAssessor
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new DataResidencyOptions()));
        services.AddSingleton<IAdequacyDecisionProvider, DefaultAdequacyDecisionProvider>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddEncinaCrossBorderTransfer();

        var provider = services.BuildServiceProvider();
        _assessor = provider.GetRequiredService<ITIARiskAssessor>();
    }

    #region Score Range Invariants

    /// <summary>
    /// Invariant: AssessRisk for any country code always produces a score between 0.0 and 1.0.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AssessRisk_AnyCountry_ScoreBetweenZeroAndOne()
    {
        var countryCodes = Arb.From(Gen.Elements(
            "US", "GB", "DE", "FR", "JP", "CN", "RU", "BR", "IN", "AU",
            "CA", "NZ", "KR", "IL", "CH", "AR", "MX", "ZA", "NG", "EG",
            "SA", "AE", "TH", "VN", "KZ", "BY", "NO", "DK", "NL", "SE"));

        return Prop.ForAll(countryCodes, countryCode =>
        {
            var result = _assessor.AssessRiskAsync(countryCode, "personal-data").AsTask().Result;

            return result.Match(
                assessment => assessment.Score >= 0.0 && assessment.Score <= 1.0,
                _ => false);
        });
    }

    /// <summary>
    /// Invariant: Sensitive data categories yield higher or equal risk than general personal data
    /// for the same destination country.
    /// </summary>
    [Fact]
    public async Task AssessRisk_SensitiveCategory_HigherOrEqualThanGeneral()
    {
        // Use a non-adequate country where the difference is observable
        const string countryCode = "BR";

        var generalResult = await _assessor.AssessRiskAsync(countryCode, "personal-data");
        var sensitiveResult = await _assessor.AssessRiskAsync(countryCode, "health-data");

        var generalScore = generalResult.Match(a => a.Score, _ => -1.0);
        var sensitiveScore = sensitiveResult.Match(a => a.Score, _ => -1.0);

        sensitiveScore.ShouldBeGreaterThanOrEqualTo(generalScore);
    }

    #endregion
}
