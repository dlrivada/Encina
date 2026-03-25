using Encina.Compliance.CrossBorderTransfer;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer;

public class CrossBorderTransferOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new CrossBorderTransferOptions();

        options.EnforcementMode.Should().Be(CrossBorderTransferEnforcementMode.Block);
        options.DefaultSourceCountryCode.Should().Be("DE");
        options.TIARiskThreshold.Should().Be(0.6);
        options.DefaultTIAExpirationDays.Should().Be(365);
        options.DefaultSCCExpirationDays.Should().BeNull();
        options.DefaultTransferExpirationDays.Should().Be(365);
        options.AutoDetectTransfers.Should().BeFalse();
        options.CacheEnabled.Should().BeTrue();
        options.CacheTTLMinutes.Should().Be(5);
        options.AddHealthCheck.Should().BeFalse();
        options.RequireTIAForNonAdequate.Should().BeTrue();
        options.RequireSCCForNonAdequate.Should().BeTrue();
        options.EnableExpirationMonitoring.Should().BeFalse();
        options.ExpirationCheckInterval.Should().Be(TimeSpan.FromHours(1));
        options.AlertBeforeExpirationDays.Should().Be(30);
        options.PublishExpirationNotifications.Should().BeTrue();
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

        options.EnforcementMode.Should().Be(CrossBorderTransferEnforcementMode.Warn);
        options.DefaultSourceCountryCode.Should().Be("FR");
        options.TIARiskThreshold.Should().Be(0.8);
        options.DefaultTIAExpirationDays.Should().Be(180);
        options.DefaultSCCExpirationDays.Should().Be(365);
        options.DefaultTransferExpirationDays.Should().Be(730);
        options.AutoDetectTransfers.Should().BeTrue();
        options.CacheEnabled.Should().BeFalse();
        options.CacheTTLMinutes.Should().Be(10);
        options.AddHealthCheck.Should().BeTrue();
        options.RequireTIAForNonAdequate.Should().BeFalse();
        options.RequireSCCForNonAdequate.Should().BeFalse();
        options.EnableExpirationMonitoring.Should().BeTrue();
        options.ExpirationCheckInterval.Should().Be(TimeSpan.FromMinutes(30));
        options.AlertBeforeExpirationDays.Should().Be(60);
        options.PublishExpirationNotifications.Should().BeFalse();
    }
}
