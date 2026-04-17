using Encina.Compliance.CrossBorderTransfer;

using Shouldly;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer;

public class CrossBorderTransferOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new CrossBorderTransferOptions();

        options.EnforcementMode.ShouldBe(CrossBorderTransferEnforcementMode.Block);
        options.DefaultSourceCountryCode.ShouldBe("DE");
        options.TIARiskThreshold.ShouldBe(0.6);
        options.DefaultTIAExpirationDays.ShouldBe(365);
        options.DefaultSCCExpirationDays.ShouldBeNull();
        options.DefaultTransferExpirationDays.ShouldBe(365);
        options.AutoDetectTransfers.ShouldBeFalse();
        options.CacheEnabled.ShouldBeTrue();
        options.CacheTTLMinutes.ShouldBe(5);
        options.AddHealthCheck.ShouldBeFalse();
        options.RequireTIAForNonAdequate.ShouldBeTrue();
        options.RequireSCCForNonAdequate.ShouldBeTrue();
        options.EnableExpirationMonitoring.ShouldBeFalse();
        options.ExpirationCheckInterval.ShouldBe(TimeSpan.FromHours(1));
        options.AlertBeforeExpirationDays.ShouldBe(30);
        options.PublishExpirationNotifications.ShouldBeTrue();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var options = new CrossBorderTransferOptions
        {
            EnforcementMode = CrossBorderTransferEnforcementMode.Warn,
            DefaultSourceCountryCode = "FR",
            TIARiskThreshold = 0.8,
            DefaultTIAExpirationDays = 180,
            DefaultSCCExpirationDays = 365,
            DefaultTransferExpirationDays = 730,
            AutoDetectTransfers = true,
            CacheEnabled = false,
            CacheTTLMinutes = 10,
            AddHealthCheck = true,
            RequireTIAForNonAdequate = false,
            RequireSCCForNonAdequate = false,
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromMinutes(30),
            AlertBeforeExpirationDays = 60,
            PublishExpirationNotifications = false
        };

        options.EnforcementMode.ShouldBe(CrossBorderTransferEnforcementMode.Warn);
        options.DefaultSourceCountryCode.ShouldBe("FR");
        options.TIARiskThreshold.ShouldBe(0.8);
        options.DefaultTIAExpirationDays.ShouldBe(180);
        options.DefaultSCCExpirationDays.ShouldBe(365);
        options.DefaultTransferExpirationDays.ShouldBe(730);
        options.AutoDetectTransfers.ShouldBeTrue();
        options.CacheEnabled.ShouldBeFalse();
        options.CacheTTLMinutes.ShouldBe(10);
        options.AddHealthCheck.ShouldBeTrue();
        options.RequireTIAForNonAdequate.ShouldBeFalse();
        options.RequireSCCForNonAdequate.ShouldBeFalse();
        options.EnableExpirationMonitoring.ShouldBeTrue();
        options.ExpirationCheckInterval.ShouldBe(TimeSpan.FromMinutes(30));
        options.AlertBeforeExpirationDays.ShouldBe(60);
        options.PublishExpirationNotifications.ShouldBeFalse();
    }
}
