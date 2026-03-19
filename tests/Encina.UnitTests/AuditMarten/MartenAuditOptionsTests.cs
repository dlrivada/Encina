using Encina.Audit.Marten;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="MartenAuditOptions"/> default values and configuration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class MartenAuditOptionsTests
{
    [Fact]
    public void Defaults_TemporalGranularity_IsMonthly()
    {
        var options = new MartenAuditOptions();
        options.TemporalGranularity.ShouldBe(TemporalKeyGranularity.Monthly);
    }

    [Fact]
    public void Defaults_EncryptionScope_IsPiiFieldsOnly()
    {
        var options = new MartenAuditOptions();
        options.EncryptionScope.ShouldBe(AuditEncryptionScope.PiiFieldsOnly);
    }

    [Fact]
    public void Defaults_RetentionPeriod_Is2555Days()
    {
        var options = new MartenAuditOptions();
        options.RetentionPeriod.ShouldBe(TimeSpan.FromDays(2555));
    }

    [Fact]
    public void Defaults_EnableAutoPurge_IsFalse()
    {
        var options = new MartenAuditOptions();
        options.EnableAutoPurge.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_PurgeIntervalHours_Is24()
    {
        var options = new MartenAuditOptions();
        options.PurgeIntervalHours.ShouldBe(24);
    }

    [Fact]
    public void Defaults_ShreddedPlaceholder_IsShredded()
    {
        var options = new MartenAuditOptions();
        options.ShreddedPlaceholder.ShouldBe("[SHREDDED]");
    }

    [Fact]
    public void Defaults_AddHealthCheck_IsFalse()
    {
        var options = new MartenAuditOptions();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void Constants_DefaultShreddedPlaceholder_MatchesDefault()
    {
        MartenAuditOptions.DefaultShreddedPlaceholder.ShouldBe("[SHREDDED]");
    }

    [Fact]
    public void Constants_DefaultRetentionDays_Is2555()
    {
        MartenAuditOptions.DefaultRetentionDays.ShouldBe(2555);
    }

    [Fact]
    public void Constants_DefaultPurgeIntervalHours_Is24()
    {
        MartenAuditOptions.DefaultPurgeIntervalHours.ShouldBe(24);
    }
}
