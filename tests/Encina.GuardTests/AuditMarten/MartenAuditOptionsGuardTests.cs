using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests exercising <see cref="MartenAuditOptions"/> property assignments and defaults.
/// </summary>
public class MartenAuditOptionsGuardTests
{
    [Fact]
    public void Defaults_AreSet()
    {
        var options = new MartenAuditOptions();

        options.TemporalGranularity.ShouldBe(TemporalKeyGranularity.Monthly);
        options.EncryptionScope.ShouldBe(AuditEncryptionScope.PiiFieldsOnly);
        options.RetentionPeriod.ShouldBe(TimeSpan.FromDays(2555));
        options.EnableAutoPurge.ShouldBeFalse();
        options.PurgeIntervalHours.ShouldBe(24);
        options.ShreddedPlaceholder.ShouldBe("[SHREDDED]");
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void Constants_AreExposed()
    {
        MartenAuditOptions.DefaultShreddedPlaceholder.ShouldBe("[SHREDDED]");
        MartenAuditOptions.DefaultRetentionDays.ShouldBe(2555);
        MartenAuditOptions.DefaultPurgeIntervalHours.ShouldBe(24);
    }

    [Fact]
    public void AllProperties_CanBeAssigned()
    {
        var options = new MartenAuditOptions
        {
            TemporalGranularity = TemporalKeyGranularity.Yearly,
            EncryptionScope = AuditEncryptionScope.AllFields,
            RetentionPeriod = TimeSpan.FromDays(365),
            EnableAutoPurge = true,
            PurgeIntervalHours = 12,
            ShreddedPlaceholder = "<REDACTED>",
            AddHealthCheck = true
        };

        options.TemporalGranularity.ShouldBe(TemporalKeyGranularity.Yearly);
        options.EncryptionScope.ShouldBe(AuditEncryptionScope.AllFields);
        options.RetentionPeriod.ShouldBe(TimeSpan.FromDays(365));
        options.EnableAutoPurge.ShouldBeTrue();
        options.PurgeIntervalHours.ShouldBe(12);
        options.ShreddedPlaceholder.ShouldBe("<REDACTED>");
        options.AddHealthCheck.ShouldBeTrue();
    }
}
