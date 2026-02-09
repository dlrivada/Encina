using Encina.Cdc;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="CdcOptions"/> configuration class.
/// </summary>
public sealed class CdcOptionsTests
{
    private static readonly string[] ExpectedTableFilters = ["Orders", "Products"];
    [Fact]
    public void Defaults_Enabled_IsFalse()
    {
        var options = new CdcOptions();
        options.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_PollingInterval_IsFiveSeconds()
    {
        var options = new CdcOptions();
        options.PollingInterval.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Defaults_BatchSize_Is100()
    {
        var options = new CdcOptions();
        options.BatchSize.ShouldBe(100);
    }

    [Fact]
    public void Defaults_MaxRetries_Is3()
    {
        var options = new CdcOptions();
        options.MaxRetries.ShouldBe(3);
    }

    [Fact]
    public void Defaults_BaseRetryDelay_IsOneSecond()
    {
        var options = new CdcOptions();
        options.BaseRetryDelay.ShouldBe(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Defaults_TableFilters_IsEmpty()
    {
        var options = new CdcOptions();
        options.TableFilters.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_EnablePositionTracking_IsTrue()
    {
        var options = new CdcOptions();
        options.EnablePositionTracking.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_UseMessagingBridge_IsFalse()
    {
        var options = new CdcOptions();
        options.UseMessagingBridge.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_UseOutboxCdc_IsFalse()
    {
        var options = new CdcOptions();
        options.UseOutboxCdc.ShouldBeFalse();
    }

    [Fact]
    public void SetProperties_AllSettable()
    {
        var options = new CdcOptions
        {
            Enabled = true,
            PollingInterval = TimeSpan.FromSeconds(10),
            BatchSize = 50,
            MaxRetries = 5,
            BaseRetryDelay = TimeSpan.FromSeconds(2),
            TableFilters = ["Orders", "Products"],
            EnablePositionTracking = false,
            UseMessagingBridge = true,
            UseOutboxCdc = true
        };

        options.Enabled.ShouldBeTrue();
        options.PollingInterval.ShouldBe(TimeSpan.FromSeconds(10));
        options.BatchSize.ShouldBe(50);
        options.MaxRetries.ShouldBe(5);
        options.BaseRetryDelay.ShouldBe(TimeSpan.FromSeconds(2));
        options.TableFilters.ShouldBe(ExpectedTableFilters);
        options.EnablePositionTracking.ShouldBeFalse();
        options.UseMessagingBridge.ShouldBeTrue();
        options.UseOutboxCdc.ShouldBeTrue();
    }
}
