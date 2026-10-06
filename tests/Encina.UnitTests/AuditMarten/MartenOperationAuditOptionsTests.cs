using Encina.Audit.Marten;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="MartenOperationAuditOptions"/> default values and configuration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class MartenOperationAuditOptionsTests
{
    [Fact]
    public void Defaults_TemporalGranularity_IsMonthly()
    {
        var options = new MartenOperationAuditOptions();
        options.TemporalGranularity.ShouldBe(TemporalKeyGranularity.Monthly);
    }

    [Fact]
    public void Defaults_EncryptionScope_IsPiiFieldsOnly()
    {
        var options = new MartenOperationAuditOptions();
        options.EncryptionScope.ShouldBe(AuditEncryptionScope.PiiFieldsOnly);
    }

    [Fact]
    public void Defaults_RetentionPeriod_Is2555Days()
    {
        var options = new MartenOperationAuditOptions();
        options.RetentionPeriod.ShouldBe(TimeSpan.FromDays(2555));
    }

    [Fact]
    public void Defaults_EnableAutoPurge_IsFalse()
    {
        var options = new MartenOperationAuditOptions();
        options.EnableAutoPurge.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_PurgeIntervalHours_Is24()
    {
        var options = new MartenOperationAuditOptions();
        options.PurgeIntervalHours.ShouldBe(24);
    }

    [Fact]
    public void Defaults_ShreddedPlaceholder_IsShredded()
    {
        var options = new MartenOperationAuditOptions();
        options.ShreddedPlaceholder.ShouldBe("[SHREDDED]");
    }

    [Fact]
    public void Defaults_AddHealthCheck_IsFalse()
    {
        var options = new MartenOperationAuditOptions();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void Constants_DefaultShreddedPlaceholder_MatchesDefault()
    {
        MartenOperationAuditOptions.DefaultShreddedPlaceholder.ShouldBe("[SHREDDED]");
    }

    [Fact]
    public void Constants_DefaultRetentionDays_Is2555()
    {
        MartenOperationAuditOptions.DefaultRetentionDays.ShouldBe(2555);
    }

    [Fact]
    public void Constants_DefaultPurgeIntervalHours_Is24()
    {
        MartenOperationAuditOptions.DefaultPurgeIntervalHours.ShouldBe(24);
    }
}
