using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Model;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Compliance.NIS2;

/// <summary>
/// Property-based tests for <see cref="NIS2OptionsValidator"/> verifying validation invariants
/// using FsCheck random data generation.
/// </summary>
public sealed class NIS2OptionsValidatorPropertyTests
{
    private readonly NIS2OptionsValidator _validator = new();

    /// <summary>
    /// Invariant: Default options fail validation because Essential entities require CompetentAuthority
    /// and EnforceEncryption=true requires at least one category or endpoint.
    /// </summary>
    [Fact]
    public void DefaultOptions_FailValidation_DueToEssentialRequirements()
    {
        var options = new NIS2Options();

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded,
            "Default options should fail because Essential requires CompetentAuthority and EnforceEncryption requires categories");
        Assert.Contains("CompetentAuthority", result.FailureMessage);
    }

    /// <summary>
    /// Invariant: Positive IncidentNotificationHours always passes that specific validation.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool PositiveNotificationHours_NeverFailsHoursValidation(PositiveInt hours)
    {
        var options = new NIS2Options { IncidentNotificationHours = hours.Get };

        var result = _validator.Validate(null, options);

        // If it fails, the failure should NOT be about IncidentNotificationHours
        if (!result.Succeeded)
        {
            return result.FailureMessage?.Contains("IncidentNotificationHours") != true;
        }

        return true;
    }

    /// <summary>
    /// Invariant: Zero or negative IncidentNotificationHours always fails validation.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool NonPositiveNotificationHours_AlwaysFails(NegativeInt hours)
    {
        var options = new NIS2Options { IncidentNotificationHours = hours.Get };

        var result = _validator.Validate(null, options);

        return !result.Succeeded
            && result.FailureMessage?.Contains("IncidentNotificationHours") == true;
    }

    /// <summary>
    /// Invariant: Essential entity without CompetentAuthority always fails validation.
    /// </summary>
    [Fact]
    public void EssentialEntity_WithoutCompetentAuthority_FailsValidation()
    {
        var options = new NIS2Options
        {
            EntityType = NIS2EntityType.Essential,
            CompetentAuthority = null
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("CompetentAuthority", result.FailureMessage);
    }

    /// <summary>
    /// Invariant: Important entity without CompetentAuthority passes that check.
    /// </summary>
    [Fact]
    public void ImportantEntity_WithoutCompetentAuthority_PassesThatCheck()
    {
        var options = new NIS2Options
        {
            EntityType = NIS2EntityType.Important,
            CompetentAuthority = null
        };

        var result = _validator.Validate(null, options);

        if (!result.Succeeded)
        {
            Assert.DoesNotContain("CompetentAuthority", result.FailureMessage);
        }
    }

    /// <summary>
    /// Invariant: EnforceEncryption=true with no categories or endpoints always fails.
    /// </summary>
    [Fact]
    public void EnforceEncryption_WithoutCategories_FailsValidation()
    {
        var options = new NIS2Options
        {
            EnforceEncryption = true,
            CompetentAuthority = "test@authority.eu"
        };
        // EncryptedDataCategories and EncryptedEndpoints are empty by default

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("EnforceEncryption", result.FailureMessage);
    }

    /// <summary>
    /// Invariant: EnforceEncryption=true with at least one category passes that check.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool EnforceEncryption_WithCategory_PassesThatCheck(NonEmptyString category)
    {
        var options = new NIS2Options
        {
            EnforceEncryption = true,
            CompetentAuthority = "test@authority.eu"
        };
        options.EncryptedDataCategories.Add(category.Get);

        var result = _validator.Validate(null, options);

        if (!result.Succeeded)
        {
            return result.FailureMessage?.Contains("EnforceEncryption") != true;
        }

        return true;
    }

    /// <summary>
    /// Invariant: Negative ComplianceCacheTTL always fails validation.
    /// </summary>
    [Fact]
    public void NegativeCacheTTL_FailsValidation()
    {
        var options = new NIS2Options
        {
            ComplianceCacheTTL = TimeSpan.FromMinutes(-1),
            CompetentAuthority = "test@authority.eu"
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("ComplianceCacheTTL", result.FailureMessage);
    }

    /// <summary>
    /// Invariant: Zero ExternalCallTimeout always fails validation.
    /// </summary>
    [Fact]
    public void ZeroExternalCallTimeout_FailsValidation()
    {
        var options = new NIS2Options
        {
            ExternalCallTimeout = TimeSpan.Zero,
            CompetentAuthority = "test@authority.eu"
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("ExternalCallTimeout", result.FailureMessage);
    }

    /// <summary>
    /// Invariant: Supplier with empty name fails validation.
    /// </summary>
    [Fact]
    public void SupplierWithEmptyName_FailsValidation()
    {
        var options = new NIS2Options
        {
            CompetentAuthority = "test@authority.eu"
        };
        options.AddSupplier("test-supplier", s =>
        {
            // Name is left as empty string (default)
            s.RiskLevel = SupplierRiskLevel.Low;
        });

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("test-supplier", result.FailureMessage);
    }

    /// <summary>
    /// Invariant: Fully valid options always pass validation.
    /// </summary>
    [Fact]
    public void FullyValidOptions_PassValidation()
    {
        var options = new NIS2Options
        {
            EntityType = NIS2EntityType.Essential,
            Sector = NIS2Sector.DigitalInfrastructure,
            EnforcementMode = NIS2EnforcementMode.Block,
            IncidentNotificationHours = 24,
            CompetentAuthority = "bsi@bsi.bund.de",
            EnforceEncryption = true,
            ComplianceCacheTTL = TimeSpan.FromMinutes(5),
            ExternalCallTimeout = TimeSpan.FromSeconds(5)
        };
        options.EncryptedDataCategories.Add("PII");
        options.AddSupplier("test-supplier", s =>
        {
            s.Name = "Test Supplier";
            s.RiskLevel = SupplierRiskLevel.Low;
        });

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded, $"Fully valid options should pass. Failures: {result.FailureMessage}");
    }
}
